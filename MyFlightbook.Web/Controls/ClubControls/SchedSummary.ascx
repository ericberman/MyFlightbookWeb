<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_SchedSummary" Codebehind="SchedSummary.ascx.cs" %>
<asp:GridView ID="gvSchedSummary" CellPadding="4" ShowHeader="false" ShowFooter="false" runat="server" GridLines="None" AutoGenerateColumns="false">
    <Columns>
        <asp:TemplateField>
            <ItemTemplate>
                <div>
                    <asp:Label ID="lblDate" Font-Bold="true" runat="server" Text='<%# String.Format("{0:d}", Eval("LocalStart")) %>'></asp:Label>
                    <asp:Label ID="lblTime" Font-Bold="false" runat="server" Text='<%# String.Format("{0:t} ({1})", Eval("LocalStart"), Eval("DurationDisplay")) %>'></asp:Label>
                </div>
                <asp:Panel ID="pnlDownloadICal" style="float:right" runat="server" Visible='<%# Page.User.Identity.Name.CompareTo(Eval("OwningUser").ToString()) == 0 %>' >
                    <asp:HyperLink ID="lnkDownload" runat="server" ToolTip="<%$ Resources:Schedule, DownloadICal %>" NavigateUrl='<%# String.Format("~/Member/IcalAppt.aspx?c={0}&sid={1}", ClubID, Eval("ID")) %>'>
                        <asp:Image ID="Image1" ToolTip="<%$ Resources:Schedule, DownloadICal %>" AlternateText="<%$ Resources:Schedule, DownloadICal %>" runat="server" ImageUrl="~/images/download.png" />
                    </asp:HyperLink>
                </asp:Panel>
                <asp:Label ID="lblName" Font-Bold="true" runat="server" Text='<%# String.IsNullOrEmpty(UserName) ? Eval("OwnerProfile.UserFullName") : "" %>'></asp:Label>
                <%# (String.IsNullOrEmpty(UserName) && String.IsNullOrEmpty(ResourceName)) ? " - " : "" %>
                <asp:Label ID="lblTail" Font-Bold="true" runat="server" Text='<%# String.IsNullOrEmpty(ResourceName) ? Eval("ResourceAircraft.DisplayTailnumber") : "" %>'></asp:Label>
                <%# (!String.IsNullOrEmpty(UserName) && !String.IsNullOrEmpty(ResourceName)) ? "" : " - " %>
                <asp:Label ID="lblBody" runat="server" Text='<%# Eval("Body") %>'></asp:Label>
            </ItemTemplate>
            <ItemStyle VerticalAlign="Top" />
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <p><asp:Localize ID="locNoEvents" Text="<%$ Resources:Club, errNoUpcomingEvents %>" runat="server"></asp:Localize></p>
    </EmptyDataTemplate>
</asp:GridView>
<asp:HiddenField ID="hdnClubID" runat="server" />
