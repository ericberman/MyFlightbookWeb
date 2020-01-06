<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_METAR" Codebehind="METAR.ascx.cs" %>
<asp:GridView ID="gvMetar" runat="server" GridLines="None" AutoGenerateColumns="false" OnRowDataBound="gvMetar_RowDataBound" CellPadding="5" ShowFooter="false">
    <Columns>
        <asp:TemplateField>
            <ItemTemplate>
                <div><asp:Label ID="lblStation" runat="server" ToolTip='<%# Eval("raw_text") %>' Font-Bold="true" Font-Size="Larger" Text='<%# Eval("station_id") %>'></asp:Label></div> 
                <div><asp:Label ID="lblTime" runat="server" Text='<%# Eval("TimeDisplay") %>'></asp:Label></div>
                <div><asp:Label ID="lblType" runat="server" Text='<%# Eval("METARTypeDisplay") %>'></asp:Label></div>
                <div><asp:Label ID="lblQuality" runat="server" Text='<%# Eval("QualityDisplay") %>'></asp:Label></div>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField  HeaderText="<%$ Resources:Weather, headerCategory %>">
            <ItemTemplate>
                <asp:Label ID="lblFlightCategory" runat="server" Text='<%# Eval("flight_category") %>' Font-Bold="true" ForeColor='<%# ColorForFlightRules((MyFlightbook.Weather.ADDS.METAR) Container.DataItem) %>'></asp:Label> 
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Weather, headerWind %>">
            <ItemTemplate>
                <asp:Panel ID="pnlWind" runat="server" Visible='<%# Eval("wind_dir_degreesSpecified") %>' style='<%# WindVectorInlineStyle((MyFlightbook.Weather.ADDS.METAR) Container.DataItem) %>'>
                    <asp:Image ID="imgWindVector" ImageUrl="~/images/windvector.png" ToolTip='<%# String.Format(Resources.Weather.wind_dir_degreesField, Eval("wind_dir_degrees")) %>' AlternateText='<%# String.Format(Resources.Weather.wind_dir_degreesField, Eval("wind_dir_degrees")) %>' runat="server"  /></asp:Panel>
                <asp:Label ID="lblWindDir" runat="server" Text='<%# Eval("WindDirDisplay") %>'></asp:Label> 
                <asp:Label ID="lblWindSpeed" runat="server" Text='<%# String.Format(Resources.Weather.wind_speed_nolabel, Eval("wind_speed_kt")) %>' Visible='<%# Eval("wind_speed_ktSpecified") %>'></asp:Label> 
                <asp:Label ID="lblWindGust" runat="server" Text='<%# String.Format(Resources.Weather.wind_gust_ktField, Eval("wind_gust_kt")) %>' Visible='<%# Eval("wind_gust_ktSpecified") %>'></asp:Label> 
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Weather, headerVisibility %>">
            <ItemTemplate>
                <asp:Label ID="lblVisibility" runat="server" Text='<%# Eval("VisibilityDisplay") %>'></asp:Label>  
                <asp:Label ID="lblVertVis" runat="server" Text='<%# String.Format(Resources.Weather.altitudeFtFormat, Eval("vert_vis_ft")) %>' Visible='<%# Eval("vert_vis_ftSpecified") %>'></asp:Label>  
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Weather, headerCeiling %>">
            <ItemTemplate>
                <asp:Repeater ID="rptSkyConditions" runat="server">
                    <ItemTemplate>
                        <div>
                            <asp:Label ID="lblSkyCover" runat="server" Text='<%# Eval("SkyCoverDisplay") %>'></asp:Label> 
                            <asp:Label ID="lblBase" runat="server" Text='<%# String.Format(Resources.Weather.cloud_base_ft_aglField, Eval("cloud_base_ft_agl")) %>' Visible='<%# Eval("cloud_base_ft_aglSpecified") %>'></asp:Label>
                        </div>
                    </ItemTemplate>
                </asp:Repeater> 
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:BoundField DataField="TempAndDewpointDisplay" HeaderText="<%$ Resources:Weather, headerTemp %>" ItemStyle-VerticalAlign="Top" />
        <asp:BoundField DataField="AltitudeHgDisplay" HeaderText="<%$ Resources:Weather, headerAltimeter %>" ItemStyle-VerticalAlign="Top" />
        <asp:BoundField DataField="wx_string" ItemStyle-VerticalAlign="Top" />
    </Columns>
    <EmptyDataTemplate>
        <p><asp:Label ID="lblNoWeather" runat="server" Text="<%$ Resources:Weather, NoMetarsFound %>"></asp:Label></p>
    </EmptyDataTemplate>
</asp:GridView>
