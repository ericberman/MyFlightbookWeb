<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbImportAircraft" Codebehind="mfbImportAircraft.ascx.cs" %>
<asp:GridView ID="gvAircraftCandidates" runat="server" AutoGenerateColumns="False"
    OnRowDataBound="gvAircraftCandidates_RowDataBound" OnRowCommand="gvAircraftCandidates_RowCommand" 
    GridLines="None" CellPadding="5" meta:resourcekey="gvAircraftCandidatesResource1">
    <Columns>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, ImportHeaderSpecifiedAircraft %>" HeaderStyle-HorizontalAlign="Left" meta:resourcekey="TemplateFieldResource2">
            <ItemTemplate>
                <asp:HyperLink ID="lnkFAA" Font-Bold="True" runat="server" Target="_blank"
                    Text='<%# Eval("TailNumber") %>' meta:resourcekey="lnkFAAResource1"></asp:HyperLink>
                <asp:Label ID="lblGivenTail" runat="server" Font-Bold="True"
                    Text='<%# Eval("TailNumber") %>' meta:resourcekey="lblGivenTailResource1"></asp:Label>
                <asp:Label ID="lblAircraftVersion" runat="server" Font-Bold="True" meta:resourcekey="lblAircraftVersionResource1"></asp:Label>
                -
                <asp:Label ID="lblGivenModel" runat="server"
                    Text='<%# Eval("ModelGiven") %>' meta:resourcekey="lblGivenModelResource1"></asp:Label>
            </ItemTemplate>
            <HeaderStyle HorizontalAlign="Left"></HeaderStyle>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, ImportHeaderBestMatchModel %>" HeaderStyle-HorizontalAlign="Left" meta:resourcekey="TemplateFieldResource3">
            <ItemTemplate>
                <asp:HiddenField ID="hdnContext" runat="server" />
                <table>
                    <tr>
                        <td style="vertical-align:top">
                            <asp:Image ID="imgEdit" runat="server" ImageUrl="~/images/pencilsm.png" meta:resourcekey="imgSearchResource1" Visible="false" />
                        </td>
                        <td>
                            <div><asp:Label ID="lblSelectedMake" Font-Bold="true" runat="server" Text='<%# Eval("SpecifiedModelDisplay") %>' meta:resourcekey="lblMakeResource1"></asp:Label></div>
                            <asp:Panel ID="pnlStaticMake" runat="server">
                                (<asp:Label ID="lblType" Font-Size="Smaller" runat="server" Text='<%# Eval("InstanceTypeDescriptionDisplay") %>' meta:resourcekey="lblTypeResource1"></asp:Label>)
                            </asp:Panel>
                            <asp:Panel ID="pnlEditMake" runat="server" Visible="false">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:Label ID="lblModel" runat="server"
                                                Text="Model:" meta:resourcekey="lblModelResource1"></asp:Label>
                                        </td>
                                        <td>
                                            <asp:panel ID="pnlSearchModels" runat="server" style="z-index: 100" meta:resourcekey="pnlSearchModelsResource1">
                                                <asp:TextBox ID="txtSearch" runat="server" Width="250px" Font-Size="8pt" meta:resourcekey="txtSearchResource1"></asp:TextBox>
                                                <ajaxToolkit:TextBoxWatermarkExtender
                                                    ID="TextBoxWatermarkExtender1" runat="server" TargetControlID="txtSearch" EnableViewState="False"
                                                    WatermarkText="Find a model" WatermarkCssClass="watermark">
                                                </ajaxToolkit:TextBoxWatermarkExtender>
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
                                            <asp:Label ID="lblKind" runat="server"
                                                Text="Aircraft is a:" meta:resourcekey="lblKindResource1"></asp:Label>
                                        </td>
                                        <td>
                                            <asp:DropDownList ID="cmbInstType" runat="server"
                                                DataTextField="DisplayName" DataValueField="InstanceTypeInt"
                                                meta:resourcekey="cmbInstTypeResource1">
                                            </asp:DropDownList>
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
        <asp:TemplateField meta:resourcekey="TemplateFieldResource4">
            <ItemTemplate>
                <asp:HiddenField ID="hdnMatchRowID" Value='<%# Eval("ID") %>' runat="server" />
                <asp:Label ID="lblACErr" runat="server" CssClass="error" meta:resourcekey="lblACErrResource1"></asp:Label>
                <asp:Label ID="lblAllGood" runat="server" CssClass="ok" Font-Bold="True" EnableViewState="False"
                    Text="Aircraft Added" Style="display: none" meta:resourcekey="lblAllGoodResource1"></asp:Label>
                <asp:Label ID="lblAlreadyInProfile" runat="server" CssClass="success"
                    Text="This aircraft is in your profile" meta:resourcekey="lblAlreadyInProfileResource1"></asp:Label>
                <asp:Button ID="btnAddThis" runat="server"
                    CommandArgument='<%# Eval("ID") %>'
                    CommandName="AddNew" meta:resourcekey="btnAddThisResource1" />
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" Height="24px" />
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <asp:Label ID="lblNoMatchingExisting" runat="server"
            Text="(No aircraft found to import)" meta:resourcekey="lblNoMatchingExistingResource1"></asp:Label>
    </EmptyDataTemplate>
</asp:GridView>
<asp:Panel ID="pnlAddingAircraft" runat="server" CssClass="modalpopup" style="display:none; width: 230px; text-align:center; padding: 20px;" meta:resourcekey="pnlAddingAircraftResource1">
    <h3><asp:Label ID="lblAddingAircraft" runat="server" Text="Working..." meta:resourcekey="lblAddingAircraftResource1"></asp:Label></h3>
    <div><asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" meta:resourcekey="imgProgressResource1" /></div>
</asp:Panel>
<asp:Label ID="lblPopupPlaceholder" runat="server" meta:resourcekey="lblPopupPlaceholderResource1"></asp:Label>
<ajaxToolkit:ModalPopupExtender ID="popupAddingInProgress" runat="server"
    PopupControlID="pnlAddingAircraft" TargetControlID="lblPopupPlaceholder"
    BackgroundCssClass="modalBackground" 
    BehaviorID="mpeAddAircraftProgress" />

