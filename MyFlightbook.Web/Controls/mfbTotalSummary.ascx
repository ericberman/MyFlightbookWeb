<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbTotalSummary.ascx.cs" Inherits="MyFlightbook.Currency.mfbTotalSummary" %>
<asp:MultiView ID="mvTotals" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwFlat" runat="server">
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
                                    ResolveUrl("~/Member/LogbookNew.aspx?fq=" + ((string) Eval("JSONSerializedQuery")).ToSafeParameter())) %>'
                                     Target="_blank" ></asp:HyperLink>
                            </asp:View>
                            <asp:View ID="vwUnlinked" runat="server">
                                <asp:Label ID="lblDescription" runat="server" Text='<%#: Eval("Description") + ":" %>'></asp:Label>
                            </asp:View>
                        </asp:MultiView>
                        <asp:Label ID="lblSubDesc" runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("SubDescription")) %>' CssClass="fineprint"><br /><%#: Eval("SubDescription") %></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-VerticalAlign="Top">
                    <ItemTemplate>
                        <asp:Label ID="lblValue" runat="server" Text='<%# ((MyFlightbook.Currency.TotalsItem) Container.DataItem).ValueString(UseHHMM) %>' />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                <p><asp:Localize ID="locNoTotals" runat="server" Text="<%$ Resources:Totals, NoTotals %>"></asp:Localize></p>
            </EmptyDataTemplate>
        </asp:GridView>
    </asp:View>
    <asp:View ID="vwGrouped" runat="server">
        <asp:Repeater ID="rptGroups" runat="server">
            <ItemTemplate>
                <div class="header"><%# Eval("GroupName") %></div>
                <asp:Repeater ID="rptItems" runat="server" DataSource='<%# Eval("Items") %>'>
                    <ItemTemplate>
                        <div class="totalsBox">
                            <div class="totalsValue">
                                <%# ((MyFlightbook.Currency.TotalsItem) Container.DataItem).ValueString(UseHHMM) %>
                            </div>
                            <div>
                                <asp:MultiView ID="MultiView1" runat="server" ActiveViewIndex='<%# (LinkTotalsToQuery && Eval("Query") != null) ? 0 : 1 %>'>
                                    <asp:View ID="vwLinked" runat="server">
                                        <asp:HyperLink ID="lnkDescription" runat="server" Text='<%# Eval("Description") + ":" %>'
                                                NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}://{1}{2}", 
                                            Request.IsSecureConnection ? "https" : "http", 
                                            Branding.CurrentBrand.HostName, 
                                            ResolveUrl("~/Member/LogbookNew.aspx?fq=" + ((string)Eval("JSONSerializedQuery")).ToSafeParameter())) %>'
                                                Target="_blank" ></asp:HyperLink>
                                    </asp:View>
                                    <asp:View ID="vwUnlinked" runat="server">
                                        <asp:Label ID="lblDescription" runat="server" Text='<%#: Eval("Description") + ":" %>'></asp:Label>
                                    </asp:View>
                                </asp:MultiView>
                            </div>
                            <div><asp:Label ID="lblSubDesc" runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("SubDescription")) %>' CssClass="fineprint"><%#: Eval("SubDescription") %></asp:Label></div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </ItemTemplate>
        </asp:Repeater>
        <div><asp:Label ID="lblNoTotals" runat="server" Text="<%$ Resources:Totals, NoTotals %>"></asp:Label></div>
    </asp:View>
</asp:MultiView>
