<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEditPropSet" Codebehind="mfbEditPropSet.ascx.cs" %>
<%@ Register src="mfbEditProp.ascx" tagname="mfbEditProp" tagprefix="uc1" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>
<%@ Register Src="~/Controls/mfbSelectTemplates.ascx" TagPrefix="uc1" TagName="mfbSelectTemplates" %>
<script>
    function toggleSearchBox() {
        var txtFilter = document.getElementById("<% =txtFilter.ClientID %>");
        if (txtFilter.style.display == "none") {
            txtFilter.style.visibility = "visible";
            txtFilter.style.display = "inline";
            txtFilter.focus();
        }
        else {
            txtFilter.style.display = "none";
            txtFilter.style.visibility = "hidden";
        }
    }

    addLoadEvent(function () { CacheItems(document.getElementById("<% =cmbPropsToAdd.ClientID %>")); });
</script>
        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
            <Triggers>
                <asp:AsyncPostBackTrigger ControlID="cmbPropsToAdd" EventName="SelectedIndexChanged" />
            </Triggers>
            <ContentTemplate>
                <div style="margin-bottom: 3px;">
                    <div><asp:Label ID="lblAddPrompt" runat="server" Text="<%$ Resources:LogbookEntry, propAdditionalPropertiesPrompt %>"></asp:Label></div>
                    <table>
                        <tr style="vertical-align:top">
                            <td>
                                <asp:Image ID="imgSearch" ImageUrl="~/images/Search.png" runat="server" onclick="javascript:toggleSearchBox();" />
                            </td>
                            <td>
                                <asp:Panel ID="pnlSearchProps" runat="server" EnableViewState="false">
                                    <asp:TextBox ID="txtFilter" EnableViewState="false" runat="server" Width="100px" style="display:none; visibility:hidden;"></asp:TextBox>
                                    <div><asp:Label ID="lblFilterMessage" CssClass="fineprint" runat="server" EnableViewState="false"></asp:Label></div>
                                </asp:Panel>
                            </td>
                            <td>
                                <asp:DropDownList ID="cmbPropsToAdd" runat="server" AppendDataBoundItems="true" AutoPostBack="true" EnableViewState="false"
                                    DataValueField="PropTypeID" DataTextField="Title" OnSelectedIndexChanged="cmbPropsToAdd_SelectedIndexChanged" >
                                    <asp:ListItem Enabled="true" Value="" Text="<%$ Resources:LogbookEntry, propSelectAdditionalProperty %>"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                            <td>
                                <uc1:popmenu runat="server" ID="popmenu">
                                    <MenuContent>
                                        <uc1:mfbSelectTemplates runat="server" ID="mfbSelectTemplates" OnTemplateSelected="mfbSelectTemplates_TemplateSelected" OnTemplateUnselected="mfbSelectTemplates_TemplateUnselected" OnTemplatesReady="mfbSelectTemplates_TemplatesReady" />
                                    </MenuContent>
                                </uc1:popmenu>
                            <td>
                        </tr>
                    </table>
                    <div class="fineprint">(<asp:HyperLink ID="lnkContactMe" 
                        NavigateUrl="~/Public/ContactMe.aspx" Target="_blank" runat="server" 
                        Text="<%$ Resources:LogbookEntry, NewPropertyPrompt1 %>"></asp:HyperLink>
                    <asp:Label ID="lblContactUsRemainder" runat="server" 
                        Text="<%$ Resources:LogbookEntry, NewPropertyPrompt2 %>"></asp:Label>)</div>
                </div>
                <asp:Panel ID="pnlProps" runat="server" CssClass="propItemContainer" ScrollBars="Auto">
                    <asp:PlaceHolder ID="plcHolderProps" runat="server">
                    </asp:PlaceHolder>
                </asp:Panel>
            </ContentTemplate>
        </asp:UpdatePanel>


