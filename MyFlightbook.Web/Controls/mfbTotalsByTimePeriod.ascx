<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbTotalsByTimePeriod.ascx.cs" Inherits="MyFlightbook.Web.Controls.mfbTotalsByTimePeriod" %>
<asp:MultiView ID="mvTotals" runat="server">
    <asp:View ID="vwNoTotals" runat="server">
        <p><asp:Localize ID="locNoTotals" runat="server" Text="<%$ Resources:Totals, NoTotals %>"></asp:Localize></p>
    </asp:View>
    <asp:View ID="vwTotals" runat="server">
        <asp:Table ID="tblTotals" runat="server" CssClass="totalsByTimeTable" ></asp:Table>
    </asp:View>
</asp:MultiView>

