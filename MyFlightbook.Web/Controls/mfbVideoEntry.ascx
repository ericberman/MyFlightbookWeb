<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbVideoEntry" Codebehind="mfbVideoEntry.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc1" %>
<%@ Register src="mfbFileUpload.ascx" tagname="mfbFileUpload" tagprefix="uc2" %>
<asp:Panel ID="pnlVideoUpload" runat="server">
    <p>
        <asp:Label ID="lblVideos" runat="server" Text="<%$ Resources:LocalizedText, videoHeader %>"></asp:Label>&nbsp;
        <asp:Label ID="lblShowHideVideo" runat="server"></asp:Label>
    </p>
</asp:Panel>
<cc1:CollapsiblePanelExtender ID="cpeAddVideo" runat="server" 
    TargetControlID="pnlNewVideo" CollapsedSize="0" ExpandControlID="lblShowHideVideo"
    CollapseControlID="lblShowHideVideo" Collapsed="True" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" 
    TextLabelID="lblShowHideVideo" Enabled="True"></cc1:CollapsiblePanelExtender>
<asp:Panel ID="pnlNewVideo" runat="server" DefaultButton="btnAdd" Height="0px" style="overflow:hidden">
        <p><asp:Label ID="lblOverview" runat="server" CssClass="fineprint" Text="<%$ Resources:LocalizedText, videoOverview %>"></asp:Label></p>
        <asp:TextBox ID="txtVideoToEmbed" runat="server" Width="450px"></asp:TextBox>
        <cc1:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" runat="server" TargetControlID="txtVideoToEmbed" WatermarkText="<%$ Resources:LocalizedText, videoPrompt %>" WatermarkCssClass="watermark"></cc1:TextBoxWatermarkExtender>
        <br />
        <asp:TextBox ID="txtComment" Width="450px" runat="server"></asp:TextBox>
        <cc1:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender2" runat="server" TargetControlID="txtComment" WatermarkText="<%$ Resources:LocalizedText, videoCommentPrompt %>" WatermarkCssClass="watermark"></cc1:TextBoxWatermarkExtender>
        <br />
        <asp:Button ID="btnAdd" CausesValidation="true" runat="server" Text="<%$Resources:LocalizedText, videoAdd %>" OnClick="btnAdd_Click" /><br />
        <asp:Label ID="lblDisclaimer" CssClass="fineprint" runat="server" Text="<%$ Resources:LocalizedText, videoDisclaimer %>"></asp:Label>
        <asp:Panel ID="pnlError" Visible="false" EnableViewState="false" runat="server">
            <asp:Label ID="lblErr" CssClass="error" EnableViewState="false" runat="server" Text=""></asp:Label>
        </asp:Panel>
</asp:Panel>
<asp:Panel ID="pnlvideos" runat="server">
    <asp:GridView ID="gvVideos" AutoGenerateColumns="false" runat="server" BorderStyle="None" GridLines="None" OnRowCommand="gvVideos_RowCommand" OnRowDataBound="gvVideos_RowDataBound" ShowHeader="False">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div style="text-align:center">
                        <asp:LinkButton ID="lnkDelete" CommandName="_Delete" runat="server">
                            <asp:Image ID="imgDelete" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LogbookEntry, LogbookDeleteVideoTooltip %>" ToolTip="<%$ Resources:LogbookEntry, LogbookDeleteVideoTooltip %>" runat="server" />
                        </asp:LinkButton>
                    </div>
                    <div><asp:Literal ID="litVideo" runat="server"></asp:Literal></div>
                    <p><asp:HyperLink ID="lnkFull" runat="server" Text='<%# Eval("DisplayString") %>' NavigateUrl='<%# Eval("VideoReference") %>' Target="_blank" ></asp:HyperLink></p>
                </ItemTemplate>
                <ItemStyle HorizontalAlign="Center" />
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Panel>
<asp:HiddenField ID="hdnDefaultFlightID" runat="server" />
<asp:HiddenField ID="hdnCanEdit" runat="server" />
<asp:HiddenField ID="hdnCanDelete" runat="server" />

