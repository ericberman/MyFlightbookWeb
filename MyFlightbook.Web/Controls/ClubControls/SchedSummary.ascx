<%@ Control Language="C#" AutoEventWireup="true" Codebehind="SchedSummary.ascx.cs" Inherits="Controls_SchedSummary" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>

<asp:GridView ID="gvSchedSummary" CellPadding="4" ShowHeader="false" ShowFooter="false" runat="server" GridLines="None" AutoGenerateColumns="false" OnRowDataBound="gvSchedSummary_RowDataBound">
    <Columns>
        <asp:TemplateField>
            <ItemTemplate>
                <div><asp:Label ID="lblDate" Font-Bold="true" runat="server" Text='<%# String.Format("{0:d}", Eval("LocalStart")) %>' Font-Size="Larger" /></div>
                <div><asp:Label ID="lblTime" Font-Bold="false" runat="server" Text='<%# String.Format("{0:t} ({1})", Eval("LocalStart"), Eval("DurationDisplay")) %>' /></div>
                <asp:Label ID="lblName" Font-Bold="true" runat="server" Text='<%#: String.IsNullOrEmpty(UserName) ? Eval("OwnerProfile.UserFullName") : "" %>'></asp:Label>
                <%# (String.IsNullOrEmpty(UserName) && String.IsNullOrEmpty(ResourceName)) ? " - " : "" %>
                <asp:Label ID="lblTail" Font-Bold="true" runat="server" Text='<%#: String.IsNullOrEmpty(ResourceName) ? Eval("ResourceAircraft.DisplayTailnumber") : "" %>'></asp:Label>
                <%# (!String.IsNullOrEmpty(UserName) && !String.IsNullOrEmpty(ResourceName)) ? "" : " - " %>
                <asp:Label ID="lblBody" runat="server" Text='<%#: Eval("Body") %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
        <asp:TemplateField>
            <ItemTemplate>
                <asp:Panel ID="pnlDownloadICal" runat="server" Visible='<%# Page.User.Identity.Name.CompareTo(Eval("OwningUser").ToString()) == 0 %>' >
                    <uc1:popmenu runat="server" ID="popmenu">
                        <MenuContent>
                            <div style="line-height: 26px">
                                <asp:HyperLink ID="lnkDownloadICS" runat="server" >
                                    <asp:Image ID="imgDwn1" ImageAlign="Middle" runat="server" style="padding-right: 4px" ImageUrl="~/images/download.png" AlternateText="<%$ Resources:Schedule, DownloadApptICS %>" />
                                    <asp:Label ID="lblICS" runat="server" Text="<%$ Resources:Schedule, DownloadApptICS %>"></asp:Label>
                                </asp:HyperLink>
                            </div>
                            <div style="line-height: 26px">
                                <asp:HyperLink ID="lnkDownloadYahoo" runat="server" Target="_blank">
                                    <asp:Image ID="imgDwn2" ImageAlign="Middle" runat="server" style="padding-right: 4px" ImageUrl="~/images/download.png" AlternateText="<%$ Resources:Schedule, DownloadApptYahoo %>" />
                                    <asp:Label ID="lblYahoo" runat="server" Text="<%$ Resources:Schedule, DownloadApptYahoo %>"></asp:Label>
                                </asp:HyperLink>
                            </div>
                            <div style="line-height: 26px">
                                <asp:HyperLink ID="lnkDownloadGoogle" runat="server" Target="_blank">
                                    <asp:Image ID="imgDwn3" ImageAlign="Middle" runat="server" style="padding-right: 4px" ImageUrl="~/images/download.png" AlternateText="<%$ Resources:Schedule, DownloadApptGoogle %>" />
                                    <asp:Label ID="lblGoogle" runat="server" Text="<%$ Resources:Schedule, DownloadApptGoogle %>"></asp:Label>
                                </asp:HyperLink>
                            </div>
                        </MenuContent>
                    </uc1:popmenu>
                </asp:Panel>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <p><asp:Localize ID="locNoEvents" Text="<%$ Resources:Club, errNoUpcomingEvents %>" runat="server"></asp:Localize></p>
    </EmptyDataTemplate>
</asp:GridView>
<asp:HiddenField ID="hdnClubID" runat="server" />
