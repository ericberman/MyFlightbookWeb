<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbDownload.ascx.cs" Inherits="MyFlightbook.mfbDownload" %>
<asp:Panel ID="pnlDownloadPDF" Visible="false" runat="server">
    <p>
        <asp:Hyperlink ID="lnkDownloadPDF" runat="server" NavigateUrl="~/Member/Printview.aspx">
            <asp:Image ID="imgDownloadPDF" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
            <asp:Image ID="imgPDF" runat="server" ImageAlign="Middle" ImageUrl="~/images/pdficon_med.png" style="padding-right: 5px;" />
            <asp:Label ID="lblDownloadPDF" runat="server" Text="<%$ Resources:LocalizedText, DownloadAsPDF %>"></asp:Label>
        </asp:Hyperlink>
    </p>
    <div><asp:Label ID="lblPDFErr" EnableViewState="false" CssClass="error" runat="server" Text=""></asp:Label></div>
</asp:Panel>
<asp:GridView ID="gvFlightLogs" runat="server" 
    OnRowDataBound="gvFlightLogs_RowDataBound" AutoGenerateColumns="False" 
    BorderStyle="None" CellPadding="3" DataKeyNames="FlightID" PageSize="50" 
    Font-Size="8pt" EnableViewState="False" 
    EnableModelValidation="True" >
    <Columns>
        <asp:BoundField DataField="Date" DataFormatString="{0:yyyy-MM-dd}" HeaderText="Date" ReadOnly="True"  />
        <asp:BoundField DataField="FlightID" HeaderText="Flight ID" ReadOnly="True"  />
        <asp:BoundField DataField="ModelDisplay" HeaderText="Model" ReadOnly="True" />
        <asp:BoundField DataField="FamilyName" HeaderText="ICAO Model" ReadOnly="true" />
        <asp:TemplateField HeaderText="Tail Number" >
            <ItemTemplate>
                <asp:Label ID="lblTail" runat="server" Text='<%#: AircraftForUser[(int) Eval("AircraftID")].TailNumber %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="TailNumDisplay" HeaderText="Display Tail" ReadOnly="True" />
        <asp:BoundField DataField="AircraftID" HeaderText="Aircraft ID" ReadOnly="true" />
        <asp:BoundField DataField="CatClassDisplay" HeaderText="Category/Class" ReadOnly="True" />
        <asp:TemplateField HeaderText="Alternate Cat/Class">
            <ItemTemplate>
                <asp:Label ID="lblCatClassOverride" runat="server" Text='<%# ((bool) Eval("IsOverridden")) ? ((int) Eval("CatClassOverride")).ToString() : string.Empty %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="Approaches" HeaderText="Approaches">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:TemplateField HeaderText="Hold">
            <ItemTemplate>
                <asp:Label ID="lblHold" runat="server" Text='<%# Eval("fHoldingProcedures").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="Landings" HeaderText="Landings">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:BoundField DataField="NightLandings" HeaderText="FS Night Landings">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:BoundField DataField="FullStopLandings" HeaderText="FS Day Landings">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:TemplateField HeaderText="X-Country">
            <ItemTemplate>
                <asp:Label ID="Label1" runat="server" Text='<%# Eval("CrossCountry").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Night">
            <ItemTemplate>
                <asp:Label ID="Label2" runat="server" Text='<%# Eval("Nighttime").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="IMC">
            <ItemTemplate>
                <asp:Label ID="Label3" runat="server" Text='<%# Eval("IMC").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Simulated Instrument">
            <ItemTemplate>
                <asp:Label ID="lsimimc" runat="server" Text='<%# Eval("SimulatedIFR").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Ground Simulator">
            <ItemTemplate>
                <asp:Label ID="lgrnd" runat="server" Text='<%# Eval("GroundSim").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Dual Received">
            <ItemTemplate>
                <asp:Label ID="ldual" runat="server" Text='<%# Eval("Dual").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="CFI">
            <ItemTemplate>
                <asp:Label ID="lcfi" runat="server" Text='<%# Eval("CFI").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="SIC">
            <ItemTemplate>
                <asp:Label ID="lsic" runat="server" Text='<%# Eval("SIC").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="PIC">
            <ItemTemplate>
                <asp:Label ID="lpic" runat="server" Text='<%# Eval("PIC").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Total Flight Time">
            <ItemTemplate>
                <asp:Label ID="ltft" runat="server" Text='<%# Eval("TotalFlightTime").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="CFI Time (HH:MM)">
            <ItemTemplate>
                <asp:Label ID="Label7" runat="server" Text='<%# Eval("CFI").FormatDecimal(true) %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="SIC Time (HH:MM)">
            <ItemTemplate>
                <asp:Label ID="Label8" runat="server" Text='<%# Eval("SIC").FormatDecimal(true) %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="PIC (HH:MM)">
            <ItemTemplate>
                <asp:Label ID="Label9" runat="server" Text='<%# Eval("PIC").FormatDecimal(true) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Total Flight Time (HH:MM)">
            <ItemTemplate>
                <asp:Label ID="Label10" runat="server" Text='<%# Eval("TotalFlightTime").FormatDecimal(true) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:BoundField DataField="Route" HeaderText="Route" ReadOnly="True" />
        <asp:TemplateField HeaderText="Flight Properties">
            <ItemTemplate>
                <asp:PlaceHolder ID="plcProperties" runat="server"></asp:PlaceHolder>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="Comment" HeaderText="Comments" ReadOnly="True" />
        <asp:BoundField DataField="HobbsStart" HeaderText="Hobbs Start" ReadOnly="true" />
        <asp:BoundField DataField="HobbsEnd" HeaderText="Hobbs End" ReadOnly="true" />
        <asp:TemplateField HeaderText="Engine Start">
            <ItemTemplate>
                <asp:Label ID="lblEngineStart" runat="server" Text='<%# Eval("EngineStart").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Engine End">
            <ItemTemplate>
                <asp:Label ID="lblEngineEnd" runat="server" Text='<%# Eval("EngineEnd").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Engine Time">
            <ItemTemplate>
                <asp:Label ID="lblEngineTime" runat="server" Text='<%# FormatTimeSpan(Eval("EngineStart"), Eval("EngineEnd")) %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flight Start">
            <ItemTemplate>
                <asp:Label ID="lblFlightStart" runat="server" Text='<%# Eval("FlightStart").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flight End">
            <ItemTemplate>
                <asp:Label ID="lblFlightEnd" runat="server" Text='<%# Eval("FlightEnd").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flying Time">
            <ItemTemplate>
                <asp:Label ID="lblFlightTime" runat="server" Text='<%# FormatTimeSpan(Eval("FlightStart"), Eval("FlightEnd")) %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Complex">
            <ItemTemplate>
                <asp:Label ID="lblComplex" runat="server"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Constant-speed prop">
            <ItemTemplate>
                <asp:Label ID="lblCSP" runat="server"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flaps">
            <ItemTemplate>
                <asp:Label ID="lblFlaps" runat="server"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Retract">
            <ItemTemplate>
                <asp:Label ID="lblRetract" runat="server"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Tailwheel">
            <ItemTemplate>
                <asp:Label ID="lblTailwheel" runat="server"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="High Performance">
            <ItemTemplate>
                <asp:Label ID="lblHP" runat="server"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Turbine">
            <ItemTemplate>
                <asp:Label ID="lblTurbine" runat="server"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Signature State">
            <ItemTemplate>
                <asp:Label ID="Label17" runat="server" Text='<%# Eval("CFISignatureState").FormatSignatureState() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Date of Signature">
            <ItemTemplate>
                <asp:Label ID="Label18" runat="server" Text='<%# Eval("CFISignatureDate").FormatOptionalInvariantDate() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="CFIComments" HeaderText="CFI Comment" />
        <asp:BoundField DataField="CFICertificate" HeaderText="CFI Certificate" />
        <asp:BoundField DataField="CFIName" HeaderText="CFI Name" />
        <asp:BoundField DataField="CFIEmail" HeaderText="CFI Email" />
        <asp:TemplateField HeaderText="CFI Expiration">
            <ItemTemplate>
                <asp:Label ID="Label19" runat="server" Text='<%# Eval("CFIExpiration").FormatOptionalInvariantDate() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Public">
            <ItemTemplate>
                <asp:Label ID="lblPublic" runat="server" Text='<%# Eval("fIsPublic").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
    <PagerSettings Mode="NumericFirstLast" Position="TopAndBottom" />
</asp:GridView>
