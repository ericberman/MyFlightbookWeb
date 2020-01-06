<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSearchbox" Codebehind="mfbSearchbox.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" style="padding-top:3px;">
    <div style="border: 1px solid darkgray; border-radius: 14px; height: 24px; display: table-cell; vertical-align: middle; text-align:left; padding-left: 8px; padding-right:3px; ">
        <asp:Image ID="Image1" runat="server" ImageUrl="~/images/Search.png" style="vertical-align:middle" GenerateEmptyAlternateText="true" Height="20px" />
        <asp:TextBox ID="txtSearch" runat="server" Width="120px" Font-Size="8pt" BorderStyle="None" CssClass="noselect" style="vertical-align:middle"></asp:TextBox>
        <cc1:TextBoxWatermarkExtender
            ID="TextBoxWatermarkExtender1" runat="server" TargetControlID="txtSearch" EnableViewState="false"
            WatermarkText="<%$ Resources:LocalizedText, SearchBoxWatermark %>" WatermarkCssClass="watermark">
        </cc1:TextBoxWatermarkExtender>
    </div>
    <asp:Button ID="btnSearch" style="display:none" runat="server" Text="<%$ Resources:LocalizedText, SearchBoxGo %>" CausesValidation="false" onclick="btnSearch_Click" Font-Size="9px" CssClass="itemlabel" />
</asp:Panel>