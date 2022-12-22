<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Codebehind="makes.aspx.cs" Inherits="MyFlightbook.MemberPages.makes" culture="auto" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbMakeListItem.ascx" tagname="mfbMakeListItem" tagprefix="uc2" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc2" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc2" TagName="mfbSearchbox" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locMakesHeader" runat="server" Text="<%$ Resources:Makes, makesHeader %>" />
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <script src='<%= ResolveUrl("~/public/Scripts/endless-scroll.js") %>'></script>
    <div class="calloutSmall shadowed" style="vertical-align:middle; width: 90%; display:flex; margin-right: auto; margin-left: auto; margin-bottom: 5pt;" >
        <div style="flex:auto;" >
            <div style="display:inline-block">
                    <asp:MultiView ID="mvSearchForm" runat="server" ActiveViewIndex="0">
                        <asp:View ID="vwSimpleSearch" runat="server">
                            <div style="vertical-align:top; height: 40px;">
                                <div style="display:inline-block; vertical-align:middle;"><uc2:mfbSearchbox runat="server" ID="mfbSearchbox" Hint="<%$ Resources:Makes, SearchTip %>" OnSearchClicked="FilterTextChanged" /></div>
                                <asp:LinkButton ID="lnkAdvanced" runat="server" Text="<%$ Resources:Makes, makesAdvancedSearch %>" OnClick="lnkAdvanced_Click" style="vertical-align:middle;" />
                            </div>
                            <div class="fineprint"><% =Resources.Makes.searchWildcardTip %></div>
                        </asp:View>
                        <asp:View ID="vwAdvancedSearch" runat="server">
                            <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locManufacturer" runat="server" Text="<%$ Resources:Makes, makesAdvManufacturer %>" />
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtManufacturer" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locModel" runat="server" Text="<%$ Resources:Makes, makesAdvModel %>" /><br />
                                            <span class="fineprint"><asp:Localize ID="locModelIDExample" Text="<%$ Resources:Makes, makesAdvModelSample %>" runat="server" /></span>
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtModel" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locModelName" runat="server" Text="<%$ Resources:Makes, makesAdvModelName %>"/><br />
                                            <span class="fineprint"><asp:Localize ID="locModelNameExample" Text="<%$ Resources:Makes, makesAdvModelNameSample %>" runat="server" /></span>
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtModelName" runat="server" /> <uc2:mfbTooltip runat="server" ID="mfbTooltip" BodyContent="<%$ Resources:Makes, searchICAOTip %>" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locType" runat="server" Text="<%$ Resources:Makes, makesAdvModelType %>" />
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtTypeName" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locCatClass" runat="server" Text="<%$ Resources:Makes, makesAdvCatClass %>" />
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtCatClass" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>&nbsp;</td>
                                        <td><asp:Button ID="btnSearch" runat="server" Text="<%$ Resources:Makes, makesSearchMakes %>" OnClick="FilterTextChanged" />
                                        </td>
                                    </tr>
                                </table>
                                <asp:LinkButton ID="lnkSimpleSearch" runat="server" Text="<%$ Resources:Makes, makesSimpleSearch %>" OnClick="lnkSimpleSearch_Click" />
                            </asp:Panel>
                        </asp:View>
                    </asp:MultiView>
            </div>
        </div>
        <div style="flex:auto; text-align: right;" >
            <div style="display: inline-block">
                <asp:Image ID="imgAdd" runat="server" ImageUrl="~/images/add.png" style="margin-right: 15px; margin-left: 0px; vertical-align:middle;" />
                <asp:LinkButton ID="btnAddNew" runat="server" Text="<%$ Resources:Makes, makesAddModel %>" Font-Bold="true" onclick="btnAddNew_Click"  />
            </div>
        </div>
    </div>
    <asp:HiddenField runat="server" ID="hdnQueryJSON" />
    <div style="width:100%; max-width: 800px; margin-left: auto; margin-right: auto;">
        <table class="gvhDefault" style="width:100%; position: sticky; padding-top: 10px; top: 0px;" runat="server" id="tblHeaderRow" visible="false">
            <tr>
                <td style="width:160px;"></td>
                <td style="text-align:left;">
                    <table style="width:100%">
                        <tr>
                            <td style="width:30%;">
                                <asp:LinkButton ID="lnkSortManufacturer" runat="server" Text="<%$ Resources:Makes, makesSortManufacturer %>" onclick="lnkSortManufacturer_Click" />
                            </td>
                            <td style="width:40%;">
                                <asp:LinkButton ID="lnkSortModel" runat="server" Text="<%$ Resources:Makes, makesSortModel %>" onclick="lnkSortModel_Click" />
                            </td>
                            <td style="width:30%;">
                                <asp:LinkButton ID="lnkSortCatclass" runat="server" Text="<%$ Resources:Makes, makesSortCatClass %>" onclick="lnkSortCatclass_Click" />
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>    
        <asp:GridView ID="gvMakes" GridLines="None" CellPadding="10" runat="server" EnableViewState="False"
        AutoGenerateColumns="False" OnRowDataBound="MakesRowDataBound" ShowHeader="False" Width="100%">
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <uc2:mfbMakeListItem ID="mfbMakeListItem1" runat="server" />
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Top" />
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                <div style="text-align: center"><%# Resources.Makes.makesNoResults %></div>
            </EmptyDataTemplate>
        </asp:GridView>
        <div style="text-align: center;"><asp:Label ID="lblSearchPrompt" runat="server" /></div>
    </div>
    <script>
        var params = new Object();
        params.szRestrict = $('#<% =hdnQueryJSON.ClientID %>').val();
        params.skip = "<%=PageSize %>";
        params.pageSize = "<%=PageSize %>";

        $(document).ready(function () 
        {
            $(document).endlessScroll(
            {
                bottomPixels: 300,
                fireOnce: true,
                fireDelay: 2000,
                callback: function (p) 
                {
                    // ajax call to fetch next set of rows 
                    $.ajax(
                        {
                            type: "POST",
                            data: JSON.stringify(params),
                            url: "Makes.aspx/HtmlRowsForMakes",
                            dataType: "json",
                            contentType: "application/json",
                            error: function (response) {
                                alert(result.status + ' ' + result.statusText); 
                                 },
                            complete: function (response) {
                                params.skip = parseInt(params.skip) + parseInt(params.pageSize); 
                            },
                            success: function (response) {
                                var ModelRows = response.d;
                                // populate the rows 
                                for (i = 0; i < ModelRows.length; i++) {
                                    // Append the row (which is raw HTML)
                                    $("#<% =gvMakes.ClientID %>").append(ModelRows[i]);
                                }
                            }
                        });
                }
            });
        });
    </script>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
</asp:content>
