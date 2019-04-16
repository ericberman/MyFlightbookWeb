<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEditPropSet.ascx.cs" Inherits="Controls_mfbEditPropSet" %>
<%@ Register src="mfbEditProp.ascx" tagname="mfbEditProp" tagprefix="uc1" %>
        <script type="text/javascript" language="javascript">
            function pageLoad(sender, args) {
                $find("<%=cpeText.ClientID%>").add_expandComplete(setFocusForSearch);
            }

            function setFocusForSearch() {
                if (!$find("<%=cpeText.ClientID%>").get_collapsed())
                    $get("<%=txtFilter.ClientID%>").focus();
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
                                <asp:Image ID="imgSearch" ImageUrl="~/images/Search.png" runat="server" />
                            </td>
                            <td>
                                <ajaxToolkit:CollapsiblePanelExtender ID="cpeText" Collapsed="true" runat="server" CollapseControlID="imgSearch" TargetControlID="pnlSearchProps" ExpandControlID="imgSearch" ExpandDirection="Horizontal" TextLabelID="pnlSearchProps" />
                                <asp:Panel ID="pnlSearchProps" runat="server" EnableViewState="false">
                                    <ajaxToolkit:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LogbookEntry, PropertyWatermark %>" runat="server" TargetControlID="txtFilter" />
                                    <asp:TextBox ID="txtFilter" EnableViewState="false" runat="server" Width="100px"></asp:TextBox>
                                    <div><asp:Label ID="lblFilterMessage" CssClass="fineprint" runat="server" EnableViewState="false"></asp:Label></div>
                                </asp:Panel>
                            </td>
                            <td>
                                <asp:DropDownList ID="cmbPropsToAdd" runat="server" AppendDataBoundItems="true" AutoPostBack="true" EnableViewState="false"
                                    DataValueField="PropTypeID" DataTextField="Title" OnSelectedIndexChanged="cmbPropsToAdd_SelectedIndexChanged" >
                                    <asp:ListItem Enabled="true" Value="" Text="<%$ Resources:LogbookEntry, propSelectAdditionalProperty %>"></asp:ListItem>
                                </asp:DropDownList>
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


