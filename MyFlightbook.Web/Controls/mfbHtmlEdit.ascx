<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbHtmlEdit.ascx.cs" Inherits="Controls_mfbHtmlEdit" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<asp:TextBox ID="txtMain" BackColor="White" runat="server" TextMode="MultiLine" Rows="5" Height="120" Width="400px"></asp:TextBox>
<asp:HtmlEditorExtender ID="TextBox1_HtmlEditorExtender" OnPreRender="TextBox1_HtmlEditorExtender_PreRender" runat="server" TargetControlID="txtMain"></asp:HtmlEditorExtender>
<br />