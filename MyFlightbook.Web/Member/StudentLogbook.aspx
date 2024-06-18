<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="StudentLogbook.aspx.cs" Inherits="MyFlightbook.Instruction.StudentLogbook" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbCurrency.ascx" tagname="mfbCurrency" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbSimpleTotals.ascx" tagname="mfbSimpleTotals" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbTotalSummary.ascx" TagPrefix="uc1" TagName="mfbTotalSummary" %>
<%@ Register Src="~/Controls/mfbSearchForm.ascx" TagPrefix="uc1" TagName="mfbSearchForm" %>
<%@ Register Src="~/Controls/mfbQueryDescriptor.ascx" TagPrefix="uc1" TagName="mfbQueryDescriptor" %>
<%@ Register Src="~/Controls/mfbEditFlight.ascx" TagPrefix="uc1" TagName="mfbEditFlight" %>
<%@ Register Src="~/Controls/mfbDownload.ascx" TagPrefix="uc1" TagName="mfbDownload" %>
<%@ Register Src="~/Controls/GoogleChart.ascx" TagPrefix="uc1" TagName="GoogleChart" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
<script type="text/javascript">
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
    var fAnalysisLoaded = false;
    var proxyControl = null;

    $(() => {
        proxyControl = new accordionProxy($("#accordionproxycontainer"), {
            defaultPane: "",
                proxies: [
                    { idButton: "<% = apcNewFlight.ClientID %>", idTarget: "targetNewFlight" },
                    { idButton: "apcFilter", idTarget: "targetFilter", isEnhanced: <% = Restriction.IsDefault ? "false" : "true" %> },
                    { idButton: "apcTotals", idTarget: "targetTotals" },
                    { idButton: "apcCurrency", idTarget: "targetCurrency" },
                    {
                        idButton: "apcAnalysis", idTarget: "targetAnalysis", onclick: function () {
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
                ]
            });
        });
</script>
    <div style="padding:5px">
        <asp:Image ID="ib" ImageAlign="AbsMiddle" ImageUrl="~/images/back.png" runat="server" /><asp:HyperLink ID="lnkReturn" runat="server" NavigateUrl="~/mvc/training/students" Text="<%$ Resources:Profile, ReturnToProfile %>"></asp:HyperLink>
    </div>
    <div id="accordionproxycontainer" style="display:none;">
        <div id="apcNewFlight" runat="server"><% =Resources.LocalizedText.LogTabNewFlight %></div>
        <div id="apcFilter"><% =Resources.LocalizedText.LogTabFilter %></div>
        <div id="apcTotals"><% =Resources.LocalizedText.LogTabTotals %></div>
        <div id="apcCurrency"><% =Resources.LocalizedText.LogTabCurrency %></div>
        <div id="apcAnalysis"><% =Resources.LocalizedText.LogTabAnalysis %></div>
        <div id="apcPrinting"><% =Resources.LocalizedText.LogTabPrint %></div>
    </div>
    <asp:Panel ID="pnlFilter" runat="server" CssClass="filterApplied" >
        <div style="display:inline-block;"><%=Resources.LocalizedText.ResultsFiltered %></div>
        <uc1:mfbQueryDescriptor runat="server" ID="mfbQueryDescriptor" OnQueryUpdated="mfbQueryDescriptor_QueryUpdated" />
    </asp:Panel>
    <div id="targetNewFlight" style="display: none">
        <uc1:mfbEditFlight runat="server" ID="mfbEditFlight" OnFlightWillBeSaved="mfbEditFlight_FlightWillBeSaved" OnFlightUpdated="mfbEditFlight_FlightUpdated" />
    </div>
    <div id="targetFilter" style="display: none">
        <uc1:mfbSearchForm runat="server" ID="mfbSearchForm" OnQuerySubmitted="ShowResults" OnReset="ClearForm" InitialCollapseState="true" />
    </div>
    <div id="targetTotals" style="display: none">
        <uc1:mfbTotalSummary runat="server" ID="mfbTotalSummary" LinkTotalsToQuery="false" />
    </div>
    <div id="targetCurrency" style="display: none">
        <uc1:mfbCurrency ID="mfbCurrency1" runat="server" />
    </div>
    <div id="targetAnalysis" style="display: none">
        <uc1:GoogleChart runat="server" ID="GoogleChart" Visible="false" />
        <div id="analysisContainer">
            <div style="text-align: center;"><asp:Image runat="server" ID="imgAnalysisInProgress" ImageUrl="~/images/progress.gif" /></div>
        </div>
    </div>
    <div id="targetPrintView" style="display: none">
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
            <div>
                <asp:LinkButton ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadCSV_Click" style="vertical-align:middle">
                    <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" style="padding-right: 5px; vertical-align:middle" />
                    <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_sm.png" style="padding-right: 5px; vertical-align:middle;" />
                    <span style="vertical-align:middle"><asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:LocalizedText, DownloadFlyingStats %>"></asp:Localize></span>
                </asp:LinkButton>
            </div>
            <uc1:mfbDownload runat="server" ID="mfbDownload1" />
        </div>
    </div>
    <asp:Label ID="lblErr" CssClass="error" runat="server" Text=""></asp:Label>
    <asp:Panel ID="pnlLogbook" runat="server">
        <uc3:mfbLogbook ID="mfbLogbook1" runat="server" DetailsPageUrlFormatString="~/mvc/flights/details/{0}"
         EditPageUrlFormatString="~/mvc/flights/details/{0}" AnalysisPageUrlFormatString="~/mvc/flights/details/{0}?tabID=Chart"  />
    </asp:Panel>
    <asp:HiddenField ID="hdnStudent" runat="server" />
</asp:Content>
