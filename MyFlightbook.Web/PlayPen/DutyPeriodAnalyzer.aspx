<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="DutyPeriodAnalyzer.aspx.cs" Inherits="MyFlightbook.Web.PlayPen.DutyPeriodAnalyzer" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>


<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    Analyze Duty Periods
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnViewDutyPeriods">
        <p>View duty stats since <uc1:mfbDecimalEdit runat="server" EditingMode="Integer" DefaultValueInt="30" ID="decDayTimeSpan" /> days ago (Same UTC time as now) <asp:Button ID="btnViewDutyPeriods" runat="server" Text="Go" OnClick="btnViewDutyPeriods_Click" />
        </p>
    </asp:Panel>
    <asp:Panel ID="pnlResults" runat="server" Visible="false">
        <div><asp:Label ID="lblCutoffDate" runat="server"></asp:Label></div>
        <div><asp:Label ID="lblTotalFlightDuty" runat="server"></asp:Label></div>
        <div><asp:Label ID="lblTotalDuty" runat="server"></asp:Label></div>
        <div><asp:Label ID="lblTotalRest" runat="server"></asp:Label></div>
    
        <h2>All duty periods</h2>
        <asp:GridView ID="gvDutyPeriods" runat="server" CellPadding="3" AutoGenerateColumns="false">
            <Columns>
                <asp:BoundField HeaderText="FDP Start" SortExpression="FlightDutyStart" DataField="FlightDutyStart" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="FDP End" SortExpression="FlightDutyEnd" DataField="FlightDutyEnd" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Elapsed Flight Duty" SortExpression="ElapsedFlightDuty" DataField="ElapsedFlightDuty" DataFormatString="{0:#,##0.0}" />
                <asp:BoundField HeaderText="Duty Start" SortExpression="AdditionalDutyStart" DataField="AdditionalDutyStart" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Duty End" SortExpression="AdditionalDutyEnd" DataField="AdditionalDutyEnd" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Non-rest Start" SortExpression="EffectiveDutyStart" DataField="EffectiveDutyStart" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Non-rest End" SortExpression="EffectiveDutyEnd" DataField="EffectiveDutyEnd" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                <asp:BoundField HeaderText="Elapsed NonRest" SortExpression="NonRestTime" DataField="NonRestTime" DataFormatString="{0:#,##0.0}" />
                <asp:BoundField HeaderText="Rest since end of duty" SortExpression="RestSince" DataField="RestSince" DataFormatString="{0:#,##0.0}" />
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Content>

