<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbTimeZone" Codebehind="mfbTimeZone.ascx.cs" %>
    <asp:HiddenField ID="hdnTZOffset" Value="0" runat="server" />
<script>
//<![CDATA[
    document.getElementById('<% =hdnTZOffset.ClientID %>').value = -(new Date()).getTimezoneOffset();
    //]]>
</script>
