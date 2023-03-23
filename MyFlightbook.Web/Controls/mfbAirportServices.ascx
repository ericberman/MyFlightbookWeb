<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbAirportServices.ascx.cs" Inherits="Controls_mfbAirportServices" %>
<asp:GridView ID="gvAirports" runat="server" GridLines="None" 
    ShowHeader="False" EnableModelValidation="True" CellPadding="5" 
    onrowdatabound="RowDataBound" AutoGenerateColumns="False">
    <Columns>
        <asp:TemplateField>
            <ItemTemplate>
                <asp:Image ID="imgAirport" runat="server" ImageUrl='<%# ((MyFlightbook.Airports.airport) Container.DataItem).IsPort ? "~/images/airport.png" : "~/images/tower.png" %>' />&nbsp;&nbsp;
                <asp:MultiView ID="mvAirportName" runat="server">
                    <asp:View ID="vwDynamic" runat="server">
                        <asp:HyperLink ID="lnkZoom" runat="server">
                            <%#: Eval("FullName") %>
                        </asp:HyperLink>
                    </asp:View>
                    <asp:View ID="vwStatic" runat="server">
                        <%#: Eval("FullName") %>
                    </asp:View>
                </asp:MultiView>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemTemplate>
                <asp:HyperLink runat="server" Visible ='<%# false && ((MyFlightbook.Airports.airport) Container.DataItem).Country.CompareCurrentCultureIgnoreCase("United States") == 0 %>' Text="<%$ Resources:LocalizedText, AirportServiceGuide %>"
                    NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://pirep.io/airports/{0}", ((MyFlightbook.Airports.airport) Container.DataItem).Code) %>' Target="_blank" />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:HyperLinkField Text="<%$ Resources:LocalizedText, AirportServiceAirportInformation %>" DataNavigateUrlFormatString="https://acukwik.com/Airport-Info/{0}" DataNavigateUrlFields="Code" Target="_blank" />
        <asp:HyperLinkField Text="<%$ Resources:LocalizedText, AirportServiceFBO %>" DataNavigateUrlFields="Code" DataNavigateUrlFormatString="https://www.aopa.org/destinations/airports/{0}/details?q=kpae&public=0#fbos" Target="_blank" />
        <asp:HyperLinkField Text="<%$ Resources:LocalizedText, AirportServiceMetar %>" DataNavigateUrlFields="Code" DataNavigateUrlFormatString="https://www.checkwx.com/weather/{0}" Target="_blank" />
        <asp:TemplateField>
            <ItemTemplate>
                <asp:HyperLink ID="lnkHotels" Target="_blank" runat="server"></asp:HyperLink>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>

