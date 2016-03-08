using System;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.Logger.Log.Output;
using Sitecore.SharedSource.DataSync.Mail;

namespace Sitecore.SharedSource.DataSync.Managers
{
    public class MailManager
    {
        private const string Failure = "Failure";
        private const string Success = "Success";
        private const string FieldNameMailRecipients = "Mail Recipients";
        private const string FieldNameMailReplyTo = "Mail Reply To";
        private const string FieldNameMailSubject = "Mail Subject";
        private const string FieldNameDoNotSendMailOnSuccess = "Do Not Send Mail On Success";
        private const string DefaultReplyTo = "noreply@netlab.no";
        private const string SitecoreBooleanTrue = "1";
        private const string DefaultSubject = "Status report mail from DataSync task - {0} - Result: {1}.";

        public static void SendLogReport(ref LevelLogger logger, OutputHandlerBase exporter)
        {
            var dataSyncItem = logger.GetData(Utility.Constants.DataSyncItem) as Item;
            if (dataSyncItem == null)
            {
                logger.AddError("Error", "The DataSyncItem was null. Therefor we couldn't retrieve any mail recipients.");
                return;
            }
            if (exporter == null)
            {
                logger.AddError("Error", "The ExporterBase was null. This class is used to output the logger.");
                return;
            }
            var recipient = dataSyncItem[FieldNameMailRecipients];
            if (String.IsNullOrEmpty(recipient))
            {
                logger.AddInfo("Add Mail Recipients to receive email reports", "If you want to receive email, then fill out the field 'Mail Recipients'.");
            }
            else
            {
                var replyTo = dataSyncItem[FieldNameMailReplyTo];
                if (String.IsNullOrEmpty(replyTo))
                {
                    replyTo = DefaultReplyTo;
                    logger.AddError("Error", "The 'Mail Reply To' field must be defined. Please provide a replyTo address for the mail.");
                }
                var subject = dataSyncItem[FieldNameMailSubject];
                if (String.IsNullOrEmpty(subject))
                {
                    subject = DefaultSubject;
                }

                var result = !logger.HasFatalsOrErrors() ? Success : Failure;
                try
                {
                    subject = String.Format(subject, exporter.GetIdentifier(), result);
                }
                catch (Exception exception)
                {
                    logger.AddError("Error", "SendLogReport had an exception trying to format the subject of the mail." + exception.Message);
                }
                var doNotSendMailOnSuccess = dataSyncItem[FieldNameDoNotSendMailOnSuccess] == SitecoreBooleanTrue;
                if((doNotSendMailOnSuccess && result == Failure) || !doNotSendMailOnSuccess)
                {
                    try{
                        if (SendMail.SendMailWithoutAttachment(recipient, replyTo, subject, exporter.Export()) == Failure)
                        {
                            logger.AddError("Error", "The SendMailWithoutAttachment failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.AddError("Error", "The SendMailWithoutAttachment failed with an exception: " + ex.Message);
                    }
                }
            }
        }
    }
}