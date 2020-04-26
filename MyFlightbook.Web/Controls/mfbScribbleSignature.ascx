<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbScribbleSignature.ascx.cs" Inherits="Controls_mfbScribbleSignature" %>
<script>
    function loaded() {
        var signature = new ns.SignatureControl({ imgDataControlId: "<% = hdnSigData.ClientID %>", clearControlId: "btnClear", watermarkX: 250, watermarkY: 90, watermarkHREF: "<% = hdnWM.Value %>", strokeColor: "<% = hdnColor.Value %>" });
        signature.init();
    }

    window.addEventListener('DOMContentLoaded', loaded, false);
</script> 
<div><canvas id="signatureCanvas" width="280" height="120" style="border: solid 2px black; background-color:White"></canvas></div>
<div><input id="btnClear" type="button" value="Clear" /> 
    <span id="statusText"></span>
    <asp:Button ID="btnSave" runat="server" Text="<%$ Resources:LocalizedText, StudentSigningDefaultScribbleSaveSig %>" OnClick="btnSave_Click" Visible="false" />
    <asp:Button ID="btnCancel" runat="server" Text="<%$ Resources:LocalizedText, StudentSigningDefaultScribbleCancel %>" OnClick="btnCancel_Click" Visible="false" />
</div>
<asp:HiddenField ID="hdnSigData" runat="server" />
<asp:HiddenField ID="hdnWM" runat="server" />
<asp:HiddenField ID="hdnColor" runat="server" Value="#0000ff" />
<div>
    <asp:CustomValidator ID="valSignature" runat="server" 
    ErrorMessage="<%$ Resources:Signoff, errScribbleRequired %>" 
    CssClass="error" Display="Dynamic" 
    onservervalidate="valSignature_ServerValidate"></asp:CustomValidator>
    <asp:Label ID="lblSigErr" CssClass="error" runat="server" EnableViewState="false" ></asp:Label>
</div>
