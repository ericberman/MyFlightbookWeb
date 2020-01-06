<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSearchAndTotals" Codebehind="mfbSearchAndTotals.ascx.cs" %>
<%@ Register src="mfbSimpleTotals.ascx" tagname="mfbSimpleTotals" tagprefix="uc1" %>
<%@ Register src="mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc2" %>
<%@ Register src="mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc3" %>
<%@ Register src="mfbCurrency.ascx" tagname="mfbCurrency" tagprefix="uc5" %>
<%@ Register src="mfbQueryDescriptor.ascx" tagname="mfbQueryDescriptor" tagprefix="uc7" %>
<asp:MultiView ID="mvQuery" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwTotals" runat="server">
        <div style="display: inline-block; vertical-align:top; padding: 1px;">
            <uc3:mfbTotalSummary ID="mfbTotalSummary1" runat="server" />
        </div>
        <div style="display: inline-block; vertical-align:top; padding: 1px;">
            <p class="header">
                <asp:Localize ID="locHeader" runat="server" Text="<%$ Resources:LocalizedText, SearchAndTotalsHeader %>"></asp:Localize></p>
            <uc7:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" ShowEmptyFilter="true" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
            <p class="noprint" style="text-align:right">
                <asp:Button ID="btnEditQuery" runat="server" onclick="btnEditQuery_Click" Text="Change Query" />
            </p>
        </div>
    </asp:View>
    <asp:View ID="vwQueryForm" runat="server">
        <uc2:mfbSearchForm ID="mfbSearchForm1" runat="server" OnQuerySubmitted="ShowResults" OnReset="ClearForm" />
    </asp:View>
</asp:MultiView>