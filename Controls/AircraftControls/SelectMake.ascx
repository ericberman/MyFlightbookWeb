<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelectMake.ascx.cs" Inherits="Controls_AircraftControls_SelectMake" %>
<asp:MultiView ID="mvModel" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwEdit" runat="server">
        <h3><% =Resources.Aircraft.editAircraftMakeModelPrompt %></h3>
        <div style="margin-bottom:4px">
            <asp:DropDownList ID="cmbManufacturers" runat="server"
                AppendDataBoundItems="True" AutoPostBack="True" EnableViewState="False"
                DataTextField="ManufacturerName"
                DataValueField="ManufacturerID"
                OnSelectedIndexChanged="cmbManufacturers_SelectedIndexChanged">
                <asp:ListItem Selected="True"
                    Text="<%$ Resources:Aircraft, editAircraftSelectManufacturer %>" Value="-1"></asp:ListItem>
            </asp:DropDownList>
            <asp:HiddenField ID="hdnLastMan" runat="server" />
            <asp:DropDownList ID="cmbMakeModel" runat="server" AppendDataBoundItems="True"
                AutoPostBack="True" DataTextField="ModelDisplayName"
                DataValueField="MakeModelID"
                OnSelectedIndexChanged="cmbMakeModel_SelectedIndexChanged">
                <asp:ListItem Selected="True"
                    Text="<%$ Resources:Aircraft, editAircraftSelectModel %>" Value="-1"></asp:ListItem>
            </asp:DropDownList>
            <asp:HyperLink ID="lnkNewMake" runat="server" Text="<%$ Resources:Aircraft, editAircraftAddModelPrompt %>" NavigateUrl="~/Member/EditMake.aspx"></asp:HyperLink>
            <div>
                <asp:RangeValidator ID="RangeValidator1" runat="server"
                    ControlToValidate="cmbMakeModel" CssClass="error" Display="Dynamic"
                    MaximumValue="1000000" ErrorMessage="&lt;br /&gt;Please select a make/model from the list.  You can add one if needed."
                    MinimumValue="0" Type="Integer" ValidationGroup="EditAircraft"></asp:RangeValidator>
            </div>
        </div>
    </asp:View>
    <asp:View ID="vwReadOnly" runat="server">
        <div style="vertical-align:bottom">
            <asp:Label ID="lblMakeModel" runat="server" Font-Size="Larger" Font-Bold="true"></asp:Label>
            <asp:ImageButton ID="imgEditAircraftModel" ImageAlign="Top" ToolTip="<%$ Resources:Aircraft, editAircraftModelPrompt %>" ImageUrl="~/images/pencilsm.png" runat="server" />
            <ul>
                <asp:Repeater ID="rptAttributes" runat="server">
                    <ItemTemplate>
                        <li>
                            <asp:MultiView ID="mvAttribute" runat="server" ActiveViewIndex='<%# String.IsNullOrEmpty((string) Eval("Link")) ? 0 : 1 %>'>
                                <asp:View ID="vwNoLink" runat="server">
                                    <%# Eval("Value") %>
                                </asp:View>
                                <asp:View ID="vwLink" runat="server">
                                    <asp:HyperLink ID="lnkAttrib" runat="server" Text='<%# Eval("Value") %>' NavigateUrl='<%# Eval("Link") %>'></asp:HyperLink>
                                </asp:View>
                            </asp:MultiView>
                        </li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
        </div>
    </asp:View>
</asp:MultiView>
<asp:Panel ID="pnlAdviseModelChange" runat="server" BackColor="White" Style="margin: 3px; padding:15px; display:none; width: 450px;" DefaultButton="btnChangeModelCancel">
    <p>
        <asp:Label ID="lblAdviseModelChange" runat="server" Text="<%$ Resources:Aircraft, editAircraftModelChangeHeader %>" Font-Bold="True"></asp:Label>
    </p>
    <p><% =Branding.ReBrand(Resources.Aircraft.editAircraftModelChange1) %></p>
    <p><% =Resources.Aircraft.editAircraftModelChange2 %></p>
    <p><% =Resources.Aircraft.editAircraftModelChange3 %></p>
    <div style="text-align:center">
        <asp:Button ID="btnChangeModelTweak" runat="server" Width="45%" Text="<%$ Resources:Aircraft, editAircraftTweak %>" OnClick="btnChangeModelTweak_Click" />
        <asp:Button ID="btnChangeModelClone" runat="server" Width="45%" Text="<%$ Resources:Aircraft, editAircraftClone %>" OnClick="btnChangeModelClone_Click" />
        <br /><br />
        <asp:Button ID="btnChangeModelCancel" runat="server" Width="30%" Text="<%$ Resources:LocalizedText, Cancel %>" />
    </div>
</asp:Panel>
<ajaxToolkit:ModalPopupExtender runat="server" DropShadow="true" PopupControlID="pnlAdviseModelChange" BackgroundCssClass="modalBackground" CancelControlID="btnChangeModelCancel" ID="modalModelChange" BehaviorID="modalModelChange" TargetControlID="imgEditAircraftModel"></ajaxToolkit:ModalPopupExtender>
<script type="text/javascript">
    function hideModelChange() {
        document.getElementById('<% =pnlAdviseModelChange.ClientID %>').style.display = 'none';
        $find("modalModelChange").hide();
    }

    /* Handle escape to dismiss */
    function pageLoad(sender, args) {
        if (!args.get_isPartialLoad()) {
            $addHandler(document, "keydown", onKeyDown);
        }
    }

    function onKeyDown(e) {
        if (e && e.keyCode == Sys.UI.Key.esc)
            hideModelChange();
    }
</script>
