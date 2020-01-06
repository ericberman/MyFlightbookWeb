<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_oAuthClientTest" Codebehind="oAuthClientTest.aspx.cs" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    OAuth Test Bed
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <table>
        <tr>
            <td>Client ID:</td>
            <td>
                <asp:TextBox ID="txtClientID" runat="server" Width="400px"></asp:TextBox>
                <asp:RequiredFieldValidator ID="reqClientID" runat="server" ErrorMessage="Client ID is required for authorization" ControlToValidate="txtClientID" CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
            </td>
        </tr>
        <tr>
            <td>Client Secret:</td>
            <td>
                <asp:TextBox ID="txtClientSecret" runat="server" Width="400px"></asp:TextBox>
                <asp:RequiredFieldValidator ID="reqClientSecret" runat="server" ErrorMessage="Client secret is required for authorization" ControlToValidate="txtClientSecret"  CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <h2>1) Authorization</h2>
                <p>Pass your client ID to the the authorization URL (below).  The user signs in and authorizes the app to have the requested permissions</p>
                <p>The user is then redirected back to the Ridirect URL (below), along with an authorization, which can be used to get a token.</p>
            </td>
        </tr>
        <tr>
            <td>Authorization URL:</td>
            <td><asp:TextBox ID="txtAuthURL" runat="server" Width="400px">https://myflightbook.com/logbook/member/oAuthAuthorize.aspx</asp:TextBox></td>
        </tr>
        <tr>
            <td>Redirect URL:</td>
            <td>
                <asp:TextBox ID="txtRedirectURL" runat="server" Width="400px">https://myflightbook.com/logbook/playpen/oAuthClientTest.aspx</asp:TextBox>
            </td>
        </tr>
        <tr>
            <td style="vertical-align:top">Scope:</td>
            <td>
                <asp:TextBox ID="txtScope" runat="server" Width="400px">currency totals addflight readflight addaircraft readaircraft visited namedqueries images</asp:TextBox>
                <asp:RequiredFieldValidator ID="reqScopeRequired" runat="server" ErrorMessage="At least some scopes are required for authorization" ControlToValidate="txtScope"  CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
                <p><asp:Button ID="btnGetAuth" runat="server" Text="Get Authorization" OnClick="btnGetAuth_Click" /></p>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <h2>2) Convert the authorization into an authtoken</h2>
                <p>The authorization proves that the user gave your app permission, but the authorization is not secure because it was visible in the user's browser and if hijacked could be used by others</p>
                <p>So now you convert it to a secure oAuth authtoken by passing that authorization back to the server - over a secure server-to-server channel (i.e., from your app, not using the browser) 
                    along with your client ID and secret, using the Token URL below.</p>
                <p>The presence of the client ID and secret prove that your app is your app.  If successful, an authtoken is returned.</p>
            </td>
        </tr>
        <tr>
            <td style="vertical-align:top">Token URL</td>
            <td>
                <asp:TextBox ID="txtTokenURL" runat="server" Width="400px">https://myflightbook.com/logbook/OAuth/oAuthToken.aspx</asp:TextBox>
                <asp:RequiredFieldValidator ID="valReqToken" runat="server" ErrorMessage="A token endpoint (URL) is required" ControlToValidate="txtScope"  CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
                <p><asp:Button ID="btnGetToken" runat="server" Text="Get Token" OnClick="btnGetToken_Click" /></p>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <h2>3. Use the authtoken to interact with the server</h2>
                <p>You can use "POST" for any of the actions, GET for only some of them (that don't require things like file upload)</p>
                <p>You can request results in either XML or JSON</p>
            </td>
        </tr>
        <tr style="vertical-align:top">
            <td style="vertical-align:top"><div>Resource URL:</div>
                <div class="fineprint">(minus verb, other data)</div></td>
            <td>
                <div><asp:TextBox ID="txtResourceURL" runat="server" Width="400px">https://myflightbook.com/logbook/OAuth/oAuthToken.aspx/</asp:TextBox>
                <asp:RequiredFieldValidator ID="valReqResource" runat="server" ErrorMessage="The URL for resources is required." ControlToValidate="txtScope"  CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator></div>
            </td>
        </tr>
        <tr style="vertical-align:top">
            <td>Resource Action:</td>
            <td>
                <div>
                    <asp:DropDownList ID="cmbResourceAction" AutoPostBack="true" OnSelectedIndexChanged="cmbResourceAction_SelectedIndexChanged" runat="server">
                        <asp:ListItem Selected="True" Text="(Custom - GET only)" Value="none"></asp:ListItem>
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
                        <asp:ListItem Value="GetNamedQueries" Text="Get Named Queries"></asp:ListItem>
                        <asp:ListItem Value="UploadImage" Text="Upload an image (POST only)"></asp:ListItem>
                    </asp:DropDownList>
                    <asp:CheckBox ID="ckJSON" runat="server" Text="Request JSON data (default is XML)" />
                    <div>For JSONP, provide a callback name (&callback=...): <asp:TextBox ID="txtCallBack" runat="server"></asp:TextBox></div>
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
                    <asp:View ID="vwImage" runat="server">
                        <div class="detailsSection">
                            <table>
                                <tr>
                                    <td>File to upload</td>
                                    <td>
                                        <asp:FileUpload ID="fuImage" runat="server" /></td>
                                </tr>
                                <tr>
                                    <td>Comment:</td>
                                    <td>
                                        <asp:TextBox ID="txtImgComment" runat="server"></asp:TextBox></td>
                                </tr>
                                <tr>
                                    <td>Latitude<br />(Optional)</td>
                                    <td><uc1:mfbDecimalEdit runat="server" ID="decImgLat" EditingMode="Decimal" /></td>
                                </tr>
                                <tr>
                                    <td>Longitude<br />(Optional)</td>
                                    <td><uc1:mfbDecimalEdit runat="server" ID="decImgLon" EditingMode="Decimal" /></td>
                                </tr>
                                <tr>
                                    <td>Additional params</td>
                                    <td>
                                        <table>
                                            <tr>
                                                <td>Param Name</td>
                                                <td>Param Value</td>
                                            </tr>
                                            <tr>
                                                <td><asp:TextBox ID="txtImageParamName1" runat="server"></asp:TextBox></td>
                                                <td><asp:TextBox ID="txtImageParam1" runat="server"></asp:TextBox></td>
                                            </tr>
                                            <tr>
                                                <td><asp:TextBox ID="txtImageParamName2" runat="server"></asp:TextBox></td>
                                                <td><asp:TextBox ID="txtImageParam2" runat="server"></asp:TextBox></td>
                                            </tr>
                                            <tr>
                                                <td><asp:TextBox ID="txtImageParamName3" runat="server"></asp:TextBox></td>
                                                <td><asp:TextBox ID="txtImageParam3" runat="server"></asp:TextBox></td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td>Target page</td>
                                    <td>
                                        <asp:TextBox ID="txtImgUploadURL" runat="server" Text="https://myflightbook.com/logbook/public/UploadPicture.aspx" Width="100%"></asp:TextBox></td>
                                </tr>
                            </table>
                        </div>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr>
            <td>State</td>
            <td>
                <asp:TextBox ID="txtState" runat="server" Width="400px"></asp:TextBox>
            </td>
        </tr>
    </table>
    
    <asp:Button ID="btnGetResource" runat="server" Text="Get Resource (GET)" OnClick="btnGetResource_Click" />
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

