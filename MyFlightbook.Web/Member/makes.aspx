<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Title="Aircraft Makes and Models" Inherits="makes" culture="auto" meta:resourcekey="PageResource2" Codebehind="makes.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbMakeListItem.ascx" tagname="mfbMakeListItem" tagprefix="uc2" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc2" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc2" TagName="mfbSearchbox" %>


<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locMakesHeader" runat="server" 
            Text="Aircraft Models" meta:resourcekey="locMakesHeaderResource1"></asp:Localize>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/endless-scroll.js") %>'></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <div style="float:right; margin: 5px; padding:5px; max-width: 200px; border: 1px solid black">
        <asp:Localize ID="locPageTop" runat="server" Text="Don&#39;t see the make/model of aircraft that you fly?" 
            meta:resourcekey="locPageTopResource1"></asp:Localize>
        <div><asp:LinkButton ID="btnAddNew" runat="server" Text="Create a new model" Font-Bold="true"
            onclick="btnAddNew_Click" meta:resourcekey="btnAddNewResource1" /></div>
    </div>
    <h2><asp:Label ID="lblSearchPrompt" runat="server" Text="Find models of aircraft"></asp:Label></h2>
    <asp:MultiView ID="mvSearchForm" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwSimpleSearch" runat="server">
            <uc2:mfbSearchbox runat="server" ID="mfbSearchbox" Hint="<%$ Resources:Makes, SearchTip %>" OnSearchClicked="FilterTextChanged" />
            <div><asp:LinkButton ID="lnkAdvanced" runat="server" Text="Advanced Search" OnClick="lnkAdvanced_Click" meta:resourcekey="lnkAdvancedResource1"></asp:LinkButton></div>
        </asp:View>
        <asp:View ID="vwAdvancedSearch" runat="server">
            <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" meta:resourcekey="pnlSearchResource2">
                <table>
                    <tr>
                        <td>
                            <asp:Localize ID="locManufacturer" runat="server" Text="Manufacturer:" meta:resourcekey="locManufacturerResource1"></asp:Localize>
                        </td>
                        <td>
                            <asp:TextBox ID="txtManufacturer" runat="server" meta:resourcekey="txtManufacturerResource1"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Localize ID="locModel" runat="server" Text="Model ID:" meta:resourcekey="locModelResource1"></asp:Localize><br />
                            <span class="fineprint"><asp:Localize ID="locModelIDExample" Text='(e.g., "C-172")' runat="server" meta:resourcekey="locModelIDExampleResource1"></asp:Localize></span>
                        </td>
                        <td>
                            <asp:TextBox ID="txtModel" runat="server" meta:resourcekey="txtModelResource1"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Localize ID="locModelName" runat="server" Text="Model Name:" meta:resourcekey="locModelNameResource1"></asp:Localize><br />
                            <span class="fineprint"><asp:Localize ID="locModelNameExample" Text='(e.g., "Skyhawk")' runat="server" meta:resourcekey="locModelNameExampleResource1"></asp:Localize></span>
                        </td>
                        <td>
                            <asp:TextBox ID="txtModelName" runat="server" meta:resourcekey="txtModelNameResource1"></asp:TextBox> <uc2:mfbTooltip runat="server" ID="mfbTooltip" BodyContent="<%$ Resources:Makes, searchICAOTip %>" />
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Localize ID="locType" runat="server" Text="Type designator:" meta:resourcekey="locTypeResource1"></asp:Localize>
                        </td>
                        <td>
                            <asp:TextBox ID="txtTypeName" runat="server" meta:resourcekey="txtTypeNameResource1"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Localize ID="locCatClass" runat="server" Text="Category/Class:" meta:resourcekey="locCatClassResource1"></asp:Localize>
                        </td>
                        <td>
                            <asp:TextBox ID="txtCatClass" runat="server" meta:resourcekey="txtCatClassResource1"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td><asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="FilterTextChanged" meta:resourcekey="btnSearchResource1" />
                        </td>
                    </tr>
                </table>
                <asp:LinkButton ID="lnkSimpleSearch" runat="server" Text="Simple Search" OnClick="lnkSimpleSearch_Click" meta:resourcekey="lnkSimpleSearchResource1"></asp:LinkButton>
            </asp:Panel>
        </asp:View>
    </asp:MultiView>
    <p><% =Resources.Makes.searchWildcardTip %></p>
    <asp:HiddenField runat="server" ID="hdnQueryJSON" />
    <div style="width:800px">
        <table width="100%" runat="server" id="tblHeaderRow" visible="false">
            <tr>
                <td style="width:160px;"></td>
                <td style="text-align:left;">
                    <table style="width:100%">
                        <tr>
                            <td style="width:30%;">
                                <asp:LinkButton ID="lnkSortManufacturer" runat="server" Text="Manufacturer" 
                                    Font-Bold="False" meta:resourcekey="lnkSortManufacturerResource1" 
                                    onclick="lnkSortManufacturer_Click"></asp:LinkButton>&nbsp;&nbsp;&nbsp;
                            </td>
                            <td style="width:40%;">
                                <asp:LinkButton ID="lnkSortModel" runat="server" Text="Model" Font-Bold="True" 
                                    meta:resourcekey="lnkSortModelResource1" onclick="lnkSortModel_Click"></asp:LinkButton>&nbsp;&nbsp;&nbsp;
                            </td>
                            <td style="width:30%;">
                                <asp:LinkButton ID="lnkSortCatclass" runat="server" Text="Category/Class" 
                                    Font-Bold="False" meta:resourcekey="lnkSortCatclassResource1" 
                                    onclick="lnkSortCatclass_Click"></asp:LinkButton>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>    
        <asp:GridView ID="gvMakes" GridLines="None" CellPadding="10" runat="server" EnableViewState="False"
        AutoGenerateColumns="False" OnRowDataBound="MakesRowDataBound" 
            ShowHeader="False"
            Width="100%" meta:resourcekey="gvMakesResource1">
            <Columns>
                <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
                    <ItemTemplate>
                        <uc2:mfbMakeListItem ID="mfbMakeListItem1" runat="server" />
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Top" />
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
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
                            data: $.toJSON(params),
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
    <asp:Label ID="lblError" runat="server" CssClass="error" 
        meta:resourcekey="lblErrorResource1"></asp:Label><br />
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
</asp:content>
