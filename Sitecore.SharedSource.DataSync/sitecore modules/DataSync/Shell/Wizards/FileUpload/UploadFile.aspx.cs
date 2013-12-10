using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Security.Accounts;
using Sitecore.Web;
using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace Sitecore.SharedSource.DataSync.Shell.Wizards.FileUpload
{
    public partial class UploadFile : Page
    {
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            if (!CheckSecurity())
                return;
            ClientScript.RegisterArrayDeclaration("dictionary", "'NOT_XML_FILE'");
            base.OnLoad(e);
            if (!IsPostBack || FileInput.PostedFile == null)
                return;
            string fileName = FileInput.PostedFile.FileName;
            if (string.IsNullOrEmpty(fileName) || !string.IsNullOrEmpty(LastFile.Value) && LastFile.Value.Equals(fileName))
                return;
            if (!string.IsNullOrEmpty(LastFile.Value))
            {
                try
                {
                    FileUtil.Delete(FileUtil.MapPath("/temp/" + FileUtil.GetFileName(LastFile.Value)));
                }
                catch
                {
                }
            }
            LastFile.Value = fileName;
            FileInput.PostedFile.SaveAs(FileUtil.MapPath("/temp/" + FileUtil.GetFileName(fileName)));
        }

        private bool CheckSecurity()
        {
            User user = Sitecore.Context.User;
            if ((Account)user != (Account)null && (user.IsAdministrator))
                return true;
            WebUtil.Redirect(Settings.ItemNotFoundUrl);
            return false;
        }
    }
}