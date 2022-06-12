<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbCloudStorage.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbCloudStorage" %>
<div class="prefSectionRow">
    <p><asp:Localize ID="locAboutCloudStorage" runat="server"></asp:Localize></p>
    <table>
        <tr style="vertical-align:top">
            <td style="width:180px">
                <!-- This comes from https://www.dropbox.com/developers/reference/branding, per their guidelines -->
                <asp:Image ID="DropboxLogo" 
                    ImageUrl="~/images/dropbox-logos_dropbox-logotype-blue.png" runat="server" 
                    AlternateText="<%$ Resources:Preferences, CloudStorageDropboxName %>" Width="180px" />
            </td>
            <td>
                <asp:MultiView ID="mvDropBoxState" runat="server">
                    <asp:View ID="vwAuthDropBox" runat="server">
                        <p><asp:LinkButton ID="lnkAuthDropbox" runat="server" 
                            onclick="lnkAuthDropbox_Click" /></p>
                    </asp:View>
                    <asp:View ID="vwDeAuthDropbox" runat="server">
                        <p><asp:Localize ID="locDropboxIsAuthed" runat="server" /></p>
                        <p>
                        <asp:LinkButton ID="lnkDeAuthDropbox" runat="server" 
                            onclick="lnkDeAuthDropbox_Click" /></p>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr style="vertical-align:top" runat="server" id="rowGDrive">
            <td style="width:180px">
                <!-- This comes from https://developers.google.com/drive/v2/web/branding, per their guidelines -->
                <asp:Image ID="GDriveLogo" 
                    ImageUrl="~/images/google-drive-logo-lockup.png" runat="server" 
                    AlternateText="<%$ Resources:Preferences, CloudStorageGoogleDriveName %>" Width="180px" />
            </td>
            <td>
                <asp:MultiView ID="mvGDriveState" runat="server">
                    <asp:View ID="vwAuthGDrive" runat="server">
                        <p><asp:LinkButton ID="lnkAuthorizeGDrive" runat="server" OnClick="lnkAuthorizeGDrive_Click" /></p>
                    </asp:View>
                    <asp:View ID="vwDeAuthGDrive" runat="server">
                        <p><asp:Localize ID="locGoogleDriveIsAuthed" runat="server"></asp:Localize></p>
                        <p><asp:LinkButton ID="lnkDeAuthGDrive" runat="server" OnClick="lnkDeAuthGDrive_Click"></asp:LinkButton></p>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr style="vertical-align:top" runat="server" id="rowOneDrive">
            <td style="width:180px">
                <!-- This comes from https://msdn.microsoft.com/en-us/onedrive/dn673556.aspx, per their guidelines -->
                <asp:Image ID="Image3" 
                    ImageUrl="~/images/OneDrive_rgb_Blue2728.png" runat="server" 
                    AlternateText="<%$ Resources:Preferences, CloudStorageOneDriveName %>" Width="180px" />
            </td>
            <td>
                <asp:MultiView ID="mvOneDriveState" runat="server">
                    <asp:View ID="vwAuthOneDrive" runat="server">
                        <p><asp:LinkButton ID="lnkAuthorizeOneDrive" runat="server" OnClick="lnkAuthorizeOneDrive_Click"></asp:LinkButton></p>
                    </asp:View>
                    <asp:View ID="vwDeAuthOneDrive" runat="server">
                        <p><asp:Localize ID="locOneDriveIsAuthed" 
                            runat="server"></asp:Localize></p>
                        <p><asp:LinkButton ID="lnkDeAuthOneDrive" runat="server" OnClick="lnkDeAuthOneDrive_Click"></asp:LinkButton></p>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
    </table>
</div>
<asp:RadioButtonList ID="rblCloudBackupAppendDate" runat="server" AutoPostBack="true" OnSelectedIndexChanged="rblCloudBackupAppendDate_SelectedIndexChanged">
    <asp:ListItem Selected="True" Value="False" Text="<%$ Resources:Preferences, CloudStorageAppendDate %>"></asp:ListItem>
    <asp:ListItem Value="True" Text="<%$ Resources:Preferences, CloudStorageOverwrite %>"></asp:ListItem>
</asp:RadioButtonList>
<asp:CheckBox ID="ckGroupByMonth" runat="server" Text="<%$ Resources:Preferences, CloudStorageGroupByMonth %>" AutoPostBack="true" OnCheckedChanged="ckGroupByMonth_CheckedChanged" />
<asp:Panel ID="pnlDefaultCloud" Visible="False" runat="server">
    <hr />
    <asp:Label ID="lblPickDefault" runat="server" Text="<%$ Resources:Preferences, CloudStoragePickDefault %>" />
    <asp:DropDownList ID="cmbDefaultCloud" AutoPostBack="True" OnSelectedIndexChanged="cmbDefaultCloud_SelectedIndexChanged" runat="server">
    </asp:DropDownList>
</asp:Panel>
