<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbImportAircraft.ascx.cs" Inherits="Controls_mfbImportAircraft" %>
<asp:GridView ID="gvAircraftCandidates" runat="server" AutoGenerateColumns="False"
    OnRowDataBound="gvAircraftCandidates_RowDataBound" OnRowCommand="gvAircraftCandidates_RowCommand" OnSelectedIndexChanged="gvAircraftCandidates_SelectedIndexChanged"
    GridLines="None" CellPadding="5" meta:resourcekey="gvAircraftCandidatesResource1">
    <Columns>
        <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
            <ItemTemplate>
                <asp:LinkButton ID="lnkEdit" runat="server" CommandName="Select" Text="<%$ Resources:Aircraft, AircraftEditEdit %>" meta:resourcekey="lnkEditResource1"></asp:LinkButton>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
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
                <asp:MultiView ID="mvMatch" runat="server">
                    <asp:View ID="vwStatic" runat="server">
                        <div>
                            <asp:Label ID="lblMake" runat="server" Text='<%# Eval("SpecifiedModelDisplay") %>' meta:resourcekey="lblMakeResource1"></asp:Label></div>
                        <div>(<asp:Label ID="lblType" Font-Size="Smaller" runat="server" Text='<%# Eval("InstanceTypeDescriptionDisplay") %>' meta:resourcekey="lblTypeResource1"></asp:Label>)</div>
                    </asp:View>
                    <asp:View ID="vwEdit" runat="server">
                        <table>
                            <tr>
                                <td>
                                    <asp:Label ID="lblKind" runat="server"
                                        Text="Aircraft is a:" meta:resourcekey="lblKindResource1"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="cmbInstType" runat="server" AutoPostBack="True"
                                        DataTextField="DisplayName" DataValueField="InstanceTypeInt"
                                        OnSelectedIndexChanged="cmbModel_DataChanged" meta:resourcekey="cmbInstTypeResource1">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="lblMan" runat="server"
                                        Text="Manufacturer:" meta:resourcekey="lblManResource1"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="cmbManufacturer" runat="server"
                                        AppendDataBoundItems="True" AutoPostBack="True"
                                        DataTextField="ManufacturerName" DataValueField="ManufacturerID"
                                        OnSelectedIndexChanged="cmbManufacturer_DataChanged" meta:resourcekey="cmbManufacturerResource1">
                                    </asp:DropDownList>
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Label ID="lblModel" runat="server"
                                        Text="Model:" meta:resourcekey="lblModelResource1"></asp:Label>
                                </td>
                                <td>
                                    <asp:DropDownList ID="cmbModel" runat="server" AppendDataBoundItems="True"
                                        AutoPostBack="True" DataTextField="ModelDisplayName"
                                        DataValueField="MakeModelID"
                                        OnSelectedIndexChanged="cmbModel_DataChanged" meta:resourcekey="cmbModelResource1">
                                        <asp:ListItem Selected="True"
                                            Text="(Please select a model)" Value="-1" meta:resourcekey="ListItemResource1"></asp:ListItem>
                                    </asp:DropDownList>
                                </td>
                            </tr>
                        </table>
                    </asp:View>
                </asp:MultiView>
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
<asp:Panel ID="pnlAddingAircraft" runat="server" style="display:none; width: 230px; text-align:center; background-color:white; padding: 20px;" meta:resourcekey="pnlAddingAircraftResource1">
    <h3><asp:Label ID="lblAddingAircraft" runat="server" Text="Adding aircraft..." meta:resourcekey="lblAddingAircraftResource1"></asp:Label></h3>
    <div><asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" meta:resourcekey="imgProgressResource1" /></div>
</asp:Panel>
<asp:Label ID="lblPopupPlaceholder" runat="server" Text="" meta:resourcekey="lblPopupPlaceholderResource1"></asp:Label>
<ajaxToolkit:ModalPopupExtender ID="popupAddingInProgress" runat="server"
    PopupControlID="pnlAddingAircraft" TargetControlID="lblPopupPlaceholder"
    BackgroundCssClass="modalBackground" DropShadow="True" 
    BehaviorID="mpeAddAircraftProgress" DynamicServicePath="" />

