<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UploadFile.aspx.cs" Inherits="Sitecore.SharedSource.DataSync.Shell.Wizards.FileUpload.UploadFile" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>UserSync</title>

    <style type="text/css">
        html
        {
            height: 24px;
        }
    </style>
    
    <script type="text/javascript" language="javascript">
        function correctFileInput() {
            if (navigator.userAgent.indexOf('MSIE') == -1)
                document.getElementById('FileInput').setAttribute('style', 'float: right; height: 24px;');
        }

        function saveFile() {
            var finput = document.getElementById('FileInput');

            var strFilename = finput.value;
            var fname = window.parent.document.getElementById('Filename');

            if (finput.value.length > 0)
                if (fname.value != strFilename) {
                    if (finput.value.toLowerCase().lastIndexOf('xml') == (finput.value.length - 3)) {
                        fname.value = finput.value;
                        window.document.forms["UploadFileForm"].submit();
                    }
                    else {
                        var message = 'is not a XML file.';

                        if (dictionary != null)
                            message = dictionary[0];

                        alert('"' + finput.value + '" ' + message);
                    }
                }
        }
    </script>
</head>
<body style="background: transparent;" onload="correctFileInput()">
    <form id="UploadFileForm" runat="server" enctype="multipart/form-data">
    <div style="width: 75px; overflow: hidden;">
        <input id="LastFile" type="hidden" runat="server" />
        <input id="FileInput" type="file" runat="server" style="width: 75px; height: 24px;" onchange="saveFile()" />
    </div>
    </form>
</body>
</html>