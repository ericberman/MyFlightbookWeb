<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbCloudAhoy.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbCloudAhoy" %>
<table>
    <tr>
        <td><asp:Image ID="imgCloudAhoy" runat="server" ImageUrl="~/images/CloudAhoyTrans.png" AlternateText="<%$ Resources:Preferences, CloudAhoyName %>" ToolTip="<%$ Resources:Preferences, CloudAhoyName %>" /></td>
        <td>
            <asp:MultiView ID="mvCloudAhoy" runat="server">
                <asp:View ID="vwAuthCloudAhoy" runat="server">
                    <p><asp:LinkButton ID="lnkAuthCloudAhoy" runat="server" OnClick="lnkAuthCloudAhoy_Click" /></p>
                </asp:View>
                <asp:View ID="vwDeAuthCloudAhoy" runat="server">
                    <p><asp:Localize ID="locCloudAhoyIsAuthed" runat="server"></asp:Localize></p>
                    <p><asp:LinkButton ID="lnkDeAuthCloudAhoy" runat="server" OnClick="lnkDeAuthCloudAhoy_Click" meta:resourcekey="lnkDeAuthCloudAhoyResource1"></asp:LinkButton></p>
                </asp:View>
            </asp:MultiView>
        </td>
    </tr>
</table>