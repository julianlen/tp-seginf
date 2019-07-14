using System;
using Microsoft.Win32;
using System.Collections.Generic;

namespace TP
{
    public class WindowsProductKeyDataExtractor : DataExtractor
    {

        public WindowsProductKeyDataExtractor(string outputFileName)
        {
            fileName = outputFileName;
        }

        // Enumeración que especifica la versión de DigitalProductId
        public enum DigitalProductIdVersion
        {
            // Todos los sistemas hasta Windows 7 (Windows 7 y versiones más viejas)
            UpToWindows7,
            // Windows 8 en adelante (Windows 8 y versiones más nuevas)
            Windows8AndUp
        }

        // Extrae los datos y devuelve la información como string
        protected override string ExtractData()
        {
            List<List<string>> values = new List<List<string>>();
            values.Add(GetWindowsLicenseData());

            string productKey = GetWindowsProductKeyFromRegistry();
            List<string> headers = new List<string> {"Product Name", "Product ID", "Product Key", "Installation Folder", "Version", "Build Number"};
            string extractedData = ToHTMLFormat("Product Key List", headers, values);
            return extractedData;
        }

        // Obtiene toda la información sobre la licencia de Windows
        protected List<string> GetWindowsLicenseData()
        {
            List<string> licenseData = new List<string>();
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            var registryKeyValue = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (registryKeyValue == null)
                throw new Exception("Failed to get Windows License data from registry");

            List<string> valueNames = new List<string> {"ProductName", "ProductId", "DigitalProductId", "PathName", "ReleaseId", "CurrentBuild"};
            foreach (string valueName in valueNames)
                if (valueName == "DigitalProductId")
                    licenseData.Add(GetWindowsProductKeyFromRegistry());
                else
                    licenseData.Add(registryKeyValue.GetValue(valueName).ToString());

            return licenseData;
        }

        // Obtiene ya decodificada la clave de producto del Registro de Windows
        protected string GetWindowsProductKeyFromRegistry()
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

        // Decodifica la clave de producto a partir del DigitalProductId dado como parámetro, utilizando un algoritmo particular dependiendo de la versión
        protected string GetWindowsProductKeyFromDigitalProductId(byte[] digitalProductId, DigitalProductIdVersion digitalProductIdVersion)
        {
            var productKey = digitalProductIdVersion == DigitalProductIdVersion.Windows8AndUp
                ? DecodeProductKeyWin8AndUp(digitalProductId)
                : DecodeProductKey(digitalProductId);
            return productKey;
        }
        
        // Decodifica la clave de producto a partir del DigitalProductId. Algoritmo para Windows 7 o menor.
        protected string DecodeProductKey(byte[] digitalProductId)
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
            var hexPid = new List<byte>();
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

        // Decodifica la clave de producto a partir del DigitalProductId. Algoritmo para Windows 8 o mayor.
        protected string DecodeProductKeyWin8AndUp(byte[] digitalProductId)
        {
            var key = String.Empty;
            const int keyOffset = 52;
            var isWin8 = (byte)((digitalProductId[66] / 6) & 1);
            digitalProductId[66] = (byte)((digitalProductId[66] & 0xf7) | (isWin8 & 2) * 4);

            // Caracteres alfa-numericos posibles para la clave de producto
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

    }
}
