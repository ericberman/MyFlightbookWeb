<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Title="Manage Achievements" CodeBehind="AdminAchievements.aspx.cs" Inherits="MyFlightbook.Web.Admin.AdminAchievements" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbDecimalEdit.ascx" TagName="mfbDecimalEdit" TagPrefix="uc3" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools - Achievements
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:Button ID="btnInvalidateUserAchievements" runat="server" OnClick="btnInvalidateUserAchievements_Click" Text="Invalidate Badge Cache" />
    &nbsp;(if any badge criteria changes, use this to set everybody to &quot;needs computed&quot;)

    <h2>Airport-list achievements:</h2>
    <table style="width: 400px">
        <tr>
            <td style="vertical-align: top"><b>Title</b></td>
            <td>
                <asp:TextBox ID="txtAirportAchievementName" runat="server" /></td>
        </tr>
        <tr>
            <td style="vertical-align: top"><b>Binary (all or nothing)?</b></td>
            <td>
                <asp:CheckBox ID="ckBinaryAchievement" runat="server" /></td>
        </tr>
        <tr>
            <td style="vertical-align: top"><b>Bronze Level:</b></td>
            <td>
                <uc3:mfbDecimalEdit ID="mfbDecEditBronze" DefaultValueInt="0" EditingMode="Integer" runat="server" />
            </td>
        </tr>
        <tr>
            <td style="vertical-align: top"><b>Silver Level:</b></td>
            <td>
                <uc3:mfbDecimalEdit ID="mfbDecEditSilver" DefaultValueInt="0" EditingMode="Integer" runat="server" />
            </td>
        </tr>
        <tr>
            <td style="vertical-align: top"><b>Gold Level:</b></td>
            <td>
                <uc3:mfbDecimalEdit ID="mfbDecEditGold" DefaultValueInt="0" EditingMode="Integer" runat="server" />
            </td>
        </tr>
        <tr>
            <td style="vertical-align: top"><b>Platinum Level:</b></td>
            <td>
                <uc3:mfbDecimalEdit ID="mfbDecEditPlatinum" DefaultValueInt="0" EditingMode="Integer" runat="server" />
            </td>
        </tr>
        <tr>
            <td style="vertical-align: top"><b>Overlay</b></td>
            <td>
                <asp:TextBox ID="txtOverlay" runat="server" /></td>
        </tr>
        <tr>
            <td style="vertical-align: top"><b>Airport Codes</b></td>
            <td>
                <asp:TextBox ID="txtAirportAchievementList" TextMode="MultiLine" runat="server" Width="300px"></asp:TextBox></td>
        </tr>
    </table>
    <asp:Button ID="btnAddAirportAchievement" OnClick="btnAddAirportAchievement_Click" runat="server" Text="Add" />
    <asp:SqlDataSource ID="sqlDSAirportAchievements" runat="server"
        ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
        UpdateCommand="UPDATE airportlistachievement SET idAchievement=?idAchievement, name=?name, overlayname=?overlayname, airportcodes=?airportcodes, thresholdBronze=?thresholdBronze, thresholdSilver=?thresholdSilver, thresholdGold=?thresholdGold, thresholdPlatinum=?thresholdPlatinum, fBinaryOnly=?fBinaryOnly WHERE idAchievement=?idAchievement"
        SelectCommand="SELECT * FROM airportlistachievement">
        <UpdateParameters>
            <asp:Parameter Name="idAchievement" Type="Int32" Direction="InputOutput" />
            <asp:Parameter Name="name" Type="String" Size="100" Direction="InputOutput" />
            <asp:Parameter Name="overlayname" Type="String" Size="45" Direction="InputOutput" />
            <asp:Parameter Name="airportcodes" Type="String" Size="1000" Direction="InputOutput" />
            <asp:Parameter Name="thresholdBronze" Type="Int32" Direction="InputOutput" />
            <asp:Parameter Name="thresholdSilver" Type="Int32" Direction="InputOutput" />
            <asp:Parameter Name="thresholdGold" Type="Int32" Direction="InputOutput" />
            <asp:Parameter Name="thresholdPlatinum" Type="Int32" Direction="InputOutput" />
            <asp:Parameter Name="fBinaryOnly" Type="Int16" Direction="InputOutput" />
        </UpdateParameters>
    </asp:SqlDataSource>
    <asp:UpdatePanel ID="UpdatePanel2" runat="server">
        <ContentTemplate>
            <asp:GridView ID="gvAirportAchievements" runat="server" AutoGenerateColumns="false" AutoGenerateEditButton="true" CellPadding="5" DataSourceID="sqlDSAirportAchievements">
                <EmptyDataTemplate>
                    (No airportlist achievements defined)
                </EmptyDataTemplate>
                <Columns>
                    <asp:TemplateField>
                        <ItemTemplate>
                            <table>
                                <tr>
                                    <td>
                                        <asp:Image ID="imgOverlay" ImageUrl='<%# String.Format("~/images/BadgeOverlays/{0}", Eval("overlayname")) %>' runat="server" />
                                    </td>
                                    <td>
                                        <asp:Label ID="txtID" runat="server" Text='<%# Bind("idAchievement") %>' Font-Bold="true" />
                                        -
                                        <asp:Label ID="txtTitle" runat="server" Text='<%# Bind("name") %>' />
                                        <br />
                                        <asp:HyperLink ID="lnkViewAirports" Target="_blank" runat="server" NavigateUrl='<%# String.Format("~/mvc/Airport/MapRoute?Airports={0}", HttpUtility.UrlEncode(Eval("airportcodes").ToString())) %>'>
                                            <asp:Label ID="txtText" runat="server" Width="300px" Text='<%# Bind("airportcodes")%>' />
                                        </asp:HyperLink><asp:Label ID="lblLevels" runat="server" Text='<%# Convert.ToInt32(Eval("fBinaryOnly")) != 0 ? "Binary Only" : String.Format("{0}, {1}, {2}, {3}", Eval("thresholdBronze"), Eval("thresholdSilver"), Eval("thresholdGold"), Eval("thresholdPlatinum"))  %>'></asp:Label></td>
                                </tr>
                            </table>
                        </ItemTemplate>
                        <EditItemTemplate>
                            <table style="width: 400px">
                                <tr>
                                    <td style="vertical-align: top"><b>ID</b></td>
                                    <td>
                                        <asp:Label ID="txtID" runat="server" Text='<%# Bind("idAchievement") %>' /></td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Title</b></td>
                                    <td>
                                        <asp:TextBox ID="txtTitle" runat="server" Text='<%# Bind("name") %>' /></td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Binary (all or nothing)?</b></td>
                                    <td>
                                        <asp:TextBox ID="txtBinaryOnly" runat="server" Text='<%# Bind("fBinaryOnly") %>' /></td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Bronze Level:</b></td>
                                    <td>
                                        <asp:TextBox ID="txtBronze" runat="server" Text='<%# Bind("thresholdBronze") %>' /><br />
                                    </td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Silver Level:</b></td>
                                    <td>
                                        <asp:TextBox ID="txtSilver" runat="server" Text='<%# Bind("thresholdSilver") %>' /><br />
                                    </td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Gold Level:</b></td>
                                    <td>
                                        <asp:TextBox ID="txtGold" runat="server" Text='<%# Bind("thresholdGold") %>' /><br />
                                    </td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Platinum Level:</b></td>
                                    <td>
                                        <asp:TextBox ID="txtPlatinum" runat="server" Text='<%# Bind("thresholdPlatinum") %>' />
                                    </td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Overlay</b></td>
                                    <td>
                                        <asp:TextBox ID="txtOverlay" runat="server" Text='<%# Bind("overlayname") %>' /></td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Airport Codes</b></td>
                                    <td>
                                        <asp:TextBox ID="txtText" runat="server" Width="300px" Text='<%# Bind("airportcodes")%>' TextMode="MultiLine" /></td>
                                </tr>
                            </table>
                        </EditItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
</asp:Content>