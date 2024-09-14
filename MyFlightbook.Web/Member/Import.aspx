﻿<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="Import.aspx.cs" Inherits="MyFlightbook.ImportFlights.ImportPage" Title="Import Logbook" culture="auto" Async="true" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbImportAircraft.ascx" tagname="mfbImportAircraft" tagprefix="uc1" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/SponsoredAd.ascx" TagPrefix="uc1" TagName="SponsoredAd" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locHeader" runat="server" Text="<%$ Resources:LogbookEntry, ImportHeader %>" />
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Wizard ID="wzImportFlights" runat="server" onfinishbuttonclick="Import"
        DisplaySideBar="False"
        onactivestepchanged="wzImportFlights_ActiveStepChanged"          
        FinishCompleteButtonText="<%$ Resources:LogbookEntry, ImportWizardFinishButton %>">
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
            <asp:WizardStep ID="wsCreateFile" runat="server" Title="<%$ Resources:LogbookEntry, ImportWizardStep1 %>" >
                <div>
                    <div>
                        <% =Branding.ReBrand(Resources.LogbookEntry.ImportOverview).Replace("%SAMPLEREF%", "~/images/flightimportsample.png".ToAbsoluteURL(Request).ToString()) %>
                    </div>
                    <ul class="nextStep">
                        <li><asp:Image ID="imgCSV" runat="server" ImageUrl="~/images/csvicon_sm.png" style="vertical-align:middle" /> <asp:LinkButton ID="lnkDefaultTemplate" runat="server" Font-Bold="True" 
                                OnClick="lnkDefaultTemplate_Click"
                                Text="<%$ Resources:LogbookEntry, ImportStartingTemplate %>" /></li>
                        <li><asp:HyperLink ID="lnkImportTable" runat="server" Font-Bold="True" 
                                NavigateUrl="~/mvc/pub/ImportTable" Target="_blank" 
                                Text="<%$ Resources:LogbookEntry, ImportColumnDescriptions %>" /></li>
                        <li><asp:HyperLink ID="lnkFAQTips" Font-Bold="true" runat="server" NavigateUrl="~/mvc/faq?q=44#44" Text="<%$ Resources:LogbookEntry, ImportFAQTipsLink %>" /></li>
                        <li><asp:HyperLink ID="lnkBlogTips" Font-Bold="true" runat="server" Text="<%$ Resources:LogbookEntry, ImportBlogLink %>" NavigateUrl="https://myflightbookblog.blogspot.com/2020/05/importing-in-depth.html" /></li>
                    </ul>
                </div>
            </asp:WizardStep>
            <asp:WizardStep ID="wsUpload" runat="server" Title="<%$ Resources:LogbookEntry, ImportWizardStep2 %>">
                <p>
                    <asp:Localize ID="locStep3Desc1" runat="server" Text="<%$ Resources:LogbookEntry, ImportWizardUploadPrompt %>" />
                </p>
                <div>
                    <asp:AjaxFileUpload ID="AjaxFileUpload1" runat="server" CssClass="mfbDefault" AllowedFileTypes="csv,txt,htm,html" AutoStartUpload="true"
                        ThrobberID="myThrobber" MaximumNumberOfFiles="1" ClearFileListAfterUpload="true" OnClientUploadCompleteAll="ajaxFileUploadAttachments_UploadComplete" OnUploadComplete="AjaxFileUpload1_UploadComplete" />
                    <asp:Image ID="myThrobber" ImageUrl="~/images/ajax-loader.gif" runat="server" style="display:None" />
                    <script>
                        function ajaxFileUploadAttachments_UploadComplete(sender, e) {
                            document.getElementById('<% =myThrobber.ClientID %>').style.display = "block";
                            document.getElementById('<% =IDNext %>').disabled = document.getElementById('<% =IDPrev %>').disabled = true;
                            document.getElementById('<% =btnForceRefresh.ClientID %>').click();
                        }
                    </script>
                    <asp:Button ID="btnForceRefresh" runat="server" Text="(Refresh)" OnClick="btnForceRefresh_Click" style="display:none" />
                </div>
                <div>
                    <table>
                        <tr style="vertical-align:top">
                            <td><asp:RadioButton ID="rbAutofillNone" runat="server" GroupName="rbAutofill" Checked="true" /></td>
                            <td><asp:Label ID="Label2" runat="server" Text="<%$ Resources:LogbookEntry, ImportWizardAutofillNone %>" AssociatedControlID="rbAutofillNone"></asp:Label></td>
                        </tr>
                        <tr>
                            <td colspan="2"><% =Resources.LogbookEntry.ImportWizardAutofillPromptPart1 %></td>
                        </tr>
                        <tr style="vertical-align:top">
                            <td><asp:RadioButton ID="rbAutofillUtc" runat="server" GroupName="rbAutofill" /></td>
                            <td>
                                <asp:Label ID="Label5" runat="server"  AssociatedControlID="rbAutofillUtc"><% =Resources.LogbookEntry.ImportWizardAutofillPrompt.Linkify() %></asp:Label><br />
                            </td>
                        </tr>
                        <tr style="vertical-align:top">
                            <td><asp:RadioButton ID="rbAutofillLocal" runat="server" GroupName="rbAutofill" /></td>
                            <td>
                                <asp:Label ID="lblAutofillLabel" runat="server"  AssociatedControlID="rbAutofillLocal"><% =Resources.LogbookEntry.ImportWizardAutofillTryLocal.Linkify() %></asp:Label><br />
                                <asp:Label ID="lblNoteAutofill" runat="server" Text="<%$ Resources:LocalizedText, Note %>" CssClass="fineprint" Font-Bold="true" AssociatedControlID="rbAutofillLocal"></asp:Label>
                                <% =Resources.LogbookEntry.ImportWizardAutofillTryLocalNote.Linkify() %>
                            </td>
                        </tr>
                        <tr style="vertical-align:top;">
                            <td><asp:RadioButton ID="rbAutofillPreferred" runat="server" GroupName="rbAutofill" /></td>
                            <td><asp:Label ID="lblAutofillPreferred" runat="server" GroupName="rbAutofill" AssociatedControlID="rbAutofillPreferred" /></td>
                        </tr>
                    </table>
                </div>
                <div style="white-space:pre"><asp:Label ID="lblFileRequired" runat="server" Text="" CssClass="error" EnableViewState="false" /></div>
            </asp:WizardStep>
            <asp:WizardStep ID="wsMissingAircraft" runat="server" Title="<%$ Resources:LogbookEntry, ImportWizardStep3 %>"  >
                <asp:MultiView ID="mvMissingAircraft" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwNoMissingAircraft" runat="server">
                        <p><asp:Label ID="lblNoMissingaircraft" runat="server" Text="<%$ Resources:LogbookEntry, ImportWizardAllAircraftFound %>" /></p>
                    </asp:View>
                    <asp:View ID="vwAddMissingAircraft" runat="server">
                        <p><asp:Label ID="lblMissingAircraft" runat="server" Text="<%$ Resources:LogbookEntry, ImportWizardMissingAircraft %>" ></asp:Label></p>
                        <uc1:mfbImportAircraft ID="mfbImportAircraft1" runat="server" />
                    </asp:View>
                </asp:MultiView>
                <br />
                <asp:Panel ID="pnlAudit" runat="server" Visible="false" Font-Size="Smaller">
                    <uc1:Expando runat="server" ID="ExpandoAudit">
                        <Header>
                            <asp:Localize ID="locAuditHeader" runat="server" Text="<%$ Resources:LogbookEntry, importAuditHeader %>" />
                        </Header>
                        <Body>
                            <asp:Panel ID="pnlConverted" runat="server">
                                <p><asp:Label ID="lblFileWasConverted" runat="server" Text=""></asp:Label> <asp:LinkButton ID="btnDownloadConverted" runat="server" Text="<%$ Resources:LogbookEntry, importLabelDownloadConverted %>" OnClick="btnDownloadConverted_Click" /></p>
                            </asp:Panel>
                            <br />
                            <div style="white-space:pre"><asp:Label ID="lblAudit" runat="server"></asp:Label></div>
                        </Body>
                    </uc1:Expando>
                    <asp:HiddenField ID="hdnAuditState" runat="server" />
                </asp:Panel>
            </asp:WizardStep>
            <asp:WizardStep ID="wsPreview" runat="server" Title="<%$ Resources:LogbookEntry, ImportWizardStep4 %>" >
                <p><asp:Localize ID="locPreviewReady" runat="server" 
                    Text="<%$ Resources:LogbookEntry, ImportWizardPreviewHeader %>" /></p>
                <p>
                    <asp:Label ID="lblImportant2" Font-Bold="true" runat="server" 
                        Text="<%$ Resources:LogbookEntry, ImportWizardImportant %>" />
                    <asp:Label ID="lblOnlyClickOnce" runat="server" 
                        Text="<%$ Resources:LogbookEntry, ImportWizardOnlyClickOnce %>" />
                </p>
                <div>
                    <asp:Label ID="lblUpdateOverwriteKey" runat="server" Text="<%$ Resources:LogbookEntry, ImportWizardUpdateVsNew %>" ></asp:Label>
                    <table>
                        <tr style="vertical-align:middle">
                            <td>&nbsp;&nbsp;</td>
                            <td><asp:Image ID="imgAddKey" runat="server" ImageAlign="Middle" ImageUrl="~/images/add.png"  /></td>
                            <td><asp:Label ID="lblKeyAdd" runat="server" Text="<%$ Resources:LogbookEntry, ImportIconNewFlight %>" ></asp:Label></td>
                        </tr>
                        <tr style="vertical-align:middle">
                            <td>&nbsp;&nbsp;</td>
                            <td><asp:Image ID="imgUpdateKey" runat="server" ImageAlign="Middle" ImageUrl="~/images/update.png" /></td>
                            <td><asp:Label ID="lblKeyUpdate" runat="server" Text="<%$ Resources:LogbookEntry, ImportIconUpdatedFlight %>" ></asp:Label></td>
                        </tr>
                    </table>
                </div>
                <asp:Label ID="lblError" runat="server" CssClass="error" EnableViewState="False" />
                <asp:PlaceHolder ID="plcErrorList" runat="server" EnableViewState="false"></asp:PlaceHolder>
                <div>&nbsp;</div>
            </asp:WizardStep>
        </WizardSteps>
        <StartNavigationTemplate>
            <div style="text-align:center">
                <asp:Button CommandName="MoveNext" Runat="server" Text="<%$ Resources:LogbookEntry, ImportWizardBeginButton %>" />
            </div>
        </StartNavigationTemplate>
        <StepNavigationTemplate>
            <div style="text-align:center">
                <asp:Button CommandName="MovePrevious" ID="btnPrev" Runat="server" Text="<%$ Resources:LocalizedText, PreviousPrompt %>" />
                <asp:Button CommandName="MoveNext" ID="btnNext" Runat="server" Text="<%$ Resources:LocalizedText, NextPrompt %>" />
            </div>
        </StepNavigationTemplate>
        <FinishNavigationTemplate>
            <div style="text-align:center">
                <asp:Button ID="btnNewFile" OnClick="btnNewFile_Click" Runat="server" Visible="false" Text="<%$ Resources:LogbookEntry, ImportUploadNewFile %>" />
                <asp:Button CommandName="MovePrevious" Runat="server" Text="<%$ Resources:LocalizedText, PreviousPrompt %>" />
                <button type="button" onclick="this.disabled=true;document.getElementById('<% =ImportButtonClientID %>').click();"><% =Resources.LogbookEntry.ImportWizardFinishButton %></button>
                <asp:Button CommandName="MoveComplete" Runat="server" ID="btnImport" Text="<%$ Resources:LogbookEntry, ImportWizardFinishButton %>" style="display:none" />
            </div>
            <ajaxToolkit:ConfirmButtonExtender ID="confirmImportWithErrors" runat="server" ConfirmText="<%$ Resources:LogbookEntry, ImportErrorsConfirm %>" TargetControlID="btnImport" />
        </FinishNavigationTemplate>
    </asp:Wizard>
    <asp:Panel ID="pnl3rdPartyServices" runat="server" style="margin-left:auto; margin-right:auto; text-align:center; margin-top: 15px">
        <br /><br />
        <hr />
        <asp:Panel ID="pnlAcuLog" runat="server" CssClass="calloutSmall calloutSponsor shadowed">
            <table style="text-align:left;">
                <tr>
                    <td style="max-width:200px;">
                        <span style="font-weight: bold;">
                            <% =Branding.ReBrand(Resources.LogbookEntry.ImportAculogHeader) %>
                        </span>
                        <% =Branding.ReBrand(Resources.LogbookEntry.ImportAculogPromo).Linkify() %>
                    </td>
                    <td>
                        <uc1:SponsoredAd runat="server" ID="SponsoredAd" SponsoredAdID="2" Visible="true" />
                    </td>
                </tr>
            </table>
        </asp:Panel>
    </asp:Panel>
    <asp:Panel ID="pnlImportSuccessful" runat="server" Visible="False">
        <p><asp:Label ID="lblImportSuccessful" runat="server" Text="<%$ Resources:LogbookEntry, ImportSuccessful %>" ></asp:Label></p>
        <ul>
            <asp:Repeater ID="rptImportResults" runat="server">
                <ItemTemplate>
                    <li><%# Container.DataItem %></li>
                </ItemTemplate>
            </asp:Repeater>
        </ul>
        <ul class="nextStep">
            <li><asp:HyperLink ID="lnkDone" runat="server" NavigateUrl="~/mvc/flights" Text="<%$ Resources:LogbookEntry, ImportViewImportedFlights %>" /></li>
            <li runat="server" visible="false" id="reviewPending"><asp:HyperLink ID="lnkPending" runat="server" NavigateUrl="~/mvc/flightedit/pending" Text="<%$ Resources:LogbookEntry, ImportViewImportedFlightsPending %>" /></li>
        </ul>
    </asp:Panel>
    <br /><br />
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
    <asp:MultiView ID="mvContent" runat="server">
        <asp:View ID="View1" runat="server"></asp:View>
        <asp:View ID="View2" runat="server"></asp:View>
        <asp:View ID="View3" runat="server"></asp:View>
        <asp:View ID="View4" runat="server">
    <div style="margin-left:auto; margin-right:auto; max-width:90%">
        <asp:MultiView ID="mvPreviewResults" runat="server">
            <asp:View runat="server" ID="vwPreviewResults">
                <asp:MultiView ID="mvPreview" runat="server">
                    <asp:View ID="vwPreview" runat="server">
                        <table class="stickyHeaderTable">
                            <thead>
                                <tr>
                                    <th class="gvhDefault"></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldTail %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldApproaches %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldHold %></th>
                                    <th class="gvhDefault">
                                        <% =Resources.LogbookEntry.FieldLanding %>
                                        <uc1:mfbTooltip runat="server" ID="mfbTooltip" BodyContent="<%$ Resources:LogbookEntry, LogbookLandingKey %>" />
                                    </th>
                                    <th class="gvhDefault"><% =Resources.LogbookEntry.FieldXCountry %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldNight %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldSimIMC %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldIMC %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldGroundSim %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldDual %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldCFI %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldSIC %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldPIC %></th>
                                    <th class="gvhDefault"><%=Resources.LogbookEntry.FieldTotal %></th>
                                </tr>
                            </thead>
                            <asp:Repeater ID="rptPreview" OnItemDataBound="rptPreview_ItemDataBound" runat="server">
                                <ItemTemplate>
                                    <tr runat="server" id="rowError" visible='false'>
                                        <td colspan="15" runat="server" >
                                            <div><asp:Label ID="lblFlightErr" CssClass="error" runat="server" Text='<%# Eval("ErrorString") %>' /></div>
                                            <div><asp:Label ID="lblRawRow" CssClass="error" runat="server" /></div>
                                        </td>
                                    </tr>
                                    <tr runat="server" id="rowFlight">
                                        <td runat="server" >
                                            <div>
                                                <asp:Image ID="imgNewOrUpdate" runat="server" ImageUrl='<%# String.IsNullOrEmpty((string) Eval("ErrorString")) ? (Convert.ToBoolean(Eval("IsNewFlight")) ? "~/images/add.png" : "~/images/update.png") : "~/images/circleslash.png" %>' 
                                                ToolTip='<%# String.IsNullOrEmpty((string) Eval("ErrorString")) ? (Convert.ToBoolean(Eval("IsNewFlight")) ? Resources.LogbookEntry.ImportAddTooltip : Resources.LogbookEntry.ImportUpdateTooltip) : (string) Eval("ErrorString") %>'  />
                                                <span style="font-weight:bold"><%# ((DateTime) Eval("Date")).ToShortDateString() %></span>
                                                <%#: Eval("Route") %>
                                            </div>
                                            <div><%#: Eval("Comment") %></div>
                                            <div><asp:PlaceHolder ID="plcAdditional" runat="server"></asp:PlaceHolder></div>
                                            <asp:Panel ID="pnlDiffs" runat="server" Visible="False" CssClass="calloutBackground calloutSmall">
                                                <ul>
                                                    <asp:Repeater ID="rptDiffs" runat="server">
                                                        <ItemTemplate>
                                                            <li><%#: Container.DataItem.ToString() %></li>
                                                        </ItemTemplate>
                                                    </asp:Repeater>
                                                </ul>
                                            </asp:Panel>
                                        </td>
                                        <td runat="server" >
                                            <div style="font-weight:bold"><%#: Eval("TailNumDisplay") %></div>
                                            <div><%#: Eval("ModelDisplay") %></div>
                                            <div><%#: Eval("CatClassDisplay") %></div>
                                        </td>
                                        <td runat="server" ><%# Eval("Approaches").FormatInt() %></td>
                                        <td style="font-weight:bold; padding: 3px;" runat="server" ><%# Eval("fHoldingProcedures").FormatBoolean() %></td>
                                        <td runat="server" ><%# LogbookEntryDisplay.LandingDisplayForFlight((LogbookEntry) Container.DataItem) %></td>
                                        <td runat="server" ><%# Eval("CrossCountry").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("Nighttime").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("SimulatedIFR").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("IMC").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("GroundSim").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("Dual").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("CFI").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("SIC").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("PIC").FormatDecimal(UseHHMM) %></td>
                                        <td runat="server" ><%# Eval("TotalFlightTime").FormatDecimal(UseHHMM) %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </table>
                    </asp:View>
                    <asp:View ID="vwNoResults" runat="server">
                        <div style="text-align:center"><asp:Label ID="lblNoFlightsFound" runat="server" CssClass="error" Text="<%$ Resources:LogbookEntry, ImportErrNoFlightsFound %>" /></div>
                    </asp:View>
                </asp:MultiView>
            </asp:View>
            <asp:View runat="server" ID="vwImportResults">
                <asp:PlaceHolder ID="plcProgress" runat="server"></asp:PlaceHolder>
            </asp:View>
        </asp:MultiView>
    </div>
        </asp:View>
    </asp:MultiView>
</asp:content>
