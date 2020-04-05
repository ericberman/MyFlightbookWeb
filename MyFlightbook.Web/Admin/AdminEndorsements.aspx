<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="AdminEndorsements.aspx.cs" Title="Manage Endorsements" Inherits="MyFlightbook.Web.Admin.AdminEndorsements" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbEditEndorsement.ascx" tagname="mfbEditEndorsement" tagprefix="uc2" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools - Endorsements
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:Panel ID="pnlEditEndorsement" runat="server" DefaultButton="btnAddTemplate">
        <h2><asp:Label ID="lblEndorsements" runat="server" Text=""></asp:Label></h2>
        <p>To edit: </p>
        <ul>
            <li>{x} for a single-line entry with watermark &quot;x&quot;</li>
            <li>{Freeform} for freeform multi-line text (no watermark prompt)</li>
            <li>{Date} for the date (prefilled)</li>
            <li>{Student} for the Student&#39;s name (pre-filled)</li>
            <li>{x/y/z} for a drop-down of choices x, y, and z</li>
        </ul>
        <table>
            <tr><td>Title:</td><td>
                <asp:TextBox ID="txtEndorsementTitle" runat="server" Width="400px"></asp:TextBox></td></tr>
            <tr><td>FARRef:</td><td>
                <asp:TextBox ID="txtEndorsementFAR" runat="server" Width="400px"></asp:TextBox></td></tr>
            <tr><td>Template Text:</td><td>
                <asp:TextBox ID="txtEndorsementTemplate" runat="server" Rows="3" 
                    TextMode="MultiLine"  Width="400px"></asp:TextBox></td></tr>
        </table>
        <asp:Button ID="btnAddTemplate" runat="server" 
            Text="Add an endorsment template" onclick="btnAddTemplate_Click" />
    </asp:Panel>
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
    <h2>Existing Endorsements</h2><asp:UpdatePanel ID="updPnlEndorsements" runat="server">
        <ContentTemplate>
            <asp:SqlDataSource ID="sqlEndorsementTemplates" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT * FROM endorsementtemplates ORDER BY FARRef ASC"
                UpdateCommand="UPDATE endorsementtemplates SET FARRef=?FARRef, Title=?Title, Text=?Text WHERE ID=?id">
                <UpdateParameters>
                    <asp:Parameter Name="FARRef" DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
                    <asp:Parameter Name="Title" DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
                    <asp:Parameter Name="Text"  DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
                    <asp:Parameter Name="id" Direction="Input" Type="Int32" />
                </UpdateParameters>
            </asp:SqlDataSource>
            <asp:GridView ID="gvEndorsementTemplate" runat="server" DataSourceID="sqlEndorsementTemplates"
                AutoGenerateEditButton="True" AutoGenerateColumns="false" 
                EnableModelValidation="True" 
                onrowdatabound="gvEndorsementTemplate_RowDataBound" CellPadding="5">
                <Columns>
                    <asp:TemplateField HeaderText="Endorsement Template Data">
                        <ItemStyle VerticalAlign="Top" />
                        <ItemTemplate>
                            <table style="width:600px">
                                <tr>
                                    <td style="vertical-align: top"><b>ID</b></td><td><asp:Label ID="Label1" runat="server"><%# Eval("id") %></asp:Label></td></tr><tr>
                                    <td style="vertical-align: top"><b>Title</b></td><td><asp:Label ID="Label2" runat="server"><%# Eval("Title") %></asp:Label></td></tr><tr>
                                    <td style="vertical-align: top"><b>FAR</b></td><td><asp:Label ID="Label3" runat="server"><%# Eval("FARRef") %></asp:Label></td></tr><tr>
                                    <td style="vertical-align: top"><b>Template</b></td><td><asp:Label ID="Label4" runat="server"><%# Eval("Text") %></asp:Label></td></tr></table></ItemTemplate><EditItemTemplate>
                            <table style="width:400px">
                                <tr>
                                    <td style="vertical-align: top"><b>ID</b></td><td><asp:Label ID="txtID" runat="server" Text='<%# Bind("id") %>' /></td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Title</b></td><td><asp:TextBox ID="txtTitle" runat="server" Text='<%# Bind("Title") %>' /></td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>FAR</b></td><td><asp:TextBox ID="txtFAR" runat="server" Text='<%# Bind("FARRef") %>' /></td>
                                </tr>
                                <tr>
                                    <td style="vertical-align: top"><b>Template</b></td><td><asp:TextBox ID="txtText" runat="server" Width="300px" Text='<%# Bind("Text")%>' TextMode="MultiLine" /></td>
                                </tr>
                            </table>
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Preview">
                        <ItemTemplate>
                            <uc2:mfbEditEndorsement ID="mfbEditEndorsement1" PreviewMode="true" runat="server" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
