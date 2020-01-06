<%@ Control Language="C#" AutoEventWireup="true"
    Inherits="Controls_mfbAirportIDGame" Codebehind="mfbAirportIDGame.ascx.cs" %>
<%@ Register Src="mfbGoogleMapManager.ascx" TagName="mfbGoogleMap" TagPrefix="uc1" %>
<p style="font-weight: bold; font-size: 14px;" runat="server" id="pRunningScore"
    visible="false">
    <span style="color: Green;">
        <asp:Label ID="lblCorrect" runat="server" Visible="false">Correct!</asp:Label></span>
    <span style="color: Red;">
        <asp:Label ID="lblIncorrect" runat="server" Visible="false">Incorrect!</asp:Label></span>
    <asp:Label ID="lblPreviousAnswer" runat="server" Text=""></asp:Label><br />
    <asp:Label ID="lblRunningScore" runat="server" Text=""></asp:Label>
</p>
<asp:MultiView ID="mvQuiz" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwBegin" runat="server">
        <p><asp:Literal runat="server" Text="<%$ Resources:LocalizedText, AirportGameTitle %>" /></p>
        <div style="text-align: center; width: 480px">
            <asp:LinkButton ID="lnkBusyUS" runat="server" OnClick="lnkBusyUS_Click"><asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:LocalizedText, AirportGameUseUSAirports %>" /></asp:LinkButton><br /><span style="font-weight:bold"><asp:Literal ID="Literal3" runat="server" Text="<%$ Resources:LocalizedText, ORSeparator %>" /></span><br /> 
            <asp:LinkButton ID="lnkYourAirports" runat="server" OnClick="lnkYourAirports_Click"><asp:Literal ID="Literal2" runat="server" Text="<%$ Resources:LocalizedText, AirportGameUseYourAirports %>" /></asp:LinkButton><br /><asp:Literal ID="Literal4" runat="server" Text="<%$ Resources:LocalizedText, AirportGameMustBeSignedIn %>" />
        </div>
    </asp:View>
    <asp:View ID="vwQuestions" runat="server">
        <p><asp:Label ID="lblQuestionProgress" runat="server" Text=""></asp:Label></p><script>
// <![CDATA[  
var t = 30;
setTimeout("TickTock()", 1000);

function TickTock()
{
    if (t > 0)
    {
        t=t-1;
        document.getElementById("timer").innerHTML = parseInt(t / 60) + ":" + (((t % 60) < 10) ? "0" : "") + (t % 60);
        setTimeout("TickTock()", 1000);
    }
    else
    {
        TimeOutExpired();
    }
}
// ]]>
</script><asp:Literal ID="Literal5" runat="server" Text="<%$ Resources:LocalizedText, AirportGameTimeRemaining %>" /><span id="timer" style="font-weight: bold;">0:30</span><br /> <br /><asp:Label ID="Label1" runat="server" Text="<%$Resources:LocalizedText, AirportGameAirportPrompt %>"></asp:Label><br />
        <uc1:mfbGoogleMap ID="MfbGoogleMap1" runat="server" Width="400px" Height="400px" ShowMarkers="false" Mode="Static" />
        <asp:RadioButtonList ID="rbGuesses" runat="server" AutoPostBack="true" OnSelectedIndexChanged="rbGuesses_SelectedIndexChanged">
        </asp:RadioButtonList><br />
        <br />
        <asp:Button ID="btnSkip" runat="server" Text="<%$Resources:LocalizedText,AirportGameSkipAirport %>" OnClick="btnSkip_Click" />
        <br />
        <asp:Label ID="lblDebug" runat="server"></asp:Label></asp:View><asp:View ID="vwResult" runat="server">
        <h2><asp:Literal ID="Literal6" runat="server" Text="<%$ Resources:LocalizedText, AirportGameFinished %>" /></h2>
        <asp:Label ID="lblResults" runat="server" Text=""></asp:Label>
        <asp:Label ID="lblSnark" runat="server" Text=""></asp:Label>
        <br />
        <asp:LinkButton ID="lnkPlayAgain" runat="server" OnClick="lnkPlayAgain_Click">Play Again</asp:LinkButton>
        <br /><br /><br /><br /><br /><br /><br /></asp:View></asp:MultiView><asp:Label ID="lblError" runat="server" Text="" CssClass="error"></asp:Label>