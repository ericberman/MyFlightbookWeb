<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelectMake.ascx.cs" Inherits="Controls_AircraftControls_SelectMake" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:MultiView ID="mvModel" runat="server" ActiveViewIndex="0">
    <asp:View ID="vwEdit" runat="server">
        <script type="text/javascript">
            function ModelSelected(source, eventArgs) {
                $find("cpeFilter")._doClose();
                document.getElementById('<% = imgAutofillProgress.ClientID %>').style.display = 'inline-block';
                document.getElementById('<% = hdnSelectedModel.ClientID %>').value = eventArgs._value;
                document.getElementById('<% = lnkPopulateModel.ClientID %>').click();
            }
        </script>
        <table>
            <tr>
                <td style="padding:0px"><h3><% =Resources.Aircraft.editAircraftMakeModelPrompt %></h3></td>
                <td> <asp:Image ID="imgSearch" ImageUrl="~/images/Search.png" runat="server" Height="20px" Width="20px" /></td>
                <td>
                    <ajaxToolkit:CollapsiblePanelExtender ID="cpeFilter" Collapsed="true" runat="server" CollapseControlID="imgSearch" EnableViewState="false" 
                        TargetControlID="pnlFilterModels" ExpandControlID="imgSearch" ExpandDirection="Horizontal" TextLabelID="pnlSearchProps" BehaviorID="cpeFilter" />
                    <asp:Panel ID="pnlFilterModels" runat="server" EnableViewState="false">
                        <ajaxToolkit:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Makes, SearchTip %>" runat="server" TargetControlID="txtFilter" />
                        <asp:TextBox ID="txtFilter" EnableViewState="false" runat="server" Width="320px" onfocusout="$find('cpeFilter')._doClose()"></asp:TextBox>
                        <span style="display: none">
                            <asp:LinkButton ID="lnkPopulateModel" runat="server" OnClick="lnkPopulateModel_Click" />
                            <asp:HiddenField ID="hdnSelectedModel" runat="server" />
                        </span>
                    </asp:Panel>
                    <asp:Image ID="imgAutofillProgress" style="display: none" runat="server" ImageUrl="~/images/ajax-loader-transparent-ball.gif" />
                    <cc1:AutoCompleteExtender ID="txtTail_AutoCompleteExtender" runat="server"
                        CompletionInterval="100" CompletionListCssClass="AutoExtender"
                        CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                        CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                        OnClientItemSelected="ModelSelected"
                        Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestFullModels"
                        ServicePath="~/Member/EditAircraft.aspx" TargetControlID="txtFilter" CompletionSetCount="20">
                    </cc1:AutoCompleteExtender>
                </td>
            </tr>
        </table>
         
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
            <asp:HyperLink ID="lnkMakeModel" runat="server" Font-Size="Larger" Font-Bold="true"></asp:HyperLink>
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

