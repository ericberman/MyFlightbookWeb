<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="RatingProgress.aspx.cs" Inherits="MyFlightbook.RatingsProgress.RatingProgressPage" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblTitle" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="false" Text=""></asp:Label>
    <div class="noprint">
        <p>
            <asp:DropDownList ID="cmbMilestoneGroup" runat="server" 
                AppendDataBoundItems="true" AutoPostBack="True" DataTextField="GroupName" DataValueField="GroupName" 
                onselectedindexchanged="cmbMilestoneGroup_SelectedIndexChanged">
                <asp:ListItem Selected="True" Value="" Text="<%$ Resources:MilestoneProgress, PromptRatingGroup %>"></asp:ListItem>
            </asp:DropDownList>
        </p>
        <p>
            <asp:DropDownList ID="cmbMilestones" runat="server" AutoPostBack="True" AppendDataBoundItems="true" 
                DataTextField="Title" DataValueField="Title" 
                onselectedindexchanged="cmbMilestones_SelectedIndexChanged">
                <asp:ListItem Selected="True" Value="" Text="<%$ Resources:MilestoneProgress, PromptRatingItem %>"></asp:ListItem>
            </asp:DropDownList>
        </p>
    </div>
    <div class="printonly" style="text-align:center;">
        <h2><asp:Label ID="lblPrintHeader" runat="server" /></h2>
    </div>
    <asp:Panel ID="pnlOverallProgress" runat="server" Visible="false">
        <asp:Label Font-Bold="true" ID="lblOverallProgress" runat="server" Text=""></asp:Label><br />
        <asp:Label ID="lblNoteProgress" runat="server" Font-Bold="true" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
        <asp:Label ID="lblOverallProgressDisclaimer" CssClass="fineprint" runat="server" Text=""></asp:Label>
        <asp:Panel ID="pnlRatingDisclaimer" runat="server">
            <p class="fineprint"><asp:Label ID="lblRatingOverallDisclaimer" runat="server" Text=""></asp:Label></p>
        </asp:Panel>
    </asp:Panel>
    <asp:GridView ID="gvMilestoneProgress" runat="server" ShowHeader="false" ShowFooter="false" 
        onrowdatabound="gvMilestoneProgress_RowDataBound" CellPadding="3" CellSpacing="3" RowStyle-CssClass="progressRow" AlternatingRowStyle-CssClass="progressRow"
        AutoGenerateColumns="False" GridLines="None">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div class="checkedBox">
                        <asp:Label ID="lblDone" Text="✔" runat="server" Visible='<%# Eval("IsSatisfied") %>'></asp:Label>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <div><span style="font-weight:bold"><%# ((string) Eval("FARRef")).Linkify(false) %></span> - <%# ((string) Eval("Title")).Linkify(true) %> <asp:Label ID="lblExpiration" runat="server" Font-Bold="true" Text='<%#: Eval("ExpirationNote") %>' /></div>
                    <asp:Panel ID="pnlNote" CssClass="fineprint" runat="server">
                        <asp:Label ID="lblNoteHeader" runat="server" Font-Bold="true" Text="<%$ Resources:MilestoneProgress, NoteHeader %>" />
                        <span style="font-style:italic"><%# ((string) Eval("Note")).Linkify(true) %></span>
                    </asp:Panel>
                    <asp:MultiView ID="mvProgress" runat="server">
                        <asp:View ID="vwPercentage" runat="server">
                            <div class="progress">
                                <div class="percent"><asp:Label ID="lblProgress" runat="server" Text='<%# Eval("ProgressDisplay") %>'></asp:Label></div>
                                <div class="bar" id="divPercent" runat="server">&nbsp;</div>
                            </div>
                        </asp:View>
                        <asp:View ID="vwAchievement" runat="server">
                            <asp:MultiView ID="mvAchievement" runat="server">
                                <asp:View ID="vwAchieved" runat="server">
                                    <asp:Label ID="lblCompleted" runat="server" Text="<%$ Resources:MilestoneProgress, CompletedHeader %>" Font-Bold="true"></asp:Label>
                                    <asp:HyperLink runat="server" ID="lnkFlight" Target="_blank" Text='<%#: Eval("MatchingEventText") %>'></asp:HyperLink>
                                </asp:View>
                                <asp:View ID="vwNotAchieved" runat="server">
                                    <asp:Label ID="lblNotDone" Font-Bold="true" runat="server" Text="<%$ Resources:MilestoneProgress, NotMet %>"></asp:Label>
                                </asp:View>
                            </asp:MultiView>
                        </asp:View>
                    </asp:MultiView>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

