<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbTotalSummary.ascx.cs" Inherits="Controls_mfbTotalSummary" %>
<asp:GridView ID="gvTotals" runat="server" AutoGenerateColumns="False" 
    ShowHeader="False" GridLines="None" CellPadding="3" >
    <Columns>
        <asp:TemplateField ItemStyle-HorizontalAlign="Left" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:MultiView ID="MultiView1" runat="server" ActiveViewIndex='<%# (LinkTotalsToQuery && Eval("Query") != null) ? 0 : 1 %>'>
                    <asp:View ID="vwLinked" runat="server">
                        <asp:HyperLink ID="lnkDescription" runat="server" Text='<%# Eval("Description") + ":" %>'
                             NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}://{1}{2}", 
                            Request.IsSecureConnection ? "https" : "http", 
                            Branding.CurrentBrand.HostName, 
                            ResolveUrl("~/Member/LogbookNew.aspx?fq=" + HttpUtility.UrlEncode(Convert.ToBase64String(Eval("JSONSerializedQuery").ToString().Compress())))) %>'
                             Target="_blank" ></asp:HyperLink>
                    </asp:View>
                    <asp:View ID="vwUnlinked" runat="server">
                        <asp:Label ID="lblDescription" runat="server" Text='<%# Eval("Description") + ":" %>'></asp:Label>
                    </asp:View>
                </asp:MultiView>
                <asp:Label ID="lblSubDesc" runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("SubDescription")) %>' CssClass="fineprint"><br /><%# Eval("SubDescription") %></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:Label ID="lblValue" runat="server" Text='<%# ((MyFlightbook.FlightCurrency.TotalsItem) Container.DataItem).ValueString(UseHHMM) %>' />
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <p><asp:Localize ID="locNoTotals" runat="server" Text="<%$ Resources:Totals, NoTotals %>"></asp:Localize></p>
    </EmptyDataTemplate>
</asp:GridView>
