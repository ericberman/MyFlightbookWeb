<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_ChecklistControls_ChecklistItem" Codebehind="ChecklistItem.ascx.cs" %>
<asp:Panel ID="pnlChecklistRow" runat="server">
    <asp:MultiView ID="mvItem" runat="server" ActiveViewIndex="-1">
        <asp:View ID="vwRepeater" runat="server">
            <div><asp:Label ID="lblHeader" runat="server" CssClass="checklistSubHeader"></asp:Label></div>
            <table style="width: 100%; border-collapse:separate; border-spacing: 0px">
                <asp:Repeater ID="rptRows" runat="server" OnItemDataBound="rptRows_ItemDataBound">
                    <ItemTemplate>
                        <asp:MultiView ID="mvContent" runat="server" ActiveViewIndex='<%# Container.DataItem is MyFlightbook.Checklists.CheckboxRow ? 0 : 1 %>'>
                            <asp:View ID="vwCheckItem" runat="server">
                                <tr runat="server" id="rowCheckItem" class='<%# Container.ItemIndex % 2 == 0 ? "rowEven" : "rowOdd" %>' onclick="clickRow(event, this);">
                                    <td class="checkCell"><asp:CheckBox ID="ckItem" runat="server" onclick="onCheckClick(event)" /></td>
                                    <td class="checklistCell"><asp:Label ID="lblChallenge" CssClass="challengeItem" runat="server" Text='<%#: Eval("Content") %>'></asp:Label></td>
                                    <td class="responseCell"><asp:Label CssClass="responseItem" ID="lblResponse" runat="server" Text='<%#: Eval("Response") %>'></asp:Label></td>
                                </tr>
                            </asp:View>
                            <asp:View ID="vwContentItem" runat="server">
                                <tr runat="server" id="rowContentItem">
                                    <td colspan="3"><asp:Label ID="lblContent" runat="server" Text='<%#: Eval("Content") %>' CssClass='<%# CssClassForContentStyle((MyFlightbook.Checklists.ContentStyle) Eval("Style")) %>'></asp:Label></td>
                                </tr>
                            </asp:View>
                        </asp:MultiView>
                    </ItemTemplate>
                </asp:Repeater>
            </table>
            <asp:Repeater ID="rptSubHeaders" runat="server" OnItemDataBound="rptSubHeaders_ItemDataBound">
                <ItemTemplate>
                </ItemTemplate>
            </asp:Repeater>
            <ajaxToolkit:Accordion ID="accordionRows" RequireOpenedPane="false" SelectedIndex="-1" runat="server" OnItemDataBound="accordion_ItemDataBound" HeaderCssClass="checklistHeader" HeaderSelectedCssClass="checklistHeaderSelected" ContentCssClass="checklistHeaderContent" TransitionDuration="250" >
                <HeaderTemplate>
                    <%#: Eval("Content") %>
                </HeaderTemplate>
                <ContentTemplate>
                </ContentTemplate>
            </ajaxToolkit:Accordion>
            <ajaxToolkit:TabContainer ID="tabRows" runat="server" CssClass="checklistTab">
            </ajaxToolkit:TabContainer>
        </asp:View>
    </asp:MultiView>
</asp:Panel>

