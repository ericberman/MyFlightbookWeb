<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbMakeListItem" Codebehind="mfbMakeListItem.ascx.cs" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc1" %>
<asp:FormView ID="modelView" Width="100%" runat="server">
    <ItemTemplate>
        <table style="width: 100%;">
            <tr style="vertical-align:middle">
                <td style="width:160px" runat="server" id="cellImages" visible="<%# !SuppressImages %>">
                    <asp:HyperLink ID="lnkImage" runat="server" Target="_blank">
                        <asp:Image ID="imgThumbSample" runat="server" ToolTip="<%$ Resources:LocalizedText, NoImageTooltip %>" AlternateText="<%$ Resources:LocalizedText, NoImageTooltip %>" style="max-width:150px;" />
                    </asp:HyperLink>
                </td>
                <td style="text-align:left; vertical-align:top;">
                    <table style="width:100%">
                        <tr>
                            <td style="width:30%">
                                <asp:Label ID="lblManufacturer" runat="server" Text='<%# Eval("ManufacturerDisplay") %>' Font-Bold='<%# SortMode == ModelQuery.ModelSortMode.Manufacturer %>' Font-Size='<%# SortMode == ModelQuery.ModelSortMode.Manufacturer ? FontUnit.Larger : FontUnit.Empty %>'></asp:Label>
                            </td>
                            <td style="width:40%">
                                <div>
                                    <asp:HyperLink ID="lnkEditMake" runat="server" NavigateUrl='<%# "~/Member/EditMake.aspx?id=" + Eval("MakeModelID").ToString() %>' Font-Bold='<%# SortMode == ModelQuery.ModelSortMode.ModelName %>' Font-Size='<%# SortMode == ModelQuery.ModelSortMode.ModelName ? FontUnit.Larger : FontUnit.Empty %>'>
                                        <%# Eval("ModelDisplayNameNoCatclass") %>
                                    </asp:HyperLink>
                                </div>
                                <asp:Panel ID="pnlICAO" Visible='<%# !String.IsNullOrEmpty((string) Eval("FamilyName")) %>' runat="server">
                                    <% = ModelQuery.ICAOPrefix %> <%# Eval("FamilyName") %>
                                </asp:Panel>
                                <asp:Panel ID="pnlMDS" runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("ArmyMDS")) %>'>
                                    <% = Resources.LocalizedText.EditMakeWatermarkMDS %> <%# Eval("ArmyMDS") %>
                                </asp:Panel>
                            </td>
                            <td style="width:30%">
                                <asp:Label ID="lblCategoryClass" runat="server" Text='<%# Eval("CategoryClassDisplay") %>' Font-Bold='<%# SortMode == ModelQuery.ModelSortMode.CatClass %>' Font-Size='<%# SortMode == ModelQuery.ModelSortMode.CatClass ? FontUnit.Larger : FontUnit.Empty %>'></asp:Label>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <asp:Panel ID="pnlAttributes" runat="server">
                                    <ul>
                                        <asp:Repeater ID="rptAttributes" runat="server" DataSource='<%# ((MakeModel) Container.DataItem).AttributeList() %>'>
                                            <ItemTemplate>
                                                <li><%# Container.DataItem %></li>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ul>
                                </asp:Panel>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table> 
        <div style="border-bottom: 1px solid #888888">&nbsp;</div>
    </ItemTemplate>
</asp:FormView>
