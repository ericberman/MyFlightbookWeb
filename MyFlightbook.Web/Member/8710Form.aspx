<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_8710Form" culture="auto" meta:resourcekey="PageResource1" Codebehind="8710Form.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc6" %>
<%@ Register src="../Controls/mfbSearchAndTotals.ascx" tagname="mfbSearchAndTotals" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbQueryDescriptor.ascx" tagname="mfbQueryDescriptor" tagprefix="uc3" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Label ID="lblUserName" runat="server" 
            meta:resourcekey="lblUserNameResource1"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvQuery" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwTotals" runat="server">
            <p class="header"><asp:Localize ID="loc8710DiclaimerHeader" runat="server" 
                    Text="Totals are computed using the criteria below:" 
                    meta:resourcekey="loc8710DiclaimerHeaderResource1"></asp:Localize></p>
            <uc3:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" ShowEmptyFilter="true" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
            <p class="noprint">
                <asp:Button ID="btnEditQuery" runat="server" onclick="btnEditQuery_Click" 
                    Text="Change Query" meta:resourcekey="btnEditQueryResource1" />
            </p>    
            <div>
                <asp:RadioButtonList ID="rblReport" runat="server" AutoPostBack="True" RepeatDirection="Horizontal" OnSelectedIndexChanged="rblReport_SelectedIndexChanged" meta:resourcekey="rblReportResource1">
                    <asp:ListItem Value="0" Text="8710 (IACRA) Report" Selected="True" meta:resourcekey="ListItemResource1"></asp:ListItem>
                    <asp:ListItem Value="1" Text="Rollup by model" meta:resourcekey="ListItemResource2"></asp:ListItem>
                </asp:RadioButtonList>
            </div>
            <asp:MultiView ID="mvReport" runat="server" ActiveViewIndex="0">
                <asp:View ID="vw8710" runat="server">
                    <asp:GridView ID="gv8710" runat="server" Font-Size="8pt" CellPadding="0" OnRowDataBound="gv8710_RowDataBound"
                        AutoGenerateColumns="False" meta:resourcekey="gv8710Resource1">
                        <Columns>
                            <asp:BoundField DataField="Category" meta:resourcekey="BoundFieldResource1">
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center"></ItemStyle>
                            </asp:BoundField>
                            <asp:TemplateField HeaderText="Total" meta:resourcekey="TemplateFieldResource1">
                                <ItemTemplate>
                                    <%# Eval("TotalTime").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Instruc-<br />tion<br />Received" 
                                meta:resourcekey="TemplateFieldResource2">
                                <ItemTemplate>
                                    <%# Eval("InstructionReceived").FormatDecimal(UseHHMM)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Solo" meta:resourcekey="TemplateFieldResource3">
                                <ItemTemplate>
                                    <%# Eval("SoloTime").FormatDecimal(UseHHMM)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="PIC<br />and<br />SIC" 
                                meta:resourcekey="TemplateFieldResource4">
                                <ItemTemplate>
                                    <table cellpadding="3px" cellspacing="0" style="width: 100%; font-size:8pt">
                                        <tr>
                                            <td style="border-bottom: 1px solid black;">
                                                <asp:Localize ID="locPICMain" runat="server" Text="PIC" meta:resourcekey="locPICMainResource1"></asp:Localize></td>
                                            <td style="border-bottom: 1px solid black;"><%# Eval("PIC").FormatDecimal(UseHHMM)%>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:Localize ID="locSICHeaderPIC" runat="server" Text="SIC" meta:resourcekey="locSICHeaderPICResource1"></asp:Localize>
                                            </td>
                                            <td>
                                                <%# Eval("SIC").FormatDecimal(UseHHMM)%>
                                            </td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource5">
                                <HeaderTemplate>
                                    <asp:Localize ID="locCrossCountryDualHeader" runat="server" 
                                        meta:resourcekey="locCrossCountryDualHeaderResource1" 
                                        Text="Cross&lt;br /&gt;Country&lt;br /&gt;Instruc-&lt;br /&gt;tion&lt;br /&gt;Received"></asp:Localize><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("CrossCountryDual").FormatDecimal(UseHHMM)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource6">
                                <HeaderTemplate>
                                    <asp:Localize ID="locXCSoloHeader" runat="server" 
                                        meta:resourcekey="locXCSoloHeaderResource1" 
                                        Text="Cross&lt;br /&gt;Country&lt;br /&gt;Solo"></asp:Localize><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("CrossCountrySolo").FormatDecimal(UseHHMM)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource7" >
                                <HeaderTemplate>
                                    <asp:Localize ID="locXCPICHeader" runat="server" 
                                        Text="Cross&lt;br /&gt;Country&lt;br /&gt;PIC/SIC" 
                                        meta:resourcekey="locXCPICHeaderResource1"></asp:Localize><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <table cellpadding="3px" cellspacing="0" style="width: 100%; font-size:8pt">
                                        <tr>
                                            <td style="border-bottom: 1px solid black;">
                                                <asp:Localize ID="locPICHeader" runat="server" Text="PIC" 
                                                    meta:resourcekey="locPICHeaderResource1"></asp:Localize></td>
                                             <td style="border-bottom: 1px solid black;"><%# Eval("CrossCountryPIC").FormatDecimal(UseHHMM)%>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:Localize ID="locSICHeaderXCPIC" runat="server" Text="SIC" meta:resourcekey="locSICHeaderXCPICResource1"></asp:Localize></td>
                                            <td><%# Eval("CrossCountrySIC").FormatDecimal(UseHHMM)%>
                                            </td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource8">
                                <HeaderTemplate>
                                    <asp:Localize ID="locInstrumentHeader" runat="server" Text="Instru-&lt;br /&gt;ment" 
                                        meta:resourcekey="locInstrumentHeaderResource1"></asp:Localize><span class="FootNote">2</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("InstrumentTime").FormatDecimal(UseHHMM)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource9">
                                <HeaderTemplate>
                                    <asp:Localize ID="locNightDual" runat="server" 
                                        Text="Night&lt;br /&gt;Instruc-&lt;br /&gt;tion&lt;br /&gt;Received" 
                                        meta:resourcekey="locNightDualResource1"></asp:Localize><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("NightDual").FormatDecimal(UseHHMM)%>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Night<br />Take-off/<br />Landings" 
                                meta:resourcekey="TemplateFieldResource10">
                                <ItemTemplate>
                                    <%# Eval("NightTakeoffs") %>
                                    /
                                    <%# Eval("NightLandings") %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle HorizontalAlign="Center" />
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource11">
                                <HeaderTemplate><asp:Localize ID="locNightPICHeader" runat="server" 
                                        Text="Night PIC/SIC" meta:resourcekey="locNightPICHeaderResource1"></asp:Localize><span class="FootNote">1</span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <table cellpadding="3px" cellspacing="0" style="width: 100%; font-size:8pt">
                                        <tr>
                                            <td style="border-bottom: 1px solid black;">
                                                <asp:Localize ID="locPICNight" runat="server" Text="PIC" 
                                                    meta:resourcekey="locPICNightResource1"></asp:Localize>
                                            </td>
                                            <td style="border-bottom: 1px solid black;">
                                                <%# Eval("NightPIC").FormatDecimal(UseHHMM)%>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:Localize ID="locSICNight" runat="server" Text="SIC" 
                                                    meta:resourcekey="locSICNightResource1"></asp:Localize>
                                            </td>
                                            <td>
                                                <%# Eval("NightSIC").FormatDecimal(UseHHMM)%>       
                                            </td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource12">
                                <HeaderTemplate><asp:Localize ID="locNightTakeoffLandingHeader" runat="server" 
                                        Text="Night&lt;br /&gt;Take-off/&lt;br /&gt;Landing PIC/SIC" 
                                        meta:resourcekey="locNightTakeoffLandingHeaderResource1"></asp:Localize><span class="FootNote">3</span></HeaderTemplate>
                                <ItemTemplate>
                                    <table cellpadding="3px" cellspacing="0" style="width: 100%; font-size:8pt">
                                        <tr>
                                            <td style="border-bottom: 1px solid black;">
                                                <asp:Localize ID="locNightLandingPIC" runat="server" Text="PIC" 
                                                    meta:resourcekey="locNightLandingPICResource1"></asp:Localize>
                                            </td>
                                            <td style="border-bottom: 1px solid black;">
                                                <%# Eval("NightPICTakeoffs") %>&nbsp;/&nbsp;<%# Eval("NightPICLandings") %></td>
                                        </tr>
                                        <tr>
                                            <td>
                                                <asp:Localize ID="locNightLandingSIC" runat="server" Text="SIC" 
                                                    meta:resourcekey="locNightLandingSICResource1"></asp:Localize>
                                            </td>
                                            <td>
                                                <%# Eval("NightSICTakeoffs") %>&nbsp;/&nbsp;<%# Eval("NightSICLandings") %></td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                            </asp:TemplateField>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource13" HeaderText="Class Totals">
                                <ItemTemplate>
                                    <asp:Panel ID="pnlClassTotals" runat="server" Visible="False" style="width:100%;" meta:resourcekey="pnlClassTotalsResource1">
                                        <table style="border-collapse:collapse; width: 100%; font-size:smaller">
                                            <tr>
                                                <td style="border-right: 1px solid darkgray;"></td>
                                                <td style="border-right: 1px solid darkgray;"><% =Resources.LogbookEntry.FieldTotal %></td>
                                                <td style="border-right: 1px solid darkgray;"><% =Resources.LogbookEntry.FieldPIC %></td>
                                                <td><% =Resources.LogbookEntry.FieldSIC %></td>
                                            </tr>
                                            <asp:Repeater ID="rptClassTotals" runat="server">
                                                <ItemTemplate>
                                                    <tr>
                                                        <td style="border-right: 1px solid darkgray; border-top: 1px solid darkgray;"><%# Eval("ClassName") %></td>
                                                        <td style="border-right: 1px solid darkgray; border-top: 1px solid darkgray;"><%# Eval("Total").FormatDecimal(UseHHMM) %></td>
                                                        <td style="border-right: 1px solid darkgray; border-top: 1px solid darkgray;"><%# Eval("PIC").FormatDecimal(UseHHMM) %></td>
                                                        <td style="border-top: 1px solid darkgray;"><%# Eval("SIC").FormatDecimal(UseHHMM) %></td>
                                                    </tr>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </table>
                                    </asp:Panel>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="# of..." 
                                meta:resourcekey="BoundFieldResource2">
                                <ItemTemplate>
                                    <div><%# (Convert.ToInt32(Eval("NumberOfFlights")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText._8710NumberFlights, Eval("NumberOfFlights")) : string.Empty %></div>
                                    <div><%# (Convert.ToInt32(Eval("AeroTows")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.GliderAeroTows, Eval("AeroTows")) : string.Empty %></div>
                                    <div><%# (Convert.ToInt32(Eval("WinchedLaunches")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.GliderGroundLaunches, Eval("WinchedLaunches")) : string.Empty %></div>
                                    <div><%# (Convert.ToInt32(Eval("SelfLaunches")) > 0) ? String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.GliderSelfLaunches, Eval("SelfLaunches")) : string.Empty %></div>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell" />
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center" />
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <p><asp:Label ID="lblNoMatchingFlights" CssClass="error" runat="server" Text="<%$ Resources:LocalizedText, errNoMatchingFlightsFound %>" meta:resourcekey="lblNoMatchingFlightsResource1"></asp:Label></p>
                        </EmptyDataTemplate>
                    </asp:GridView>                    
                    <p><asp:Localize ID="loc8710Notes" runat="server" Text="Notes:" 
                            meta:resourcekey="loc8710NotesResource1"></asp:Localize></p>
                    <p><span class="FootNote">1</span><asp:Localize ID="loc8710Footnote1" 
                            runat="server" 
                            Text="To determine the amount of time where two fields are used, the minimum of the two fields is used.  For example, night PIC for a flight is the smaller of the amount of PIC or Night flying that is logged." 
                            meta:resourcekey="loc8710Footnote1Resource1"></asp:Localize></p>
                    <p><span class="FootNote">2</span><asp:Localize ID="loc8710Footnote2" 
                            runat="server" 
                            Text="Instrument time comprises both actual and simulated instrument time." 
                            meta:resourcekey="loc8710Footnote2Resource1"></asp:Localize></p>
                    <p><span class="FootNote">3</span><asp:Localize ID="loc8710Footnote3" 
                            runat="server" 
                            Text="If a flight has night-time take-offs/landings and PIC (SIC) time, then it is assumed that those landings are done while acting as PIC (SIC)." 
                            meta:resourcekey="loc8710Footnote3Resource1"></asp:Localize></p>
                </asp:View>
                <asp:View ID="vwRollup" runat="server">
                    <asp:GridView ID="gvRollup" runat="server" AutoGenerateColumns="False" Font-Size="8pt" CellPadding="0" meta:resourcekey="gvRollupResource1">
                        <Columns>
                            <asp:TemplateField meta:resourcekey="TemplateFieldResource15">
                                <ItemTemplate>
                                    <%# ModelDisplay(Container.DataItem) %>
                                </ItemTemplate>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldDual %>" meta:resourcekey="TemplateFieldResource17">
                                <ItemTemplate>
                                    <%# Eval("DualReceived").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="Flight<br />Eng." meta:resourcekey="TemplateFieldResource21">
                                <ItemTemplate>
                                    <%# Eval("flightengineer").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="Military" meta:resourcekey="TemplateFieldResource22">
                                <ItemTemplate>
                                    <%# Eval("MilTime").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldCFI %>" meta:resourcekey="TemplateFieldResource23">
                                <ItemTemplate>
                                    <%# Eval("CFI").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldNight %>" meta:resourcekey="TemplateFieldResource24">
                                <ItemTemplate>
                                    <%# Eval("Night").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldIMC %>" meta:resourcekey="TemplateFieldResource25">
                                <ItemTemplate>
                                    <%# Eval("IMC").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldSimIMC %>" meta:resourcekey="TemplateFieldResource26">
                                <ItemTemplate>
                                    <%# Eval("SimIMC").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText ="<%$ Resources:LogbookEntry, FieldXCountry %>" meta:resourcekey="TemplateFieldResource27">
                                <ItemTemplate>
                                    <%# Eval("XC").FormatDecimal(UseHHMM) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:LogbookEntry, FieldLanding %>" meta:resourcekey="TemplateFieldResource29">
                                <ItemTemplate>
                                    <%# Eval("landings").FormatInt() %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Apprch<br />(All/6/12)<br />Month" meta:resourcekey="TemplateFieldResource30">
                                <ItemTemplate>
                                    <%# FormatMultiInt(" / ", Eval("approaches"), Eval("_6MonthApproaches"), Eval("_12MonthApproaches")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="(PIC/SIC)" meta:resourcekey="TemplateFieldResource16">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("PIC"), Eval("SIC")) %>
                                </ItemTemplate>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="TurboProp<br />(PIC/SIC)" meta:resourcekey="TemplateFieldResource18">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("TurboPropPIC"), Eval("TurboPropSIC")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Jet<br />(PIC/SIC)" meta:resourcekey="TemplateFieldResource19">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("JetPIC"), Eval("JetSIC")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Multi<br />(PIC/SIC)" meta:resourcekey="TemplateFieldResource20">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("MultiPIC"), Eval("MultiSIC")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Total<br />(All//12/24)<br />Month" meta:resourcekey="TemplateFieldResource28">
                                <ItemTemplate>
                                    <%# FormatMultiDecimal(UseHHMM, " / ", Eval("Total"), Eval("_12MonthTotal"), Eval("_24MonthTotal")) %>
                                </ItemTemplate>
                                <HeaderStyle CssClass="PaddedCell"></HeaderStyle>
                                <ItemStyle CssClass="PaddedCell" HorizontalAlign="Right"></ItemStyle>
                            </asp:TemplateField>
                            <asp:BoundField HeaderText="Last Flight" DataField="LastFlight" DataFormatString="{0:d}" meta:resourcekey="BoundFieldResource5" >
                            <ItemStyle CssClass="PaddedCell" HorizontalAlign="Center" />
                            </asp:BoundField>
                        </Columns>
                    </asp:GridView>
                </asp:View>
            </asp:MultiView>    
        </asp:View>
        <asp:View ID="vwQueryForm" runat="server">
            <uc2:mfbSearchForm ID="mfbSearchForm1" runat="server" OnQuerySubmitted="ShowResults" OnReset="ClearForm" InitialCollapseState="true" />
        </asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <uc6:mfbLogbook ID="MfbLogbook1" runat="server" />
</asp:Content>
