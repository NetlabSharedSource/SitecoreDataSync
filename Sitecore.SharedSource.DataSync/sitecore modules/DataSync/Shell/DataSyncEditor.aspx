<%@ Page Language="C#" AutoEventWireup="true" Debug="true" CodeBehind="DataSyncEditor.aspx.cs" Inherits="sitecore_modules.Shell.BackPack_Modules.Import.DataSyncEditor" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Data Import</title>
	<link href="css/global.css?id=dded" rel="stylesheet" type="text/css"></link>
    <script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.4.2/jquery.min.js"></script>
    <script type="text/javascript">
    	$(document).ready(function () {
    		$('h2').dblclick(function () {
    			$(this).next(".Section").toggle();
    		});
    	});
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="scrptManager" runat="server"></asp:ScriptManager>
    <div class="MainWrapper">
        <h2>Import Data To Sitecore</h2>
        <div class="Section">
            <div class="Controls">
					<div class="formRow">
                        <div class="rowTitle">Choose import to run:</div>
                        <div class="btnBox">
                            <div class="btn">
                                <asp:Button ID="btnRefresh" CssClass="refreshBtn" Text="Refresh" OnClick="btnRefresh_Click" runat="server" />
                                <div class="clear"></div>
                            </div>
                            <div class="clear"></div>
                        </div>
                        <div class="rowSpacer"></div>
                        <asp:DropDownList ID="ddlImport" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlImport_SelectedIndexChanged"></asp:DropDownList>
                        <div class="clear"></div>
                    </div>
            </div>
        </div>
        <div class="clear"></div>
        <h2>Results</h2>
        <asp:UpdatePanel ID="updatePanelStatus" UpdateMode="Conditional" runat="server" ChildrenAsTriggers="false">
            <ContentTemplate>
                <%--<asp:Timer runat="server" id="Timer1" Interval="1000" OnTick="Timer1_Tick"></asp:Timer>--%>
                <div class="Section">
                    <div class="Controls">
			            <div class="formRow">
                            <div class="btnBox">
                                <div class="btn">
                                    <asp:Button ID="btnImport" CssClass="runBtn" Text="Run Import" OnClick="btnImport_Click" runat="server" />
                                    <div class="clear"></div>
                                </div>
                                <div class="clear"></div>
			                </div>
                        </div>
                        <div class="rowSpacer"></div>
                        <div class="formRow">
                            <div class="rowTitle">Messages:</div>
                            <div class="Message">
                                <asp:Literal runat="server" ID="litStatus"></asp:Literal>
                                <asp:TextBox ID="txtMessage" TextMode="MultiLine" runat="server"></asp:TextBox>
		                    </div>
                            <div class="clear"></div>
                        </div>
                        <div class="clear"></div>
                    </div>
                </div>
            </ContentTemplate>
             <Triggers>
                  <asp:PostBackTrigger ControlID="btnImport" /> 
                </Triggers>
        </asp:UpdatePanel>
    </div>
    </form>
</body>
</html>
