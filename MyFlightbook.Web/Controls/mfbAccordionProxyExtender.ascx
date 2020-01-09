<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbAccordionProxyExtender" Codebehind="mfbAccordionProxyExtender.ascx.cs" %>
<%@ Register src="mfbAccordionProxyControl.ascx" tagname="mfbAccordionProxyControl" tagprefix="uc1" %>
<script>
    var <%=JScriptObjectName%> = new mfbAccordionProxy(<% = Newtonsoft.Json.JsonConvert.SerializeObject(Settings) %>);
</script>


