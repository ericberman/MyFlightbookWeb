<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="DutyPeriodAnalyzer.aspx.cs" Inherits="MyFlightbook.Web.PlayPen.DutyPeriodAnalyzer" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    Analyze Duty Periods
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnViewDutyPeriods">
        <p>View duty stats since <uc1:mfbDecimalEdit runat="server" EditingMode="Integer" IntValue="30" DefaultValueInt="30" ID="decDayTimeSpan" /> days ago (Same UTC time as now) <asp:Button ID="btnViewDutyPeriods" runat="server" Text="Go" OnClick="btnViewDutyPeriods_Click" />
        </p>
    </asp:Panel>
    <asp:Panel ID="pnlResults" runat="server" Visible="false">
        <table>
            <tr><td>Current UTC Time:</td><td style="font-weight:bold"><% = DateTime.UtcNow.UTCDateFormatString() %></td></tr>
            <tr><td>Period start date/time: </td><td style="font-weight:bold"><asp:Label ID="lblCutoffDate" runat="server"></asp:Label></td></tr>
            <tr><td>Total Flight Duty: </td><td style="font-weight:bold"><asp:Label ID="lblTotalFlightDuty" runat="server"></asp:Label></td></tr>
            <tr><td>Total Duty (non-rest): </td><td style="font-weight:bold"><asp:Label ID="lblTotalDuty" runat="server"></asp:Label></td></tr>
            <tr><td>Total Rest: </td><td style="font-weight:bold"><asp:Label ID="lblTotalRest" runat="server"></asp:Label></td></tr>
        </table>
        <h2>All duty periods</h2>
        <asp:GridView ID="gvDutyPeriods" runat="server" CellPadding="3" AutoGenerateColumns="false">
            <Columns>
                <asp:TemplateField HeaderText="FDP Start" SortExpression="FlightDutyStart">
                    <ItemTemplate>
                        <%# ((DateTime) Eval("FlightDutyStart")).UTCFormattedStringOrEmpty(false) %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="FDPEnd" SortExpression="FlightDutyEnd">
                    <ItemTemplate>
                        <%# ((DateTime) Eval("FlightDutyEnd")).UTCFormattedStringOrEmpty(false) %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Elapsed Flight Duty" SortExpression="ElapsedFlightDuty">
                    <ItemTemplate>
                        <%# ((double) Eval("ElapsedFlightDuty")).ToString("#,##0.#", System.Globalization.CultureInfo.CurrentCulture) %> (<%# ((decimal) ((double) Eval("ElapsedFlightDuty"))).ToHHMM() %>)
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderText="Duty Start" SortExpression="AdditionalDutyStart" DataField="AdditionalDutyStart" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Duty End" SortExpression="AdditionalDutyEnd" DataField="AdditionalDutyEnd" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Non-rest Start" SortExpression="EffectiveDutyStart" DataField="EffectiveDutyStart" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Non-rest End" SortExpression="EffectiveDutyEnd" DataField="EffectiveDutyEnd" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:TemplateField HeaderText="Elapsed NonRest" SortExpression="NonRestTime">
                    <ItemTemplate>
                        <%# ((double) Eval("NonRestTime")).ToString("#,##0.#", System.Globalization.CultureInfo.CurrentCulture) %> (<%# ((decimal) ((double) Eval("NonRestTime"))).ToHHMM() %>)
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Rest since end of period" SortExpression="RestSince">
                    <ItemTemplate>
                        <%# ((double) Eval("RestSince")).ToString("#,##0.#", System.Globalization.CultureInfo.CurrentCulture) %> (<%# ((decimal) ((double) Eval("RestSince"))).ToHHMM() %>)
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Content>

