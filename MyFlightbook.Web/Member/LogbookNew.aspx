<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Trace="false" Codebehind="LogbookNew.aspx.cs" Inherits="MyFlightbook.MemberPages.LogbookNew" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbEditFlight.ascx" tagname="mfbEditFlight" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbQueryDescriptor.ascx" tagname="mfbQueryDescriptor" tagprefix="uc7" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>
<%@ Register Src="~/Controls/GoogleChart.ascx" TagPrefix="uc1" TagName="GoogleChart" %>


<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblUserName" runat="server"></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:HiddenField ID="hdnLastViewedPane" runat="server" />
    <script type="text/javascript">
        function closeWelcome() {
            dismissDlg("#" + "<%=pnlWelcomeNewUser.ClientID %>");
            return false;
        }

        function onAccordionPaneShown(idx) {
            $("#" + "<% =hdnLastViewedPane.ClientID %>").val(idx);
        }

        function updPrint() {
            var ckEndorsements = $('#<% =ckEndorsements.ClientID %>')[0];
            var ckIncludeImgs = $('#<% =ckIncludeEndorsementImages.ClientID %>')[0];
            if (!ckEndorsements.checked)
                ckIncludeImgs.checked = false;
            ckIncludeImgs.disabled = !ckEndorsements.checked;
            var ckTotals = $('#<% = ckTotals.ClientID %>')[0];
            var ckCompactTotals = $('#<% = ckCompactTotals.ClientID %>')[0]
            if (!ckTotals.checked)
                ckCompactTotals.checked = false;
            ckCompactTotals.disabled = !ckTotals.checked;
            var sects = new Object();
            sects["Endorsements"] = ckEndorsements.checked ? (ckIncludeImgs.checked ? "DigitalAndPhotos" : "DigitalOnly") : "None";
            sects["IncludeCoverPage"] = $('#<% = ckIncludeCoverSheet.ClientID %>')[0].checked;
            sects["IncludeFlights"] = true;
            sects["IncludeTotals"] = $('#<% =ckTotals.ClientID %>')[0].checked;
            sects["CompactTotals"] = ckCompactTotals.checked;

            var lnkPreview = $('#<% =lnkPrintView.ClientID %>')[0];

            var params = new Object();
            params.szExisting = lnkPreview.href;
            params.ps = sects;

            var d = JSON.stringify(params);

            $.ajax(
                {
                    url: '<% =ResolveUrl("~/Member/Ajax.asmx/PrintLink") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) {
                        window.alert(xhr.responseJSON.Message);
                    },
                    complete: function (response) { },
                    success: function (response) {
                        lnkPreview.href = response.d
                    }
                });
            return false;
        }

        var currentQuery = <% =Restriction.ToJSONString() %>;
        var fTotalsLoaded = false;
        var fCurrencyLoaded = false;
        var fAnalysisLoaded = false;
        var proxyControl = null;

        $(() => {
            proxyControl = new accordionProxy($("#accordionproxycontainer"), {
                defaultPane: "<% =DefaultPane %>",
                proxies: [
                    { idButton: "apcAdd", idTarget: "targetNewFlight", onclick: function () { onAccordionPaneShown("<% = FlightsTab.Add.ToString() %>") } },
                    { idButton: "apcSearch", idTarget: "targetFilter", isEnhanced: <% = Restriction.IsDefault ? "false" : "true" %> },
                    {
                        idButton: "apcTotals", idTarget: "targetTotals", onclick: function () {
                            onAccordionPaneShown("<% = FlightsTab.Totals.ToString() %>");
                            if (!fTotalsLoaded) {
                                var params = new Object();
                                params.userName = "<% =Page.User.Identity.Name %>";
                                params.linkItems = true;
                                params.grouped = <% = GroupedTotals ? "true" : "false" %>;
                                params.fq = currentQuery;
                                $.ajax({
                                    url: "<% =ResolveUrl("~/mvc/flights/GetTotalsForUser") %>",
                                    type: "POST", data: JSON.stringify(params), dataType: "html", contentType: 'application/json',
                                    error: function (xhr, status, error) { window.alert(xhr.responseText); },
                                    complete: function () { },
                                    success: function (r) {
                                        fTotalsLoaded = true;
                                        $("#totalsContainer").html(r);
                                    }
                                });
                            }
                        }
                    },
                    {
                        idButton: "apcCurrency", idTarget: "targetCurrency", onclick: function () {
                            onAccordionPaneShown("<% = FlightsTab.Currency.ToString() %>");
                            if (!fCurrencyLoaded) {
                                var params = new Object();
                                params.userName = "<% =Page.User.Identity.Name %>";
                                params.linkItems = true;
                                $.ajax({
                                    url: "<% =ResolveUrl("~/mvc/flights/GetCurrencyForUser") %>",
                                    type: "POST", data: JSON.stringify(params), dataType: "html", contentType: 'application/json',
                                    error: function (xhr, status, error) { window.alert(xhr.responseText); },
                                    complete: function () { },
                                    success: function (r) {
                                        fCurrencyLoaded = true;
                                        $("#currencyContainer").html(r);
                                    }
                                });
                            }
                        }
                    },
                    {
                        idButton: "apcAnalysis", idTarget: "targetAnalysis", onclick: function () {
                            onAccordionPaneShown("<% = FlightsTab.Analysis.ToString() %>");
                            if (!fAnalysisLoaded) {
                                var params = new Object();
                                params.fq = currentQuery;
                                $.ajax({
                                    url: "<% =ResolveUrl("~/mvc/flights/GetAnalysisForUser") %>",
                                    type: "POST", data: JSON.stringify(params), dataType: "html", contentType: 'application/json',
                                    error: function (xhr, status, error) { window.alert(xhr.responseText); },
                                    complete: function () { },
                                    success: function (r) {
                                        fAnalysisLoaded = true;
                                        $("#analysisContainer").html(r);
                                        chartDataChanged();
                                    }
                                });
                            }
                        }
                    },
                    { idButton: "apcPrinting", idTarget: "targetPrintView" },
                    { idButton: "apcMore", idTarget: "targetMore" }
                ]
            });

            if (document.getElementById('<%=pnlWelcomeNewUser.ClientID %>'))
                showModalById('<%=pnlWelcomeNewUser.ClientID %>', '<%=Resources.LocalizedText.WelcomeHeader %>', '320');

        });
    </script>
    <div id="accordionproxycontainer">
        <div id="apcAdd"><% =Resources.LocalizedText.LogTabNewFlight %></div>
        <div id="apcSearch"><% =Resources.LocalizedText.LogTabFilter %></div>
        <div id="apcTotals"><% =Resources.LocalizedText.LogTabTotals %></div>
        <div id="apcCurrency"><% =Resources.LocalizedText.LogTabCurrency %></div>
        <div id="apcAnalysis"><% =Resources.LocalizedText.LogTabAnalysis %></div>
        <div id="apcPrinting"><% =Resources.LocalizedText.LogTabPrint %></div>
        <div id="apcMore"><% =Resources.LocalizedText.LogTabMore %></div>
    </div>
    <asp:Panel ID="pnlFilter" runat="server" CssClass="filterApplied" >
        <div style="display:inline-block;"><%=Resources.LocalizedText.ResultsFiltered %></div>
            <uc7:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
    </asp:Panel>
    <div id="targetNewFlight" style="display:none;">
        <uc2:mfbEditFlight ID="mfbEditFlight1" runat="server" OnFlightUpdated="mfbEditFlight1_FlightUpdated" OnFlightEditCanceled="mfbEditFlight1_FlightEditCanceled" />
    </div>
    <div id="targetFilter" style="display:none;">
        <uc3:mfbSearchForm ID="mfbSearchForm1" InitialCollapseState="True" runat="server" OnQuerySubmitted="mfbSearchForm1_QuerySubmitted" OnReset="mfbSearchForm1_Reset" />
    </div>
    <div id="targetTotals" style="display:none;">
        <div style="float:right;">
            <uc1:popmenu runat="server" ID="popmenu" OffsetX="-100">
                <MenuContent>
                    <asp:RadioButtonList ID="rblTotalsMode" runat="server" OnSelectedIndexChanged="rblTotalsMode_SelectedIndexChanged" AutoPostBack="true">
                        <asp:ListItem Text="<%$ Resources:Totals, TotalsModeFlat %>" Value="False" Selected="True"></asp:ListItem>
                        <asp:ListItem Text="<%$ Resources:Totals, TotalsModeGrouped %>" Value="True"></asp:ListItem>
                    </asp:RadioButtonList>
                </MenuContent>
            </uc1:popmenu>
        </div>
        <div id="totalsContainer">
            <div style="text-align: center;"><asp:Image runat="server" ID="imgTotalsInProgress" ImageUrl="~/images/progress.gif" /></div>
        </div>
    </div>
    <div id="targetCurrency" style="display:none;">
        <div id="currencyContainer">
            <div style="text-align: center;"><asp:Image runat="server" ID="imgCurrencyInProgress" ImageUrl="~/images/progress.gif" /></div>
        </div>
    </div>
    <div id="targetAnalysis" style="display:none;">
        <uc1:GoogleChart runat="server" ID="GoogleChart" Visible="false" />
        <div id="analysisContainer">
            <div style="text-align: center;"><asp:Image runat="server" ID="imgAnalysisInProgress" ImageUrl="~/images/progress.gif" /></div>
        </div>
    </div>
    <div id="targetPrintView" style="display:none;">
        <div style="margin-left: auto; margin-right:auto; text-align:center">
            <div style="padding:5px; margin-left: auto; margin-right:auto; text-align:left; width: 50%">
                <h3><%=Resources.LocalizedText.PrintViewTabFilter %></h3>
                <table>
                    <tr>
                        <td>
                            <asp:CheckBox ID="ckIncludeCoverSheet" runat="server" Checked="true" onclick="updPrint();" />
                        </td>
                        <td>
                            <asp:Label ID="lblCoverSheet" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeCoverSheet %>" AssociatedControlID="ckIncludeCoverSheet" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:CheckBox ID="ckTotals" runat="server" Checked="true" onclick="updPrint();" />
                        </td>
                        <td>
                            <asp:Label ID="lblIncludeTotals" AssociatedControlID="ckTotals" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeTotals %>" />
                            <asp:CheckBox ID="ckCompactTotals" runat="server" onclick="updPrint();" />
                            <asp:Label ID="lblTotalsCompact" runat="server" Text="<%$ Resources:LocalizedText, PrintViewTotalsCompact %>" AssociatedControlID="ckCompactTotals" />
                            <span class="fineprint"><% =Resources.LocalizedText.PrintViewTotalsCompactNote %></span>
                        </td>
                    </tr>
                    <tr>
                        <td style="vertical-align:top;">
                            <asp:CheckBox ID="ckEndorsements" runat="server" Checked="true" onclick="updPrint();" />
                        </td>
                        <td>
                            <div>
                                <asp:Label ID="lblIncludeEndorsements" AssociatedControlID="ckEndorsements" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeEndorsements %>" />
                                <asp:CheckBox ID="ckIncludeEndorsementImages" runat="server" Checked="true" onclick="updPrint();" />
                                <asp:Label ID="lblIncludeWhat" AssociatedControlID="ckIncludeEndorsementImages" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeJPEGEndorsements %>" />
                            </div>
                            <div class="fineprint"><% =Resources.LocalizedText.PrintViewNoEmbeddedPDFsNote %></div>
                        </td>
                    </tr>
                </table>
                <ul class="nextStep">
                    <li><asp:HyperLink ID="lnkPrintView" runat="server" Target="_blank" Text="<%$ Resources:LocalizedText, OpenPrintView %>" /></li>
                </ul>
            </div>
        </div>
    </div>
    <div id="targetMore" style="display:none;">
        <table style="border-spacing: 10px;">
            <tr>
                <td style="text-align:center;">
                    <asp:Image ID="imgDownload" ImageUrl="~/images/download.png" AlternateText="<%$ Resources:Tabs, LogbookDownload %>" runat="server" />
                </td>
                <td>
                    <asp:HyperLink ID="lnkDownload" Font-Bold="true" runat="server" NavigateUrl="~/Member/Download.aspx?lm=Accordion" Text="<%$ Resources:Tabs, LogbookDownload %>"></asp:HyperLink>
                </td>
                <td>
                    <% =Resources.LocalizedText.LogbookDownloadDescription %>
                </td>
            </tr>
            <tr>
                <td style="text-align:center;">
                    <asp:Image ID="imgImport" ImageUrl="~/images/import.png" runat="server" AlternateText="<%$ Resources:Tabs, LogbookImport %>" />
                </td>
                <td>
                    <asp:HyperLink ID="lnkImport" Font-Bold="true" runat="server" NavigateUrl="~/Member/Import.aspx?lm=Accordion" Text="<%$ Resources:Tabs, LogbookImport %>"></asp:HyperLink>
                </td>
                <td>
                    <% =Branding.ReBrand(Resources.LocalizedText.LogbookImportDescription) %>
                </td>
            </tr>
            <tr>
                <td style="text-align:center;">
                    <asp:Image ID="imgCheckFlights" runat="server" ImageUrl="~/images/CheckFlights.png" AlternateText="<%$ Resources:FlightLint, TitleCheckFlights %>" />
                </td>
                <td>
                    <asp:HyperLink ID="lnkCheckFlights" Font-Bold="true" runat="server" Text="<%$ Resources:FlightLint, TitleCheckFlights %>" NavigateUrl="~/Member/CheckFlights.aspx"></asp:HyperLink>
                </td>
                <td>
                    <% =Branding.ReBrand(Resources.FlightLint.CheckFlightsShortDescription) %>
                </td>
            </tr>
            <tr>
                <td style="text-align:center;">
                    <asp:Image ID="imgStartingTotals" ImageUrl="~/images/startingtotals.png" AlternateText="<%$ Resources:LocalizedText, StartingTotalsLink %>" runat="server" />
                </td>
                <td>
                    <asp:HyperLink ID="lnkStartingTotals" Font-Bold="true" NavigateUrl="~/Member/StartingTotals.aspx?lm=Accordion" runat="server" Text="<%$ Resources:LocalizedText, StartingTotalsLink %>"></asp:HyperLink>
                </td>
                <td>
                    <% =Branding.ReBrand(Resources.LocalizedText.LogbookStartingTotalsDescription) %>
                </td>
            </tr>
            <tr>
                <td style="text-align:center;">
                    <asp:Image ID="imgPendingFlights" ImageUrl="~/images/pendingflights.png" AlternateText="<%$ Resources:LocalizedText, PendingFlightsLink %>" runat="server" />
                </td>
                <td>
                    <asp:HyperLink ID="lnkPendingFlights" Font-Bold="true" NavigateUrl="~/Member/ReviewPendingFlights.aspx" runat="server" Text="<%$ Resources:LocalizedText, PendingFlightsLink %>"></asp:HyperLink>
                </td>
                <td>
                    <% =Branding.ReBrand(Resources.LocalizedText.LogbookPendingFlightsDescription) %>
                </td>
            </tr>
        </table>
    </div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <uc1:mfbLogbook ID="mfbLogbook1" runat="server" DetailsPageUrlFormatString="~/Member/FlightDetail.aspx/{0}"
         EditPageUrlFormatString="~/Member/LogbookNew.aspx/{0}" AnalysisPageUrlFormatString="~/Member/FlightDetail.aspx/{0}?tabID=Chart" SendPageTarget="~/Member/LogbookNew.aspx" />
    <asp:Panel ID="pnlWelcomeNewUser" runat="server" style="display:none;">
        <p><%=Resources.LocalizedText.WelcomeThanks %></p>
        <p><%=Resources.LocalizedText.WelcomeNextSteps %></p>
        <table style="border-spacing: 10px;">
            <tr>
                <td>
                    <asp:Image ID="imgLogbook" runat="server" ImageUrl="~/Public/tabimages/logbookTab.png" Width="24" AlternateText="<%$ Resources:LocalizedText, WelcomeEnterFlights %>"  />
                </td>
                <td><% =Resources.LocalizedText.WelcomeEnterFlights %><br />
                <% =Resources.LocalizedText.ORSeparator %></td>
            </tr>
            <tr>
                <td>
                    <asp:Image ID="imgAircraft" runat="server" ImageUrl="~/Public/tabimages/AircraftTab.png" Width="24" AlternateText="<%$ Resources:LocalizedText, WelcomeEnterAircraft %>" />
                </td>
                <td><asp:HyperLink ID="lnkAddAircraft" NavigateUrl="~/mvc/Aircraft" runat="server" Text="<%$ Resources:LocalizedText, WelcomeEnterAircraft %>" /><br />
                <% =Resources.LocalizedText.ORSeparator %></td>
            </tr>
            <tr>
                <td>
                    <asp:Image ID="imgImport2" runat="server" ImageUrl="~/images/import.png" AlternateText="<%$ Resources:LocalizedText, WelcomeImportFlights %>"  />
                </td>
                <td><asp:HyperLink ID="lnkImportWelcome" NavigateUrl="~/Member/Import.aspx" runat="server" Text="<%$ Resources:LocalizedText, WelcomeImportFlights %>" /><br />
                <% =Resources.LocalizedText.ORSeparator %></td>
            </tr>
            <tr>
                <td>
                    <asp:Image ID="imgStartingTotals2" runat="server" ImageUrl="~/images/startingtotals.png" AlternateText="<%$ Resources:LocalizedText, WelcomeSetStartingTotals %>" />
                </td>
                <td><asp:HyperLink ID="lnkSetStartingTotals" runat="server" Text="<%$ Resources:LocalizedText, WelcomeSetStartingTotals %>" NavigateUrl="~/Member/StartingTotals.aspx" /></td>
            </tr>
        </table>

        <div style="text-align:center">
        <asp:Button ID="btnClose" runat="server" Text="<%$ Resources:LocalizedText, Close %>" OnClientClick="return closeWelcome();" /></div>
    </asp:Panel>
</asp:Content>

