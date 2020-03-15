<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Title="<%$ Resources:FlightLint, TitleCheckFlights %>" CodeBehind="CheckFlights.aspx.cs" Inherits="MyFlightbook.Web.Member.CheckFlights" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="locHeader" runat="server" Text="<%$ Resources:FlightLint, TitleCheckFlights %>"></asp:Localize></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <script>
        function onDateAutofill() {
            $find('<% =mfbDateLastCheck.WatermarkExtender.ClientID %>').set_text(document.getElementById('<% =hdnLastDateCheck.ClientID %>').value);
        }
    </script>
    <h2><asp:Label ID="lblTitleCheckFlights" runat="server" Text="<%$ Resources:FlightLint, TitleCheckFlights %>"></asp:Label></h2>
    <p><asp:Label ID="lblCheckFlightsDescription" runat="server" Text="<%$ Resources:FlightLint, CheckFlightsDescription1 %>"></asp:Label></p>
    <p><asp:Label ID="lblCheckFlightsCategories" runat="server"></asp:Label></p>
    <asp:Panel ID="pnlMainForm" runat="server" DefaultButton="btnCheckAll">
        <div style="margin-left: 3em;">
            <div><asp:CheckBox ID="ckSim" runat="server" Text="<%$ Resources:FlightLint, LintCategorySim %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip" BodyContent="<%$ Resources:FlightLint, LintCategorySimTip %>" /></div>
            <div><asp:CheckBox ID="ckIFR" runat="server" Text="<%$ Resources:FlightLint, LintCategoryIFR %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip1" BodyContent="<%$ Resources:FlightLint, LintCategoryIFRTip %>" /></div>
            <div><asp:CheckBox ID="ckAirports" runat="server" Text="<%$ Resources:FlightLint, LintCategoryAirports %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip2" BodyContent="<%$ Resources:FlightLint, LintCategoryAirportsTip %>" /></div>
            <div><asp:CheckBox ID="ckXC" runat="server" Text="<%$ Resources:FlightLint, LintCategoryCrossCountry %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip3" BodyContent="<%$ Resources:FlightLint, LintCategoryCrossCountryTip %>" /></div>
            <div><asp:CheckBox ID="ckPICSICDualMath" runat="server" Text="<%$ Resources:FlightLint, LintCategoryPICSICDualMath %>" /><uc1:mfbtooltip runat="server" id="mfbTooltip4" BodyContent="<%$ Resources:FlightLint, LintCategoryPICSICDualMathTip %>" /></div>
            <div><asp:CheckBox ID="ckTimes" runat="server" Text="<%$ Resources:FlightLint, LintCategoryTimes %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip5" BodyContent="<%$ Resources:FlightLint, LintCategoryTimesTip %>" /></div>
            <div><asp:CheckBox ID="ckDateTime" runat="server" Text="<%$ Resources:FlightLint, LintCategoryDateTime %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip6" BodyContent="<%$ Resources:FlightLint, LintCategoryDateTimeTip %>" /></div>
            <div><asp:CheckBox ID="ckMisc" runat="server" Text="<%$ Resources:FlightLint, LintCategoryMisc %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip7" BodyContent="<%$ Resources:FlightLint, LintCategoryMiscTip %>" /></div>
            <asp:Panel ID="pnlCheckSinceDate" runat="server">
                <asp:Label ID="lblCheckSince" runat="server" Text="<%$ Resources:FlightLint, PromptOnlyCheckNewFlights %>"></asp:Label>
                <uc1:mfbTypeInDate runat="server" ID="mfbDateLastCheck" />
                <span runat="server" visible="false" id="spanLastCheck">
                    <asp:ImageButton ID="ImageButton1" runat="server" ImageUrl="~/images/cross-fill.png" OnClientClick="javascript:onDateAutofill(); return false;" ToolTip="<%$ Resources:FlightLint, PromptCopyLastCheckDate %>" AlternateText="<%$ Resources:FlightLint, PromptCopyLastCheckDate %>" />
                    <asp:Label ID="lblLastCheck" runat="server" Text="<%$ Resources:FlightLint, PromptLastCheckDate %>"></asp:Label>
                    <asp:HiddenField ID="hdnLastDateCheck" runat="server" />
                </span>
            </asp:Panel>
        </div>
        <p><asp:Button ID="btnCheckAll" runat="server" Text="<%$ Resources:FlightLint, CheckFlightsBegin %>" OnClick="btnCheckAll_Click" /> <asp:Label ID="lblSummary" runat="server"></asp:Label></p>
    </asp:Panel>
    <div><asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="false" ></asp:Label></div>
    <asp:GridView ID="gvFlights" runat="server" ShowHeader="false" GridLines="None" AutoGenerateColumns="false" Font-Size="8pt">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div>
                        <asp:HyperLink ID="lnkEditFlight" runat="server" Font-Size="Larger" Font-Bold="true" Text='<%# ((LogbookEntryBase) Eval("Flight")).Date.ToShortDateString() %>' Target="_blank" NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}", ((LogbookEntryBase) Eval("Flight")).FlightID) %>'></asp:HyperLink>
                        <asp:Label ID="lblTail" Font-Size="Larger" Font-Bold="true" runat="server" Text='<%#: ((LogbookEntryBase) Eval("Flight")).TailNumDisplay %>'></asp:Label>
                        <asp:Label ID="lblRoute" Font-Bold="true" runat="server" Text='<%#: ((LogbookEntryBase) Eval("Flight")).Route %>'></asp:Label>
                        <asp:Label ID="lblComments" runat="server" Text='<%# ((LogbookEntryBase) Eval("Flight")).Comment.Linkify() %>'></asp:Label>
                    </div>
                    <ul>
                        <asp:Repeater ID="rptIssues" runat="server" DataSource='<%# Eval("Issues") %>'>
                            <ItemTemplate>
                                <li><%# Eval("IssueDescription") %></li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <p><asp:Label ID="lblNoIssues" runat="server" Text="<%$ Resources:FlightLint, CheckFlightsNoIssuesFound %>"></asp:Label></p>
        </EmptyDataTemplate>
        <AlternatingRowStyle CssClass="logbookAlternateRow" />
        <RowStyle CssClass="logbookRow" />
    </asp:GridView>
</asp:Content>
