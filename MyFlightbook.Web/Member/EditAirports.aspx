<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_EditAirports" Title="Edit Airports and Navigation Aids" culture="auto" meta:resourcekey="PageResource2" Codebehind="EditAirports.aspx.cs" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMapManager"
    TagPrefix="uc1" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc2" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Literal ID="locEditAirportsHeader" runat="server" 
            Text="Edit Airports and NavAids" 
            meta:resourcekey="locEditAirportsHeaderResource2"></asp:Literal>
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <p><asp:Localize ID="locEditAirportsDesc" runat="server" Text="Missing a local airport that you love? That's probably because we compile
        our data from a variety of public (and free!) sources, but these are not exhaustive.
        You can help keep the dataset complete by adding your own below." meta:resourcekey="locEditAirportsDescResource2"></asp:Localize>
    </p>
        <table style="width: 100%">
            <tr>
                <td style="vertical-align: top">
                    <asp:Panel ID="pnlEdit" runat="server" DefaultButton="btnAdd" 
                        meta:resourcekey="pnlEditResource2">
                        <table style="width:200px">
                            <tr style="vertical-align: top">
                                <td>
                                    <asp:Label ID="lblCodePrompt" runat="server" Text="Code:" 
                                        meta:resourceKey="lblCodePromptResource2"></asp:Label>
                                    <br />
                                    <asp:Label ID="lblcodePromptExample" CssClass="fineprint" runat="server" 
                                        Text="e.g., &quot;KABC&quot;" meta:resourceKey="lblcodePromptExampleResource2"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtCode" runat="server" meta:resourceKey="txtCodeResource2"></asp:TextBox>
                                    <cc1:FilteredTextBoxExtender ID="txtCode_FilteredTextBoxExtender" FilterType="Numbers, UppercaseLetters, LowercaseLetters"
                                        runat="server" TargetControlID="txtCode" Enabled="True">
                                    </cc1:FilteredTextBoxExtender>
                                    <cc1:TextBoxWatermarkExtender ID="wmeCode" WatermarkText="<%$ Resources:Airports, wmAirportCode %>" 
                                        runat="server" Enabled="True" TargetControlID="txtCode" WatermarkCssClass="watermark">
                                    </cc1:TextBoxWatermarkExtender>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="You must supply a valid code between 2-6 characters long"
                                        ControlToValidate="txtCode" Display="Dynamic" 
                                        meta:resourceKey="RequiredFieldValidator1Resource2"></asp:RequiredFieldValidator>
                                    <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ErrorMessage="RegularExpressionValidator"
                                        ControlToValidate="txtCode" Display="Dynamic" Text="Code must be 2-5 characters in length (3-6 for airports)"
                                        ValidationExpression="[a-zA-Z0-9]{2,6}" 
                                        meta:resourceKey="RegularExpressionValidator1Resource2"></asp:RegularExpressionValidator>
                                </td>
                            </tr>
                            <tr style="vertical-align: top">
                                <td>
                                    <asp:Localize ID="locNamePrompt" runat="server" Text="Name:" 
                                        meta:resourceKey="locNamePromptResource2"></asp:Localize>
                                    <br />
                                    <asp:Label ID="lblNameExample" CssClass="fineprint" runat="server" 
                                        Text="e.g., &quot;My Airport&quot;" meta:resourceKey="lblNameExampleResource2"></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtName" runat="server" meta:resourceKey="txtNameResource2"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ControlToValidate="txtName"
                                        ErrorMessage="Please provide a name for the facility" Display="Dynamic" 
                                        meta:resourceKey="RequiredFieldValidator2Resource2"></asp:RequiredFieldValidator>
                                    <cc1:TextBoxWatermarkExtender ID="wmeName" WatermarkText="<%$ Resources:Airports, wmFriendlyname %>" 
                                        runat="server" Enabled="True" TargetControlID="txtName" WatermarkCssClass="watermark">
                                    </cc1:TextBoxWatermarkExtender>
                                </td>
                            </tr>
                            <tr style="vertical-align: top">
                                <td>
                                    <asp:Localize ID="locType" runat="server" Text="Type:" 
                                        meta:resourceKey="locTypeResource2"></asp:Localize>
                                </td>
                                <td>
                                    <asp:DropDownList ID="cmbType" runat="server" DataSourceID="SqlDataSourceNavaidTypes"
                                        DataTextField="FriendlyName" DataValueField="Code" 
                                        meta:resourceKey="cmbTypeResource2">
                                    </asp:DropDownList>
                                    <asp:SqlDataSource ID="SqlDataSourceNavaidtypes" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                                        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT * FROM NavAidTypes">
                                    </asp:SqlDataSource>
                                </td>
                            </tr>
                            <tr style="vertical-align: top">
                                <td>
                                    <asp:Localize ID="locLatitude" runat="server" Text="Latitude:" 
                                        meta:resourceKey="locLatitudeResource2"></asp:Localize>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtLat" runat="server" meta:resourceKey="txtLatResource2"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server" ControlToValidate="txtLat"
                                        Display="Dynamic" 
                                        ErrorMessage="<br />Type in a latitude or click on the map to set" 
                                        meta:resourceKey="RequiredFieldValidator3Resource2"></asp:RequiredFieldValidator>
                                    <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender2" ValidChars="-.0123456789"
                                        runat="server" TargetControlID="txtLat" Enabled="True">
                                    </cc1:FilteredTextBoxExtender>
                                    <asp:RegularExpressionValidator ID="RegularExpressionValidatorLat" runat="server"
                                        Display="Dynamic" ControlToValidate="txtLat" ErrorMessage="<br />Please type a valid latitude or click on the map"
                                        ValidationExpression="-?\d{0,2}(.\d*)?" 
                                        meta:resourceKey="RegularExpressionValidatorLatResource2"></asp:RegularExpressionValidator>
                                </td>
                            </tr>
                            <tr style="vertical-align: top">
                                <td>
                                    <asp:Localize ID="locLongitude" runat="server" Text="Longitude:" 
                                        meta:resourceKey="locLongitudeResource2"></asp:Localize>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtLong" runat="server" meta:resourceKey="txtLongResource2"></asp:TextBox>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator4" runat="server" ControlToValidate="txtLong"
                                        Display="Dynamic" 
                                        ErrorMessage="<br />Type in a longitude or click on the map to set" 
                                        meta:resourceKey="RequiredFieldValidator4Resource2"></asp:RequiredFieldValidator>
                                    <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" ValidChars="-.0123456789"
                                        runat="server" TargetControlID="txtLong" Enabled="True">
                                    </cc1:FilteredTextBoxExtender>
                                    <asp:RegularExpressionValidator ID="RegularExpressionValidatorLong" runat="server"
                                        Display="Dynamic" ControlToValidate="txtLong" ErrorMessage="<br />Please type a valid latitude or click on the map"
                                        ValidationExpression="-?\d{0,3}(.\d*)?" 
                                        meta:resourceKey="RegularExpressionValidatorLongResource2"></asp:RegularExpressionValidator>
                                </td>
                            </tr>
                            <tr runat="server" id="rowAdmin">
                                <td>&nbsp;</td>
                                <td><asp:CheckBox ID="ckAsAdmin" runat="server" Text="Add as admin" /></td>
                            </tr>
                            <tr>
                                <td>
                                    &nbsp;
                                </td>
                                <td>
                                    <asp:Button ID="btnAdd" runat="server" Text="Create" OnClick="btnAdd_Click" 
                                        meta:resourceKey="btnAddResource2" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    &nbsp;
                                </td>
                                <td>
                                    <asp:Label ID="lblErr" runat="server" ForeColor="Red" 
                                        meta:resourceKey="lblErrResource2"></asp:Label>
                                </td>
                            </tr>
                        </table>
                        <p>
                            <asp:Label ID="lblTipHeader" runat="server" Text="Tip" Font-Bold="True" 
                                meta:resourceKey="lblTipHeaderResource2"></asp:Label>&nbsp;
                            <asp:Label ID="lblClickToSetLatLon" runat="server" 
                                Text="Click on the map to fill in the latitude/longitude" 
                                meta:resourceKey="lblClickToSetLatLonResource2"></asp:Label>
                        </p>
                    </asp:Panel>
                </td>
                <td  style="vertical-align: top; width: 90%">
                    <div style="margin-left: 20px; margin-right:10px;">
                        <uc1:mfbGoogleMapManager ID="MfbGoogleMapManager1" runat="server" AllowResize="false" Height="400px"
                            Width="100%" />
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
        <table>
            <tr>
                <td>
                    <asp:FileUpload ID="fileUploadAirportList" runat="server" />
                    <asp:Button ValidationGroup="importAirports" ID="btnImport" runat="server" Text="Import" OnClick="btnImport_Click" />
                </td>
                <td>
                    <asp:UpdateProgress ID="UpdateProgress1" AssociatedUpdatePanelID="updAddAirports" runat="server">            
                        <ProgressTemplate>
                            <asp:Image ID="imgProgress" ImageUrl="~/images/ajax-loader.gif" runat="server" />
                        </ProgressTemplate>
                    </asp:UpdateProgress>
                </td>
            </tr>
        </table>
        <p><asp:Label ID="lblUploadErr" runat="server" Text="" EnableViewState="false" CssClass="error"></asp:Label></p>
        <asp:Panel ID="pnlImportResults" runat="server" ScrollBars="Auto" Height="400px" Width="100%" Visible="false">
            <asp:UpdatePanel runat="server" ID="updAddAirports">
                <ContentTemplate>
                    <asp:GridView ID="gvImportResults" runat="server" AutoGenerateColumns="false" OnRowDataBound="gvImportResults_RowDataBound" OnRowCommand="gvImportResults_RowCommand">
                        <Columns>
                            <asp:TemplateField HeaderText="To Import">
                                <ItemTemplate>
                                    <asp:Label ID="lblProposed" runat="server" Text=""></asp:Label>
                                    <asp:PlaceHolder ID="plcAirportProposed" runat="server"></asp:PlaceHolder>
                                    <div>
                                        <asp:Button ID="btnAddFAA" runat="server" Text="Add FAA" CommandArgument="FAA" CommandName="AddAirport" />
                                        <asp:Button ID="btnAddIATA" runat="server" Text="Add IATA" CommandArgument="IATA" CommandName="AddAirport"  />
                                        <asp:Button ID="btnAddICAO" runat="server" Text="Add ICAO" CommandArgument="ICAO" CommandName="AddAirport" />
                                    </div>
                                    <asp:CheckBox ID="ckUseMap" Text="Use map location" runat="server" />
                                </ItemTemplate>
                                <ItemStyle Height="90px" />
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="FAA">
                                <ItemTemplate>
                                    <asp:PlaceHolder ID="plcFAAMatch" runat="server"></asp:PlaceHolder>
                                    <asp:Button ID="btnFixLocationFAA" runat="server" Text="Update location" CommandArgument="FAA" CommandName="FixLocation" />
                                    <asp:Button ID="btnFixTypeFAA" runat="server" Text="Update Type" CommandArgument="FAA" CommandName="FixType" />
                                    <asp:Button ID="btnOverwriteFAA" runat="server" Text="Overwrite" CommandArgument="FAA" CommandName="Overwrite" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="ICAO">
                                <ItemTemplate>
                                    <asp:PlaceHolder ID="plcICAOMatch" runat="server"></asp:PlaceHolder>
                                    <asp:Button ID="btnFixLocationICAO" runat="server" Text="Update location" CommandArgument="ICAO" CommandName="FixLocation" />
                                    <asp:Button ID="btnFixTypeICAO" runat="server" Text="Update Type" CommandArgument="ICAO" CommandName="FixType" />
                                    <asp:Button ID="btnOverwriteICAO" runat="server" Text="Overwrite" CommandArgument="ICAO" CommandName="Overwrite" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="IATA">
                                <ItemTemplate>
                                    <asp:PlaceHolder ID="plcIATAMatch" runat="server"></asp:PlaceHolder>
                                    <asp:Button ID="btnFixLocationIATA" runat="server" Text="Update location" CommandArgument="IATA" CommandName="FixLocation" />
                                    <asp:Button ID="btnFixTypeIATA" runat="server" Text="Update Type" CommandArgument="IATA" CommandName="FixType" />
                                    <asp:Button ID="btnOverwriteIATA" runat="server" Text="Overwrite" CommandArgument="IATA" CommandName="Overwrite" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <p>No candidates yet.</p>
                        </EmptyDataTemplate>
                    </asp:GridView>
                </ContentTemplate>
            </asp:UpdatePanel>
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
                        url: '<% =ResolveUrl("~/Member/EditAirports.aspx/DeleteDupeUserAirport") %>',
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
                        url: '<% =ResolveUrl("~/Member/EditAirports.aspx/SetPreferred") %>',
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
                        url: '<% =ResolveUrl("~/Member/EditAirports.aspx/MakeNative") %>',
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
                        url: '<% =ResolveUrl("~/Member/EditAirports.aspx/MergeWith") %>',
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
            <p><asp:Label ID="lblAdminReviewDupeAirports" runat="server" Text="Review likely duplicate airports"></asp:Label> <asp:Button ID="btnRefreshDupes" runat="server" Text="Refresh (slow)" OnClick="btnRefreshDupes_Click" />
            </p>
            <asp:Panel ID="pnlDupeAirports" runat="server" ScrollBars="Auto" Height="400px" Width="100%" Visible="false"> 
                <asp:GridView ID="gvDupes" runat="server" AutoGenerateColumns="false">
                    <Columns>
                        <asp:TemplateField HeaderText="Airport 1">
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDeleteDupe1" ImageUrl="~/images/x.gif" CausesValidation="False" 
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
                                <asp:ImageButton ID="imgDeleteDupe2" ImageUrl="~/images/x.gif" CausesValidation="False"
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
            AND ROUND(ap1.latitude, 2) = ROUND(ap2.latitude, 2)
            AND ROUND(ap1.longitude, 2) = ROUND(ap2.longitude, 2)
    WHERE
        ap1.sourceusername &lt;&gt; '' AND ap1.type IN ('A', 'H', 'S')
    ORDER BY
        ap1.type ASC, ap1.AirportID ASC;">
                </asp:SqlDataSource>
            </asp:Panel>
            </ContentTemplate>
        </asp:UpdatePanel>
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <asp:Panel ID="pnlMyAirports" runat="server" Width="100%" 
        meta:resourcekey="pnlMyAirportsResource2">
        <p><asp:Localize ID="locYourAirportsHeader" runat="server" Text="Airports or navaids you have created are shown below. To edit an airport or navaid you have created, simply recreate it, re-using the facility code." meta:resourceKey="locYourAirportsHeaderResource2"></asp:Localize></p>
        <asp:GridView ID="gvMyAirports" EnableViewState="False" CellSpacing="5" CellPadding="5"
            runat="server" AutoGenerateColumns="False" GridLines="None"
            OnRowCommand="gvMyAirports_RowCommand" 
            OnRowDataBound="gvMyAirports_RowDataBound" 
            meta:resourceKey="gvMyAirportsResource2">
            <Columns>
                <asp:TemplateField meta:resourceKey="TemplateFieldResource6">
                    <ItemTemplate>
                        <asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" CausesValidation="False"
                            AlternateText="Delete this airport" ToolTip="Delete this airport" CommandName="_Delete"
                            CommandArgument='<%# Bind("Code") %>' runat="server" 
                            meta:resourceKey="imgDeleteResource2" />
                        <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="imgDelete"
                            ConfirmOnFormSubmit="True" 
                            ConfirmText="Are you sure you want to delete this airport/navaid?  This action cannot be undone!" 
                            Enabled="True">
                        </cc1:ConfirmButtonExtender>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Code" SortExpression="AirportID" 
                    meta:resourceKey="TemplateFieldResource7">
                    <ItemTemplate>
                        <asp:HyperLink ID="lnkZoomCode" runat="server" 
                            Text='<%# Bind("Code") %>' meta:resourcekey="lnkZoomCodeResource1" ></asp:HyperLink>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Name" SortExpression="FacilityName" 
                    meta:resourceKey="TemplateFieldResource8">
                    <ItemTemplate>
                        <asp:Label ID="lblFacilityName" runat="server" Text='<%# Bind("Name") %>' 
                            meta:resourceKey="lblFacilityNameResource2"></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Type of Facility" SortExpression="FriendlyName" 
                    meta:resourceKey="TemplateFieldResource9">
                    <ItemTemplate>
                        <asp:Label ID="lblFacilityType" runat="server" 
                            Text='<%# Bind("FacilityType") %>' meta:resourceKey="lblFacilityTypeResource2"></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="UserName" SortExpression="UserName"  
                    Visible="False" meta:resourceKey="TemplateFieldResource10">
                    <ItemTemplate>
                        <asp:Label ID="lblUsername" runat="server" Text='<%# Bind("Username") %>' 
                            meta:resourceKey="lblUsernameResource2"></asp:Label>
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
        
        function updateForAirport(code, name, type, lat, lon)
        {
            elLat.value = lat;
            elLon.value = lon;
            $find(elCodeWE).set_text(code);
            $find(elNameWE).set_text(name);
            elType.value = type;
            clickAndZoom(new google.maps.LatLng(lat, lon));
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
    </script>

</asp:Content>
