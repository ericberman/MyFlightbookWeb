<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="oAuthClientTest.aspx.cs" Inherits="Public_oAuthClientTest" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    
    
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <table>
        <tr>
            <td>Authorization URL:</td>
            <td><asp:TextBox ID="txtAuthURL" runat="server" Width="400px">https://myflightbook.com/logbook/member/oAuthAuthorize.aspx</asp:TextBox></td>
        </tr>
        <tr>
            <td>Token URL:</td>
            <td>
    <asp:TextBox ID="txtTokenURL" runat="server" Width="400px">https://myflightbook.com/logbook/public/oAuthToken.aspx</asp:TextBox>
            </td>
        </tr>
        <tr style="vertical-align:top">
            <td><div>Resource URL:</div>
                <div class="fineprint">(minus verb, other data)</div></td>
            <td>
                <asp:TextBox ID="txtResourceURL" runat="server" Width="400px">https://myflightbook.com/logbook/public/oAuthToken.aspx/</asp:TextBox>
            </td>
        </tr>
        <tr style="vertical-align:top">
            <td>Resource Action:</td>
            <td>
                <div>
                    <asp:DropDownList ID="cmbResourceAction" AutoPostBack="true" OnSelectedIndexChanged="cmbResourceAction_SelectedIndexChanged" runat="server">
                        <asp:ListItem Selected="True" Text="(Custom - GET only)" Value=""></asp:ListItem>
                        <asp:ListItem Value="currency" Text="Currency"></asp:ListItem>
                        <asp:ListItem Value="totals" Text="Totals"></asp:ListItem>
                        <asp:ListItem Value="addflight" Text="Add flight (POST only)"></asp:ListItem>
                    </asp:DropDownList>
                    <asp:CheckBox ID="ckJSON" runat="server" Text="Request JSON data (default is XML)" />
                </div>
                <asp:MultiView ID="mvService" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwCustom" runat="server">
                        <div class="detailsSection">
                            <table>
                                <tr style="vertical-align:top">
                                    <td>Verb:</td>
                                    <td><asp:TextBox ID="txtCustomVerb" runat="server"></asp:TextBox></td>
                                </tr>
                                <tr style="vertical-align:top">
                                    <td><div>Additional URL data:</div><div class="fineprint">(will NOT be URL encoded)</div></td>
                                    <td><asp:TextBox ID="txtCustomData" runat="server"></asp:TextBox></td>
                                </tr>
                            </table>
                        </div>
                    </asp:View>
                    <asp:View ID="vwTotals" runat="server">
                        <div class="detailsSection">
                            <div>(Optional): JSON-ified Flight Query:</div>
                            <asp:TextBox ID="txtFlightQuery" runat="server" TextMode="MultiLine" Width="200px" Rows="5"></asp:TextBox>
                        </div>
                    </asp:View>
                    <asp:View ID="vwCurrency" runat="server">

                    </asp:View>
                    <asp:View ID="vwAddFlight" runat="server">
                        <div class="detailsSection">
                            <table>
                                <tr style="vertical-align:top">
                                    <td><div>Flight data JSON-ified:</div></td>
                                    <td><asp:TextBox ID="txtFlightToAdd" runat="server" TextMode="MultiLine" Width="200px" Rows="5"></asp:TextBox></td>
                                </tr>
                                <tr style="vertical-align:top">
                                    <td>Format:</td>
                                    <td>
                                        <asp:DropDownList ID="cmbFlightFormat" runat="server">
                                            <asp:ListItem Value="0" Text="Native" Selected="True"></asp:ListItem>
                                            <asp:ListItem Value="1" Text="LogTen Pro"></asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr>
            <td>Client ID:</td>
            <td>
                <asp:TextBox ID="txtClientID" runat="server" Width="400px"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>Client Secret:</td>
            <td>
                <asp:TextBox ID="txtClientSecret" runat="server" Width="400px"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>Redirect URL:</td>
            <td>
                <asp:TextBox ID="txtRedirectURL" runat="server" Width="400px">http://myflightbook.com/logbook/playpen/oAuthClientTest.aspx</asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>Scope:</td>
            <td>
                <asp:TextBox ID="txtScope" runat="server" Width="400px">currency totals addflight</asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>State</td>
            <td>
                <asp:TextBox ID="txtState" runat="server" Width="400px"></asp:TextBox>
            </td>
        </tr>
    </table>
    <br />
    <asp:Button ID="btnGetAuth" runat="server" Text="Get Authorization" OnClick="btnGetAuth_Click" />
    &nbsp;<asp:Button ID="btnGetToken" runat="server" Text="Get Token" OnClick="btnGetToken_Click" />
    &nbsp;<asp:Button ID="btnGetResrouce" runat="server" Text="Get Resource (GET)" OnClick="btnGetResrouce_Click" />
    &nbsp;<asp:Button ID="btnPostResource" runat="server" Text="Get Resource (POST)" OnClick="btnPostResource_Click" />
    &nbsp;<asp:Button ID="btnClearState" runat="server" OnClick="btnClearState_Click" Text="Clear State" />
    <div>
        <p>Output/Result:</p>
        <asp:GridView ID="gvResults" runat="server" AutoGenerateColumns="true">
        </asp:GridView>
    </div>
    <div>
        <table cellpadding="3">
            <tr valign="top">
                <td>Authorization result:</td>
                <td><asp:Label ID="lblAuthorization" runat="server" Text=""></asp:Label></td>
            </tr>
            <tr valign="top">
                <td>Token result:</td>
                <td><asp:Label ID="lblToken" runat="server" Text=""></asp:Label></td>
            </tr>
        </table>
    </div>
    <div>
        Private:<br />
        <asp:Label ID="lblPrivate" runat="server" Text="Label"></asp:Label>
        <br /><br />
        Public:<br />
        <asp:Label ID="lblPublic" runat="server" Text="Label"></asp:Label>
    </div>
</asp:Content>

