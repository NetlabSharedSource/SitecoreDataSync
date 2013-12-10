using System;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.DataSync.Mail;

namespace Sitecore.SharedSource.DataSync.Managers
{
    public class MailManager
    {
        private const string Failure = "Failure";
        private const string Success = "Success";

        public static void SendLogReport(ref Logging log, string identifier, Item dataSyncItem)
        {
            var recipient = dataSyncItem["Mail Recipients"];
            if (String.IsNullOrEmpty(recipient))
            {
                log.Log("Error", "The 'Mail Recipients' field must be defined. Please provide a recipient address for the mail.");
            }
            else
            {
                var replyTo = dataSyncItem["Mail Reply To"];
                if (String.IsNullOrEmpty(replyTo))
                {
                    replyTo = "noreply@netlab.no";
                    log.Log("Error", "The 'Mail Reply To' field must be defined. Please provide a replyTo address for the mail.");
                }
                var subject = dataSyncItem["Mail Subject"];
                if (String.IsNullOrEmpty(subject))
                {
                    subject = "Status report mail from DataSync task - {0} - Result: {1}.";
                }

                var logString = log.LogBuilder.ToString();
                var result = string.Empty;
                if (String.IsNullOrEmpty(logString))
                {
                    result = Success;
                    logString += "The import completed successfully.\r\n\r\nStatus:\r\n" + log.GetStatusText();
                }
                else
                {
                    logString = "The import failed.\r\n\r\nStatus:\r\n" + log.GetStatusText() + "\r\n\r\n" + logString;
                    result = Failure;
                }
                try
                {
                    subject = String.Format(subject, identifier, result);
                }
                catch (Exception exception)
                {
                    log.Log("Error", "SendLogReport had an exception trying to format the subject of the mail." + exception.Message);
                }
                var doNotSendMailOnSuccess = dataSyncItem["Do Not Send Mail On Success"] == "1";
                if((doNotSendMailOnSuccess && result == Failure) || !doNotSendMailOnSuccess)
                {
                    try{
                        if (SendMail.SendMailWithoutAttachment(recipient, replyTo, subject, logString) == "Failure")
                        {
                            log.Log("Error", "The SendMailWithoutAttachment failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Log("Error", "The SendMailWithoutAttachment failed with an exception: " + ex.Message);
                    }
                }
            }
        }
    }
}