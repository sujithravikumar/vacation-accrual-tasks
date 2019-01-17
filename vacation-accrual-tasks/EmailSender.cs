using System;
using System.Net;
using System.Net.Mail;
using NLog;

namespace vacation_accrual_tasks
{
    public static class EmailSender
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public static void SendEmail(string email, string subject, string body)
        {
            Execute(Program.Smtp_Host, Program.Smtp_Port,
                Program.Smtp_Username, Program.Smtp_Password, subject, body, email);
        }

        static void Execute(string host, int port, string username,
            string password, string subject, string body, string email)
        {

            MailMessage message = new MailMessage();
            message.IsBodyHtml = true;
            message.From = new MailAddress("vacation.accrual.buddy@gmail.com", "Vacation Accrual Buddy");
            message.To.Add(new MailAddress(email));
            message.Subject = subject;
            message.Body = body;

            using (var client = new SmtpClient(host, port))
            {
                // Pass SMTP credentials
                client.Credentials =
                    new NetworkCredential(username, password);

                // Enable SSL encryption
                client.EnableSsl = true;

                try
                {
                    client.Send(message);
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                    throw;
                }
            }
        }
    }
}
