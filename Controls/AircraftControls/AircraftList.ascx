<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AircraftList.ascx.cs" Inherits="Controls_AircraftControls_AircraftList" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ Register src="../popmenu.ascx" tagname="popmenu" tagprefix="uc2" %>
<%@ Register src="../mfbHoverImageList.ascx" tagname="mfbHoverImageList" tagprefix="uc3" %>
<asp:GridView ID="gvAircraft" runat="server" AutoGenerateColumns="False" EnableViewState="false"
    AllowSorting="True" OnRowCommand="gvAircraft_RowCommand" ShowFooter="false" ShowHeader="false"
    OnRowDataBound="AddPictures" GridLines="None" Width="100%" 
    HeaderStyle-HorizontalAlign="left" CellSpacing="5" CellPadding="5">
    <Columns>
        <asp:TemplateField>
            <ItemStyle HorizontalAlign="Center" VerticalAlign="Middle" Width="160px" />
            <ItemTemplate>
                <uc3:mfbHoverImageList ID="mfbHoverThumb" runat="server" ImageListKey='<%# Eval("AircraftID") %>' ImageListDefaultImage='<%# Eval("DefaultImage") %>' ImageListAltText='<%#: Eval("TailNumber") %>' MaxWidth="150px" 
                    SuppressRefresh="<%# IsAdminMode %>" ImageListDefaultLink='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}{1}", Eval("AircraftID"), IsAdminMode ? "&a=1" : string.Empty) %>' Visible="<%# !IsAdminMode %>" ImageClass="Aircraft" CssClass='<%# ((bool) Eval("HideFromSelection")) ? "inactiveRow" : "activeRow" %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemStyle VerticalAlign="Top" />
            <ItemTemplate>
                <asp:Panel ID="pnlAircraftID" runat="server">
                    <div>
                        <asp:HyperLink ID="lnkEditAircraft" Font-Size="Larger" Font-Bold="true" runat="server" NavigateUrl='<%#  String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}{1}", Eval("AircraftID"), IsAdminMode ? "&a=1" : string.Empty) %>'><%# Eval("DisplayTailNumber") %></asp:HyperLink>
                        - <%#: Eval("ModelDescription")%> - <%# Eval("ModelCommonName")%> (<%# Eval("CategoryClassDisplay") %>)
                    </div>
                    <div><%#: Eval("InstanceTypeDescription")%></div>
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
                <div><asp:Label ID="lblAircraftErr" runat="server" CssClass="error" EnableViewState="false" Text="" Visible="false"></asp:Label></div>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemTemplate>
                <asp:Image ID="imgInactivve" runat="server" Visible='<%# Eval("HideFromSelection") %>' ImageUrl="~/images/circleslash.png" AlternateText="<%$ Resources:Aircraft, InactiveAircraftNote %>" ToolTip="<%$ Resources:Aircraft, InactiveAircraftNote %>" />
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" Width="20px" />
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemTemplate>
                <uc2:popmenu ID="popmenu1" runat="server" Visible='<%# !IsAdminMode %>' >
                    <MenuContent>
                        <h2><asp:Label ID="lblOptionHeader" runat="server" Text=""></asp:Label></h2>
                        <asp:CheckBox ID="ckShowInFavorites" OnCheckedChanged="ckShowInFavorites_CheckedChanged" AutoPostBack="true" Text="<%$ Resources:Aircraft, optionHideFromMainList %>" runat="server" /><br />
                        <hr />
                        <asp:Label ID="lblRolePrompt" runat="server" Text="<%$ Resources:Aircraft, optionRolePrompt %>"></asp:Label>
                        <asp:RadioButtonList ID="rblRole" OnSelectedIndexChanged="rblRole_SelectedIndexChanged" runat="server" AutoPostBack="true">
                            <asp:ListItem Text="<%$ Resources:Aircraft, optionRoleCFI %>" Value="CFI"></asp:ListItem>
                            <asp:ListItem Text="<%$ Resources:Aircraft, optionRolePIC %>" Value="PIC"></asp:ListItem>
                            <asp:ListItem Text="<%$ Resources:Aircraft, optionRoleSIC %>" Value="SIC"></asp:ListItem>
                            <asp:ListItem Text="<%$ Resources:Aircraft, optionRoleNone %>" Value="None"></asp:ListItem>
                        </asp:RadioButtonList>
                        <asp:CheckBox ID="ckAddNameAsPIC" runat="server" AutoPostBack="true" Text="<%$ Resources:Aircraft, optionRolePICName %>" OnCheckedChanged="ckAddNameAsPIC_CheckedChanged" />
                    </MenuContent>
                </uc2:popmenu>
                <asp:HyperLink ID="lnkRegistration" Text="Registration" Visible="false" Target="_blank" runat="server"></asp:HyperLink>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" Width="21px" />
        </asp:TemplateField>
        <asp:TemplateField HeaderText="">
            <ItemTemplate>
                <asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LocalizedText, MyAircraftDeleteAircraft %>" ToolTip="<%$ Resources:LocalizedText, MyAircraftDeleteAircraft %>" CommandName="_Delete" CommandArgument='<%# Eval("AircraftID") %>' Visible="<%# !IsAdminMode %>" runat="server" />
                <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="imgDelete" ConfirmOnFormSubmit="True" ConfirmText="<%$ Resources:LocalizedText, MyAircraftConfirmDeleteAircraft %>">
                </cc1:ConfirmButtonExtender>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" Width="13px" />
        </asp:TemplateField>        
    </Columns>
    <EmptyDataTemplate>
        <p style="font-weight:bold"><asp:Localize ID="Localize3" runat="server" Text="<%$ Resources:LocalizedText, MyAircraftNoAircraft %>"></asp:Localize></p> 
    </EmptyDataTemplate>
</asp:GridView>