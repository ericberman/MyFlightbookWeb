    <%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEditProp" Codebehind="mfbEditProp.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register src="mfbDateTime.ascx" tagname="mfbDateTime" tagprefix="uc1" %>
<%@ Register src="mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc2" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc4" %>
<%@ Register src="mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc3" %>
<div class="propItemFlow">
    <asp:Label ID="lblPropName" runat="server" EnableViewState="false"></asp:Label> <uc3:mfbTooltip ID="mfbTooltip" runat="server" EnableViewState="false" />
    <br />
    <asp:MultiView ID="mvProp" runat="server">
        <asp:View ID="vwDecimal" runat="server">
            <uc2:mfbDecimalEdit ID="mfbDecEdit" Width="50px" runat="server" />
            <asp:Image ID="imgXFillTach" runat="server" Visible="false" ImageUrl="~/images/cross-fill.png" onclick="javascript:onTachAutofill();" ToolTip="<%$ Resources:LogbookEntry, TachCrossfillTip %>" AlternateText="<%$ Resources:LogbookEntry, TachCrossfillTip %>" />
        </asp:View>
        <asp:View ID="vwDateTime" runat="server"><uc1:mfbDateTime ID="mfbDateTime" runat="server" /></asp:View>
        <asp:View ID="vwDate" runat="server"><uc4:mfbTypeInDate ID="mfbTypeInDate" runat="server" DefaultType="None" /></asp:View>
        <asp:View ID="vwText" runat="server">
            <asp:TextBox ID="txtString" runat="server"></asp:TextBox>
            <asp:AutoCompleteExtender ID="autocompleteStringProp" ServicePath="~/Public/WebService.asmx" ServiceMethod="PreviouslyUsedTextProperties"
                CompletionListItemCssClass="AutoExtenderList" 
                CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                CompletionListCssClass="AutoExtender"
                CompletionInterval="100" DelimiterCharacters="" CompletionSetCount="10" MinimumPrefixLength="0" runat="server" TargetControlID="txtString"></asp:AutoCompleteExtender>
        </asp:View>
        <asp:View ID="vwBool" runat="server"><asp:CheckBox ID="ckValue" runat="server" /></asp:View>
    </asp:MultiView>
</div>

