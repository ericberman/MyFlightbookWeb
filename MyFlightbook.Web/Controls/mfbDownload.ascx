<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbDownload" Codebehind="mfbDownload.ascx.cs" %>
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
    BorderStyle="None" CellPadding="3" DataKeyNames="idFlight" PageSize="50" 
    Font-Size="8pt" EnableViewState="False" 
    EnableModelValidation="True" >
    <Columns>
        <asp:BoundField DataField="Date" DataFormatString="{0:yyyy-MM-dd}" HeaderText="Date" ReadOnly="True"  />
        <asp:BoundField DataField="idFlight" HeaderText="Flight ID" ReadOnly="True"  />
        <asp:BoundField DataField="ModelDisplay" HeaderText="Model" ReadOnly="True" />
        <asp:BoundField DataField="Family" HeaderText="ICAO Model" ReadOnly="true" />
        <asp:BoundField DataField="RawTailNumber" HeaderText="Tail Number" ReadOnly="True" />
        <asp:BoundField DataField="TailNumberDisplay" HeaderText="Display Tail" ReadOnly="True" />
        <asp:BoundField DataField="idAircraft" HeaderText="Aircraft ID" ReadOnly="true" />
        <asp:BoundField DataField="CatClassDisplay" HeaderText="Category/Class" ReadOnly="True" />
        <asp:TemplateField HeaderText="Alternate Cat/Class">
            <ItemTemplate>
                <asp:Label ID="lblCatClassOverride" runat="server" Text='<%# ((int) Eval("IsOverridden")) == 0 ? "0" : Eval("CatClassOverride").ToString() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="cInstrumentApproaches" HeaderText="Approaches">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:TemplateField HeaderText="Hold">
            <ItemTemplate>
                <asp:Label ID="lblHold" runat="server" Text='<%# Eval("fHold").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="cLandings" HeaderText="Landings">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:BoundField DataField="cNightLandings" HeaderText="FS Night Landings">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:BoundField DataField="cFullStopLandings" HeaderText="FS Day Landings">
            <ItemStyle HorizontalAlign="Right" />
        </asp:BoundField>
        <asp:TemplateField HeaderText="X-Country">
            <ItemTemplate>
                <asp:Label ID="Label1" runat="server" Text='<%# Eval("crosscountry").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Night">
            <ItemTemplate>
                <asp:Label ID="Label2" runat="server" Text='<%# Eval("night").FormatDecimal(false) %>'></asp:Label>
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
                <asp:Label ID="Label4" runat="server" Text='<%# Eval("simulatedInstrument").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Ground Simulator">
            <ItemTemplate>
                <asp:Label ID="Label5" runat="server" Text='<%# Eval("groundSim").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Dual Received">
            <ItemTemplate>
                <asp:Label ID="Label6" runat="server" Text='<%# Eval("DualReceived").FormatDecimal(false) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:BoundField DataField="cfi" HeaderText="CFI" ReadOnly="true" />
        <asp:BoundField DataField="sic" HeaderText="SIC" ReadOnly="true" />
        <asp:BoundField DataField="PIC" HeaderText="PIC" ReadOnly="true" />
        <asp:BoundField DataField="totalFlightTime" HeaderText="Total Flight Time" ReadOnly="true" />
        <asp:TemplateField HeaderText="CFI Time (HH:MM)">
            <ItemTemplate>
                <asp:Label ID="Label7" runat="server" Text='<%# Eval("cfi").FormatDecimal(true) %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="SIC Time (HH:MM)">
            <ItemTemplate>
                <asp:Label ID="Label8" runat="server" Text='<%# Eval("sic").FormatDecimal(true) %>'></asp:Label>
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
                <asp:Label ID="Label10" runat="server" Text='<%# Eval("totalFlightTime").FormatDecimal(true) %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle HorizontalAlign="Right" />
        </asp:TemplateField>
        <asp:BoundField DataField="Route" HeaderText="Route" ReadOnly="True" />
        <asp:TemplateField HeaderText="Flight Properties">
            <ItemTemplate>
                <asp:PlaceHolder ID="plcProperties" runat="server"></asp:PlaceHolder>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="Comments" HeaderText="Comments" ReadOnly="True" />
        <asp:BoundField DataField="HobbsStart" HeaderText="Hobbs Start" ReadOnly="true" />
        <asp:BoundField DataField="HobbsEnd" HeaderText="Hobbs End" ReadOnly="true" />
        <asp:TemplateField HeaderText="Engine Start">
            <ItemTemplate>
                <asp:Label ID="lblEngineStart" runat="server" Text='<%# Eval("dtEngineStart").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Engine End">
            <ItemTemplate>
                <asp:Label ID="lblEngineEnd" runat="server" Text='<%# Eval("dtEngineEnd").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Engine Time">
            <ItemTemplate>
                <asp:Label ID="lblEngineTime" runat="server" Text='<%# FormatTimeSpan(Eval("dtEngineStart"), Eval("dtEngineEnd")) %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flight Start">
            <ItemTemplate>
                <asp:Label ID="lblFlightStart" runat="server" Text='<%# Eval("dtFlightStart").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flight End">
            <ItemTemplate>
                <asp:Label ID="lblFlightEnd" runat="server" Text='<%# Eval("dtFlightEnd").FormatDateZulu() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flying Time">
            <ItemTemplate>
                <asp:Label ID="lblFlightTime" runat="server" Text='<%# FormatTimeSpan(Eval("dtFlightStart"), Eval("dtFlightEnd")) %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Complex">
            <ItemTemplate>
                <asp:Label ID="Label11" runat="server" Text='<%# Eval("fComplex").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Constant-speed prop">
            <ItemTemplate>
                <asp:Label ID="Label12" runat="server" Text='<%# Eval("fConstantProp").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Flaps">
            <ItemTemplate>
                <asp:Label ID="Label13" runat="server" Text='<%# Eval("fCowlFlaps").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Retract">
            <ItemTemplate>
                <asp:Label ID="Label14" runat="server" Text='<%# Eval("fRetract").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Tailwheel">
            <ItemTemplate>
                <asp:Label ID="lblTailwheel" runat="server" Text='<%# Eval("fTailWheel").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="High Performance">
            <ItemTemplate>
                <asp:Label ID="Label15" runat="server" Text='<%# Eval("fHighPerf").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Turbine">
            <ItemTemplate>
                <asp:Label ID="Label16" runat="server" Text='<%# Eval("fTurbine").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Signature State">
            <ItemTemplate>
                <asp:Label ID="Label17" runat="server" Text='<%# Eval("SignatureState").FormatSignatureState() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Date of Signature">
            <ItemTemplate>
                <asp:Label ID="Label18" runat="server" Text='<%# Eval("SignatureDate").FormatOptionalInvariantDate() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="CFIComment" HeaderText="CFI Comment" />
        <asp:BoundField DataField="CFICertificate" HeaderText="CFI Certificate" />
        <asp:BoundField DataField="CFIName" HeaderText="CFI Name" />
        <asp:TemplateField HeaderText="CFI Expiration">
            <ItemTemplate>
                <asp:Label ID="Label19" runat="server" Text='<%# Eval("CFIExpiration").FormatOptionalInvariantDate() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Public">
            <ItemTemplate>
                <asp:Label ID="lblPublic" runat="server" Text='<%# Eval("fPublic").FormatBooleanInt() %>'></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
    <PagerSettings Mode="NumericFirstLast" Position="TopAndBottom" />
</asp:GridView>
