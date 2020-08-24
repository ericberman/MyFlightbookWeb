<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbMultiFileUpload.ascx.cs" Inherits="Controls_mfbMultiFileUpload" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Reference Control="~/Controls/mfbFileUpload.ascx" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc2" %>
<asp:MultiView ID="mvFileUpload" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwLegacy" runat="server">
        <asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>
    </asp:View>
    <asp:View ID="vwAjaxUpload" runat="server">
        <asp:Image ID="myThrobber" ImageUrl="~/images/ajax-loader.gif" runat="server" style="display:None" />
        <asp:AjaxFileUpload ID="AjaxFileUpload1" runat="server" AutoStartUpload="true" CssClass="mfbDefault" MaxFileSize="300000"
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
<div>
    <asp:LinkButton ID="lnkPullGoogle" runat="server" Visible="false" OnClick="lnkPullGoogle_Click" Text="<%$ Resources:LocalizedText, GooglePhotosViewImages %>" />
    <div><asp:Label ID="lblGPhotoResult" runat="server" EnableViewState="false" /></div>
    <div>
        <asp:Repeater ID="rptGPhotos" runat="server">
            <ItemTemplate>
                <div style="display: inline-block;">
                    <asp:HyperLink ID="lIm" runat="server" Target="_blank" NavigateUrl='<%# Eval("productUrl") %>'>
                            <asp:Image ID="img" runat="server" ImageUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}=w150-h150", Eval("baseUrl")) %>' AlternateText='<%# Eval("filename") %>' />
                    </asp:HyperLink>
                    <div class="imageMenu" runat="server" id="divActions">
                        <div><asp:ImageButton ID="imgAdd" runat="server" ImageUrl="~/images/add.png" CommandArgument='<%# Eval("productUrl") %>' OnCommand="imgAdd_Command" ToolTip="<%$ Resources:LocalizedText, GooglePhotosAddToFlight %>" AlternateText="<%$ Resources:LocalizedText, GooglePhotosAddToFlight %>" /></div>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
    <div>
        <asp:LinkButton ID="lnkMoreGPhotos" runat="server" Text="<%$ Resources:LocalizedText, GooglePhotosGetMore %>" Visible="false" OnClick="lnkMoreGPhotos_Click" />
    </div>
</div>


