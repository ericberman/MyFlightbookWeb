<%@ Control Language="C#" AutoEventWireup="true"
    Inherits="Controls_mfbEditAircraft" Codebehind="mfbEditAircraft.ascx.cs" %>
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
<%@ Register Src="~/Controls/mfbMakeListItem.ascx" TagPrefix="uc1" TagName="mfbMakeListItem" %>
<%@ Register Src="~/Controls/AircraftControls/SelectMake.ascx" TagPrefix="uc1" TagName="SelectMake" %>
<%@ Register src="../Controls/mfbATDFTD.ascx" tagname="mfbATDFTD" tagprefix="uc2" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
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
    <div><asp:Label ID="lblTailnumber" Font-Size="X-Large" Font-Bold="true" runat="server"></asp:Label> <asp:CheckBox ID="ckLocked" runat="server" Text="<%$ Resources:Aircraft, editAircraftAdminLocked %>" /></div>
    <uc1:SelectMake runat="server" id="SelectMake1" OnModelChanged="SelectMake1_ModelChanged" OnMajorChangeRequested="SelectMake1_MajorChangeRequested">
        <Prompt>
            <asp:Label ID="lblStep1" runat="server" CssClass="bigNumberSection" Text="1"></asp:Label>
            <h3 class="numberedStep"><% =Resources.Aircraft.editAircraftMakeModelPrompt %></h3>
        </Prompt>
    </uc1:SelectMake>
    <asp:MultiView ID="mvInstanceType" runat="server">
        <asp:View ID="vwInstanceNew" runat="server">
            <div>
                <asp:Label ID="lblStep2" runat="server" CssClass="bigNumberSection" Text="2"></asp:Label>
                <h3 class="numberedStep"><% = Resources.Aircraft.editAircraftInstanceTypePrompt %></h3>
            </div>
            <table>
                <tr style="vertical-align:top">
                    <td><asp:RadioButton ID="rbRealRegistered" runat="server" GroupName="rbgInstance" AutoPostBack="true" OnCheckedChanged="UpdateInstanceType" /></td>
                    <td><asp:Label ID="lblRealRegistered" runat="server" Text="<%$ Resources:Aircraft, AircraftInstanceRealRegistered %>" AssociatedControlID="rbRealRegistered"></asp:Label></td>
                </tr>
                <tr style="vertical-align:top">
                    <td><asp:RadioButton ID="rbRealAnonymous" runat="server" GroupName="rbgInstance" AutoPostBack="true" OnCheckedChanged="UpdateInstanceType" /></td>
                    <td>
                        <asp:Label ID="lblAnonymous" runat="server" Text="<%$ Resources:Aircraft, AircraftInstanceRealAnonymous %>" AssociatedControlID="rbRealAnonymous"></asp:Label>
                        <div class="fineprint"><% =Resources.Aircraft.editAircraftAnonymousNote %></div>
                    </td>
                </tr>
                <tr style="vertical-align:top">
                    <td><asp:RadioButton ID="rbTrainingDevice" runat="server" GroupName="rbgInstance" AutoPostBack="true" OnCheckedChanged="UpdateInstanceType" /></td>
                    <td>
                        <asp:Label ID="lblTrainingDevice" runat="server" Text="<%$ Resources:Aircraft, AircraftInstanceTrainingDevice %>" AssociatedControlID="rbTrainingDevice"></asp:Label>
                        <asp:Panel runat="server" ID="pnlTrainingDeviceTypes">
                            <asp:RadioButtonList ID="rblTrainingDevices" runat="server" AutoPostBack="True"
                                DataTextField="DisplayName"
                                DataValueField="InstanceTypeInt"
                                OnSelectedIndexChanged="UpdateInstanceType">
                            </asp:RadioButtonList>
                            <uc2:mfbATDFTD ID="mfbATDFTD1" runat="server" />
                        </asp:Panel>
                    </td>
                </tr>
            </table>
        </asp:View>
        <asp:View ID="vwInstanceExisting" runat="server">
        </asp:View>
    </asp:MultiView>

    <div>
        <asp:Label ID="lblStep3" runat="server" CssClass="bigNumberSection" Text="3"></asp:Label>
        <h3 class="numberedStep"><% =Resources.Aircraft.editAircraftTailNumberPrompt %></h3>
    </div>
    <div>
        <asp:MultiView ID="mvTailnumber" runat="server" ActiveViewIndex="0">
            <asp:View ID="vwRealAircraft" runat="server">
                <asp:MultiView ID="mvRealAircraft" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwRegularTail" runat="server">
                        <script>
                            function AircraftSelected(source, eventArgs) {
                                document.getElementById('<% = imgAutofillProgress.ClientID %>').style.display = 'inline-block';
                                document.getElementById('<% = hdnSelectedAircraftID.ClientID %>').value = eventArgs._value;
                                document.getElementById('<% = lnkPopulateAircraft.ClientID %>').click();
                            }
                        </script>
                        <table>
                            <tr id="rowCountry" runat="server" style="vertical-align: baseline">
                                <td>
                                    <% =Resources.Aircraft.editAircraftCountryPrompt %>
                                </td>
                                <td>
                                    <asp:DropDownList ID="cmbCountryCode" runat="server" AutoPostBack="True" EnableViewState="False"
                                        DataTextField="CountryName" DataValueField="HyphenatedPrefix"
                                        OnSelectedIndexChanged="cmbCountryCode_SelectedIndexChanged">
                                    </asp:DropDownList>
                                    <asp:HiddenField ID="hdnSimCountry" runat="server" EnableViewState="false" />
                                    <asp:HiddenField ID="hdnLastCountry" runat="server" />
                                    <asp:HiddenField ID="hdnLastTail" runat="server" />
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <% =Resources.Aircraft.editAircraftTailNumberPrompt %>
                                </td>
                                <td>
                                    <asp:TextBox ID="txtTail" runat="server" autocomplete="off"
                                        AutoCompleteType="Disabled"
                                        ValidationGroup="EditAircraft"></asp:TextBox>
                                    <cc1:AutoCompleteExtender ID="txtTail_AutoCompleteExtender" runat="server"
                                        CompletionInterval="100" CompletionListCssClass="AutoExtender"
                                        CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                                        CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                                        OnClientItemSelected="AircraftSelected"
                                        Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestAircraft"
                                        ServicePath="~/Member/EditAircraft.aspx" TargetControlID="txtTail">
                                    </cc1:AutoCompleteExtender>
                                    <asp:HiddenField ID="hdnSelectedAircraftID" runat="server" />
                                    <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender1" runat="server"
                                        Enabled="True" FilterType="Custom, Numbers, UppercaseLetters, LowercaseLetters"
                                        TargetControlID="txtTail" ValidChars="-"></cc1:FilteredTextBoxExtender>
                                    <asp:RegularExpressionValidator ID="valTailNumber" runat="server"
                                        ControlToValidate="txtTail" CssClass="error" Display="Dynamic"
                                        ErrorMessage="<%$ Resources:Aircraft, errInvalidTailChars %>"
                                        ValidationExpression="[a-zA-Z0-9]+-?[a-zA-Z0-9]+-?[a-zA-Z0-9]+"
                                        ValidationGroup="EditAircraft"></asp:RegularExpressionValidator>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                                        ControlToValidate="txtTail" CssClass="error" Display="Dynamic"
                                        ErrorMessage="<%$ Resources:Aircraft, errMissingTail %>"
                                        ValidationGroup="EditAircraft"></asp:RequiredFieldValidator>
                                    <asp:CustomValidator ID="valTail" runat="server" ControlToValidate="txtTail"
                                        CssClass="error" Display="Dynamic"
                                        ErrorMessage="<%$ Resources:Aircraft, errInvalidTail %>"
                                        OnServerValidate="ValidateTailNum"
                                        ValidationGroup="EditAircraft"></asp:CustomValidator>
                                    <asp:CustomValidator ID="valSimTail" runat="server"
                                        ControlToValidate="txtTail" CssClass="error" Display="Dynamic"
                                        OnServerValidate="ValidateSim"
                                        ErrorMessage="<%$ Resources:Aircraft, errSimMustStartWithSim %>"
                                        ValidationGroup="EditAircraft"></asp:CustomValidator>
                                    <span style="display: none">
                                        <asp:LinkButton ID="lnkPopulateAircraft" runat="server" OnClick="lnkPopulateAircraft_Click"></asp:LinkButton></span>
                                    <asp:HyperLink ID="lnkAdminFAALookup" runat="server" EnableViewState="false"
                                        Target="_blank"
                                        Text="<%$ Resources:Aircraft, editAircraftRegistrationPrompt %>" Visible="False"></asp:HyperLink>
                                    <asp:Image ID="imgAutofillProgress" Style="display: none" runat="server" ImageUrl="~/images/ajax-loader-transparent-ball.gif" />
                                </td>
                            </tr>
                            <tr>
                                <td></td>
                                <td>
                                    <asp:Panel runat="server" ID="pnlReuseWarning" CssClass="fineprint" style="max-width:400px" visible="false">
                                        <asp:Label ID="lblNote" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>&nbsp;
                                        <asp:Localize ID="locAircraftReuseWarning" runat="server" Text="<%$ Resources:LocalizedText, EditAircraftReuseAdvice %>"></asp:Localize>
                                    </asp:Panel>
                                </td>
                            </tr>
                        </table>
                    </asp:View>
                    <asp:View ID="vwAnonTail" runat="server">
                        <asp:Panel ID="pnlAnonTail" runat="server">
                            <ul>
                                <li><asp:Label ID="lblAnonTailDisplay" Font-Bold="True" Font-Size="Larger" runat="server"></asp:Label></li>
                                <li><asp:Label ID="lblAnonymousTailNote" runat="server" CssClass="fineprint"
                                    Text="<%$ Resources:Aircraft, AnonymousTailNote %>"></asp:Label></li>
                            </ul>
                        </asp:Panel>
                    </asp:View>
                </asp:MultiView>
            </asp:View>
            <asp:View ID="vwSimTail" runat="server">
                <asp:MultiView ID="vwSimTailDisplay" runat="server">
                    <asp:View ID="vwHasModel" runat="server">
                        <ul>
                            <li><asp:Label ID="lblSimTail" Font-Bold="True" Font-Size="Larger" runat="server"></asp:Label></li>
                            <li><asp:Label ID="lblAutoSuggestTail" runat="server" CssClass="fineprint" Text="<%$ Resources:Aircraft, editAircraftAutoAssignedNote %>"></asp:Label></li>
                        </ul>
                    </asp:View>
                    <asp:View ID="vwNoModel" runat="server">
                        <ul>
                            <li><asp:Label ID="lblPendingTail" CssClass="fineprint" runat="server" Text="<%$ Resources:Aircraft, editAircraftTailNumberPending %>"></asp:Label></li>
                        </ul>
                    </asp:View>
                </asp:MultiView>
            </asp:View>
        </asp:MultiView>
    </div>

    <asp:Panel ID="pnlGlassCockpit" runat="server">
        <div>
            <asp:Label ID="lblStep4" runat="server" CssClass="bigNumberSection" Text="4"></asp:Label>
            <h3 class="numberedStep"><% =Resources.Aircraft.editAircraftGlassUpgradeType %></h3>
        </div>
        <div><asp:RadioButton ID="rbAvionicsNone" runat="server" AutoPostBack="true" OnCheckedChanged="rbGlassUpgrade_CheckedChanged" GroupName="avionics" Checked="true" Text="<%$ Resources:Aircraft, editAircraftGlassUpgradeNone %>" /></div>
        <div><asp:RadioButton ID="rbAvionicsGlass" runat="server" AutoPostBack="true" OnCheckedChanged="rbGlassUpgrade_CheckedChanged" GroupName="avionics" Text="<%$ Resources:Aircraft, editAircraftGlassUpgrade %>"/></div>
        <asp:Panel ID="pnlTAA" runat="server">
            <asp:RadioButton ID="rbAvionicsTAA" runat="server" AutoPostBack="true" OnCheckedChanged="rbGlassUpgrade_CheckedChanged" GroupName="avionics" Text="<%$ Resources:Aircraft, editAircraftTAAUpgrade %>"/>
            <uc1:mfbTooltip runat="server" ID="mfbTooltip">
                <TooltipBody>
                    <%=Resources.Makes.TAADefinition %>
                </TooltipBody>
            </uc1:mfbTooltip>
        </asp:Panel>
        <asp:Panel ID="pnlGlassUpgradeDate" runat="server" Style="margin: 3px;">
            <asp:Label ID="lblDateOfGlassUpgrade" runat="server" Text="<%$ Resources:Aircraft, editAircraftGlassUpgradeDate %>"></asp:Label>
            <uc10:mfbTypeInDate runat="server" ID="mfbDateOfGlassUpgrade" DefaultType="None" />
        </asp:Panel>
    </asp:Panel>
    <h3><% =Resources.Aircraft.editAircraftImagesPrompt %></h3>
    <asp:Panel ID="pnlImageNote" runat="server" CssClass="fineprint">
        <asp:Label ID="locImageNote" runat="server" Text="<%$ Resources:LocalizedText, Note %>" Font-Bold="True"></asp:Label>
        <% =Resources.Aircraft.editAircraftSharedImagesNote %>
    </asp:Panel>
    <uc3:mfbMultiFileUpload ID="mfbMFUAircraftImages" Mode="Ajax" IncludeDocs="true" Class="Aircraft" RefreshOnUpload="true" runat="server" />
    <uc1:mfbImageList ID="mfbIl" runat="server" CanEdit="true" Columns="4" ImageClass="Aircraft" OnMakeDefault="mfbIl_MakeDefault" IncludeDocs="true"
        MaxImage="-1" />

    <div runat="server" id="rowNotes" style="margin-top: 5px">
        <uc6:Expando ID="expandoNotes" runat="server" HeaderCss="header">
            <Header>
                <asp:Localize ID="locNotesHeader" Text="<%$ Resources:Aircraft, locNotesPrompt %>" runat="server"></asp:Localize>
            </Header>
            <Body>
                <div style="padding: 5px">
                    <cc1:TabContainer ID="tabNotes" runat="server" CssClass="mfbDefault">
                        <cc1:TabPanel runat="server" HeaderText="<%$ Resources:Aircraft, locPublicNotesTab %>" ID="tabPublicNotes">
                            <ContentTemplate>
                                <div>
                                    <asp:Localize ID="locPublicNotesPrompt" Text="<%$ Resources:Aircraft, locPublicNotesPrompt %>" runat="server"></asp:Localize>
                                </div>
                                <asp:TextBox runat="server" ID="txtPublicNotes" Width="90%" MaxLength="4096" dir="auto" TextMode="MultiLine" Rows="4"></asp:TextBox>
                            </ContentTemplate>
                        </cc1:TabPanel>
                        <cc1:TabPanel runat="server" HeaderText="<%$ Resources:Aircraft, locPrivateNotesTab %>" ID="tabPrivateNotes">
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
    </div>

    <div id="rowClubSchedules" runat="server" visible="false" style="margin-top: 5px">
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
    </div>

    <div id="rowMaintenance" runat="server" style="margin-top: 5px">
        <uc6:Expando ID="expandoMaintenance" runat="server" HeaderCss="header">
            <Header>
                <%=Resources.Aircraft.editAircraftMaintenanceHeader %>
            </Header>
            <Body>
                <uc4:mfbMaintainAircraft ID="mfbMaintainAircraft" runat="server" />
            </Body>
        </uc6:Expando>
    </div>
    
    <div>&nbsp;</div>
    
    <div>
        <asp:Button ID="btnAddAircraft" runat="server"
            OnClick="btnAddAircraft_Click"
            Text="<%$ Resources:Aircraft, editAircraftAddButton %>" ValidationGroup="EditAircraft" />
    </div>
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
                <asp:TemplateField>
                    <ItemStyle VerticalAlign="Middle" />
                    <ItemTemplate>
                        <asp:Label ID="lblAltModel" runat="server" Text='<%# MakeModel.GetModel(Convert.ToInt32(Eval("ModelID"), System.Globalization.CultureInfo.InvariantCulture)).ModelDisplayName %>'></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField>
                    <ItemTemplate>
                        <ul>
                            <li>
                                <asp:LinkButton ID="lnkUseThis" runat="server"
                                Text="<%$ Resources:Aircraft, editAircraftSwitchOtherVersion %>" CommandName="_switchMigrate"
                                CommandArgument='<%# Bind("AircraftID") %>'></asp:LinkButton></li>
                            <li>
                                <asp:LinkButton ID="lnkAddThis" runat="server"
                                    Text="<%$ Resources:Aircraft, editAircraftAddOtherVersion %>"
                                    CommandName="_switchNoMigrate" CommandArgument='<%# Bind("AircraftID") %>'></asp:LinkButton>
                            </li>
                        </ul>
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Middle" />
                </asp:TemplateField>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:Button ID="lnkMergeThis" runat="server" Visible="<%# AdminMode %>"
                            Text="Merge into main (Admin)"
                            CommandName="_merge" CommandArgument='<%# Bind("AircraftID") %>'></asp:Button>
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Middle" />
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Panel>