<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbFlightColoring.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbFlightColoring" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<p><asp:Localize ID="locFlightColoringDesc" runat="server" Text="<%$ Resources:Preferences, FlightColoringDescription %>" /></p>
<table>
    <tr>
        <td><asp:Localize ID="locKeyHeader" runat="server" Text="<%$ Resources:Preferences, FlightColoringKeywordHeader %>" /></td>
        <td><asp:Localize ID="Localize1" runat="server" Text="<%$ Resources:Preferences, FlightColoringColorHeader %>" /></td>
    </tr>
    <tr>
        <td>
            <asp:TextBox ID="txtKey1" runat="server" />
            <cc1:TextBoxWatermarkExtender ID="tweKey1" runat="server" TargetControlID="txtKey1" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, FlightColoringKeywordPrompt %>" />
        </td>
        <td>
            <div><asp:Label ID="lblSamp1" runat="server"  style="padding: 3px" Text="<%$ Resources:Preferences, FlightColoringSample %>" BackColor="#CCFF66" /> <asp:TextBox ID="txtCol1" Text="CCFF66" runat="server" style="visibility:hidden;" /></div>
            <cc1:ColorPickerExtender ID="cpe1" runat="server" TargetControlID="txtCol1" PopupButtonID="lblSamp1" SampleControlID="lblSamp1" PaletteStyle="Continuous" />
        </td>
    </tr>
    <tr>
        <td>
            <asp:TextBox ID="txtKey2" runat="server" />
            <cc1:TextBoxWatermarkExtender ID="tweKey2" runat="server" TargetControlID="txtKey2" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, FlightColoringKeywordPrompt %>" />
        </td>
        <td>
            <div><asp:Label ID="lblSamp2" runat="server" style="padding: 3px" Text="<%$ Resources:Preferences, FlightColoringSample %>" BackColor="#FFFF99" /><asp:TextBox ID="txtCol2" Text="FFFF99" runat="server" style="visibility:hidden;" /></div>
            <cc1:ColorPickerExtender ID="cpe2" runat="server" TargetControlID="txtCol2" PopupButtonID="lblSamp2" SampleControlID="lblSamp2" PaletteStyle="Continuous" />
        </td>
    </tr>
    <tr>
        <td>
            <asp:TextBox ID="txtKey3" runat="server" />
            <cc1:TextBoxWatermarkExtender ID="tweKey3" runat="server" TargetControlID="txtKey3" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, FlightColoringKeywordPrompt %>" />
        </td>
        <td>
            <div><asp:Label ID="lblSamp3" runat="server" style="padding: 3px" Text="<%$ Resources:Preferences, FlightColoringSample %>" BackColor="#FFCCFF" /><asp:TextBox ID="txtCol3" Text="FFCCFF" runat="server" style="visibility:hidden;" /></div>
            <cc1:ColorPickerExtender ID="cpe3" runat="server" TargetControlID="txtCol3" PopupButtonID="lblSamp3" SampleControlID="lblSamp3" PaletteStyle="Continuous" />
        </td>
    </tr>
    <tr>
        <td>
            <asp:TextBox ID="txtKey4" runat="server" />
            <cc1:TextBoxWatermarkExtender ID="tweKey4" runat="server" TargetControlID="txtKey4" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, FlightColoringKeywordPrompt %>" />
        </td>
        <td>
            <div><asp:Label ID="lblSamp4" runat="server" style="padding: 3px" Text="<%$ Resources:Preferences, FlightColoringSample %>" BackColor="#66CCFF" /><asp:TextBox ID="txtCol4" Text="66CCFF" runat="server" style="visibility:hidden;" /></div>
            <cc1:ColorPickerExtender ID="cpe4" runat="server" TargetControlID="txtCol4" PopupButtonID="lblSamp4" SampleControlID="lblSamp4" PaletteStyle="Continuous" />
        </td>
    </tr>
</table>
<asp:Button ID="btnUpdateColors" runat="server" Text="<%$ Resources:LocalizedText, profileUpdatePreferences %>" onclick="btnUpdateColors_Click" />
    <div><asp:Label ID="lblColorsUpdated" runat="server" CssClass="success" EnableViewState="False"
        Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False" /></div>