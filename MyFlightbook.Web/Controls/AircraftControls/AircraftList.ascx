<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_AircraftControls_AircraftList" Codebehind="AircraftList.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ Register src="../popmenu.ascx" tagname="popmenu" tagprefix="uc2" %>
<%@ Register src="../mfbHoverImageList.ascx" tagname="mfbHoverImageList" tagprefix="uc3" %>
<%@ Register Src="~/Controls/mfbSelectTemplates.ascx" TagPrefix="uc1" TagName="mfbSelectTemplates" %>

<asp:GridView ID="gvAircraft" runat="server" AutoGenerateColumns="False" EnableViewState="false"
    AllowSorting="True" OnRowCommand="gvAircraft_RowCommand" ShowFooter="false" ShowHeader="false"
    OnRowDataBound="AddPictures" GridLines="None" Width="100%" 
    HeaderStyle-HorizontalAlign="left" CellSpacing="5" CellPadding="5">
    <Columns>
        <asp:TemplateField>
            <ItemStyle CssClass="gvAcImage" />
            <ItemTemplate>
                <uc3:mfbHoverImageList ID="mfbHoverThumb" runat="server" ImageListKey='<%# Eval("AircraftID") %>' ImageListDefaultImage='<%# Eval("DefaultImage") %>' ImageListAltText='<%#: Eval("TailNumber") %>' MaxWidth="150px" 
                    SuppressRefresh="<%# IsAdminMode %>" ImageListDefaultLink='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}{1}", Eval("AircraftID"), IsAdminMode ? "&a=1" : string.Empty) %>' Visible="<%# !IsAdminMode %>" ImageClass="Aircraft" CssClass='<%# ((bool) Eval("HideFromSelection")) ? "inactiveRow" : "activeRow" %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemStyle CssClass="gvAcItem" />
            <ItemTemplate>
                <asp:Panel ID="pnlAircraftID" runat="server">
                    <div>
                        <asp:HyperLink ID="lnkEditAircraft" Font-Size="Larger" Font-Bold="true" runat="server" NavigateUrl='<%#  String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}{1}", Eval("AircraftID"), IsAdminMode ? "&a=1" : string.Empty) %>'><%# Eval("DisplayTailNumber") %></asp:HyperLink>
                        - <%#: Eval("ModelDescription")%> - <%# Eval("ModelCommonName")%> (<%# Eval("CategoryClassDisplay") %>)
                    </div>
                    <asp:Panel ID="pnlInst" runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("InstanceTypeDescription")) %>'><%#: Eval("InstanceTypeDescription")%></asp:Panel>
                </asp:Panel>
                <asp:Panel ID="pnlAircraftDetails" runat="server" CssClass='<%# ((bool) Eval("HideFromSelection")) ? "inactiveRow" : "activeRow" %>'>
                    <div style="white-space:pre"><%# Eval("PublicNotes").ToString().Linkify() %></div>
                    <div style="white-space:pre"><%# Eval("PrivateNotes").ToString().Linkify() %></div>
                    <asp:Panel ID="pnlAttributes" runat="server" Visible="<%# !IsAdminMode %>">
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
                    </asp:Panel>
                </asp:Panel>
                <asp:Label ID="lblAircraftErr" runat="server" CssClass="error" EnableViewState="false" Text="" Visible="false"></asp:Label>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemTemplate>
                <asp:Image ID="imgInactivve" runat="server" Visible='<%# Eval("HideFromSelection") %>' ImageUrl="~/images/circleslash.png" AlternateText="<%$ Resources:Aircraft, InactiveAircraftNote %>" ToolTip="<%$ Resources:Aircraft, InactiveAircraftNote %>" />
            </ItemTemplate>
            <ItemStyle CssClass="gvAcInactive" />
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemTemplate>
                <uc2:popmenu ID="popmenu1" runat="server" Visible='<%# !IsAdminMode %>' >
                    <MenuContent>
                        <h2><asp:Label ID="lblOptionHeader" runat="server" Text=""></asp:Label></h2>
                        <asp:CheckBox ID="ckShowInFavorites" OnCheckedChanged="ckShowInFavorites_CheckedChanged" AutoPostBack="true" Text="<%$ Resources:Aircraft, optionHideFromMainList %>" runat="server" /><br />
                        <hr />
                        <div><asp:Label ID="lblRolePrompt" runat="server" Font-Bold="true" Text="<%$ Resources:Aircraft, optionRolePrompt %>"></asp:Label></div>
                        <table>
                            <tr style="vertical-align:top">
                                <td><asp:RadioButton ID="rbRoleCFI" runat="server" AutoPostBack="true" GroupName="rblGroup" OnCheckedChanged="rbRoleCFI_CheckedChanged" /></td>
                                <td><asp:Label ID="lblCFI" runat="server" Text="<%$ Resources:Aircraft, optionRoleCFI %>" AssociatedControlID="rbRoleCFI"></asp:Label></td>
                            </tr>
                            <tr style="vertical-align:top">
                                <td><asp:RadioButton ID="rbRolePIC" runat="server" AutoPostBack="true"  GroupName="rblGroup" OnCheckedChanged="rbRolePIC_CheckedChanged" /></td>
                                <td>
                                    <div><asp:Label ID="lblPIC" runat="server" Text="<%$ Resources:Aircraft, optionRolePIC %>" AssociatedControlID="rbRolePIC"></asp:Label></div>
                                    <div><asp:CheckBox ID="ckAddNameAsPIC" runat="server" AutoPostBack="true" Text="<%$ Resources:Aircraft, optionRolePICName %>" OnCheckedChanged="ckAddNameAsPIC_CheckedChanged" /></div>
                                </td>
                            </tr>
                            <tr style="vertical-align:top">
                                <td><asp:RadioButton ID="rbRoleSIC" runat="server" AutoPostBack="true"  GroupName="rblGroup" OnCheckedChanged="rbRoleSIC_CheckedChanged" /></td>
                                <td><asp:Label ID="lblSIC" runat="server" Text="<%$ Resources:Aircraft, optionRoleSIC %>" AssociatedControlID="rbRoleSIC"></asp:Label></td>
                            </tr>
                            <tr style="vertical-align:top">
                                <td><asp:RadioButton ID="rbRoleNone" runat="server" AutoPostBack="true"  GroupName="rblGroup" OnCheckedChanged="rbRoleNone_CheckedChanged" /></td>
                                <td><asp:Label ID="lblNone" runat="server" Text="<%$ Resources:Aircraft, optionRoleNone %>" AssociatedControlID="rbRoleNone"></asp:Label></td>
                            </tr>
                        </table>
                        <asp:Panel ID="pnlTemplates" runat="server">
                            <div><asp:Label ID="lblTemplates" Font-Bold="true" runat="server" Text="<%$ Resources:LogbookEntry, TemplateAircraftHeader %>"></asp:Label></div>
                            <uc1:mfbSelectTemplates runat="server" ID="mfbSelectTemplates" IncludeAutomaticTemplates="false" OnTemplatesReady="mfbSelectTemplates_TemplatesReady" OnTemplateSelected="mfbSelectTemplates_TemplateSelected" OnTemplateUnselected="mfbSelectTemplates_TemplateUnselected" />
                        </asp:Panel>
                    </MenuContent>
                </uc2:popmenu>
                <asp:HyperLink ID="lnkRegistration" Text="Registration" Visible="false" Target="_blank" runat="server"></asp:HyperLink>
            </ItemTemplate>
            <ItemStyle CssClass="gvContext" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="">
            <ItemTemplate>
                <asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LocalizedText, MyAircraftDeleteAircraft %>" ToolTip="<%$ Resources:LocalizedText, MyAircraftDeleteAircraft %>" CommandName="_Delete" CommandArgument='<%# Eval("AircraftID") %>' Visible="<%# !IsAdminMode %>" runat="server" />
                <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="imgDelete" ConfirmOnFormSubmit="True" ConfirmText="<%$ Resources:LocalizedText, MyAircraftConfirmDeleteAircraft %>">
                </cc1:ConfirmButtonExtender>
            </ItemTemplate>
            <ItemStyle CssClass="gvDelete" />
        </asp:TemplateField>        
    </Columns>
    <EmptyDataTemplate>
        <p style="font-weight:bold"><asp:Localize ID="Localize3" runat="server" Text="<%$ Resources:LocalizedText, MyAircraftNoAircraft %>"></asp:Localize></p> 
    </EmptyDataTemplate>
</asp:GridView>