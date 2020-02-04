<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Codebehind="BrowseTemplates.aspx.cs" Inherits="Member_BrowseTemplates" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="locHeader" runat="server" Text="<%$ Resources:LogbookEntry, TemplateBrowseTemplatesheader %>"></asp:Localize></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <p><asp:HyperLink ID="lnkReturn" runat="server" Text="<%$ Resources:LogbookEntry, TemplateBrowseReturn %>" NavigateUrl="~/Member/EditProfile.aspx/pftPrefs?pane=props"></asp:HyperLink></p>
    <p><asp:Label ID="lblDesc1" runat="server"></asp:Label></p>
    <p><asp:Label ID="lblDesc2" runat="server" Text="<%$ Resources:LogbookEntry, TemplateBrowseHeaderDescription2 %>"></asp:Label></p>
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <asp:GridView ID="gvPublicTemplates" runat="server" Width="100%" GridLines="None" CellPadding="3" ShowFooter="false" ShowHeader="false" AutoGenerateColumns="false">
                <Columns>
                    <asp:TemplateField ItemStyle-VerticalAlign="Top">
                        <ItemTemplate>
                            <div style="border-bottom: 1px solid gray; margin-top: 3px; text-align:center; font-weight:bold; font-size: larger;"><asp:Label ID="lblGroupName" runat="server" Text='<%# Eval("GroupName") %>' /></div>
                            <asp:GridView ID="gvTemplates" runat="server" Width="100%" DataSource='<%# Eval("Templates") %>' CellPadding="3" GridLines="None" ShowFooter="false" ShowHeader="false" AutoGenerateColumns="false"
                                OnRowCommand="gvTemplates_RowCommand" OnRowDataBound="gvTemplates_RowDataBound">
                                <Columns>
                                    <asp:TemplateField ItemStyle-Width="24px">
                                        <ItemTemplate>
                                            &nbsp;
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField ItemStyle-VerticalAlign="Top">
                                        <ItemTemplate>
                                            <div>
                                                <span style="font-weight:bold; font-size: larger"><%#: Eval("Name") %></span>
                                                <span><%# ((string) Eval("Description")).Linkify(true) %></span>
                                            </div>
                                            <div class="fineprint" style="font-style:italic; color: #555555; margin-left: 2em"><%# String.Join(" ● ", (IEnumerable<string>) Eval("PropertyNames")) %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Right">
                                        <ItemTemplate>
                                            <asp:MultiView ID="mvStatus" runat="server">
                                                <asp:View ID="vwOwned" runat="server">
                                                    <asp:Label ID="lblOwned" runat="server" Text="<%$ Resources:LogbookEntry, TemplateYours %>" Font-Bold="true" Font-Size="Larger"></asp:Label>
                                                </asp:View>
                                                <asp:View ID="vwUnOwned" runat="server">
                                                    <ajaxToolkit:ConfirmButtonExtender ID="confirmOverwrite" runat="server" ConfirmText='<%# String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LogbookEntry.TemplateBrowseAddPublicOverwrite, Eval("Name")) %>'
                                                        TargetControlID="lnkAdd" Enabled="false" />
                                                    <asp:LinkButton ID="lnkAdd" runat="server" Text="<%$ Resources:LogbookEntry, TemplateBrowseAddPublic %>" CommandName="_add" CommandArgument='<%# Eval("ID") %>'></asp:LinkButton>
                                                </asp:View>
                                                <asp:View ID="vwAdded" runat="server">
                                                    <asp:Label ID="lblConsumed" runat="server" CssClass="success" Text="<%$ Resources:LogbookEntry, TemplateConsumed %>"></asp:Label>
                                                </asp:View>
                                            </asp:MultiView>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <ul>
                        <li>
                            <asp:Label ID="lblNoTemplates" runat="server" Font-Italic="true" Text="<%$ Resources:LogbookEntry, TemplateNoneAvailable %>"></asp:Label>
                        </li>
                    </ul>
                </EmptyDataTemplate>
            </asp:GridView>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
