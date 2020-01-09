<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbMultiFileUpload" Codebehind="mfbMultiFileUpload.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Reference Control="~/Controls/mfbFileUpload.ascx" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc2" %>
<asp:MultiView ID="mvFileUpload" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwLegacy" runat="server">
        <asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>
    </asp:View>
    <asp:View ID="vwAjaxUpload" runat="server">
        <asp:Image ID="myThrobber" ImageUrl="~/images/ajax-loader.gif" runat="server" style="display:None" />
        <asp:AjaxFileUpload ID="AjaxFileUpload1" runat="server" AutoStartUpload="true" CssClass="mfbDefault"
                ThrobberID="myThrobber" AllowedFileTypes="heic,jpg,jpeg,pdf,jpe,png" MaximumNumberOfFiles="10" OnUploadComplete="AjaxFileUpload1_UploadComplete" OnUploadCompleteAll="AjaxFileUpload1_UploadCompleteAll" />
        <uc2:mfbImageList ID="mfbImageListPending" NoRequery="true" CanEdit="true" runat="server" Columns="4" MaxImage="-1" />
        <asp:Panel ID="pnlRefresh" runat="server" Visible="false">
            <script>
                function ajaxFileUploadAttachments_UploadComplete(sender, e) {
                    __doPostBack('<% =btnForceRefresh.ClientID %>', ''); // Do post back only after all files have been uploaded
                }
            </script>
            <asp:Button ID="btnForceRefresh" runat="server" Text="(Refresh)" OnClick="btnForceRefresh_Click" style="display:none;" />
        </asp:Panel>
        <asp:LinkButton ID="lnkBtnForceLegacy" runat="server" Text="<%$ Resources:LocalizedText, AjaxFileUploadDowngradePrompt %>" CssClass="fineprint" OnClick="lnkBtnForceLegacy_Click"></asp:LinkButton>
    </asp:View>
</asp:MultiView>
<span class="fineprint"><% =Resources.LocalizedText.ImageDisclaimer %></span>



