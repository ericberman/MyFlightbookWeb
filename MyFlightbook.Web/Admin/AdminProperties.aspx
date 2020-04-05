<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Title="Admin - Properties" CodeBehind="AdminProperties.aspx.cs" Inherits="MyFlightbook.Web.Admin.AdminProperties" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools - Properties
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <h2>
        <asp:Label ID="lblCustomProperties" runat="server" Text=""></asp:Label><asp:Label
            ID="lblCustomPropsText" runat="server" Text=""></asp:Label></h2>
    <asp:Panel ID="pnlAddProp" runat="server" DefaultButton="btnNewCustomProp">
        <table>
            <tr>
                <td>Title:</td>
                <td><asp:TextBox ID="txtCustomPropTitle" runat="server" Width="300px"></asp:TextBox></td>
            </tr>
            <tr>
                <td>Format:</td>
                <td><asp:TextBox ID="txtCustomPropFormat" runat="server" Width="300px"></asp:TextBox></td>
            </tr>
            <tr>
                <td>Description:</td>
                <td><asp:TextBox ID="txtCustomPropDesc" runat="server" Width="300px" TextMode="MultiLine"></asp:TextBox></td>
            </tr>
            <tr>
                <td>Type:</td>
                <td>
                    <asp:DropDownList ID="cmbCustomPropType" runat="server">
                        <asp:ListItem Text="Integer" Value="0"></asp:ListItem>
                        <asp:ListItem Text="Decimal" Value="1"></asp:ListItem>
                        <asp:ListItem Text="Boolean" Value="2"></asp:ListItem>
                        <asp:ListItem Text="Date" Value="3"></asp:ListItem>
                        <asp:ListItem Text="DateTime" Value="4"></asp:ListItem>
                        <asp:ListItem Text="String" Value="5"></asp:ListItem>
                        <asp:ListItem Text="Currency" Value="6"></asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>
            <tr style="vertical-align:top">
                <td>Flags:</td>
                <td>
                    <asp:Label ID="lblFlags" runat="server" Text="0"></asp:Label>
                    <asp:CheckBoxList ID="CheckBoxList1" RepeatColumns="2" runat="server" OnSelectedIndexChanged="CheckBoxList1_SelectedIndexChanged"
                        AutoPostBack="True">
                        <asp:ListItem Text="Is a BFR" Value="1"></asp:ListItem>
                        <asp:ListItem Text="Is a IPC" Value="2"></asp:ListItem>
                        <asp:ListItem Text="Unusual Attitude - Descending" Value="4"></asp:ListItem>
                        <asp:ListItem Text="Unusual Attitude - Ascending" Value="8"></asp:ListItem>
                        <asp:ListItem Text="Night Vision - Take-off" Value="16"></asp:ListItem>
                        <asp:ListItem Text="Night Vision - Landing" Value="32"></asp:ListItem>
                        <asp:ListItem Text="Night Vision - Hovering" Value="64"></asp:ListItem>
                        <asp:ListItem Text="Night Vision - Departure / Arrival" Value="128"></asp:ListItem>
                        <asp:ListItem Text="Night Vision - Transitions" Value="256"></asp:ListItem>
                        <asp:ListItem Text="Night Vision - Proficiency Check" Value="512"></asp:ListItem>
                        <asp:ListItem Text="Night Vision - Night Vision Time" Value="32768"></asp:ListItem>
                        <asp:ListItem Text="Glider - Instrument Maneuvers" Value="1024"></asp:ListItem>
                        <asp:ListItem Text="Glider - Instrument Maneuvers for passengers" Value="2048"></asp:ListItem>                    
                        <asp:ListItem Text="Is an approach" Value="4096"></asp:ListItem>                    
                        <asp:ListItem Text="Don't add to totals" Value="8192"></asp:ListItem>                    
                        <asp:ListItem Text="Night-time Takeoff" Value="16384"></asp:ListItem>
                        <asp:ListItem Text="PIC Proficiency Check" Value="65536"></asp:ListItem>
                        <asp:ListItem Text="Base Check" Value="131072"></asp:ListItem>
                        <asp:ListItem Text="Solo" Value="262144"></asp:ListItem>
                        <asp:ListItem Text="Glider Ground Launch" Value="524288"></asp:ListItem>
                        <asp:ListItem Text="Exclude from MRU" Value="1048576"></asp:ListItem>
                        <asp:ListItem Text="Decimal is not time" Value="2097152"></asp:ListItem>
                        <asp:ListItem Text="UAS Launch" Value="4194304"></asp:ListItem>
                        <asp:ListItem Text="UAS Recovery" Value="8388608"></asp:ListItem>
                        <asp:ListItem Text="Known property" Value="16777216"></asp:ListItem>
                        <asp:ListItem Text="No autocomplete" Value="33554432"></asp:ListItem>
                        <asp:ListItem Text="Convert to Caps" Value="67108864"></asp:ListItem>
                    </asp:CheckBoxList>
                </td>
            </tr>
        </table>
        <asp:Button ID="btnNewCustomProp" runat="server" Text="Add a custom property" OnClick="btnNewCustomProp_Click" />
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" runat="Server">
    <asp:SqlDataSource ID="sqlCustomProps" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT * FROM custompropertytypes ORDER BY idPropType DESC"
        UpdateCommand="UPDATE custompropertytypes SET Title=?Title, SortKey=IF(?SortKey IS NULL, '', ?SortKey), FormatString=?FormatString, Description=?Description, Type=?Type, Flags=?Flags WHERE idPropType=?idPropType">
        <UpdateParameters>
            <asp:Parameter Name="Title" Direction="Input" Type="String" />
            <asp:Parameter Name="FormatString" Direction="Input" Type="String" />
            <asp:Parameter Name="Type" Direction="Input" Type="Int32" />
            <asp:Parameter Name="Flags" Direction="Input" Type="Int32" />
            <asp:Parameter Name="idPropType" Direction="Input" Type="Int32" />
            <asp:Parameter Name="Description" Direction="Input" Type="String" />
            <asp:Parameter Name="SortKey" Direction="Input" Type="String" ConvertEmptyStringToNull="false" />
        </UpdateParameters>
    </asp:SqlDataSource>
    <h2>Existing Props</h2><asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <asp:GridView ID="gvCustomProps" runat="server" AllowSorting="True" DataSourceID="sqlCustomProps"
                AutoGenerateEditButton="True" AutoGenerateColumns="false" OnRowUpdated="CustPropsRowEdited" 
                EnableModelValidation="True">
                <Columns>
                    <asp:BoundField DataField="idPropType" HeaderText="PropertyTypeID" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="SortKey" HeaderText="SortKey" />
                    <asp:BoundField DataField="FormatString" HeaderText="Format String" />
                    <asp:BoundField DataField="Description" HeaderText="Description" />
                    <asp:BoundField DataField="Type" HeaderText="Type" />
                    <asp:BoundField DataField="Flags" HeaderText="Flags" />
                    <asp:TemplateField HeaderText="Description">
                        <ItemTemplate>
                            <asp:Label ID="lblFlagsDesc" runat="server" Text='<%# CustomPropertyType.AdminFlagsDesc((CFPPropertyType)Convert.ToInt32(Eval("Type")), (UInt32)(Convert.ToInt32(Eval("Flags")))) %>'></asp:Label>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>