<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    Codebehind="EditAirports.aspx.cs" Inherits="MyFlightbook.Mapping.EditAirports" culture="auto" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMapManager" TagPrefix="uc1" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc2" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text="<%$ Resources:Airports, EditAirportsTitle %>" />
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <div><asp:Localize ID="locEditAirportsDesc" runat="server" Text="<%$ Resources:Airports, EditAirportsDescription %>" />
    </div>
    <table style="width:100%">
        <tr>
            <td style="max-width:300px; vertical-align: top;">
                <asp:Panel ID="pnlEdit" runat="server" DefaultButton="btnAdd">
                    <table>
                        <tr style="vertical-align: top">
                            <td>
                                <asp:Label ID="lblCodePrompt" runat="server" Text="<%$ Resources:Airports, EditAirportsCodePrompt %>" />
                            </td>
                            <td>
                                <asp:TextBox ID="txtCode" runat="server" placeholder="<%$ Resources:Airports, wmAirportCode %>" />
                                <cc1:FilteredTextBoxExtender ID="txtCode_FilteredTextBoxExtender" FilterType="Numbers, UppercaseLetters, LowercaseLetters"
                                    runat="server" TargetControlID="txtCode" Enabled="True">
                                </cc1:FilteredTextBoxExtender>
                                <asp:RequiredFieldValidator ID="valReqCode" runat="server" ErrorMessage="<%$ Resources:Airports, errMissingCode %>" EnableClientScript="true"
                                    ControlToValidate="txtCode" Display="Dynamic" CssClass="error" />
                                <asp:RegularExpressionValidator ID="valRegCode" runat="server" ErrorMessage="<%$ Resources:Airports, errInvalidCodeLength %>" EnableClientScript="true"
                                    ControlToValidate="txtCode" Display="Dynamic" ValidationExpression="[a-zA-Z0-9]{2,6}" CssClass="error" />
                                <br />
                                <asp:Label ID="lblcodePromptExample" CssClass="fineprint" runat="server"  Text="<%$ Resources:Airports, EditAirportSampleCode %>" />
                            </td>
                        </tr>
                        <tr style="vertical-align: top">
                            <td>
                                <asp:Localize ID="locNamePrompt" runat="server" Text="<%$ Resources:Airports, airportNameShort %>" />
                            </td>
                            <td>
                                <asp:TextBox ID="txtName" runat="server" placeholder="<%$ Resources:Airports, wmFriendlyname %>" />
                                <asp:RequiredFieldValidator ID="valReqName" runat="server" ControlToValidate="txtName" EnableClientScript="true" 
                                    ErrorMessage="<%$ Resources:Airports, errMissingFacilityName %>" Display="Dynamic" CssClass="error" />
                                <br />
                                <asp:Label ID="lblNameExample" CssClass="fineprint" runat="server" Text="<%$ Resources:Airports, EditAirportSampleName %>" />
                            </td>
                        </tr>
                        <tr style="vertical-align: top">
                            <td>
                                <asp:Localize ID="locType" runat="server" Text="<%$ Resources:Airports, EditAirportFacilityType %>" />
                            </td>
                            <td>
                                <asp:DropDownList ID="cmbType" runat="server" DataSourceID="SqlDataSourceNavaidTypes"  
                                    DataTextField="FriendlyName" DataValueField="Code" onchange="javascript:centerToText();" />
                                <asp:SqlDataSource ID="SqlDataSourceNavaidtypes" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT * FROM NavAidTypes">
                                </asp:SqlDataSource>
                            </td>
                        </tr>
                        <tr style="vertical-align: top">
                            <td>
                                <asp:Localize ID="locLatitude" runat="server" Text="<%$ Resources:Airports, airportLatitude %>" />
                            </td>
                            <td>
                                <asp:TextBox ID="txtLat" runat="server" onchange="javascript:centerToText();"   />
                                <div>
                                    <asp:RequiredFieldValidator ID="valReqLat" runat="server" ControlToValidate="txtLat" EnableClientScript="true"  
                                        Display="Dynamic" ErrorMessage="<%$ Resources:Airports, errLatitudeMissing %>" CssClass="error" />
                                    <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender2" ValidChars="-.0123456789"
                                        runat="server" TargetControlID="txtLat" Enabled="True">
                                    </cc1:FilteredTextBoxExtender>
                                    <asp:RegularExpressionValidator ID="valRegLat" runat="server" EnableClientScript="true" CssClass="error"  
                                        Display="Dynamic" ControlToValidate="txtLat" ErrorMessage="<%$ Resources:Airports, errInvalidLatitude %>"
                                        ValidationExpression="-?\d{0,2}(.\d*)?" />
                                </div>
                            </td>
                        </tr>
                        <tr style="vertical-align: top">
                            <td>
                                <asp:Localize ID="locLongitude" runat="server" Text="<%$ Resources:Airports, airportLongitude %>" />
                            </td>
                            <td>
                                <div>
                                    <asp:TextBox ID="txtLong" runat="server" onchange="javascript:centerToText();"   />
                                    <asp:RequiredFieldValidator ID="valReqLon" runat="server" ControlToValidate="txtLong" EnableClientScript="true"
                                        Display="Dynamic"  ErrorMessage="<%$ Resources:Airports, errLongitudeMissing %>" CssClass="error" />
                                    <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" ValidChars="-.0123456789"
                                        runat="server" TargetControlID="txtLong" Enabled="True">
                                    </cc1:FilteredTextBoxExtender>
                                    <asp:RegularExpressionValidator ID="valRegLon" runat="server" EnableClientScript="true" CssClass="error"
                                        Display="Dynamic" ControlToValidate="txtLong" ErrorMessage="<%$ Resources:Airports, errInvalidLongitude %>"
                                        ValidationExpression="-?\d{0,3}(.\d*)?" />
                                </div>
                                <div>
                                    <asp:Label ID="lblTipHeader" runat="server" Text="<%$ Resources:Airports, EditAirportTipHeader %>" Font-Bold="True" />&nbsp;
                                    <asp:Label ID="lblClickToSetLatLon" runat="server" Text="<%$ Resources:Airports, EditAirportMapTip %>" />
                                </div>
                            </td>
                        </tr>
                        <tr runat="server" id="rowAdmin">
                            <td></td>
                            <td><asp:CheckBox ID="ckAsAdmin" runat="server" Text="Add as admin" /></td>
                        </tr>
                        <tr>
                            <td></td>
                            <td>
                                <asp:Button ID="btnAdd" runat="server" Text="<%$ Resources:Airports, EditAirportAddFacility %>" CausesValidation="true" OnClick="btnAdd_Click" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                            </td>
                            <td>
                                <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="false" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
            </td>
            <td style="vertical-align: top; width: 90%">
                <div style="margin-left: 10px; margin-right:10px;">
                    <uc1:mfbGoogleMapManager ID="MfbGoogleMapManager1" runat="server" AllowResize="false" Height="400px" />
                </div>
            </td>
        </tr>
    </table>
    <asp:Panel ID="pnlAdminImport" Visible="false" runat="server">
        <p>
            <asp:Label ID="Label1" runat="server" Text="Spreadsheet of airports:"></asp:Label>
            <uc2:mfbTooltip ID="mfbTooltip1" runat="server" BodyContent="FAA: FAA Designator,  ICAO: ICAO Designator, IATA:IATA Designator, Name: Facility Name,  (Latitude, Longitude) OR (LatLong), Type (optional)" />
            &nbsp;<asp:CheckBox 
                ID="ckShowAllUserAirports" runat="server" 
                Text="Continue to show all user airports (off for performance)" />
        </p>
        <div>
            <asp:FileUpload ID="fileUploadAirportList" runat="server" />
            <asp:Button ValidationGroup="importAirports" ID="btnImport" runat="server" Text="Import" OnClick="btnImport_Click" />
            <asp:Button ValidationGroup="importAirports" ID="btnBulkImport" runat="server" Text="Bulk Import (no interactive)" OnClick="btnBulkImport_Click" />
            <uc2:mfbTooltip ID="mfbTooltip2" runat="server" BodyContent="MUST be: FAA/IATA/ICAO for airportID, Name, Type, SourceUserName, Latitude, Longitude, Preferred.  SourceUserName and Preferred are optional.  OR OldCode/CurrentCode (bulk only) to backfill old codes.  Country and Admin1 are optional." />
            <cc1:ConfirmButtonExtender runat="server" ID="confirmBulkImport" TargetControlID="btnBulkImport" ConfirmText="THIS WILL BULK IMPORT WITH NO UNDO!" />
            <asp:Label ID="lblBulkImportResults" runat="server" Text="" EnableViewState="false"></asp:Label>
        </div>
        <p><asp:Label ID="lblUploadErr" runat="server" Text="" EnableViewState="false" CssClass="error"></asp:Label></p>
        <asp:Panel ID="pnlImportResults" runat="server" ScrollBars="Auto" Height="400px" Width="100%" Visible="false">
            <style>
                div.notOK {
                    background-color: lightgray;
                }

                tr.Handled, tr.Handled div.notOK {
                    background-color: lightgreen;
                }
            </style>
            <script type="text/javascript">
                function doImport(sender, aic, context, source, command) {

                    // See if the "use map" checkbox is checked
                    // If so, update to those locations.
                    if ($("#" + context["useMapCheckID"])[0].checked) {
                        aic.LatLong.Latitude = parseFloat($("#" + context["lat"])[0].value);
                        aic.LatLong.Longitude = parseFloat($("#" + context["lon"])[0].value);
                    }

                    var params = new Object();
                    params.aic = aic;
                    params.source = source;
                    params.szCommand = command;
                    var d = JSON.stringify(params);

                    $.ajax(
                        {
                            url: '<% =ResolveUrl("~/Admin/AdminAirportGeocoder.aspx/AirportImportCommand") %>',
                            type: "POST", data: d, dataType: "json", contentType: "application/json",
                            error: function (xhr, status, error) {
                                window.alert(xhr.responseJSON.Message);
                            },
                            complete: function (response) { },
                            success: function (response) {
                                // Sender should be a button within a div within a td within a tr.
                                sender.parentElement.parentElement.parentElement.className = 'Handled';
                            }
                        });
                    return false;
                }

                function addFAA(sender, aic, context) {
                    return doImport(sender, aic, context, "FAA", "AddAirport");
                }

                function addIATA(sender, aic, context) {
                    return doImport(sender, aic, context, "IATA", "AddAirport");
                }
                function addICAO(sender, aic, context) {
                    return doImport(sender, aic, context, "ICAO", "AddAirport");
                }

                function useFAALoc(sender, aic, context) {
                    return doImport(sender, aic, context, "FAA", "FixLocation");
                }
                function useFAAType(sender, aic, context) {
                    return doImport(sender, aic, context, "FAA", "FixType");
                }
                function useFAAData(sender, aic, context) {
                    return doImport(sender, aic, context, "FAA", "Overwrite");
                }

                function useICAOLoc(sender, aic, context) {
                    return doImport(sender, aic, context, "ICAO", "FixLocation");
                }

                function useICAOType(sender, aic, context) {
                    return doImport(sender, aic, context, "ICAO", "FixType");
                }

                function useICAOData(sender, aic, context) {
                    return doImport(sender, aic, context, "ICAO", "Overwrite");
                }

                function useIATALoc(sender, aic, context) {
                    return doImport(sender, aic, context, "IATA", "FixLocation");
                }

                function useIATAType(sender, aic, context) {
                    return doImport(sender, aic, context, "IATA", "FixType");
                }

                function useIATAData(sender, aic, context) {
                    return doImport(sender, aic, context, "IATA", "Overwrite");
                }
            </script>
            <asp:GridView ID="gvImportResults" runat="server" AutoGenerateColumns="false" OnRowDataBound="gvImportResults_RowDataBound">
                <Columns>
                    <asp:TemplateField HeaderText="To Import">
                        <ItemTemplate>
                            <asp:Literal ID="litRowContext" runat="server" />
                            <asp:Label ID="lblProposed" runat="server" Text=""></asp:Label>
                            <asp:PlaceHolder ID="plcAirportProposed" runat="server"></asp:PlaceHolder>
                            <div>
                                <asp:Button ID="btnAddFAA" runat="server" Text="Add FAA" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "addFAA") %>' />
                                <asp:Button ID="btnAddIATA" runat="server" Text="Add IATA" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "addIATA") %>' />
                                <asp:Button ID="btnAddICAO" runat="server" Text="Add ICAO" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "addICAO") %>' />
                            </div>
                            <asp:CheckBox ID="ckUseMap" Text="Use map location" runat="server" />
                        </ItemTemplate>
                        <ItemStyle Height="90px" />
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="FAA">
                        <ItemTemplate>
                            <asp:PlaceHolder ID="plcFAAMatch" runat="server"></asp:PlaceHolder>
                            <div>
                                <asp:Button ID="btnFixLocationFAA" runat="server" Text="Update location" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useFAALoc") %>' />
                                <asp:Button ID="btnFixTypeFAA" runat="server" Text="Update Type" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useFAAType") %>' />
                                <asp:Button ID="btnOverwriteFAA" runat="server" Text="Overwrite" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useFAAData") %>' />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="ICAO">
                        <ItemTemplate>
                            <asp:PlaceHolder ID="plcICAOMatch" runat="server"></asp:PlaceHolder>
                            <div>
                                <asp:Button ID="btnFixLocationICAO" runat="server" Text="Update location" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useICAOLoc") %>' />
                                <asp:Button ID="btnFixTypeICAO" runat="server" Text="Update Type" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useICAOType") %>' />
                                <asp:Button ID="btnOverwriteICAO" runat="server" Text="Overwrite" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useICAOData") %>' />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="IATA">
                        <ItemTemplate>
                            <asp:PlaceHolder ID="plcIATAMatch" runat="server"></asp:PlaceHolder>
                            <div>
                                <asp:Button ID="btnFixLocationIATA" runat="server" Text="Update location" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useIATALoc") %>' />
                                <asp:Button ID="btnFixTypeIATA" runat="server" Text="Update Type" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useIATAType") %>' />
                                <asp:Button ID="btnOverwriteIATA" runat="server" Text="Overwrite" OnClientClick='<%# AjaxCallForIndex(Container.DataItemIndex, "useIATAData") %>' />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <p>No candidates yet.</p>
                </EmptyDataTemplate>
            </asp:GridView>
        </asp:Panel>
        <script>
            function deleteDupeUserAirport(user, codeDelete, codeMap, type, sender) {
                var params = new Object();
                params.idDelete = codeDelete;
                params.idMap = codeMap;
                params.szUser = user;
                params.szType = type;
                var d = JSON.stringify(params);
                $.ajax(
                    {
                        url: '<% =ResolveUrl("~/Admin/AdminAirportGeocoder.aspx/DeleteDupeUserAirport") %>',
                        type: "POST", data: d, dataType: "json", contentType: "application/json",
                        error: function (xhr, status, error) {
                            window.alert(xhr.responseJSON.Message);
                        },
                        complete: function (response) { },
                        success: function (response) { sender.parentElement.parentElement.style.display = 'none'; }
                    });
            }
            function setPreferred(code, type, sender) {
                var params = new Object();
                params.szCode = code;
                params.szType = type;
                params.fPreferred = sender.checked;
                var d = JSON.stringify(params);
                $.ajax(
                    {
                        url: '<% =ResolveUrl("~/Admin/AdminAirportGeocoder.aspx/SetPreferred") %>',
                        type: "POST", data: d, dataType: "json", contentType: "application/json",
                        error: function (xhr, status, error) {
                            window.alert(xhr.responseJSON.Message);
                        },
                        complete: function (response) { },
                        success: function (response) { sender.checked = params.fPreferred; }
                    });
            }
            function makeNative(code, type, sender) {
                var params = new Object();
                params.szCode = code;
                params.szType = type;
                var d = JSON.stringify(params);
                $.ajax(
                    {
                        url: '<% =ResolveUrl("~/Admin/AdminAirportGeocoder.aspx/MakeNative") %>',
                        type: "POST", data: d, dataType: "json", contentType: "application/json",
                        error: function (xhr, status, error) {
                            window.alert(xhr.responseJSON.Message);
                        },
                        complete: function (response) { },
                        success: function (response) { sender.disabled = true; }
                    });
            }
            function mergeWith(codeTarget, typeTarget, codeSource, sender) {
                var params = new Object();
                params.szCodeTarget = codeTarget;
                params.szTypeTarget = typeTarget;
                params.szCodeSource = codeSource;
                var d = JSON.stringify(params);
                $.ajax(
                    {
                        url: '<% =ResolveUrl("~/Admin/AdminAirportGeocoder.aspx/MergeWith") %>',
                        type: "POST", data: d, dataType: "json", contentType: "application/json",
                        error: function (xhr, status, error) {
                            window.alert(xhr.responseJSON.Message);
                        },
                        complete: function (response) { },
                        success: function (response) { sender.disabled = true; }
                    });
            }
        </script>
        <asp:UpdateProgress ID="UpdateProgress2" runat="server" AssociatedUpdatePanelID="updpDupes">
            <ProgressTemplate>
                <asp:Image ID="imgUserDupes" ImageUrl="~/images/ajax-loader.gif" runat="server" />
            </ProgressTemplate>
        </asp:UpdateProgress>
        <asp:UpdatePanel ID="updpDupes" runat="server">
            <ContentTemplate>
            <p><asp:Label ID="lblAdminReviewDupeAirports" runat="server" Text="Review likely duplicate airports"></asp:Label> 
                <asp:Button ID="btnRefreshDupes" runat="server" Text="Refresh (slow)" OnClick="btnRefreshDupes_Click" />
                Limit to dupes of: <asp:TextBox ID="txtDupeSeed" runat="server" />
                <asp:HiddenField ID="hdnSeedLat" runat="server" />
                <asp:HiddenField ID="hdnSeedLon" runat="server" />
            </p>
            <asp:Panel ID="pnlDupeAirports" runat="server" ScrollBars="Auto" Height="400px" Width="100%" Visible="false"> 
                <asp:GridView ID="gvDupes" runat="server" AutoGenerateColumns="false">
                    <Columns>
                        <asp:TemplateField HeaderText="Airport 1">
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDeleteDupe1" ImageUrl="~/images/x.gif" CausesValidation="False" Visible="false" 
                                    AlternateText="Delete this airport" ToolTip="Delete this airport"
                                    OnClientClick='<%# DeleteDupeScript((string)Eval("user1"), (string)Eval("id1"), (string)Eval("id2"), (string)Eval("type1")) %>'
                                    runat="server" />
                                <asp:HyperLink ID="lnkID1" runat="server" Font-Bold="true" Text='<%# Eval("id1") %>' NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:clickAndZoom(new google.maps.LatLng({0}, {1}));", Eval("lat1"), Eval("lon1")) %>'></asp:HyperLink>
                                <asp:Label ID="lblType1" runat="server" Text='<%# String.Format(System.Globalization.CultureInfo.CurrentCulture, "({0})", Eval("type1")) %>'></asp:Label>
                                <asp:Label ID="lblName1" runat="server" Text='<%# Eval("facname1") %>'></asp:Label>
                                <asp:Label ID="lblUser1" runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("user1")) %>' Text='<%# String.Format(System.Globalization.CultureInfo.CurrentCulture, "({0})", Eval("user1")) %>'></asp:Label>
                                <br />&nbsp;&nbsp;&nbsp;
                                <asp:Checkbox ID="lblPreferred1" runat="server" Checked='<%# ((int) Eval("pref1")) != 0 %>' Text="Preferred" onclick='<%# SetPreferredScript((string) Eval("id1"), (string) Eval("type1")) %>'></asp:Checkbox>
                                <asp:Button ID="btnMerge1" runat="server" Text='<%# String.Format(System.Globalization.CultureInfo.CurrentCulture, "Merge from {0}", Eval("id2")) %>' OnClientClick='<%# MergeWithScript((string)Eval("id1"), (string)Eval("type1"), (string) Eval("id2")) %>' />
                                <asp:Button ID="btnMakeNative" runat="server" Text="Make Native" OnClientClick='<%# MakeNativeScript((string)Eval("id1"), (string)Eval("type1")) %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Airport 2">
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDeleteDupe2" ImageUrl="~/images/x.gif" CausesValidation="False" Visible="false"
                                    AlternateText="Delete this airport" ToolTip="Delete this airport"
                                    OnClientClick='<%# DeleteDupeScript((string)Eval("user2"), (string)Eval("id2"), (string)Eval("id1"), (string)Eval("type2")) %>'
                                    runat="server" />
                                <asp:HyperLink ID="lnkID2" runat="server" Font-Bold="true" Text='<%# Eval("id2") %>' NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:clickAndZoom(new google.maps.LatLng({0}, {1}));", Eval("lat2"), Eval("lon2")) %>'></asp:HyperLink>
                                <asp:Label ID="lblType2" runat="server" Text='<%# String.Format(System.Globalization.CultureInfo.CurrentCulture, "({0})", Eval("type2")) %>'></asp:Label>
                                <asp:Label ID="lblName2" runat="server" Text='<%# Eval("facname2") %>'></asp:Label>
                                <asp:Label ID="lblUser2" runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("user2")) %>' Text='<%# String.Format(System.Globalization.CultureInfo.CurrentCulture, "({0})", Eval("user2")) %>'></asp:Label>
                                <br />&nbsp;&nbsp;&nbsp;
                                <asp:Checkbox ID="lblPreferred2" runat="server" Checked='<%# ((int) Eval("pref2")) != 0 %>' Text="Preferred" onclick='<%# SetPreferredScript((string) Eval("id2"), (string) Eval("type2")) %>'></asp:Checkbox>
                                <asp:Button ID="btnMerge2" runat="server" Text='<%# String.Format(System.Globalization.CultureInfo.CurrentCulture, "Merge from {0}", Eval("id1")) %>' OnClientClick='<%# MergeWithScript((string)Eval("id2"), (string)Eval("type2"), (string) Eval("id1")) %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <p>(No potential dupes)</p>
                    </EmptyDataTemplate>
                </asp:GridView>
                <asp:SqlDataSource ID="sqlDSUserDupes" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                    OnSelecting="sqlDSUserDupes_Selecting"
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT 
        ap1.sourceusername AS 'user1', ap1.AirportID AS 'id1', ap1.facilityname AS 'facname1', ap1.latitude AS 'lat1', ap1.longitude AS 'lon1', ap1.Preferred AS 'pref1', ap1.Type AS type1, ap2.sourceusername AS 'user2', ap2.AirportID AS 'id2', ap2.facilityname AS 'facname2', ap2.latitude AS 'lat2', ap2.longitude AS 'lon2', ap2.Preferred AS 'pref2', ap2.Type as type2
    FROM
        airports ap1
            INNER JOIN
        airports ap2 ON ap1.AirportID &lt;&gt; ap2.airportid AND ap1.type = ap2.type
            AND Abs(ap1.latitude - ap2.latitude) &lt; 0.01 AND abs(ap1.longitude - ap2.longitude) &lt; 0.01
    WHERE
        ap1.sourceusername &lt;&gt; '' AND ap1.type IN ('A', 'H', 'S') AND ap2.type IN ('A', 'H', 'S')
    ORDER BY
        ap1.type ASC, ap1.latitude ASC, ap1.AirportID ASC;">
                </asp:SqlDataSource>
                <asp:SqlDataSource ID="sqlDSSingleDupe" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT 
        ap1.sourceusername AS 'user1', ap1.AirportID AS 'id1', ap1.facilityname AS 'facname1', ap1.latitude AS 'lat1', ap1.longitude AS 'lon1', ap1.Preferred AS 'pref1', ap1.Type AS type1, ap2.sourceusername AS 'user2', ap2.AirportID AS 'id2', ap2.facilityname AS 'facname2', ap2.latitude AS 'lat2', ap2.longitude AS 'lon2', ap2.Preferred AS 'pref2', ap2.Type as type2
    FROM
        airports ap1
            INNER JOIN
        airports ap2 ON ap1.AirportID &lt;&gt; ap2.airportid AND ap1.type = ap2.type
    WHERE
        ap1.sourceusername &lt;&gt; '' AND ap1.type IN ('A', 'H', 'S') AND ap2.type IN ('A', 'H', 'S')
        AND Abs(ap1.latitude - ?lat) &lt; 0.01 AND abs(ap1.longitude - ?lon) &lt; 0.01 AND Abs(ap2.latitude - ?lat) &lt; 0.01 AND abs(ap2.longitude - ?lon) &lt; 0.01 
    ORDER BY
        ap1.type ASC, ap1.latitude ASC, ap1.AirportID ASC;">
                    <SelectParameters>
                        <asp:ControlParameter ControlID="hdnSeedLat" DbType="Double" Name="lat" PropertyName="Value" />
                        <asp:ControlParameter ControlID="hdnSeedLon" DbType="Double" Name="lon" PropertyName="Value" />
                     </SelectParameters>
                </asp:SqlDataSource>
            </asp:Panel>
            </ContentTemplate>
        </asp:UpdatePanel>
        <p><asp:HyperLink ID="lnkManageGeo" runat="server" Text="Manage Georeferences" NavigateUrl="~/Admin/AdminAirportGeocoder.aspx" /></p>
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <asp:Panel ID="pnlMyAirports" runat="server" Width="100%">
        <p><asp:Localize ID="locYourAirportsHeader" runat="server" Text="<%$ Resources:Airports, EditAirportsMyAirportsHeader %>" /></p>
        <asp:GridView ID="gvMyAirports" EnableViewState="False" CellSpacing="5" CellPadding="5"
            runat="server" AutoGenerateColumns="False" GridLines="None"
            OnRowCommand="gvMyAirports_RowCommand" 
            OnRowDataBound="gvMyAirports_RowDataBound">
            <Columns>
                <asp:TemplateField HeaderText="<%$ Resources:Airports, EditAirportFacilityType %>">
                    <ItemTemplate>
                        <asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" CausesValidation="False"
                            AlternateText="<%$ Resources:Airports, EditAirportsDeleteAirport %>" ToolTip="<%$ Resources:Airports, EditAirportsDeleteAirport %>" CommandName="_Delete"
                            CommandArgument='<%#: Bind("Code") %>' runat="server"  />
                        <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="imgDelete"
                            ConfirmOnFormSubmit="True" 
                            ConfirmText="Are you sure you want to delete this airport/navaid?  This action cannot be undone!" 
                            Enabled="True">
                        </cc1:ConfirmButtonExtender>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="<%$ Resources:Airports, EditAirportsCodePrompt %>" SortExpression="AirportID">
                    <ItemTemplate>
                        <asp:HyperLink ID="lnkZoomCode" runat="server" Text='<%#: Bind("Code") %>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="<%$ Resources:Airports, airportName %>" SortExpression="FacilityName">
                    <ItemTemplate>
                        <asp:Label ID="lblFacilityName" runat="server" Text='<%#: Bind("Name") %>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="<%$ Resources:Airports, EditAirportFacilityType %>" SortExpression="FriendlyName">
                    <ItemTemplate>
                        <asp:Label ID="lblFacilityType" runat="server" Text='<%#: Bind("FacilityType") %>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="UserName" SortExpression="UserName" Visible="False">
                    <ItemTemplate>
                        <asp:Label ID="lblUsername" runat="server" Text='<%# Bind("Username") %>' />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <HeaderStyle CssClass="gvhLeft" />
        </asp:GridView>
    </asp:Panel>

    <script> 
//<![CDATA[
        function centerToText ()
        {
            if (elLat.value && elLon.value)
            {
                var latlngCenter = new google.maps.LatLng(elLat.value, elLon.value);
                clickForAirport(latlngCenter);
                zoomForAirport();
             }
        }
                
        function clickAndZoom(point) 
        {
            clickForAirport(point);
            getGMap().setCenter(point);
            getGMap().setZoom(14);
        }
        
        function zoomForAirport()
        {
            var mfbMap = getMfbMap();
            if (mfbMap.clickPositionMarker)
            {
                mfbMap.gmap.setCenter(mfbMap.clickPositionMarker.getPosition());
                mfbMap.gmap.setZoom(14);
            }
        }
        
        function updateForAirport(o)
        {
            elLat.value = o.LatLong.Latitude;
            elLon.value = o.LatLong.Longitude;
            elCode.value = o.Code;
            elName.value = o.Name;
            elType.value = o.FacilityTypeCode;
            clickAndZoom(new google.maps.LatLng(o.LatLong.Latitude, o.LatLong.Longitude));
        }
        
        function clickForAirport(point)
        {
            if (point != null)
            {
                elLat.value = point.lat();
                elLon.value = point.lng();
                getMfbMap().clickMarker(point, elName.value, elType.value, "<a href=\"javascript:zoomForAirport();\">Zoom in</a>");
            }
        }
//]]>
        /* Handle escape to dismiss */
        function pageLoad(sender, args) {
            if (!args.get_isPartialLoad()) {
                $addHandler(document, "keydown", onKeyDown);
            }
        }

        function onKeyDown(e) {
            if (e && e.keyCode == Sys.UI.Key.esc)
                $find("mpeDupeAirport").hide();
        }

        function getFlickerSolved() {
            document.getElementById('<%=pnlDupeAirport.ClientID%>').style.display = 'none';
        }

        var elCode = document.getElementById('<% =txtCode.ClientID %>');
        var elName = document.getElementById('<% =txtName.ClientID %>');
        var elType = document.getElementById('<% = cmbType.ClientID %>');
        var elLat = document.getElementById('<% = txtLat.ClientID %>');
        var elLon = document.getElementById('<% = txtLong.ClientID %>');
        $(document).ready(function () {centerToText(); });
    </script>

<asp:Panel ID="pnlDupeAirport" runat="server" CssClass="modalpopup" DefaultButton="btnAddAnyway">
    <div style="text-align:center; max-width: 450px">
        <div><asp:Label ID="lblDupe" runat="server" Text="<%$ Resources:Airports, errDupeAirport %>" /></div>
        <div style="margin-left:auto; margin-right: auto;">
            <asp:GridView ID="gvUserDupes" runat="server" CellPadding="3" AutoGenerateColumns="false" GridLines="None">
                <Columns>
                    <asp:BoundField ItemStyle-Font-Bold="true" DataField="Code" ItemStyle-HorizontalAlign="Left" ItemStyle-VerticalAlign="Top" />
                    <asp:BoundField DataField="Name" ItemStyle-HorizontalAlign="Left" ItemStyle-VerticalAlign="Top" />
                    <asp:BoundField DataField="FacilityType" DataFormatString="({0})" ItemStyle-HorizontalAlign="Left" ItemStyle-VerticalAlign="Top" />
                </Columns>
            </asp:GridView>
        </div>
        <div style="margin-top: 5px;">
            <asp:Button ID="btnAddAnyway" runat="server" Text="<%$ Resources:Airports, errDupeAirportCreateAnyway %>" OnClick="btnAddAnyway_Click" />&nbsp;&nbsp;
            <asp:Button ID="btnAirportCancel" runat="server" Text="<%$ Resources:LocalizedText, Cancel %>"  />
        </div>
    </div>
</asp:Panel>
<asp:Label ID="lblUnused" runat="server" style="display:none;" />
<cc1:ModalPopupExtender ID="mpeDupeAirport" runat="server" TargetControlID="lblUnused"
    PopupControlID="pnlDupeAirport" BackgroundCssClass="modalBackground" 
    CancelControlID="btnAirportCancel" OnCancelScript="getFlickerSolved();" />

</asp:Content>
