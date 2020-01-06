<%@ Page Title="Import Aircraft" Language="C#" MaxPageStateFieldLength="40000" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_ImpAircraft" culture="auto" meta:resourcekey="PageResource1" Codebehind="ImpAircraft.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbATDFTD.ascx" tagname="mfbATDFTD" tagprefix="uc1" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbEditMake.ascx" tagname="mfbEditMake" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbImportAircraft.ascx" tagname="mfbImportAircraft" tagprefix="uc3" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <asp:Localize ID="locHeader" runat="server" Text="Import Aircraft" meta:resourcekey="locHeaderResource1"></asp:Localize>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Wizard ID="wzImportAircraft" runat="server" Width="800px" 
        OnFinishButtonClick="ImportAllNew" DisplaySideBar="False"
        meta:resourcekey="wzImportAircraftResource1" ActiveStepIndex="0" 
        onnextbuttonclick="wzImportAircraft_NextButtonClick" 
        onactivestepchanged="wzImportAircraft_ActiveStepChanged" OnPreviousButtonClick="wzImportAircraft_PreviousButtonClick">
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
            <asp:WizardStep runat="server" ID="stepSelectFile" 
                title="1. Select a file to import" meta:resourcekey="stepSelectFileResource1">
                <h3><asp:Localize ID="locDataFormatHeader" runat="server" Text="Data Format" 
                        meta:resourcekey="locDataFormatHeaderResource1"></asp:Localize></h3>
                <p><asp:Localize ID="locDataFormatBody" runat="server" 
                        Text="Your data must be a &lt;a href='http://en.wikipedia.org/wiki/Comma-separated_values' target='_blank'&gt;CSV file&lt;/a&gt; (which you can create with Excel, for example):" 
                        meta:resourcekey="locDataFormatBodyResource1" ></asp:Localize></p>
                <p>
                    <asp:Label ID="lblHeaderRow" runat="server" Text="The FIRST row of the data must be a header row, identifying which column is which, and it MUST have the following column names:" meta:resourcekey="lblHeaderRowResource1"></asp:Label>
                </p>
                <table>
                    <tr style="vertical-align:top">
                        <td><asp:Label ID="lblHeaderTailNum" Font-Bold="true" runat="server" Text="Tail Number" meta:resourcekey="lblHeaderTailNumResource1"></asp:Label></td>
                        <td>
                            <asp:Label ID="lblHeaderTailNumDesc" runat="server" Text="This column contains the registration (tail number) for each aircraft, INCLUDING the country code (e.g., &quot;N&quot; for the US)." meta:resourcekey="lblHeaderTailNumDescResource1"></asp:Label>
                            <ul>
                                <li>
                                    <asp:Label ID="lblTipSim" runat="server"
                                        Text="For simulators/training devices, you can simply use the tailnumber &quot;SIM&quot;; a tailnumber will be assigned."
                                        meta:resourcekey="lblTipSimResource1"></asp:Label></li>
                                <li>
                                    <asp:Label ID="lblTipAnon" runat="server"
                                        Text="For anonymous aircraft (i.e., no tailnumber), use &quot;#&quot;.  This is useful if you fly many different aircraft of a given model, such as an airline pilot might do."
                                        meta:resourcekey="lblTipAnonResource1"></asp:Label>
                                </li>
                            </ul>
                        </td>
                    </tr>
                    <tr style="vertical-align:top">
                        <td><asp:Label ID="lblHeaderModel" Font-Bold="true" runat="server" Text="Model" meta:resourcekey="lblHeaderModelResource1"></asp:Label></td>
                        <td><asp:Label ID="lblModelDesc" runat="server" Text="This column contains the model identifier for the aircraft.  E.g., &quot;C-172&quot; or &quot;B737&quot;.  We will try to match it to an existing make/model, and you will have the chance to review/adjust this before import." meta:resourcekey="lblModelDescResource1"></asp:Label></td>
                    </tr>
                </table>
                <p><asp:Localize ID="locCanBeInAnyOrder" runat="server" Text="The columns can be in any order; if there are additional columns, they will be ignored" meta:resourcekey="locCanBeInAnyOrderResource1"></asp:Localize></p>
                <p><asp:Localize ID="locSamplePrompt" runat="server" 
                        Text="A sample CSV table to import a Cessna 172 and a Boeing 777 might look like this:" 
                        meta:resourcekey="locSamplePromptResource1"></asp:Localize></p>
                <table style="margin-left:auto; margin-right: auto;" cellpadding="5px">
                    <tr>
                        <td>
                            <asp:Localize ID="locExcelView" runat="server" Text="In Excel:" meta:resourcekey="locExcelViewResource1"></asp:Localize></td>
                        <td>
                            <asp:Localize ID="Localize2" runat="server" Text="In a text editor" meta:resourcekey="Localize2Resource1"></asp:Localize></td>
                    </tr>
                    <tr>
                        <td><asp:Image ID="imgSample" ImageUrl="~/images/AircraftImportSample.png" ToolTip="Image of sample import sheet in Excel" runat="server" meta:resourcekey="imgSampleResource1" /></td>
                        <td>
                            <pre>
Tail Number,Model
N12345,C172
N6789,B777
...
                    </pre>
                        </td>
                    </tr>
                </table>
                <p><asp:Localize ID="locBatches" runat="server" Text="You can import aircraft in batches, if you like - import a few aircraft now, and come back later and you can pick up where you left off." meta:resourcekey="locBatchesResource1"></asp:Localize></p>
                <p style="font-weight:bold"><asp:Localize ID="locStep1Body" runat="server"
                        Text="You can preview all aircraft and make changes as needed before anything is imported." 
                        meta:resourcekey="locStep1BodyResource1"></asp:Localize></p>
                    <asp:FileUpload ID="fuCSVAircraft" runat="server" 
                    meta:resourcekey="fuCSVAircraftResource1" />&nbsp;&nbsp;
                <br />
                <asp:Label ID="lblUploadErr" runat="server" CssClass="error" 
                    EnableViewState="False" meta:resourcekey="lblUploadErrResource1"></asp:Label>
            </asp:WizardStep>
            <asp:WizardStep ID="stepExisting" runat="server" 
                Title="2. Match to Existing aircraft" meta:resourcekey="stepExistingResource1">
                <p><asp:Label ID="lblStats" runat="server" 
                        Text="The following has been found in the file you uploaded; nothing has been imported yet:" 
                        meta:resourcekey="lblStatsResource1"></asp:Label> </p>
                <table style="margin-left:50px">
                    <tr>
                        <td>
                            <asp:Localize ID="locMatchedToProfile" runat="server" 
                                Text="Aircraft that are already in your profile:" 
                                meta:resourcekey="locMatchedToProfileResource1"></asp:Localize>
                        </td>
                        <td>
                            <asp:Label ID="lblCountMatchProfile" Font-Bold="true" runat="server" Text="" 
                                meta:resourcekey="lblCountMatchProfileResource1"></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Localize ID="locMatchedExisting" runat="server" 
                                Text="Aircraft that are in the system but not yet in your profile:" 
                                meta:resourcekey="locMatchedExistingResource1"></asp:Localize>
                        </td>
                        <td>
                            <asp:Label ID="lblCountMatchExisting" Font-Bold="true" runat="server" Text="" 
                                meta:resourcekey="lblCountMatchExistingResource1"></asp:Label>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Localize ID="locUnmatched" runat="server" 
                                Text="Aircraft that are not yet in the system:" 
                                meta:resourcekey="locUnmatchedResource1"></asp:Localize>
                        </td>
                        <td>
                            <asp:Label ID="lblCountUnmatched" Font-Bold="true" runat="server" Text="" 
                                meta:resourcekey="lblCountUnmatchedResource1"></asp:Label>
                        </td>
                    </tr>
                </table>
                <p><asp:Localize ID="locExistingAircraft" runat="server" 
                        Text="The aircraft below matched to aircraft that are already in the system; some may already be in your profile." 
                        meta:resourcekey="locExistingAircraftResource1"></asp:Localize></p>
                <p><asp:Localize ID="locImportExistingReview" runat="server" 
                        Text="You can review them to make sure they match your expectations and add them individually, or you can import them all at once." 
                        meta:resourcekey="locImportExistingReviewResource1"></asp:Localize></p>
                <p>
                    <asp:Label ID="lblNote" runat="server" Font-Bold="true" 
                        Text="<%$ Resources:LocalizedText, Note %>" meta:resourcekey="lblNoteResource1"></asp:Label> 
                    <asp:Localize ID="locImportExistingMultiVersions" runat="server" 
                        Text="Aircraft tailnumbers are sometimes re-assigned.  You may see the same tailnumber across multiple aircraft below, or you may see your tailnumber on an aircraft that is different from what you expect." 
                        meta:resourcekey="locImportExistingMultiVersionsResource1"></asp:Localize>
                    <asp:HyperLink ID="lnkContact" runat="server" Target="_blank" 
                        NavigateUrl="~/Public/ContactMe.aspx" Text="Please contact us if you need a change to an aircraft." 
                        meta:resourcekey="lnkContactResource1"></asp:HyperLink>
                </p>
                <p>
                    <asp:Button ID="btnImportAllExisting" runat="server" 
                        Text="Add all of the aircraft below to my profile" 
                        OnClick="btnImportAllExisting_Click" meta:resourcekey="btnImportAllExistingResource1" 
                        />
                </p>
            </asp:WizardStep>
            <asp:WizardStep ID="stepUnmatched" runat="server" 
                title="3. Add new aircraft" 
                meta:resourcekey="stepPreviewResource1">
                <p>
                    <asp:Localize ID="locNoMatchHeader" runat="server" 
                        Text="The aircraft below did not match any existing aircraft." 
                        meta:resourcekey="locNoMatchHeaderResource1"></asp:Localize>
                </p>
                <p>
                    <asp:Localize ID="locStep2Body" runat="server" 
                        Text="Each of the models you specified in your spreadsheet is matched to one in the system.  <b>PLEASE CHECK EACH OF THESE</b> to ensure that they are mapped to the correct make/model.  You can fix it up at any time, but it is easiest to catch this now." 
                        meta:resourcekey="locStep2BodyResource1"></asp:Localize>
                </p>
                <p>
                    <asp:Localize ID="locClickEdit" runat="server" 
                        Text="Click &quot;Edit&quot; next to each aircraft to make changes." 
                        meta:resourcekey="locClickEditResource1"></asp:Localize>
                </p>
                <uc1:mfbATDFTD ID="mfbATDFTD1" runat="server" /><br />
                <p><asp:Localize ID="locAddMakePrompt" runat="server" 
                        Text="There are many models of aircraft in the system, but if you don't see yours, you can add it." 
                        meta:resourcekey="locAddMakePromptResource1"></asp:Localize>
                    <a href="javascript:void();"><asp:Label ID="lblAddMake" runat="server" 
                        meta:resourcekey="lblAddMakeResource1"></asp:Label></a></p>
                <cc1:CollapsiblePanelExtender ID="cpeAddMake" runat="server" 
                        TargetControlID="pnlAddMake" CollapsedSize="0" ExpandControlID="lblAddMake"
                CollapseControlID="lblAddMake" Collapsed="True" 
                        CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" 
                        TextLabelID="lblAddMake" 
                        ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" Enabled="True"></cc1:CollapsiblePanelExtender>
                <asp:Panel ID="pnlAddMake" runat="server" Height="0px" Width="600px" 
                        style="overflow:hidden;" meta:resourcekey="pnlAddMakeResource1">
                    <div style="padding:3px; border:solid 1px black;" class="logbookForm">
                        <uc5:mfbEditMake ID="mfbEditMake1" runat="server" MakeID="-1" OnMakeUpdated="NewMakeAdded" />
                    </div>
                </asp:Panel>
                <p><asp:Label ID="lblSkipsErrors" runat="server" 
                        Text="When you press &quot;Finish&quot;, all of the aircraft below that don't have errors will be imported.  You can come back later and re-import if necessary." 
                        meta:resourcekey="lblSkipsErrorsResource1"></asp:Label>
                        <asp:Label ID="lblCanBeSlow" Font-Bold="true" runat="server" 
                        Text="If you have a lot of aircraft, this can be slow; please be patient!" 
                        meta:resourcekey="lblCanBeSlowResource1"></asp:Label>
                </p>
            </asp:WizardStep>
        </WizardSteps>
    </asp:Wizard>
    <asp:Label ID="lblImportError" runat="server" CssClass="error" 
        EnableViewState="False" meta:resourcekey="lblImportErrorResource1"></asp:Label>
    <asp:MultiView ID="mvAircraftToImport" runat="server" Visible="False">
        <asp:View ID="vwMatchExisting" runat="server">
            <table>
                <tr>
                    <td>
                        <h2>
                <asp:Label ID="lblExisting" runat="server" 
                    Text="These aircraft match existing aircraft" meta:resourcekey="lblExistingResource2" ></asp:Label>
                        </h2>
                    </td>
                    <td>
                        <asp:UpdateProgress ID="UpdateProgress2" AssociatedUpdatePanelID="UpdatePanel2" runat="server">            
                            <ProgressTemplate>
                                <asp:Image ID="imgProgress" ImageUrl="~/images/ajax-loader.gif" runat="server" meta:resourcekey="imgProgressResource2" />
                            </ProgressTemplate>
                        </asp:UpdateProgress>
                    </td>
                </tr>
            </table>
            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                <ContentTemplate>
                    <uc3:mfbImportAircraft ID="mfbImportAircraftExisting" runat="server" />
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwNoMatch" runat="server">
            <table>
                <tr>
                    <td>
                        <h2>
                            <asp:Label ID="lblHeaderUnmatched" runat="server" Text="These aircraft are new" meta:resourcekey="lblHeaderUnmatchedResource2"></asp:Label>
                        </h2>
                    </td>
                    <td>
                        <asp:UpdateProgress ID="UpdateProgress1" AssociatedUpdatePanelID="UpdatePanel2" runat="server">            
                            <ProgressTemplate>
                                <asp:Image ID="imgProgress" ImageUrl="~/images/ajax-loader.gif" runat="server" meta:resourcekey="imgProgressResource3" />
                            </ProgressTemplate>
                        </asp:UpdateProgress>
                    </td>
                </tr>
            </table>
            <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                <ContentTemplate>
                    <uc3:mfbImportAircraft ID="mfbImportAircraftNew" runat="server" />
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

