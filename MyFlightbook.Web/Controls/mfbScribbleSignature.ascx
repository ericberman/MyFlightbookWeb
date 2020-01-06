<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbScribbleSignature" Codebehind="mfbScribbleSignature.ascx.cs" %>
<script>
    function loaded() {
        var signature = new ns.SignatureControl({ imgDataControlId : "<% = hdnSigData.ClientID %>" });
        signature.init();
    }

    window.addEventListener('DOMContentLoaded', loaded, false);
</script> 
<div><canvas id="signatureCanvas" width="280" height="120" style="border: solid 2px black; background-color:White"></canvas></div>
<div><input id="btnClear" type="button" value="Clear" /> <span id="statusText"></span></div>
<asp:HiddenField ID="hdnSigData" runat="server" />
<div>
    <asp:CustomValidator ID="valSignature" runat="server" 
    ErrorMessage="<%$ Resources:Signoff, errScribbleRequired %>" 
    CssClass="error" Display="Dynamic" 
    onservervalidate="valSignature_ServerValidate"></asp:CustomValidator>
    <asp:Label ID="lblSigErr" CssClass="error" runat="server" EnableViewState="false" ></asp:Label>
</div>
