<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbTimeZone.ascx.cs" Inherits="Controls_mfbTimeZone" %>
    <asp:HiddenField ID="hdnTZOffset" Value="0" runat="server" />
<script>
//<![CDATA[
    document.getElementById('<% =hdnTZOffset.ClientID %>').value = -(new Date()).getTimezoneOffset();
    //]]>
</script>
