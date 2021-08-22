<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    Codebehind="MyFlights.aspx.cs" Inherits="Public_MyFlights" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ Register src="../Controls/mfbPublicFlightItem.ascx" tagname="mfbPublicFlightItem" tagprefix="uc4" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <script src='<%= ResolveUrl("~/public/Scripts/endless-scroll.js") %>'></script>
    <asp:GridView ID="gvMyFlights" runat="server" OnRowDataBound="gvMyFlights_rowDataBound"
        GridLines="None" Visible="true" AutoGenerateColumns="false" CellPadding="5" EnableViewState="false"
        AllowPaging="false" AllowSorting="false" ShowFooter="false" ShowHeader="false" >
        <HeaderStyle HorizontalAlign="Left" />
        <Columns>
            <asp:TemplateField>
                <ItemStyle VerticalAlign="Top" />
                <ItemTemplate>
                    <uc4:mfbPublicFlightItem ID="mfbPublicFlightItem1" runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <p><asp:Label ID="lblNoneFound" Font-Bold="true" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightNoneFound %>"></asp:Label></p>
        </EmptyDataTemplate>
    </asp:GridView>

    <script>
        var params = new Object();
        params.skip = "<%=PageSize %>";
        params.pageSize = "<%=PageSize %>";
        params.szUser = "<%=UserName %>";

        $(document).ready(function () {

            $(document).endlessScroll(
        {
            bottomPixels: 300,
            fireOnce: true,
            fireDelay: 2000,
            callback: function (p) {

                // ajax call to fetch next set of rows 
                $.ajax(
                {
                    type: "POST",
                    data: JSON.stringify(params),
                    url: "MyFlights.aspx/HtmlRowsForFlights",
                    dataType: "json",
                    contentType: "application/json",
                    error: function (response) { alert(result.status + ' ' + result.statusText); },
                    complete: function (response) { params.skip = parseInt(params.skip) + parseInt(params.pageSize); },
                    success: function (response) {
                        var FlightRows = response.d;
                        // populate the rows 
                        for (i = 0; i < FlightRows.length; i++) {
                            // Append the row (which is raw HTML), and parse it
                            // We have to parse it for the "Comment" tag to show up.
                            $("#<% =gvMyFlights.ClientID %>").append(FlightRows[i].HTMLRowText);
                         }
                    }
                }
                );
            }
        }
        );
        });
  </script>
</asp:Content>
