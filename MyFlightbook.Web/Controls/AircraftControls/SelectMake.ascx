<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_AircraftControls_SelectMake" Codebehind="SelectMake.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>

<asp:MultiView ID="mvModel" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwEdit" runat="server">
        <script>
            function ModelSelected(source, eventArgs) {
                document.getElementById('<% = imgAutofillProgress.ClientID %>').style.display = 'inline-block';
                document.getElementById('<% = hdnSelectedModel.ClientID %>').value = eventArgs._value;
                document.getElementById('<% = lnkPopulateModel.ClientID %>').click();
            }
        </script>
        <div>
            <asp:PlaceHolder ID="plcPrompt" runat="server"></asp:PlaceHolder>
            <div style="border: 1px solid darkgray; border-radius: 14px; height: 24px; display: inline-block; vertical-align: middle; text-align:left; padding-left: 8px; padding-right:3px; ">
                <asp:Image ID="Image1" runat="server" ImageUrl="~/images/Search.png" ImageAlign="AbsMiddle" Height="20px" />
                <asp:TextBox ID="txtFilter" runat="server" Width="240px" Font-Size="8pt" BorderStyle="None" style="vertical-align:middle; margin-right: 2px;"></asp:TextBox>
                <cc1:TextBoxWatermarkExtender
                    ID="TextBoxWatermarkExtender1" runat="server" TargetControlID="txtFilter" EnableViewState="false"
                    WatermarkText="<%$ Resources:Makes, searchTipQuick %>" WatermarkCssClass="watermark">
                </cc1:TextBoxWatermarkExtender>
            </div>
            <span style="display: none">
                <asp:LinkButton ID="lnkPopulateModel" runat="server" OnClick="lnkPopulateModel_Click" />
                <asp:HiddenField ID="hdnSelectedModel" runat="server" />
            </span>
            <asp:Image ID="imgAutofillProgress" style="display: none" runat="server" ImageUrl="~/images/ajax-loader-transparent-ball.gif" />
            <cc1:AutoCompleteExtender ID="modelAutoCompleteExtender" runat="server"
                CompletionInterval="100" CompletionListCssClass="AutoExtender"
                CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                OnClientItemSelected="ModelSelected"
                Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestFullModels"
                ServicePath="~/Member/EditAircraft.aspx" TargetControlID="txtFilter" CompletionSetCount="20">
            </cc1:AutoCompleteExtender>
        </div>
         
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
        <div style="vertical-align:middle">
            <asp:MultiView ID="mvModelDisplay" runat="server" ActiveViewIndex="0">
                <asp:View ID="vwStaticModel" runat="server"><asp:Label ID="lblMakeModel"  Font-Size="Larger" Font-Bold="true" runat="server"></asp:Label></asp:View>
                <asp:View ID="vwLinkedModel" runat="server"><asp:HyperLink ID="lnkMakeModel" runat="server" Font-Size="Larger" Font-Bold="true"></asp:HyperLink></asp:View>
            </asp:MultiView>
            <asp:ImageButton ID="imgEditAircraftModel" ToolTip="<%$ Resources:Aircraft, editAircraftModelPrompt %>" ImageUrl="~/images/pencilsm.png" runat="server" OnClick="btnChangeModelTweak_Click" />
        </div>
    </asp:View>
</asp:MultiView>
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

