<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Inherits="_Default" culture="auto" meta:resourcekey="PageResource1" Codebehind="Logbook.aspx.cs" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc6" %>
<%@ Register Src="../Controls/mfbEditFlight.ascx" TagName="mfbEditFlight" TagPrefix="uc4" %>
<%@ Register Src="../Controls/mfbSimpleTotals.ascx" TagName="mfbSimpleTotals" TagPrefix="uc3" %>
<%@ Register Src="../Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc2" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbSearchAndTotals.ascx" tagname="mfbSearchAndTotals" tagprefix="uc7" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblUserName" runat="server" 
            meta:resourcekey="lblUserNameResource1"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Button ID="btnPopWelcome" runat="server" Text="Button" 
        style="display:none" meta:resourcekey="btnPopWelcomeResource1" />
    <asp:Panel ID="pnlWelcomeNewUser" runat="server" CssClass="modalpopup"
        style="display:none;" meta:resourcekey="pnlWelcomeNewUserResource1">
        <h2><asp:Localize ID="locWelcomeHeader" runat="server" Text="Welcome" 
                meta:resourcekey="locWelcomeHeaderResource1"></asp:Localize>
        </h2>
        <p><asp:Localize ID="locThanks" runat="server" 
                Text="Thanks for creating an account; we hope you find the service to be useful." 
                meta:resourcekey="locThanksResource1"></asp:Localize></p>
        <p><asp:Localize ID="locNextSteps" runat="server" Text="Your next steps:" 
                meta:resourcekey="locNextStepsResource1"></asp:Localize>
            </p>
        <ul>
            <li><asp:HyperLink ID="HyperLink2" NavigateUrl="~/Member/Aircraft.aspx" 
                    runat="server" Text="Enter the aircraft you fly into your account." 
                    meta:resourcekey="HyperLink2Resource1"></asp:HyperLink></li>
            <li><asp:Localize ID="locManuallyEnter" runat="server" 
                    Text="Manually enter your flights directly on this page" 
                    meta:resourcekey="locManuallyEnterResource1"></asp:Localize>
                <asp:Localize ID="Localize1" runat="server" Text="<%$ Resources:LocalizedText, ORSeparator %>"></asp:Localize> 
                <asp:HyperLink ID="lnkImport" NavigateUrl="~/Member/Import.aspx" runat="server" 
                    Text="Import your flights from a spreadsheet" 
                    meta:resourcekey="lnkImportResource1"></asp:HyperLink>.</li>
        </ul>
        <div style="text-align:center">
        <asp:Button ID="btnClose" runat="server" Text="Close" 
                meta:resourcekey="btnCloseResource1" /></div>
    </asp:Panel>
    <cc1:ModalPopupExtender PopupControlID="pnlWelcomeNewUser" 
        BackgroundCssClass="modalBackground" ID="ModalPopupExtender1" runat="server" 
        TargetControlID="btnPopWelcome"
    CancelControlID="btnClose" Enabled="True">
    </cc1:ModalPopupExtender>
    <uc4:mfbEditFlight id="mfbEF1" runat="server" OnFlightUpdated="FlightUpdated">
    </uc4:mfbEditFlight>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <uc6:mfbLogbook ID="MfbLogbook1" runat="server" />
</asp:Content>
