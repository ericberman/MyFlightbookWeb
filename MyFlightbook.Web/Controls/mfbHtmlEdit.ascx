<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbHtmlEdit.ascx.cs" Inherits="Controls_mfbHtmlEdit" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<asp:TextBox ID="txtMain" BackColor="White" runat="server" TextMode="MultiLine" Rows="5" Height="120" Width="400px"></asp:TextBox>
<asp:HtmlEditorExtender ID="TextBox1_HtmlEditorExtender" runat="server" TargetControlID="txtMain"></asp:HtmlEditorExtender>
<br />