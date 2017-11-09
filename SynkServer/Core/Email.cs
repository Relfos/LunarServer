using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace SynkServer.Core
{
    public static class EmailUtils
    {
        public static void SendEmail(string sender, string password, List<string> targets, string subject, string body)
        {
            var client = new SmtpClient();
            client.Port = 587;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential(sender, password);
            client.Host = "smtp.gmail.com";


            MailMessage mm = new MailMessage();
            mm.Sender = new MailAddress(sender);

            int count = 0;
            foreach (var email in targets)
            {
                mm.Bcc.Add(email);
                count++;
                if (count >= 100)
                {
                    break;
                }
            }
            mm.BodyEncoding = UTF8Encoding.UTF8;
            mm.IsBodyHtml = true;
            mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            client.Send(mm);
        }
    }
}
