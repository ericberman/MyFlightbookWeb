<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbAccordionProxyExtender.ascx.cs" Inherits="Controls_mfbAccordionProxyExtender" %>
<%@ Register src="mfbAccordionProxyControl.ascx" tagname="mfbAccordionProxyControl" tagprefix="uc1" %>
<script>
    var <%=JScriptObjectName%> = new mfbAccordionProxy(<% = Newtonsoft.Json.JsonConvert.SerializeObject(Settings) %>);
</script>


