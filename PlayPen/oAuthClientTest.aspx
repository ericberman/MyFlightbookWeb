<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="oAuthClientTest.aspx.cs" Inherits="Public_oAuthClientTest" %>

<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>



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
                        <asp:ListItem Value="VisitedAirports" Text="Visited Airports"></asp:ListItem>
                        <asp:ListItem Value="AddAircraftForUser" Text="Add Aircraft"></asp:ListItem>
                        <asp:ListItem Value="AircraftForUser" Text="Aircraft for user"></asp:ListItem>
                        <asp:ListItem Value="MakesAndModels" Text="Makes / Models"></asp:ListItem>
                        <asp:ListItem Value="addFlight" Text="Add flight (POST only)"></asp:ListItem>
                        <asp:ListItem Value="FlightsWithQueryAndOffset" Text="View flights (POST only)"></asp:ListItem>
                        <asp:ListItem Value="FlightPathForFlight" Text="Flight Path for flight"></asp:ListItem>
                        <asp:ListItem Value="FlightPathForFlightGPX" Text="Flight Path (GPX) for flight"></asp:ListItem>
                        <asp:ListItem Value="DeleteLogbookEntry" Text="Delete Flight"></asp:ListItem>
                        <asp:ListItem Value="PropertiesForFlight" Text="Properties For Flight"></asp:ListItem>
                        <asp:ListItem Value="AvailablePropertyTypesForUser" Text="Properties for user"></asp:ListItem>
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
                    <asp:View ID="vwFlightQuery" runat="server">
                        <div class="detailsSection">
                            <div>(Optional): JSON-ified Flight Query:</div>
                            <asp:TextBox ID="txtFlightQuery" runat="server" TextMode="MultiLine" Width="200px" Rows="5"></asp:TextBox>
                        </div>
                    </asp:View>
                    <asp:View ID="vwGetFlights" runat="server">
                        <table>
                            <tr>
                                <td>(Optional): JSON-ified Flight Query:</td>
                                <td><asp:TextBox ID="txtFlightQuery2" runat="server" TextMode="MultiLine" Width="200px" Rows="5"></asp:TextBox></td>
                            </tr>
                            <tr>
                                <td>Offset:</td>
                                <td><uc1:mfbDecimalEdit runat="server" ID="decOffset" EditingMode="Integer" /></td>
                            </tr>
                            <tr>
                                <td>Limit:</td>
                                <td><uc1:mfbDecimalEdit runat="server" ID="decLimit" EditingMode="Integer" /></td>
                            </tr>
                        </table>
                    </asp:View>
                    <asp:View ID="vwAddAircraft" runat="server">
                        <div class="detailsSection">
                            <table>
                                <tr>
                                    <td>Tail Number:</td>
                                    <td><asp:TextBox ID="txtTail" runat="server"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Model ID:</td>
                                    <td><uc1:mfbDecimalEdit runat="server" ID="decModelID" EditingMode="Integer" /></td>
                                </tr>
                                <tr>
                                    <td>Instance type ID:</td>
                                    <td><asp:TextBox ID="txtInstanceType" runat="server"></asp:TextBox></td>
                                </tr>
                            </table>
                        </div>
                    </asp:View>
                    <asp:View ID="vwNoParams" runat="server"></asp:View>
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
                    <asp:View ID="vwFlightID" runat="server">
                        <div class="detailsSection">
                            Flight ID: <uc1:mfbDecimalEdit runat="server" ID="decFlightID" EditingMode="Integer" />
                        </div>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr>
            <td>Client ID:</td>
            <td>
                <asp:TextBox ID="txtClientID" runat="server" Width="400px"></asp:TextBox>
                <asp:RequiredFieldValidator ID="reqClientID" runat="server" ErrorMessage="Client ID is required for authorization" ControlToValidate="txtClientID" ValidationGroup="vgAuthorize" CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
            </td>
        </tr>
        <tr>
            <td>Client Secret:</td>
            <td>
                <asp:TextBox ID="txtClientSecret" runat="server" Width="400px"></asp:TextBox>
                <asp:RequiredFieldValidator ID="reqClientSecret" runat="server" ErrorMessage="Client secret is required for authorization" ControlToValidate="txtClientSecret"  ValidationGroup="vgAuthorize" CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
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
                <asp:TextBox ID="txtScope" runat="server" Width="400px">currency totals addflight readflight addaircraft readaircraft visited images</asp:TextBox>
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
    <div><asp:Label ID="lblErr" EnableViewState="false" runat="server" CssClass="error"></asp:Label></div>
    <div style="padding:5px; background-color:lightgray; border:1px solid black; border-radius: 4px; margin: 5px;">
        <uc1:Expando runat="server" ID="Expando">
            <Header>
                Output/Result:
            </Header>
            <Body>
                <asp:GridView ID="gvResults" runat="server" AutoGenerateColumns="true">
                </asp:GridView>
            </Body>
        </uc1:Expando>
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
</asp:Content>

