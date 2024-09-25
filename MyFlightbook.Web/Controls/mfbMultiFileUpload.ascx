<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbMultiFileUpload.ascx.cs" Inherits="MyFlightbook.Controls.ImageControls.mfbMultiFileUpload" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Reference Control="~/Controls/mfbFileUpload.ascx" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc2" %>
<%@ Register Src="~/Controls/mfbFileUpload.ascx" TagPrefix="uc2" TagName="mfbFileUpload" %>
<script>
    function ShowPanel(id, sender) {
        document.getElementById(id).style.display = 'block';
        sender.style.display = 'none';
        return false;
    }

    $(function () {
        document.onpaste = function (event) {
            var items = (event.clipboardData || event.originalEvent.clipboardData).items;
            var blob = null;
            var inputs = ['input', 'textarea'];
            var activeElement = document.activeElement;
            if (activeElement && inputs.indexOf(activeElement.tagName.toLowerCase()) !== -1 && items[0].type.indexOf("text") === 0)
                return;   // let default behavior work.
            for (var i = 0; i < items.length; i++) {
                if (items[i].type.indexOf("image") === 0) {
                    blob = items[i].getAsFile();
                    if (blob !== null) {
                        document.getElementById('<% =pnlclipboard.ClientID %>').style.display = 'block';
                        var reader = new FileReader();
                        reader.onload = function (event) {
                            document.getElementById('<% =hImg.ClientID %>').value = event.target.result;
                            __doPostBack('<% =bUpClp.ClientID %>', '');
                        };
                        reader.readAsDataURL(blob);
                        return; // no need to keep cycling through items.
                    }
                }
            }
        }
    });
</script>
<asp:MultiView ID="mvFileUpload" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwLegacy" runat="server">
        <uc2:mfbFileUpload runat="server" ID="mfbFu1" />
        <uc2:mfbFileUpload runat="server" ID="mfbFu2" ClientVisible="false" />
        <uc2:mfbFileUpload runat="server" ID="mfbFu3" ClientVisible="false" />
        <uc2:mfbFileUpload runat="server" ID="mfbFu4" ClientVisible="false" />
    </asp:View>
    <asp:View ID="vwAjaxUpload" runat="server">
        <asp:Image ID="myThrobber" ImageUrl="~/images/ajax-loader.gif" runat="server" style="display:None" />
        <div>
            <asp:AjaxFileUpload ID="AjaxFileUpload1" runat="server" AutoStartUpload="true" CssClass="mfbDefault" MaxFileSize="300000"
                    ThrobberID="myThrobber" AllowedFileTypes="heic,jpg,jpeg,pdf,jpe,png" MaximumNumberOfFiles="10" OnUploadComplete="AjaxFileUpload1_UploadComplete" OnUploadCompleteAll="AjaxFileUpload1_UploadCompleteAll" />
        </div>
        <uc2:mfbImageList ID="mfbImageListPending" NoRequery="true" CanEdit="true" runat="server" Columns="4" MaxImage="-1" />
        <asp:Panel ID="pnlRefresh" runat="server" Visible="false">
            <script>
                function ajaxFileUploadAttachments_UploadComplete(sender, e) {
                    __doPostBack('<% =btnForceRefresh.ClientID %>', ''); // Do post back only after all files have been uploaded
                }
            </script>
            <asp:Button ID="btnForceRefresh" runat="server" OnClick="btnForceRefresh_Click" style="display:none;" />
        </asp:Panel>
        <asp:LinkButton ID="lnkBtnForceLegacy" runat="server" Text="<%$ Resources:LocalizedText, AjaxFileUploadDowngradePrompt %>" CssClass="fineprint" OnClick="lnkBtnForceLegacy_Click"></asp:LinkButton>
    </asp:View>
</asp:MultiView>
<span class="fineprint"><% =Resources.LocalizedText.ImageDisclaimer %></span>
<asp:Panel runat="server" ID="pnlclipboard" style="display:none">
    <asp:Button ID="bUpClp" runat="server" OnClick="btnForceRefresh_Click" style="display:none;" />
    <asp:Image ID="iThrb" runat="server" ImageUrl="~/images/ajax-loader.gif" />
    <asp:HiddenField ID="hImg" runat="server" EnableViewState="false" />
</asp:Panel>


