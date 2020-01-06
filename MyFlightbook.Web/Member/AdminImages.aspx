<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" ValidateRequest="false" Inherits="Member_AdminImages" Codebehind="AdminImages.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbEditableImage.ascx" tagname="mfbEditableImage" tagprefix="uc2" %>
<asp:Content ID="Content3" ContentPlaceHolderID="cpPageTitle" Runat="Server">
Review Images
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:UpdatePanel ID="updatePanelMigrate" runat="server">
        <ContentTemplate>
            <p>Migrate up to: <asp:TextBox ID="txtLimitMB" Text="100" runat="server"></asp:TextBox>MB 
                &nbsp;or
                <asp:TextBox ID="txtLimitFiles" Text="100" runat="server"></asp:TextBox>
        &nbsp;images. <asp:Button ID="btnMigrateImages" runat="server" Text="Migrate Images" 
                    onclick="btnMigrateImages_Click" /> <asp:Label ID="lblMigrateResults" runat="server" Text=""></asp:Label></p>
        </ContentTemplate>
    </asp:UpdatePanel>
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="updatePanelMigrate">
        <ProgressTemplate>
            <p><asp:Image ID="imgProgress" ImageUrl="~/images/ajax-loader.gif" runat="server" /></p>
        </ProgressTemplate>
    </asp:UpdateProgress>
    <asp:HiddenField ID="hdnImageCount" runat="server" />
    <asp:HiddenField ID="hdnCurrentOffset" runat="server" />
    <div>
        <asp:Button ID="btnPrevRange" runat="server" Text="Previous Set" 
            onclick="btnPrevRange_Click" />&nbsp;
        <asp:Label ID="lblCurrentImageRange" runat="server" Text=""></asp:Label>&nbsp;
        <asp:Button ID="btnNextRange" runat="server" Text="Next Set" 
            onclick="btnNextRange_Click" />
    </div>
    <asp:GridView ID="gvImages" runat="server" AutoGenerateColumns="false" EnableViewState="true" 
        onrowdatabound="ImagesRowDataBound" AllowPaging="false"
        onrowcommand="gvImages_RowCommand">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:HyperLink ID="lnkID" Target="_blank" runat="server"></asp:HyperLink>
                    <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                        <ContentTemplate>
                            <asp:Panel ID="pnlResolveAircraft" runat="server" Visible="false">
                                <asp:LinkButton ID="lnkGetAircraft" runat="server" CommandName="GetAircraft">Get Aircraft</asp:LinkButton>
                                <asp:PlaceHolder ID="plcAircraft" runat="server"></asp:PlaceHolder>
                            </asp:Panel>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                        <ContentTemplate>
                            <uc1:mfbImageList ID="mfbImageList1" runat="server" Columns="6" MapLinkType="ZoomOnGoogleMaps" CanEdit="true" />
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <PagerSettings PageButtonCount="100" Position="TopAndBottom" />
    </asp:GridView>
    <div>
        <asp:Button ID="btnPrev2" runat="server" Text="Previous Set" 
            onclick="btnPrevRange_Click" />&nbsp;
        <asp:Label ID="lblCurrentImageRange2" runat="server" Text=""></asp:Label>&nbsp;
        <asp:Button ID="btnNext2" runat="server" Text="Next Set" 
            onclick="btnNextRange_Click" />
    </div>
</asp:Content>

