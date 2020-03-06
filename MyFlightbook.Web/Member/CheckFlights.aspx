<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="CheckFlights.aspx.cs" Inherits="MyFlightbook.Web.Member.CheckFlights" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>


<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="locHeader" runat="server" Text="<%$ Resources:FlightLint, TitleCheckFlights %>"></asp:Localize></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <h2><asp:Label ID="lblBeta" runat="server" Font-Size="Large" Text="<%$ Resources:LocalizedText, Beta %>"></asp:Label> <asp:Label ID="Label1" runat="server" Text="<%$ Resources:FlightLint, TitleCheckFlights %>"></asp:Label></h2>
    <p><asp:Label ID="lblCheckFlightsDescription" runat="server" Text="<%$ Resources:FlightLint, CheckFlightsDescription1 %>"></asp:Label></p>
    <p><asp:Label ID="lblCheckFlightsCategories" runat="server"></asp:Label></p>
    <div>
        <div><asp:CheckBox ID="ckSim" runat="server" Text="<%$ Resources:FlightLint, LintCategorySim %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip" BodyContent="<%$ Resources:FlightLint, LintCategorySimTip %>" /></div>
        <div><asp:CheckBox ID="ckIFR" runat="server" Text="<%$ Resources:FlightLint, LintCategoryIFR %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip1" BodyContent="<%$ Resources:FlightLint, LintCategoryIFRTip %>" /></div>
        <div><asp:CheckBox ID="ckAirports" runat="server" Text="<%$ Resources:FlightLint, LintCategoryAirports %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip2" BodyContent="<%$ Resources:FlightLint, LintCategoryAirportsTip %>" /></div>
        <div><asp:CheckBox ID="ckXC" runat="server" Text="<%$ Resources:FlightLint, LintCategoryCrossCountry %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip3" BodyContent="<%$ Resources:FlightLint, LintCategoryCrossCountryTip %>" /></div>
        <div><asp:CheckBox ID="ckPICSICDualMath" runat="server" Text="<%$ Resources:FlightLint, LintCategoryPICSICDualMath %>" /><uc1:mfbtooltip runat="server" id="mfbTooltip4" BodyContent="<%$ Resources:FlightLint, LintCategoryPICSICDualMathTip %>" /></div>
        <div><asp:CheckBox ID="ckTimes" runat="server" Text="<%$ Resources:FlightLint, LintCategoryTimes %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip5" BodyContent="<%$ Resources:FlightLint, LintCategoryTimesTip %>" /></div>
        <div><asp:CheckBox ID="ckDateTime" runat="server" Text="<%$ Resources:FlightLint, LintCategoryDateTime %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip6" BodyContent="<%$ Resources:FlightLint, LintCategoryDateTimeTip %>" /></div>
        <div><asp:CheckBox ID="ckMisc" runat="server" Text="<%$ Resources:FlightLint, LintCategoryMisc %>" Checked="true" /><uc1:mfbtooltip runat="server" id="mfbTooltip7" BodyContent="<%$ Resources:FlightLint, LintCategoryMiscTip %>" /></div>
    </div>
    <asp:Button ID="btnCheckAll" runat="server" Text="<%$ Resources:FlightLint, CheckFlightsBegin %>" OnClick="btnCheckAll_Click" />
    <div><asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="false" ></asp:Label></div>
    <asp:GridView ID="gvFlights" runat="server" ShowHeader="false" GridLines="None" AutoGenerateColumns="false" Font-Size="8pt">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div>
                        <asp:HyperLink ID="lnkEditFlight" runat="server" Font-Size="Larger" Font-Bold="true" Text='<%# ((LogbookEntryBase) Eval("Flight")).Date.ToShortDateString() %>' Target="_blank" NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}", ((LogbookEntryBase) Eval("Flight")).FlightID) %>'></asp:HyperLink>
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
