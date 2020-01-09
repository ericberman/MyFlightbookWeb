<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_Clubs" Codebehind="Clubs.aspx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register Src="~/Controls/ClubControls/ViewClub.ascx" tagname="ViewClub" tagprefix="uc1" %>
<%@ Register src="~/Controls/mfbGoogleMapManager.ascx" tagname="mfbGoogleMapManager" tagprefix="uc2" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/Expando.ascx" tagname="Expando" tagprefix="uc3" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblHeader" runat="server" Text="<%$ Resources:Club, LabelManageClubs %>"></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <h2><% =Branding.ReBrand(Resources.Club.ClubDescHeader) %></h2>
    <div class="clubDetailsRight" style="background-color:#DDECFF">
        <p><asp:Label ID="lblTrialStatus" runat="server"></asp:Label></p>
    </div>
    <p><% =Branding.ReBrand(Resources.Club.ClubDescOverview1) %></p>
    
    <p><% =Branding.ReBrand(Resources.Club.ClubDescOverview2Aircraft) %></p>
    <p><asp:HyperLink ID="lnkLearnMore" Target="_blank" NavigateUrl="~/Public/ClubsManual.aspx" runat="server"><% =Branding.ReBrand(Resources.Club.ClubDescLearnMore) %></asp:HyperLink></p>
    <p><% =FixUpDonationAmount() %></p>
    <asp:Panel ID="pnlCreateClub" runat="server">
        <uc3:Expando ID="expandoCreateClub" runat="server">
            <Header><asp:Label ID="lblCreateClubHeader" runat="server" Font-Bold="true" Text="<%$ Resources:Club, LabelCreateClub %>"></asp:Label></Header>
            <Body>
                <asp:Panel ID="pnlNewClub" runat="server" style="overflow:hidden;">
                    <div style="border: 1px solid black; background-color:white;">
                        <uc1:ViewClub runat="server" ID="vcNew" DefaultMode="Edit" OnClubChanged="vcNew_ClubChanged" />
                    </div>
                </asp:Panel>
            </Body>
        </uc3:Expando>
    </asp:Panel>
    <h2><asp:Label ID="Label2" runat="server" Text="<%$ Resources:Club, LabelFindClubs %>"></asp:Label></h2>
    <asp:Panel ID="pnlFindClubs" runat="server" DefaultButton="btnFindClubs">
        <div><% =Resources.Club.LabelFindAllTip %></div>
        <asp:TextBox ID="txtHomeAirport" ValidationGroup="valFindClubs" runat="server" Width="200px"></asp:TextBox>
        <asp:TextBoxWatermarkExtender ID="TextBox1_TextBoxWatermarkExtender" runat="server" TargetControlID="txtHomeAirport" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Club, WatermarkFindClubs %>">
        </asp:TextBoxWatermarkExtender>
        <asp:Button ID="btnFindClubs" runat="server" Text="<%$ Resources:Club, ButtonFindClubs %>" ValidationGroup="valFindClubs"  OnClick="btnFindClubs_Click" />
        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" ValidationGroup="valFindClubs"  runat="server" ErrorMessage="<%$ Resources:Club, errNoHomeAirport %>" ControlToValidate="txtHomeAirport" CssClass="error" Enabled="false" Display="Dynamic"></asp:RequiredFieldValidator>
        <div><asp:Label ID="lblErr" CssClass="error" EnableViewState="false" runat="server" Text=""></asp:Label></div>
        <asp:HiddenField ID="hdnMatchingHomeAirport" runat="server" />
        <asp:HyperLink ID="lnkAllClubs" runat="server" Text="<%$ Resources:Club, LabelSeeAllClubs %>" CssClass="fineprint" Target="_blank" NavigateUrl="~/Public/AllClubs.aspx"></asp:HyperLink>
    </asp:Panel>
    <asp:Panel ID="pnlSearchResults" runat="server" Visible="false">
        <h2><% =Resources.Club.HeaderFoundClubs %></h2>
        <div><asp:Localize ID="locClickPrompt" Text="<%$ Resources:Club, LabelClickClubForDetails %>" runat="server"></asp:Localize></div>
        <uc2:mfbGoogleMapManager ID="mfbGoogleMapManager2" ShowRoute="false" AllowResize="false" runat="server" />
        <asp:Panel ID="pnlDynamicClubDetails" runat="server">
        </asp:Panel>
        <script>
            function displayClubDetails(id) {
                var params = new Object();
                params.idClub = id;
                var d = $.toJSON(params);
                $.ajax(
                {
                    url: '<% =ResolveUrl("~/Public/Clubs.aspx/PopulateClub") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error)
                    {
                        window.alert(xhr.responseJSON.Message);
                        if (onError != null)
                            onError();
                    },
                    complete: function (response) { },
                    success: function (response)
                    {
                        var pnl = document.getElementById('<% = pnlDynamicClubDetails.ClientID %>');
                        pnl.innerHTML = response.d;
                    }
                });
            }
        </script>
    </asp:Panel>
    <asp:Panel ID="pnlYourClubs" runat="server">
        <h2><% =Resources.Club.HeaderYourClubs %></h2>
        <asp:GridView ID="gvClubs" CellPadding="5" AutoGenerateColumns="false" runat="server" OnRowDataBound="gvClubs_RowDataBound" GridLines="None" ShowHeader="False">
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <uc1:ViewClub runat="server" ID="viewClub1" LinkToDetails="true" />
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Top" />
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                <asp:Label ID="lblNoClubs" runat="server" Text="<%$ Resources:Club, LabelNoClubs %>"></asp:Label>
            </EmptyDataTemplate>
        </asp:GridView>
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

