<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEditAircraft.ascx.cs"
    Inherits="Controls_mfbEditAircraft" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ Register Src="mfbMultiFileUpload.ascx" TagName="mfbMultiFileUpload" TagPrefix="uc3" %>
<%@ Register Src="mfbMaintainAircraft.ascx" TagName="mfbMaintainAircraft" TagPrefix="uc4" %>
<%@ Register Src="mfbEditMake.ascx" TagName="mfbEditMake" TagPrefix="uc5" %>
<%@ Register Src="Expando.ascx" TagName="Expando" TagPrefix="uc6" %>
<%@ Register Src="mfbEditAppt.ascx" TagName="mfbEditAppt" TagPrefix="uc7" %>
<%@ Register Src="mfbResourceSchedule.ascx" TagName="mfbResourceSchedule" TagPrefix="uc8" %>
<%@ Register Src="ClubControls/SchedSummary.ascx" TagName="SchedSummary" TagPrefix="uc9" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc10" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/mfbHoverImageList.ascx" TagPrefix="uc1" TagName="mfbHoverImageList" %>

<asp:Panel ID="pnlEditAircraft" runat="server" DefaultButton="btnAddAircraft">
    <asp:HiddenField ID="hdnAdminMode" runat="server" Value="false" />
    <asp:Panel runat="server" ID="pnlLockedExplanation" CssClass="callout" Visible="False">
        <p>
            <asp:Label ID="lblWhyNoEditQ" runat="server" Font-Bold="True"
                Text="<%$ Resources:LocalizedText, LabelWhyCantEditSims %>"></asp:Label>
        </p>
        <p>
            <asp:Label ID="lblWhyNoEditA" runat="server"
                Text="<%$ Resources:LocalizedText, AnswerWhyCantEditLockedAircraft %>"></asp:Label>
        </p>
    </asp:Panel>
    <asp:Panel runat="server" ID="pnlStats" CssClass="callout">
        <ul>
            <asp:Repeater ID="rptStats" runat="server">
                <ItemTemplate>
                    <li><%# Container.DataItem %></li>
                </ItemTemplate>
            </asp:Repeater>
        </ul>
        <div><asp:HyperLink ID="lnkViewTotals" runat="server" Text="<%$ Resources:Aircraft, ViewAircraftTotalsPrompt %>"></asp:HyperLink></div>
        <asp:Panel ID="pnlLocked" runat="server" Visible="false">
            <asp:CheckBox ID="ckLocked" runat="server" Text="<%$ Resources:Aircraft, editAircraftAdminLocked %>" />
        </asp:Panel>
    </asp:Panel>
    <table>
        <tr style="vertical-align: baseline">
            <td style="width: 120px;">
                <% = Resources.Aircraft.editAircraftInstanceTypePrompt %>
            </td>
            <td>
                <asp:MultiView ID="mvInstanceType" runat="server">
                    <asp:View ID="vwInstanceNew" runat="server">
                        <asp:DropDownList ID="cmbAircraftInstance" runat="server" AutoPostBack="True"
                            DataTextField="DisplayName"
                            DataValueField="InstanceTypeInt"
                            OnSelectedIndexChanged="cmbAircraftInstance_SelectedIndexChanged">
                        </asp:DropDownList>
                    </asp:View>
                    <asp:View ID="vwInstanceExisting" runat="server">
                        <asp:Label ID="lblInstanceType" Font-Bold="true" runat="server"></asp:Label>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr id="rowCountry" runat="server" style="vertical-align: baseline">
            <td runat="server">
                <asp:Localize ID="Localize1" Text="<%$ Resources:Aircraft, editAircraftCountryPrompt %>" runat="server"></asp:Localize>
            </td>
            <td runat="server">
                <asp:DropDownList ID="cmbCountryCode" runat="server" AutoPostBack="True" EnableViewState="False"
                    DataTextField="CountryName" DataValueField="Prefix"
                    OnSelectedIndexChanged="cmbCountryCode_SelectedIndexChanged">
                </asp:DropDownList>
                <asp:HiddenField ID="hdnSimCountry" runat="server" EnableViewState="false" />
                <asp:HiddenField ID="hdnLastCountry" runat="server" />
                <asp:HiddenField ID="hdnLastTail" runat="server" />
            </td>
        </tr>
        <tr style="vertical-align: baseline">
            <td><%= Resources.Aircraft.editAircraftTailNumberPrompt %>
            </td>
            <td>
                <asp:MultiView ID="mvTailnumber" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwRealAircraft" runat="server">
                        <asp:MultiView ID="mvRealAircraft" runat="server" ActiveViewIndex="0">
                            <asp:View ID="vwRegularTail" runat="server">
                                <script type="text/javascript">
                                    function AircraftSelected(source, eventArgs) {
                                        document.getElementById('<% = imgAutofillProgress.ClientID %>').style.display = 'inline-block';
                                        document.getElementById('<% = lnkPopulateAircraft.ClientID %>').click();
                                    }
                                </script>
                                <asp:TextBox ID="txtTail" runat="server" autocomplete="off"
                                    AutoCompleteType="Disabled"
                                    ValidationGroup="EditAircraft"></asp:TextBox>
                                <cc1:AutoCompleteExtender ID="txtTail_AutoCompleteExtender" runat="server"
                                    CompletionInterval="100" CompletionListCssClass="AutoExtender"
                                    CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                                    CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                                    OnClientItemSelected="AircraftSelected"
                                    Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestAircraft"
                                    ServicePath="~/Public/Webservice.asmx" TargetControlID="txtTail">
                                </cc1:AutoCompleteExtender>
                                <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server"
                                    Enabled="True" FilterType="Custom, Numbers, UppercaseLetters, LowercaseLetters"
                                    TargetControlID="txtTail" ValidChars="-"></cc1:FilteredTextBoxExtender>
                                <asp:RegularExpressionValidator ID="valTailNumber" runat="server"
                                    ControlToValidate="txtTail" CssClass="error" Display="Dynamic"
                                    ErrorMessage="Please enter a valid tail number (numbers and letters)"
                                    ValidationExpression="[a-zA-Z0-9]+-?[a-zA-Z0-9]+-?[a-zA-Z0-9]+"
                                    ValidationGroup="EditAircraft"></asp:RegularExpressionValidator>
                                <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                                    ControlToValidate="txtTail" CssClass="error" Display="Dynamic"
                                    ErrorMessage="Please enter a tail number"
                                    ValidationGroup="EditAircraft"></asp:RequiredFieldValidator>
                                <asp:CustomValidator ID="valTail" runat="server" ControlToValidate="txtTail"
                                    CssClass="error" Display="Dynamic"
                                    ErrorMessage="Please provide a complete tail number, including country prefix"
                                    OnServerValidate="ValidateTailNum"
                                    ValidationGroup="EditAircraft"></asp:CustomValidator>
                                <asp:CustomValidator ID="valPrefix" runat="server"
                                    ControlToValidate="txtTail" CssClass="error" Display="Dynamic"
                                    ErrorMessage="Please include a valid country prefix (or choose &quot;Other&quot; from the list of countries)."
                                    OnServerValidate="ValidateTailNumHasCountry" ValidationGroup="EditAircraft"></asp:CustomValidator>
                                <asp:CustomValidator ID="CustomValidator2" runat="server"
                                    ControlToValidate="txtTail" CssClass="error" Display="Dynamic"
                                    OnServerValidate="ValidateSim"
                                    ErrorMessage="Any aircraft that is not a real aircraft must have a tailnumber that begins with &quot;SIM&quot;.  Real aircraft must NOT begin with SIM."
                                    ValidationGroup="EditAircraft"></asp:CustomValidator>
                                <span style="display: none">
                                    <asp:LinkButton ID="lnkPopulateAircraft" runat="server" OnClick="lnkPopulateAircraft_Click"></asp:LinkButton></span>
                                <asp:HyperLink ID="lnkAdminFAALookup" runat="server" EnableViewState="false"
                                    Target="_blank"
                                    Text="<%$ Resources:Aircraft, editAircraftRegistrationPrompt %>" Visible="False"></asp:HyperLink>
                                <asp:Image ID="imgAutofillProgress" Style="display: none" runat="server" ImageUrl="~/images/ajax-loader-transparent-ball.gif" />
                            </asp:View>
                            <asp:View ID="vwAnonTail" runat="server">
                                <asp:Panel ID="pnlAnonTail" runat="server">
                                    <asp:Label ID="lblAnonTailDisplay" Font-Bold="True" runat="server"></asp:Label>
                                    <asp:Label ID="lblAnonymousTailNote" runat="server" CssClass="fineprint"
                                        Text="<%$ Resources:Aircraft, AnonymousTailNote %>"></asp:Label>
                                </asp:Panel>
                            </asp:View>
                        </asp:MultiView>
                        <div>
                            <asp:Panel ID="pnlAnonymous" runat="server">
                                <asp:CheckBox ID="ckAnonymous" runat="server"
                                    Text="<%$ Resources:Aircraft, editAircraftAnonymousCheck %>"
                                    AutoPostBack="True"
                                    OnCheckedChanged="ckAnonymous_CheckedChanged" />
                                <div class="fineprint"><% =Resources.Aircraft.editAircraftAnonymousNote %></div>
                            </asp:Panel>
                        </div>
                    </asp:View>
                    <asp:View ID="vwSimTail" runat="server">
                        <asp:Panel ID="pnlSimTail" runat="server">
                            <asp:Label ID="lblSimTail" Font-Bold="True" runat="server"></asp:Label>
                            <asp:Label ID="lblAutoSuggestTail" runat="server" CssClass="fineprint" Text="<%$ Resources:Aircraft, editAircraftAutoAssignedNote %>"></asp:Label>
                        </asp:Panel>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr style="vertical-align: baseline">
            <td>
                <%=Resources.Aircraft.editAircraftMakeModelPrompt %>
                <div>
                    <asp:HyperLink ID="lnkNewMake" Visible="false" runat="server" Text="<%$ Resources:LocalizedText, ClickToAddMakeModel %>" NavigateUrl="~/Member/EditMake.aspx"></asp:HyperLink>
                </div>
            </td>
            <td>
                <asp:MultiView ID="mvModel" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwReadOnlyModel" runat="server">
                        <asp:ImageButton ID="imgEditAircraftModel" ImageAlign="Top" ToolTip="<%$ Resources:Aircraft, editAircraftModelPrompt %>" ImageUrl="~/images/pencilsm.png" runat="server" />
                        <asp:Label ID="lblMakeModel" runat="server" Font-Bold="true"></asp:Label>
                    </asp:View>
                    <asp:View ID="vwEditableModel" runat="server">
                        <asp:DropDownList ID="cmbManufacturers" runat="server"
                            AppendDataBoundItems="True" AutoPostBack="True" EnableViewState="False"
                            DataTextField="ManufacturerName"
                            DataValueField="ManufacturerID"
                            OnSelectedIndexChanged="cmbManufacturers_SelectedIndexChanged">
                            <asp:ListItem Selected="True"
                                Text="<%$ Resources:Aircraft, editAircraftSelectManufacturer %>" Value="-1"></asp:ListItem>
                        </asp:DropDownList>
                        <asp:HiddenField ID="hdnLastMan" runat="server" />
                        <asp:DropDownList ID="cmbMakeModel" runat="server" AppendDataBoundItems="True"
                            AutoPostBack="True" DataTextField="ModelDisplayName"
                            DataValueField="MakeModelID"
                            OnSelectedIndexChanged="cmbMakeModel_SelectedIndexChanged">
                            <asp:ListItem Selected="True"
                                Text="<%$ Resources:Aircraft, editAircraftSelectModel %>" Value="-1"></asp:ListItem>
                        </asp:DropDownList>
                        <asp:RangeValidator ID="RangeValidator1" runat="server"
                            ControlToValidate="cmbMakeModel" CssClass="error" Display="Dynamic"
                            MaximumValue="1000000" ErrorMessage="&lt;br /&gt;Please select a make/model from the list.  You can add one if needed."
                            MinimumValue="0" Type="Integer" ValidationGroup="EditAircraft"></asp:RangeValidator>
                    </asp:View>
                </asp:MultiView>
                <asp:Panel ID="pnlGlassCockpit" runat="server">
                    <asp:CheckBox ID="ckIsGlass" runat="server" AutoPostBack="true" OnCheckedChanged="ckIsGlass_CheckedChanged" Text="<%$ Resources:Aircraft, editAircraftHasGlass %>" />
                    <asp:Panel ID="pnlGlassUpgradeDate" runat="server" Style="margin: 3px;">
                        <asp:Label ID="lblDateOfGlassUpgrade" runat="server" Text="<%$ Resources:Aircraft, editAircraftGlassUpgradeDate %>"></asp:Label>
                        <uc10:mfbTypeInDate runat="server" ID="mfbDateOfGlassUpgrade" />
                    </asp:Panel>
                </asp:Panel>
            </td>
        </tr>
        <tr style="vertical-align: baseline">
            <td style="vertical-align: top">
                <asp:Localize ID="locImagesPrompt" runat="server" Text="<%$ Resources:Aircraft, editAircraftImagesPrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:Panel ID="pnlImageNote" runat="server">
                    <asp:Label ID="locImageNote" runat="server" Text="<%$ Resources:LocalizedText, Note %>" Font-Bold="True"></asp:Label>
                    <% =Resources.Aircraft.editAircraftSharedImagesNote %>
                </asp:Panel>
                <uc3:mfbMultiFileUpload ID="mfbMFUAircraftImages" Mode="Ajax" IncludeDocs="false" Class="Aircraft" RefreshOnUpload="true" runat="server" />
            </td>
        </tr>
        <tr>
            <td></td>
            <td>
                <uc1:mfbImageList ID="mfbIl" runat="server" CanEdit="true" Columns="4" ImageClass="Aircraft" OnMakeDefault="mfbIl_MakeDefault"
                    MaxImage="-1" />
            </td>
        </tr>
        <tr>
            <td colspan="2">&nbsp;</td>
        </tr>
        <tr style="vertical-align: top" runat="server" id="rowNotes">
            <td colspan="2">
                <uc6:Expando ID="expandoNotes" runat="server" HeaderCss="header">
                    <Header>
                        <asp:Localize ID="locNotesHeader" Text="<%$ Resources:Aircraft, locNotesPrompt %>" runat="server"></asp:Localize>
                    </Header>
                    <Body>
                        <div style="padding: 5px">
                            <cc1:TabContainer ID="tabNotes" runat="server">
                                <cc1:TabPanel runat="server" HeaderText="<%$ Resources:Aircraft, locPublicNotesTab %>" CssClass="mfbDefault" ID="tabPublicNotes">
                                    <ContentTemplate>
                                        <div>
                                            <asp:Localize ID="locPublicNotesPrompt" Text="<%$ Resources:Aircraft, locPublicNotesPrompt %>" runat="server"></asp:Localize>
                                        </div>
                                        <asp:TextBox runat="server" ID="txtPublicNotes" Width="90%" MaxLength="4096" dir="auto" TextMode="MultiLine" Rows="4"></asp:TextBox>
                                    </ContentTemplate>
                                </cc1:TabPanel>
                                <cc1:TabPanel runat="server" HeaderText="<%$ Resources:Aircraft, locPrivateNotesTab %>" CssClass="mfbDefault" ID="tabPrivateNotes">
                                    <ContentTemplate>
                                        <div>
                                            <asp:Localize ID="Localize2" Text="<%$ Resources:Aircraft, locPrivateNotesPrompt %>" runat="server"></asp:Localize>
                                        </div>
                                        <asp:TextBox runat="server" ID="txtPrivateNotes" Width="90%" MaxLength="4096" dir="auto" TextMode="MultiLine" Rows="4"></asp:TextBox>
                                    </ContentTemplate>
                                </cc1:TabPanel>
                            </cc1:TabContainer>
                        </div>
                    </Body>
                </uc6:Expando>
            </td>
        </tr>
        <tr id="rowClubSchedules" runat="server" visible="false" style="vertical-align: top">
            <td colspan="2">
                <uc7:mfbEditAppt ID="mfbEditAppt1" runat="server" />
                <uc6:Expando ID="expandoSchedules" runat="server" HeaderCss="header">
                    <Header>
                        <%=Resources.Aircraft.editAircraftShceduleHeader %>
                    </Header>
                    <Body>
                        <asp:Repeater ID="rptSchedules" runat="server" OnItemDataBound="rptSchedules_ItemDataBound">
                            <ItemTemplate>
                                <h3>
                                    <asp:HyperLink ID="lnkClub" NavigateUrl='<%# String.Format("~/Member/ClubDetails.aspx/{0}", Eval("ID")) %>' runat="server"><%# Eval("Name") %></asp:HyperLink></h3>
                                <div class="upcomingEventSummary">
                                </div>
                                <uc8:mfbResourceSchedule ID="schedAircraft" ShowResourceDetails="false" ClubID='<%# Eval("ID") %>' ResourceID="<%# AircraftID.ToString() %>" runat="server">
                                    <SubNavTemplate>
                                        <uc9:SchedSummary ID="schedSummary" runat="server" UserName="<%# Page.User.Identity.Name %>" ResourceName="<%# AircraftID.ToString() %>" ClubID='<%# Eval("ID") %>' />
                                    </SubNavTemplate>
                                </uc8:mfbResourceSchedule>
                            </ItemTemplate>
                        </asp:Repeater>
                    </Body>
                </uc6:Expando>
            </td>
        </tr>
        <tr id="rowMaintenance" runat="server" style="vertical-align: top">
            <td colspan="2">
                <uc6:Expando ID="expandoMaintenance" runat="server" HeaderCss="header">
                    <Header>
                        <%=Resources.Aircraft.editAircraftMaintenanceHeader %>
                    </Header>
                    <Body>
                        <uc4:mfbMaintainAircraft ID="mfbMaintainAircraft" runat="server" />
                    </Body>
                </uc6:Expando>
            </td>
        </tr>
        <tr>
            <td></td>
            <td style="text-align: right">
                <asp:Button ID="btnAddAircraft" runat="server"
                    OnClick="btnAddAircraft_Click"
                    Text="<%$ Resources:Aircraft, editAircraftAddButton %>" ValidationGroup="EditAircraft" />
            </td>
        </tr>
    </table>
    <div><asp:Label ID="lblError" runat="server" CssClass="error" EnableViewState="False"></asp:Label></div>
    <asp:Panel ID="pnlAlternativeVersions" runat="server" Visible="False">
        <h2><% =Resources.Aircraft.editAircraftOtherVersionsHeader %></h2>
        <p><%=Resources.Aircraft.editAicraftOtherVersionsDescription %></p>
        <asp:GridView ID="gvAlternativeVersions" runat="server" GridLines="None"
            AutoGenerateColumns="False" ShowHeader="False" CellPadding="4" OnRowDataBound="gvAlternativeVersions_RowDataBound"
            OnRowCommand="gvAlternativeVersions_RowCommand">
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <uc1:mfbHoverImageList ID="mfbHoverThumb" runat="server" ImageListKey='<%# Eval("AircraftID") %>' ImageListDefaultImage='<%# Eval("DefaultImage") %>' ImageListAltText='<%#: Eval("TailNumber") %>' MaxWidth="150px" 
                            ImageListDefaultLink='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}{1}", Eval("AircraftID"), AdminMode ? "&a=1" : string.Empty) %>' ImageClass="Aircraft" />
                    </ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="160px" />
                </asp:TemplateField>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:HyperLink ID="lnkEditTail" runat="server" Target="_blank"
                            Text='<%# Eval("TailNumber") %>'
                            NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}{1}", Eval("AircraftID"), AdminMode ? "&a=1" : string.Empty) %>'></asp:HyperLink>
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Middle" />
                </asp:TemplateField>
                <asp:BoundField DataField="LongModelDescription">
                    <ItemStyle VerticalAlign="Middle" />
                </asp:BoundField>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:LinkButton ID="lnkUseThis" runat="server"
                            Text="<%$ Resources:Aircraft, editAircraftSwitchOtherVersion %>" CommandName="_switchMigrate"
                            CommandArgument='<%# Bind("AircraftID") %>'></asp:LinkButton>
                        &nbsp;|&nbsp;
                                <asp:LinkButton ID="lnkAddThis" runat="server"
                                    Text="<%$ Resources:Aircraft, editAircraftAddOtherVersion %>"
                                    CommandName="_switchNoMigrate" CommandArgument='<%# Bind("AircraftID") %>'></asp:LinkButton>
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Middle" />
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <asp:Panel ID="pnlAdviseModelChange" runat="server" BackColor="White" Style="margin: 3px; padding:15px; display:none; width: 450px;" DefaultButton="btnChangeModelCancel">
        <p>
            <asp:Label ID="lblAdviseModelChange" runat="server" Text="<%$ Resources:Aircraft, editAircraftModelChangeHeader %>" Font-Bold="True"></asp:Label>
        </p>
        <p><% =Branding.ReBrand(Resources.Aircraft.editAircraftModelChange1) %></p>
        <p><% =Resources.Aircraft.editAircraftModelChange2 %></p>
        <p><% =Resources.Aircraft.editAircraftModelChange3 %></p>
        <div style="text-align:center">
            <asp:Button ID="btnChangeModelTweak" runat="server" Width="45%" Text="<%$ Resources:Aircraft, editAircraftTweak %>" OnClick="btnChangeModelTweak_Click" />
            <asp:Button ID="btnChangeModelClone" runat="server" Width="45%" Text="<%$ Resources:Aircraft, editAircraftClone %>" OnClick="btnChangeModelClone_Click" />
            <br /><br />
            <asp:Button ID="btnChangeModelCancel" runat="server" Width="30%" Text="<%$ Resources:LocalizedText, Cancel %>" />
        </div>
    </asp:Panel>
    <ajaxToolkit:ModalPopupExtender runat="server" DropShadow="true" PopupControlID="pnlAdviseModelChange" BackgroundCssClass="modalBackground" CancelControlID="btnChangeModelCancel" ID="modalModelChange" BehaviorID="modalModelChange" TargetControlID="imgEditAircraftModel"></ajaxToolkit:ModalPopupExtender>
    <script type="text/javascript">
        function hideModelChange() {
            document.getElementById('<% =pnlAdviseModelChange.ClientID %>').style.display = 'none';
            $find("modalModelChange").hide();
        }

        /* Handle escape to dismiss */
        function pageLoad(sender, args) {
            if (!args.get_isPartialLoad()) {
                $addHandler(document, "keydown", onKeyDown);
            }
        }

        function onKeyDown(e) {
            if (e && e.keyCode == Sys.UI.Key.esc)
                hideModelChange();
        }
    </script>
</asp:Panel>