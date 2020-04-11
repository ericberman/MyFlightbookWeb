<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbDonate.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbDonate" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<h2><asp:Label ID="lblDonateHeader" runat="server" Text="<%$ Resources:Preferences, DonateHeader %>" /></h2>
<asp:Panel ID="pnlPaypalSuccess" runat="server" Visible="False">
    <asp:Label ID="lblSuccess" runat="server" Text="<%$ Resources:Preferences, PaymentSuccess %>" Font-Bold="True" CssClass="success" />
</asp:Panel>
<asp:Panel ID="pnlPaypalCanceled" runat="server" Visible="False" EnableViewState="False">
    <asp:Label ID="lblCancelled" runat="server" Text="<%$ Resources:Preferences, PaymentCancelled %>" CssClass="error" />
</asp:Panel>
<p><asp:Label ID="lblDonatePrompt" runat="server" /></p>
<p><asp:Label ID="lblDonatePromptGratuity" runat="server" Text="<%$ Resources:LocalizedText, DonatePromptGratuity %>" /></p>
<table style="border-spacing: 0px; border-collapse: collapse;">
    <tr>
        <td></td>
        <td style="width: 5em; text-align: center; font-weight:bold;">US$10</td>
        <td style="width: 5em; text-align: center; font-weight:bold;">US$15</td>
        <td style="width: 5em; text-align: center; font-weight:bold;">US$25</td>
        <td style="width: 5em; text-align: center; font-weight:bold;">US$40</td>
        <td style="width: 5em; text-align: center; font-weight:bold;">US$75</td>
        <td style="width: 5em; text-align: center; font-weight:bold;">US$100</td>
    </tr>
    <asp:Repeater ID="rptAvailableGratuities" runat="server">
        <ItemTemplate>
            <tr>
                <td style="max-width: 250px; text-align:left; padding: 3px; border-bottom: 1px solid gray;"><%# Eval("Name") %>&nbsp;<uc1:mfbTooltip runat="server" ID="mfbTooltip" BodyContent='<%# Eval("Description") %>' />
                </td>
                <td style="border: 1px solid gray; padding: 3px; text-align:center;"><%# ((decimal)(Eval("Threshold"))) <= 10 ? "●" : string.Empty %></td>
                <td style="border: 1px solid gray; padding: 3px; text-align:center;"><%# ((decimal)(Eval("Threshold"))) <= 15 ? "●" : string.Empty %></td>
                <td style="border: 1px solid gray; padding: 3px; text-align:center;"><%# ((decimal)(Eval("Threshold"))) <= 25 ? "●" : string.Empty %></td>
                <td style="border: 1px solid gray; padding: 3px; text-align:center;"><%# ((decimal)(Eval("Threshold"))) <= 40 ? "●" : string.Empty %></td>
                <td style="border: 1px solid gray; padding: 3px; text-align:center;"><%# ((decimal)(Eval("Threshold"))) <= 75 ? "●" : string.Empty %></td>
                <td style="border: 1px solid gray; padding: 3px; text-align:center;"><%# ((decimal)(Eval("Threshold"))) <= 100 ? "●" : string.Empty %></td>
            </tr>
        </ItemTemplate>
    </asp:Repeater>
</table>
<div>&nbsp;</div>
<iframe id="iframeDonate" runat="server" style="border:none;" width="300" height="120"></iframe>
<div><asp:Label ID="lblDonateCrypto" runat="server" Text="<%$ Resources:Preferences, DonateCrypto %>" /> 
    <asp:HyperLink ID="lnkContact" runat="server" Text="<%$ Resources:Preferences, DonateCryptoContact %>" NavigateUrl="~/Public/ContactMe.aspx" /></div>
<h2><asp:Label ID="lblDonationHistory" runat="server" Text="<%$ Resources:Preferences, DonationHistoryHeader %>" /></h2>
<asp:Panel ID="pnlEarnedGratuities" runat="server" CssClass="callout" Visible="False">
    <p><asp:Label ID="lblThankYou" runat="server" Font-Bold="True" Text="<%$ Resources:Preferences, DonateThankYou %>" /></p>
    <p><asp:Localize ID="locEarnedGratuities" runat="server" Text="<%$ Resources:LocalizedText, GratuityEarnedHeader %>"></asp:Localize></p>
    <ul>
        <asp:Repeater ID="rptEarnedGratuities" runat="server">
            <ItemTemplate>
                <li><%# Eval("ThankYou") %></li>
            </ItemTemplate>
        </asp:Repeater>
    </ul>
</asp:Panel>
<asp:GridView ID="gvDonations" runat="server" CellPadding="4" ShowHeader="False" AutoGenerateColumns="False" GridLines="None">
    <Columns>
        <asp:BoundField DataField="TimeStamp" DataFormatString="{0:d}">
            <ItemStyle Font-Bold="True" />
        </asp:BoundField>
        <asp:BoundField DataField="Amount" DataFormatString="US${0}" />
        <asp:BoundField DataField="Notes" />
    </Columns>
    <EmptyDataTemplate>
        <p><asp:Label ID="lblNoDonations" runat="server" 
                Text="<%$ Resources:Preferences, DonateNoDonations %>" /></p>
    </EmptyDataTemplate>
</asp:GridView>
