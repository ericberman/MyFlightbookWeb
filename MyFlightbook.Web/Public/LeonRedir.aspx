<%@ Page Language="C#" AutoEventWireup="true" Codebehind="LeonRedir.aspx.cs" MasterPageFile="~/MasterPage.master" Async="true" Inherits="MyFlightbook.OAuth.Leon.LeonRedir" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblPageHeader" runat="server" Text="<%$ Resources:LogbookEntry, LeonImportHeader %>" />
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <img style="float:left; margin-right: 40px; width: 100px;" src='<% =VirtualPathUtility.ToAbsolute("~/images/LeonLogo.svg") %>' />
	<div style="margin-top: 30px">
	    <asp:MultiView ID="mvLeonState" runat="server">
			<asp:View ID="vwUnauthenticated" runat="server">
				<asp:Label ID="lblUnauth" runat="server" Text="<%$ Resources:LogbookEntry, LeonMustBeSignedIn %>" />
			</asp:View>
			<asp:View ID="vwNoAuthToken" runat="server">
				<p><asp:Label ID="lblNoAuthToken" runat="server" /></p>
				<p><asp:Label ID="lblSubdomainPrompt" runat="server" Text="<%$ Resources:LogbookEntry, LeonSubDomainPrompt %>" /></p>
				<div>https://<asp:TextBox ID="txtSubDomain" runat="server" Width="50px" />.leon.aero</div>
				<div><asp:LinkButton ID="lnkAuthorize" runat="server" OnClick="lnkAuthorize_Click" /></div>
				<div><asp:RequiredFieldValidator ID="reqSubDomain" runat="server" ErrorMessage="<%$ Resources:LogbookEntry, LeonSubDomainRequired %>" CssClass="error" ControlToValidate="txtSubDomain" Display="Dynamic" /> </div>
				<div><asp:RegularExpressionValidator ID="regSubDomain" runat="server" ErrorMessage="<%$ Resources:LogbookEntry, LeonInvalidSubDomain %>" CssClass="error" ControlToValidate="txtSubDomain" Display="Dynamic"  ValidationExpression="[a-zA-Z0-9]+(\.[a-zA-Z0-9]+)*" /></div>
			</asp:View>
			<asp:View ID="vwAuthorized" runat="server">
				<div><asp:Label ID="lblAuthedHeader" runat="server" /></div>
				<ul>
					<li><asp:HyperLink ID="lnkImport" runat="server" Text="<%$ Resources:LogbookEntry, LeonGoToImport %>" NavigateUrl="~/mvc/import" /></li>
					<li><asp:LinkButton ID="btnDeAuth" runat="server" OnClick="btnDeAuth_Click" /></li>
				</ul>
			</asp:View>
		</asp:MultiView>
	</div>
</asp:Content>
