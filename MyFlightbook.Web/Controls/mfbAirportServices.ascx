<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbAirportServices" Codebehind="mfbAirportServices.ascx.cs" %>
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
                            <%# Eval("FullName") %>
                        </asp:HyperLink>
                    </asp:View>
                    <asp:View ID="vwStatic" runat="server">
                        <%# Eval("FullName") %>
                    </asp:View>
                </asp:MultiView>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:HyperLinkField Text="<%$ Resources:LocalizedText, AirportServiceAirportInformation %>" DataNavigateUrlFormatString="https://acukwik.com/Airport-Info/{0}" DataNavigateUrlFields="Code" />
        <asp:HyperLinkField Text="<%$ Resources:LocalizedText, AirportServiceFBO %>" DataNavigateUrlFields="Code" DataNavigateUrlFormatString="http://www.aopa.org/airports/{0}#businesses" />
        <asp:HyperLinkField Text="<%$ Resources:LocalizedText, AirportServiceMetar %>" DataNavigateUrlFields="Code" DataNavigateUrlFormatString="http://www.checkwx.com/weather/{0}" />
        <asp:TemplateField>
            <ItemTemplate>
                <asp:HyperLink ID="lnkHotels" Target="_blank" runat="server"></asp:HyperLink>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</asp:GridView>

