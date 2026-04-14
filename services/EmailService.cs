using System.Net;
using System.Net.Mail;

namespace TiendaApi.Services
{
    public class EmailService
    {
        private readonly string _email = "soportex20k@gmail.com";
        private readonly string _password = "oldc cvju rukm wolt";

        private readonly SmtpClient _smtp;

        public EmailService()
        {
            _smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_email, _password),
                EnableSsl = true
            };
        }

        public void Send(string to, string subject, string body, string replyTo = null)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(_email),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(to);

            if (!string.IsNullOrEmpty(replyTo))
                mail.ReplyToList.Add(new MailAddress(replyTo));

            _smtp.Send(mail);
        }
    }
}