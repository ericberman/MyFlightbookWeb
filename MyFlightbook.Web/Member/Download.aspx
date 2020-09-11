<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Async="true"
    Codebehind="Download.aspx.cs" Inherits="Member_Download" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc6" %>
<%@ Register Src="../Controls/mfbSimpleTotals.ascx" TagName="mfbSimpleTotals" TagPrefix="uc3" %>
<%@ Register Src="../Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc2" %>
<%@ Register Src="../Controls/mfbSearchAndTotals.ascx" TagName="mfbSearchAndTotals"
    TagPrefix="uc7" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../Controls/mfbDownload.ascx" TagName="mfbDownload" TagPrefix="uc10" %>
<%@ Register src="../Controls/mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc4" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><% = Resources.LocalizedText.DownloadHeader %></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <div style="width:600px; margin-left: auto; margin-right: auto">
    <p><% = Resources.LocalizedText.DownloadYourData %></p>
    <table>
        <tr style="vertical-align: top; padding: 3px">
            <td style="width:50%">
                <div style="vertical-align:middle;">
                    <asp:HyperLink ID="lnkDownloadXL" NavigateUrl="~/Public/MyFlightbook backup.xls"
                        runat="server">
                        <asp:Image ID="imgDownloadXL" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                        <asp:Image ID="imgXLIcon" runat="server" ImageAlign="Middle" ImageUrl="~/images/xlicon_med.png" style="padding-right: 5px;" />
                        <asp:Localize ID="locDownloadXL" runat="server" Text="<%$ Resources:LocalizedText, DownloadLogbookExcel %>"></asp:Localize>
                    </asp:HyperLink>
                </div>
            </td>
            <td>
                <asp:Localize ID="locExcelDesc" runat="server" Text="<%$ Resources:LocalizedText, DownloadLogbookExcelDesc %>"></asp:Localize>
            </td>
        </tr>
        <tr>
            <td style="vertical-align: middle; font-weight: bold; text-align: center;" colspan="2">
                <hr />
                <asp:Label Font-Bold="true" ID="Label1" runat="server" Text="<%$ Resources:LocalizedText, DownloadLogbookSeparator %>"></asp:Label>
            </td>
        </tr>
        <tr style="vertical-align: top; padding: 3px">
            <td>
                <div style="vertical-align:middle;">
                    <asp:LinkButton ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadCSV_Click">
                        <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                        <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_med.png" style="padding-right: 5px;" />
                        <asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:LocalizedText, DownloadLogbookCSV %>"></asp:Localize>
                    </asp:LinkButton>
                </div>
            </td>
            <td>
                <asp:Localize ID="locCSVDesc" runat="server" Text="<%$ Resources:LocalizedText, DownloadLogbookCSVDesc %>"></asp:Localize>
            </td>
        </tr>
        <tr style="vertical-align: top; padding: 3px;">
            <td>
                <br />
                <div style="vertical-align:middle;">
                    <asp:LinkButton ID="lnkDownloadImagesZip" runat="server"
                        onclick="lnkDownloadImagesZip_Click">
                        <asp:Image ID="imgDownloadZIP" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                        <asp:Image ID="imgZipIcon" runat="server" ImageAlign="Middle" ImageUrl="~/images/zip_med.png"  style="padding-right: 5px;"/>
                        <asp:Localize ID="locDownloadZIP" runat="server" Text="<%$ Resources:LocalizedText, ImagesBackupPrompt %>"></asp:Localize>
                    </asp:LinkButton>
                </div>
            </td>
            <td>
                <br />
                <%=Resources.LocalizedText.DownloadLogbookZIPDesc %>
            </td>
        </tr>
        <tr style="vertical-align: top; padding: 3px" runat="server" visible="false" id="rowInsDownload">
            <td>
                <br />
                <div style="vertical-align:middle; " >
                    <asp:LinkButton ID="lnkDownloadInsurance" runat="server" OnClick="lnkDownloadInsurance_Click">
                        <asp:Image ID="imgDwnIns" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                        <asp:Image ID="imgDownZip" runat="server" ImageAlign="Middle" ImageUrl="~/images/zip_med.png"  style="padding-right: 5px;"/>
                        <asp:Localize ID="locIns1" runat="server" Text="<%$ Resources:LocalizedText, InsuranceBackupPrompt %>"></asp:Localize>
                    </asp:LinkButton>
                </div>
            </td>
            <td>
                <br />
                <% =Resources.LocalizedText.InsuranceDownloadDescription %>
            </td>
        </tr>
        <tr>
            <td style="vertical-align: middle;font-weight: bold; text-align: center;" colspan="2">
                <hr />
                <asp:Label Font-Bold="true" ID="lblOR" runat="server" Text="<%$ Resources:LocalizedText, DownloadLogbookSeparator %>"></asp:Label>
            </td>
        </tr>
        <tr style="vertical-align: top; padding: 3px">
            <td>
                <asp:UpdatePanel ID="updDropbox" runat="server">
                    <ContentTemplate>
                        <div>
                            <asp:Button ID="lnkSaveDropbox" runat="server" style="vertical-align:middle;" 
                                Text="<%$ Resources:LocalizedText, DownloadSaveToCloud %>" 
                                onclick="lnkSaveDropbox_Click"></asp:Button>
                            <asp:Image ID="imgDropBox" runat="server" ImageUrl="~/images/dropbox-logos_dropbox-logotype-blue.png" ImageAlign="Middle" Width="150px" />
                        </div>
                        <div>
                            <asp:Button ID="lnkSaveGoogleDrive" runat="server" Text="<%$ Resources:LocalizedText, DownloadSaveToCloud %>" OnClick="lnkSaveGoogleDrive_Click" />
                            <asp:Image ID="imgGoogleDrive" runat="server" ImageUrl="~/images/google-drive-logo-lockup.png" ImageAlign="Middle" Width="150px" />
                        </div>
                        <div>
                            <asp:Button ID="lnkSaveOneDrive" runat="server" Text="<%$ Resources:LocalizedText, DownloadSaveToCloud %>" OnClick="lnkSaveOneDrive_Click" />
                            <asp:Image ID="imgOneDrive" runat="server" ImageUrl="~/images/OneDrive_rgb_Blue2728.png" ImageAlign="Middle" Width="150px" />
                        </div>
                        <asp:CheckBox ID="ckIncludeImages" runat="server" Text="<%$ Resources:LocalizedText, ImagesBackupDropboxPrompt %>" /><uc4:mfbTooltip ID="tooltipZip" runat="server" BodyContent="<%$ Resources:LocalizedText, ImagesBackupFineprint %>" /><br />
                        <div>
                            <asp:Label ID="lblDropBoxSuccess" runat="server" Visible="false" Text="<%$ Resources:LocalizedText, CloudStorageSuccess %>" CssClass="success" EnableViewState="false"></asp:Label>
                            <asp:Label ID="lblDropBoxFailure" runat="server" Visible="false" Text="" CssClass="error" EnableViewState="false"></asp:Label>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
                <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="updDropbox">
                    <ProgressTemplate>
                        <asp:Image ID="imgProgress" ImageUrl="~/images/ajax-loader.gif" runat="server" />
                    </ProgressTemplate>
                </asp:UpdateProgress>
            </td>
            <td>
                <p><asp:Localize ID="locCloudStorageDesc" runat="server" Text="<%$ Resources:LocalizedText, CloudDownloadDescription %>"></asp:Localize></p>
                <p><asp:HyperLink ID="lnkConfigure" runat="server" Text="<%$ Resources:LocalizedText, CloudStorageClickToConfigure %>" NavigateUrl="~/Member/EditProfile.aspx/pftPrefs?pane=backup"></asp:HyperLink></p>
                <p>
                    <asp:Label ID="lblNote" runat="server" Font-Bold="true" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label> 
                    <asp:Localize ID="locDonate" runat="server"></asp:Localize>
                </p>
            </td>
        </tr>
        <tr>
            <td style="vertical-align: middle;font-weight: bold; text-align: center;" colspan="2">
                <hr />
                <asp:Label Font-Bold="true" ID="lblOR2" runat="server" Text="<%$ Resources:LocalizedText, DownloadLogbookSeparator %>"></asp:Label>
            </td>
        </tr>
        <tr>
            <td>
                <uc10:mfbDownload ID="mfbDownload1" runat="server" Visible="true" ShowLogbookData="false" OfferPDFDownload="true" />
            </td>
            <td>
                <% =Resources.LocalizedText.DownloadPDFDescription %>
            </td>
        </tr>
    </table>
        <asp:Panel ID="pnlSkyWatch" runat="server" Visible="false" style="text-align:center; border-radius: 5px; border: 1px solid gray; margin-right:auto; margin-left: auto; margin-top: 40px; padding: 8px; max-width: 300px; box-shadow: 2px 2px 2px 0px rgba(0,0,0,0.75);">
            <div style="vertical-align:middle;">
                <asp:LinkButton ID="lnkPostInsurance" runat="server" OnClick="lnkPostInsurance_Click">
                    <asp:Image ID="imgPostIns" runat="server" ImageAlign="Middle" ImageUrl="~/images/SkyWatch_Logo_New2020.png" Width="250px" style="padding-right: 5px;"/><br />
                    <asp:Localize ID="locIns2" runat="server" Text="<%$ Resources:LocalizedText, InsurancePostPrompt %>" />
                </asp:LinkButton>
            </div>
            <div class="error"><asp:Label ID="lblInsErr" runat="server" EnableViewState="false" /></div>
            <div class="fineprint"><% = Resources.LocalizedText.InsurancePostDisclaimer %></div>
        </asp:Panel>
    </div>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
</asp:Content>
