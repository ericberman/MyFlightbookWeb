<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_StartingTotals" culture="auto" meta:resourcekey="PageResource1" Codebehind="StartingTotals.aspx.cs" %>
<%@ Reference Control="~/Controls/mfbDecimalEdit.ascx" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc2" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text="Starting Totals" 
                meta:resourcekey="lblHeaderResource1"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Wizard ID="wizStartingTotals" runat="server" 
        onactivestepchanged="wizStartingTotals_ActiveStepChanged" 
        onfinishbuttonclick="wizStartingTotals_FinishButtonClick" 
        DisplaySideBar="False" 
        FinishDestinationPageUrl="~/Member/LogbookNew.aspx?ft=Totals" 
        meta:resourcekey="wizStartingTotalsResource1" ActiveStepIndex="0" Width="100%">
        <HeaderTemplate>
            <div style="width:100%">
                <asp:Repeater ID="SideBarList" runat="server">
                    <ItemTemplate>
                        <span class="<%# GetClassForWizardStep(Container.DataItem) %>">
                            &nbsp;
                            <%# DataBinder.Eval(Container, "DataItem.Title") %>
                        </span>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </HeaderTemplate>
        <WizardSteps>
            <asp:WizardStep ID="stepIntro" runat="server" Title="1. Overview" 
                meta:resourcekey="stepIntroResource1">
                <p><asp:Label ID="lblOverview" runat="server" 
                        Text="Have a paper logbook with all of your historical flights?  While it's best to get them all in the system, that can be a lot of work." 
                        meta:resourcekey="lblOverviewResource1"></asp:Label></p>
                <p><asp:Label ID="lblOverview2" runat="server" 
                        Text="You can instead initialize your logbook with starting flights that represent your flying totals to date, so that your totals are up to date." meta:resourcekey="lblOverview2Resource1" 
                        ></asp:Label></p>
                <p><asp:Label ID="lblWhyStartingFlights" runat="server" Font-Bold="true" 
                        Text="Why use starting flights instead of just using starting values?" meta:resourcekey="lblWhyStartingFlightsResource2" 
                        ></asp:Label> 
                    <asp:Label
                        ID="lblAnswerWhyStartingFlights" runat="server" Text="" meta:resourcekey="lblAnswerWhyStartingFlightsResource2" 
                        ></asp:Label></p>
                <cc1:CollapsiblePanelExtender ID="CollapsiblePanelExtender1" runat="server" 
                        CollapseControlID="lblAnswerWhyStartingFlights" ExpandControlID="lblAnswerWhyStartingFlights" CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>"
                        ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" 
                        TargetControlID="pnlWhyStartingFlights" TextLabelID="lblAnswerWhyStartingFlights" Collapsed="True" 
                        Enabled="True" >
                </cc1:CollapsiblePanelExtender>
                <asp:Panel ID="pnlWhyStartingFlights" runat="server" 
                    meta:resourcekey="pnlWhyStartingFlightsResource2">
                    <asp:Localize ID="locWhyStartingFlights" runat="server" Text="" 
                        meta:resourcekey="locWhyStartingFlightsResource2"></asp:Localize>
                </asp:Panel>
            </asp:WizardStep>
            <asp:WizardStep ID="stepInputAircraft" runat="server" 
                Title="2. Get aircraft ready" 
                meta:resourcekey="stepInputAircraftResource1">
                <p><asp:Label ID="lblInputAircraft" runat="server" 
                        Text="First, Make sure that all of the aircraft that you've flown are in your account." 
                        meta:resourcekey="lblInputAircraftResource1"></asp:Label></p>
                <ul>
                    <li>
                        <p><asp:Label ID="lblInputAircraft2" runat="server"
                            Text="You don't need every aircraft, but you should have at least one that represents each type of total you would like to represent in your totals." meta:resourcekey="lblInputAircraft2Resource2"></asp:Label></p>
                        <p><asp:Label ID="lblInputAircraft3" runat="server"
                            Text="For example, if all of your flying is in single-engine aircraft, one of the C-172's that you've flown may be sufficient." meta:resourcekey="lblInputAircraft3Resource1"></asp:Label></p></li>
                    <li>
                        <p><asp:Label ID="lblInputAircraftGeneric" runat="server" Text="There is also a special set of generic aircraft in the system by the pseudo manufacturer &quot;Generic&quot;, which can be useful for lumping together common characteristics without picking a specific model.  E.g., &quot;Generic - ASEL&quot; can be used to represent single-engine piston time that is not complex/retract/tailwheel, or &quot;Generic - AMEL Turbine&quot; for multi-engine turbine time." meta:resourcekey="lblInputAircraftGenericResource1"></asp:Label></p></li>
                    <li>
                        <p><asp:Label ID="lblInputAircraft4" runat="server"
                            Text="If you have flown Boeing 747's and Bell R-22 helicopters, though, you probably want to capture those totals separately, so make sure that one of the 747's and one of the R-22's is in your list." meta:resourcekey="lblInputAircraft4Resource1"></asp:Label></p></li>
                    <li>
                        <p><asp:Label ID="lblCanUseAnonymous" runat="server"
                            Text="If you don't remember the tailnumber of an aircraft, or if you have flown many different aircraft of a particular model (such as an airline pilot might do), you may wish to create &quot;anonymous&quot; aircraft.  These aircraft have no specific tailnumber."
                            meta:resourcekey="lblCanUseAnonymousResource1"></asp:Label></p>
                    </li>
                </ul>
                <p><asp:HyperLink ID="lnkAircraft" runat="server" Text="Add/Edit/Import Aircraft" 
                        NavigateUrl="~/Member/Aircraft.aspx" Target="_blank" 
                        meta:resourcekey="lnkAircraftResource1"></asp:HyperLink></p>
                <p><asp:Label ID="lblCurrentAircraft" runat="server" Text="Your aircraft:" Font-Bold="true" 
                        meta:resourcekey="lblCurrentAircraftResource1"></asp:Label> 
                    <asp:Label ID="lblViewExisting" runat="server" Text="" Font-Bold="false" 
                        meta:resourcekey="lblViewExistingResource1"></asp:Label></p>
                <cc1:CollapsiblePanelExtender ID="cpeExistingAircraft" runat="server" 
                        CollapseControlID="lblViewExisting" ExpandControlID="lblViewExisting" CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>"
                        ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" 
                        TargetControlID="pnlExistingAircraft" TextLabelID="lblViewExisting" Collapsed="True" 
                        Enabled="True" >
                </cc1:CollapsiblePanelExtender>
                <asp:Panel ID="pnlExistingAircraft" runat="server" 
                    meta:resourcekey="pnlExistingAircraftResource1">
                    <asp:GridView ID="gvAircraft" runat="server" ShowHeader="false" 
                        GridLines="None" AutoGenerateColumns="False" 
                        meta:resourcekey="gvAircraftResource1">
                        <EmptyDataTemplate>
                            <asp:Label ID="lblNoAircraft" CssClass="error" runat="server" 
                                Text="You do not currently have any aircraft set up for your account!" 
                                meta:resourcekey="lblNoAircraftResource1"></asp:Label>
                        </EmptyDataTemplate>
                        <Columns>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
                                <ItemTemplate>
                                    <b><%# Eval("TailNumber") %></b> <%# Eval("ModelDescription")%> - <%# Eval("ModelCommonName")%> <%# Eval("InstanceTypeDescription")%>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </asp:Panel>
            </asp:WizardStep>
            <asp:WizardStep ID="stepPickDate" runat="server" 
                Title="3. Options" 
                meta:resourcekey="stepPickDateResource1">
                <p><asp:Label ID="lblPickStartingDate" runat="server" 
                        Text="Pick a date to use for the starting flights.  Good dates to choose are either (a) the date of your first flight, or (b) the date of the last flight in your logbook." 
                        meta:resourcekey="lblPickStartingDateResource1"></asp:Label> </p>
                <p><uc1:mfbTypeInDate ID="mfbTypeInDate1" runat="server" DefaultType="Today" /></p>
                <p><asp:Label ID="lblGranularity" runat="server" 
                        Text="How would you like to enter your totals?" 
                        meta:resourcekey="lblGranularityResource1"></asp:Label> </p>
                <table>
                    <tr valign="top">
                        <td><asp:RadioButton ID="rbSimple" runat="server" GroupName="rbMode" Checked="true" AutoPostBack="true"
                        Text="" meta:resourcekey="rbSimpleResource1" OnCheckedChanged="InitFlights" /> </td>
                        <td><asp:Label ID="lblSimple" Font-Bold="true" runat="server" Text="Simple" 
                                AssociatedControlID="rbSimple" meta:resourcekey="lblSimpleResource2"></asp:Label> <asp:Label ID="lblSimpleDesc" runat="server" AssociatedControlID="rbSimple" 
                        Text="Enter starting totals by category/class and, if appropriate, type.  This is the simplest to do, but is also the coarsest approximation of various categories of flying time." meta:resourcekey="lblSimpleDescResource1" 
                        ></asp:Label></td>
                    </tr>
                    <tr valign="top">
                        <td><asp:RadioButton ID="rbMedium" runat="server" GroupName="rbMode" AutoPostBack="true"
                        Text="" meta:resourcekey="rbMediumResource1" OnCheckedChanged="InitFlights"/> </td>
                        <td><asp:Label ID="lblMedium" Font-Bold="true" runat="server" Text="Medium" 
                                AssociatedControlID="rbMedium" meta:resourcekey="lblMediumResource2"></asp:Label> <asp:Label ID="lblMediumDesc" runat="server" AssociatedControlID="rbMedium"
                        Text="Enter starting totals by category/class/type, but also by aircraft capabilities.  For example, you can break out retract or high-performance time.  This is more work than simple, but will provide a better approximation of your flying totals." meta:resourcekey="lblMediumDescResource1" 
                            ></asp:Label></td>
                    </tr>
                    <tr valign="top">
                        <td><asp:RadioButton ID="rbModels" runat="server" GroupName="rbMode" AutoPostBack="true"
                        Text="" meta:resourcekey="rbModelsResource1" OnCheckedChanged="InitFlights" /> </td>
                        <td><asp:Label ID="lblDetailed" Font-Bold="true" runat="server" Text="Detailed" 
                                AssociatedControlID="rbModels" meta:resourcekey="lblDetailedResource1"></asp:Label>  <asp:Label ID="lblModelsDesc" runat="server" AssociatedControlID="rbModels"
                        Text="Enter starting totals based on time in each model of aircraft you have flown.  This is the most complex, but will also provide the best approximation of your flying totals." meta:resourcekey="lblModelsDescResource1" 
                            ></asp:Label></td>
                    </tr>
                </table>
            </asp:WizardStep>
            <asp:WizardStep ID="stepFillInData" runat="server" 
                Title="4. Specify Times" 
                meta:resourcekey="stepFillInDataResource1">
                <p>
                    <asp:Label ID="lblFillInForm" runat="server" Text="Fill in your times below" Font-Bold="true" 
                        meta:resourcekey="lblFillInFormResource1"></asp:Label>
                    <asp:Label ID="lblCanFillInMoreLater" runat="server" Text="For simplicity, this focuses on the top-level totals.  After you create your starting totals, you will have &quot;Catch-up&quot; flights which you can edit to include whatever additional details you like."
                        meta:resourcekey="lblCanFillInMoreLaterResource1"></asp:Label>
                </p>
                <asp:Table CellPadding="4" ID="tblStartingFlights" runat="server" 
                    EnableViewState="false" meta:resourcekey="tblStartingFlightsResource1">
                </asp:Table>
                <asp:Label EnableViewState="false" ID="lblNoAircraft" runat="server" 
                    Text="No aircraft (other than training devices) are set up in your account.  To enter totals, please add some of the aircraft that you have flown." 
                    CssClass="error" Visible="false" meta:resourcekey="lblNoAircraftResource2"></asp:Label>
            </asp:WizardStep>
            <asp:WizardStep ID="stepPreview" runat="server" Title="5. Preview & Finish" 
                meta:resourcekey="stepPreviewResource1">
                <br />
                <div style="float:left; padding:4px; border: 1px solid black;">
                    <h2><asp:Label ID="lblTotalsBefore" runat="server" 
                            Text="Totals BEFORE changes are made" meta:resourcekey="lblTotalsBeforeResource1"></asp:Label></h2>
                    <uc2:mfbTotalSummary ID="mfbTotalsBefore" runat="server" />
                </div>
                <div style="float:left">&nbsp;</div>
                <div style="float:left; padding:4px;border: 1px solid black;">
                    <h2><asp:Label ID="Label1" runat="server" Text="Totals AFTER changes are made" 
                            meta:resourcekey="Label1Resource1"></asp:Label></h2>
                    <uc2:mfbTotalSummary ID="mfbTotalsAfter" runat="server" />
                </div>
            </asp:WizardStep>
        </WizardSteps>
    </asp:Wizard>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

