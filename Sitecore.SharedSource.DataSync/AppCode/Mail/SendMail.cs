using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web.Mail;
using Sitecore.Diagnostics;
using MailMessage = System.Net.Mail.MailMessage;

namespace Sitecore.SharedSource.DataSync.Mail
{
    public static class SendMail
    {
        private const string SettingMailServer = "MailServer";
        private const string SettingMailServerPassword = "MailServerPassword";
        private const string SettingMailServerUserName = "MailServerUserName";
        private const string Failure = "Failure";
        public const string Success = "Success";

        private static string Host
        {
            get;
            set;
        }

        private static string Login
        {
            get;
            set;
        }


        private static string Password
        {
            get;
            set;
        }

        // Methods
        private static void SetupMail()
        {
            Host = Sitecore.Configuration.Settings.GetSetting(SettingMailServer);
            Password = Sitecore.Configuration.Settings.GetSetting(SettingMailServerPassword);
            Login = Sitecore.Configuration.Settings.GetSetting(SettingMailServerUserName);
        }

        public static void SendMailWithAttachment(string recipient, string sender, string subject, string text, string filePath)
        {
            var server = Sitecore.Configuration.Settings.GetSetting(SettingMailServer);
            // Specify the file to be attached and sent.
            // This example assumes that a file named Data.xls exists in the
            // current working directory.
            var file = filePath;
            // Create a message and set up the recipients.
            var message = new MailMessage(sender, recipient, subject, text);

            // Create  the file attachment for this e-mail message.
            var data = new Attachment(file, MediaTypeNames.Application.Octet);
            // Add time stamp information for the file.
            var disposition = data.ContentDisposition;
            disposition.CreationDate = File.GetCreationTime(file);
            disposition.ModificationDate = File.GetLastWriteTime(file);
            disposition.ReadDate = File.GetLastAccessTime(file);
            // Add the file attachment to this e-mail message.
            message.Attachments.Add(data);

            //Send the message.
            var client = new SmtpClient(server);
            // Add credentials if the SMTP server requires them.
            client.Credentials = CredentialCache.DefaultNetworkCredentials;

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Diagnostics.Log.Error("SendMailWithAttachment : Error sending mail", ex.InnerException);
            }

        }

        public static string SendMailWithoutAttachment(string recipient, string sender, string subject, string text)
        {
            if (!string.IsNullOrEmpty(recipient) && !string.IsNullOrEmpty(sender) && !string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(text))
            {
                var msg = new MailMessage(sender, recipient, subject, text) { IsBodyHtml = false };

                return SendEmailWithSMTP(msg);
            }
            return Failure;
        }

        /// <summary>
        /// Gets the PDF stream.
        /// </summary>
        /// <param name="pdf">The PDF.</param>
        /// <returns></returns>


        /// <summary>
        /// Sends the email with SMTP.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns></returns>
        private static string SendEmailWithSMTP(MailMessage msg)
        {
            if (msg != null)
            {
                SetupMail();
                var smtp = new SmtpClient(Host);

                var strArray = Login.Split(new[] { '\\' });

                if ((strArray.Length > 0) && !string.IsNullOrEmpty(strArray[0]))
                {
                    ICredentialsByHost host;
                    if ((strArray.Length == 2) && !string.IsNullOrEmpty(strArray[1]))
                    {
                        host = new NetworkCredential(strArray[1], Password, strArray[0]);
                    }
                    else
                    {
                        host = new NetworkCredential(strArray[0], Password);
                    }
                    smtp.Credentials = host;
                }
                smtp.Send(msg);
                return Success;

            }
            return Failure;
        }
    }
}