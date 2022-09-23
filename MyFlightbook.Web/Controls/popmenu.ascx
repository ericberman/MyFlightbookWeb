<%@ Control Language="C#" AutoEventWireup="true" Codebehind="popmenu.ascx.cs" Inherits="Controls_popmenu" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<div class="popContainer">
    <img src="<%=VirtualPathUtility.ToAbsolute("~/images/MenuChevron.png") %>" class="popTrigger" />
    <asp:Panel ID="pnlMenuContent" runat="server" CssClass="popMenuContent popMenuHidden" style="margin-top: -5px; margin-left: 0px">
        <asp:PlaceHolder ID="plcMenuContent" runat="server"></asp:PlaceHolder>
    </asp:Panel>
</div>
