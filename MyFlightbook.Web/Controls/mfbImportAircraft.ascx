<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbImportAircraft.ascx.cs" Inherits="Controls_mfbImportAircraft" %>
<asp:HiddenField ID="hdnModelMap" runat="server" />
<asp:GridView ID="gvAircraftCandidates" runat="server" AutoGenerateColumns="False"
    OnRowDataBound="gvAircraftCandidates_RowDataBound" OnRowCommand="gvAircraftCandidates_RowCommand" 
    GridLines="None" CellPadding="5">
    <Columns>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, ImportHeaderSpecifiedAircraft %>" HeaderStyle-HorizontalAlign="Left">
            <ItemTemplate>
                <asp:HyperLink ID="lnkFAA" Font-Bold="True" runat="server" Target="_blank"
                    Text='<%#: Eval("TailNumber") %>' />
                <asp:Label ID="lblGivenTail" runat="server" Font-Bold="True"
                    Text='<%#: Eval("TailNumber") %>' />
                <asp:Label ID="lblAircraftVersion" runat="server" Font-Bold="True" />
                -
                <asp:Label ID="lblGivenModel" runat="server"
                    Text='<%#: Eval("ModelGiven") %>' />
            </ItemTemplate>
            <HeaderStyle HorizontalAlign="Left"></HeaderStyle>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, ImportHeaderBestMatchModel %>" HeaderStyle-HorizontalAlign="Left">
            <ItemTemplate>
                <asp:HiddenField ID="hdnContext" runat="server" />
                <table>
                    <tr>
                        <td style="vertical-align:top">
                            <asp:Image ID="imgEdit" runat="server" ImageUrl="~/images/pencilsm.png" Visible="false" />
                        </td>
                        <td>
                            <div><asp:Label ID="lblSelectedMake" Font-Bold="true" runat="server" Text='<%#: Eval("SpecifiedModelDisplay") %>' /></div>
                            <asp:Panel ID="pnlStaticMake" runat="server">
                                <asp:Label ID="lblType" Font-Size="Smaller" runat="server" Text='<%#: Eval("InstanceTypeDescriptionDisplay") %>' />
                            </asp:Panel>
                            <asp:Panel ID="pnlEditMake" runat="server" Visible="false">
                                <table>
                                    <tr>
                                        <td><asp:Label ID="lblModel" runat="server" Text="<%$ Resources:Aircraft, ImportModel %>" /></td>
                                        <td>
                                            <asp:panel ID="pnlSearchModels" runat="server" style="z-index: 100">
                                                <asp:TextBox ID="txtSearch" runat="server" Width="250px" Font-Size="8pt" placeholder="<%$ Resources:Makes, searchTip %>" />
                                            </asp:panel>
                                            <ajaxToolkit:AutoCompleteExtender ID="autocompleteModel" runat="server"
                                                CompletionInterval="100" CompletionListCssClass="AutoExtender"
                                                CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                                                CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                                                OnClientItemSelected="ImportModelSelected" UseContextKey="True"
                                                TargetControlID="txtSearch" MinimumPrefixLength="2" ServiceMethod="SuggestFullModelsWithTargets"
                                                ServicePath="~/Member/ImpAircraftService.aspx" CompletionSetCount="20">
                                            </ajaxToolkit:AutoCompleteExtender>
                                            <asp:HiddenField ID="hdnSelectedModel" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:Label ID="lblKind" runat="server" Text="<%$ Resources:Aircraft, ImportKind %>" />
                                        </td>
                                        <td>
                                            <asp:DropDownList ID="cmbInstType" runat="server"
                                                DataTextField="DisplayName" DataValueField="InstanceTypeInt" />
                                        </td>
                                
                                    </tr>
                                </table>
                            </asp:Panel>
                        </td>
                    </tr>
                </table>
            </ItemTemplate>
            <HeaderStyle HorizontalAlign="Left"></HeaderStyle>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemTemplate>
                <asp:HiddenField ID="hdnMatchRowID" Value='<%# Eval("ID") %>' runat="server" />
                <asp:Label ID="lblACErr" runat="server" CssClass="error" />
                <asp:Label ID="lblAllGood" runat="server" CssClass="ok" Font-Bold="True" EnableViewState="False"
                    Text="<%$ Resources:Aircraft, ImportAircraftAdded %>" Style="display: none" />
                <asp:Label ID="lblAlreadyInProfile" runat="server" CssClass="success"
                    Text="<%$ Resources:Aircraft, ImportAlreadyInProfile %>" />
                <asp:Button ID="btnAddThis" runat="server"
                    CommandArgument='<%# Eval("ID") %>'
                    CommandName="AddNew"  />
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" Height="24px" />
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <asp:Label ID="lblNoMatchingExisting" runat="server"
            Text="<%$ Resources:Aircraft, ImportNoAircraftFound %>" />
    </EmptyDataTemplate>
</asp:GridView>
<asp:Panel ID="pnlAddingAircraft" runat="server" CssClass="modalpopup" style="display:none; width: 230px; text-align:center; padding: 20px;">
    <h3><asp:Label ID="lblAddingAircraft" runat="server" Text="<%$ Resources:Aircraft, ImportAircraftAdding %>" /></h3>
    <div><asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" /></div>
</asp:Panel>
<asp:Label ID="lblPopupPlaceholder" runat="server" />
<ajaxToolkit:ModalPopupExtender ID="popupAddingInProgress" runat="server"
    PopupControlID="pnlAddingAircraft" TargetControlID="lblPopupPlaceholder"
    BackgroundCssClass="modalBackground" 
    BehaviorID="mpeAddAircraftProgress" />

