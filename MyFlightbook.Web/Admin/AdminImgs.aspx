<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Title="Admin - Images" CodeBehind="AdminImgs.aspx.cs" Inherits="MyFlightbook.Web.Admin.AdminImgs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools - Images
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <h2>Images</h2>
    <p>
        <asp:Button ID="btnDeleteOrphans" runat="server" Text="Delete Orphans" 
            onclick="btnDeleteOrphans_Click" /><asp:Label ID="lblDeleted" runat="server" Text=""></asp:Label>
        <asp:Button ID="btnDeleteS3Debug" runat="server" 
            onclick="btnDeleteS3Debug_Click" Text="Delete DEBUG S3 Images" /> 
    </p>
    <table>
        <tr>
            <td>
                <asp:HyperLink ID="lnkFlightImages" NavigateUrl="~/Admin/AdminImages.aspx?r=Flight" Target="_blank" runat="server">Review Flight Images</asp:HyperLink>
            </td>
            <td>
                <asp:Button ID="btnSyncFlight" runat="server" Text="Sync Flight Images to DB" 
                    onclick="btnSyncFlight_Click" />
            </td>
            <td>
                <asp:Button ID="btnDelS3FlightOrphans" runat="server" OnClick="btnDelS3FlightOrphans_Click" Text="Delete Orphan S3 Images" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:HyperLink ID="lnkAircraftImages" NavigateUrl="~/Admin/AdminImages.aspx?r=Aircraft" Target="_blank" runat="server">Review Aircraft Images</asp:HyperLink>
            </td>
            <td>
                <asp:Button ID="btnSyncAircraftImages" runat="server" 
                    Text="Sync Aircraft Images to DB" onclick="btnSyncAircraftImages_Click" />
            </td>
            <td>
                <asp:Button ID="btnDelS3AircraftOrphans" runat="server" OnClick="btnDelS3AircraftOrphans_Click" Text="Delete Orphan S3 Images" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:HyperLink ID="lnkEndorsementImages" NavigateUrl="~/Admin/AdminImages.aspx?r=Endorsement" Target="_blank" runat="server">Review Endorsements</asp:HyperLink>
            </td>
            <td>
                <asp:Button ID="btnSyncEndorsements" runat="server" 
                    Text="Sync Endorsement Images to DB" onclick="btnSyncEndorsements_Click" />
            </td>
            <td>
                <asp:Button ID="btnDelS3EndorsementOrphans" runat="server" OnClick="btnDelS3EndorsementOrphans_Click" Text="Delete Orphan S3 Images" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:HyperLink ID="lnkOfflineEndorsementImages" NavigateUrl="~/Admin/AdminImages.aspx?r=OfflineEndorsement" Target="_blank" runat="server">Review Offline Endorsements</asp:HyperLink>
            </td>
            <td>
                <asp:Button ID="btnSyncOfflineEndorsements" runat="server" Text="Sync Offline Endorsements to DB" OnClick="btnSyncOfflineEndorsements_Click" />
            </td>
            <td>
                <asp:Button ID="btnDelS3OfflineEndorsementOrphans" runat="server" Text="Delete Orphan S3 Images" OnClick="btnDelS3OfflineEndorsementOrphans_Click" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:HyperLink ID="lnkBasicMedImages" NavigateUrl="~/Admin/AdminImages.aspx?r=BasicMed" Target="_blank" runat="server">Review BasicMed</asp:HyperLink>
            </td>
            <td>
                <asp:Button ID="btnSyncBasicMed" runat="server" 
                    Text="Sync BasicMed Images to DB" onclick="btnSyncBasicMed_Click" />
            </td>
            <td>
                <asp:Button ID="btnDelS3BasicMedOrphans" runat="server" OnClick="btnDelS3BasicMedOrphans_Click" Text="Delete Orphan S3 Images" />
            </td>
        </tr>
    </table>
    <asp:CheckBox ID="ckPreviewOnly" runat="server" Text="Preview Only (no changes made)" />
    <asp:PlaceHolder ID="plcDBSync" runat="server"></asp:PlaceHolder>
    <h3>Images to fix:</h3>
    <asp:SqlDataSource ID="sqlDSImagesToFix" runat="server" 
        ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
        UpdateCommand="UPDATE images SET ThumbWidth=?thumbWidth, ThumbHeight=?thumbHeight WHERE VirtPathID=?VirtpathID AND ImageKey=?imagekey AND ThumbFilename=?thumbfilename"
        SelectCommand="SELECT CONCAT('~/images/', ELT(VirtPathID + 1, 'Flights/', 'Aircraft/ID/', 'Endorsements/'), ImageKey, '/', ThumbFilename) AS URL, images.* from images where imageType=1 OR thumbwidth=0 OR thumbheight=0 OR islocal&lt;&gt;0">
        <UpdateParameters>
            <asp:Parameter Name="thumbWidth" Type="Int32"  Direction="InputOutput" />
            <asp:Parameter Name="thumbHeight" Type="Int32" Direction="InputOutput" />
            <asp:Parameter Name="virtpathID" Type="Int32" Direction="InputOutput" />
            <asp:Parameter Name="imagekey" Type="String" Size="50"  Direction="InputOutput" />
            <asp:Parameter Name="thumbfilename" Type="String" Size="50"  Direction="InputOutput" />
        </UpdateParameters>
    </asp:SqlDataSource>
    <asp:GridView ID="gvImagesToFix" AutoGenerateColumns="false" AutoGenerateEditButton="True" EnableModelValidation="true" DataKeyNames="VirtPathID,ImageKey,ThumbFilename" DataSourceID="sqlDSImagesToFix" runat="server">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:HyperLink ID="lnkImage" runat="server" NavigateUrl='<%# Eval("URL") %>' Target="_blank">
                        <asp:Image ID="Image1" runat="server" ImageUrl='<%# Eval("URL") %>' />
                    </asp:HyperLink>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="VirtPathID" HeaderText="VirtPathID" />
            <asp:BoundField DataField="ImageKey" HeaderText="ImageKey"/>
            <asp:BoundField DataField="ThumbFileName" HeaderText="ThumbFileName"/>
            <asp:BoundField DataField="ImageType" HeaderText="ImageType"/>
            <asp:BoundField DataField="ThumbWidth" HeaderText="ThumbWidth"/>
            <asp:BoundField DataField="ThumbHeight" HeaderText="ThumbHeight" />
            <asp:BoundField DataField="Comment" HeaderText="Comment"/>
            <asp:BoundField DataField="Latitude" HeaderText="Latitude" />
            <asp:BoundField DataField="Longitude" HeaderText="Longitude" />
            <asp:BoundField DataField="IsLocal" HeaderText="IsLocal" />
        </Columns>
        <EmptyDataTemplate>
            <p>(No problematic images found)</p>
        </EmptyDataTemplate>
    </asp:GridView>
    <h2>Videos</h2>
    <p>
        <asp:Button ID="btnCleanPendingVideos" runat="server" Text="Process Incomplete Pending Videos" OnClick="btnCleanPendingVideos_Click" /> <asp:Label ID="lblPVResults" runat="server" />
        <asp:GridView ID="gvVideos" runat="server" AutoGenerateColumns="false">
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:HyperLink ID="lnkVideo" runat="server" Target="_blank" Text='<%# Container.DataItem.ToString() %>' NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}?a=1", Container.DataItem) %>' />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </p>
</asp:Content>