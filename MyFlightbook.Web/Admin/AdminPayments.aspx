<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminPayments.aspx.cs" MasterPageFile="~/MasterPage.master" Title="Manage Donations" Inherits="MyFlightbook.Web.Admin.AdminPayments" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc3" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools - Payments
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
    <asp:SqlDataSource ID="sqlDSDonations" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
        SelectCommand=""
        UpdateCommand="UPDATE payments SET Notes=?Notes WHERE idPayments=?idPayments" >
        <SelectParameters>
            <asp:Parameter Name="user" DefaultValue="%" />
        </SelectParameters>
        <UpdateParameters>
            <asp:Parameter Name="Notes" DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
            <asp:Parameter Name="idPayments" Direction="Input" Type="Int32" />
        </UpdateParameters>
    </asp:SqlDataSource>
    <asp:Panel ID="Panel2" runat="server" DefaultButton="btnFindDonations">
        <p><asp:Button ID="btnResetGratuities" runat="server" OnClick="btnResetGratuities_Click" Text="Reset Gratuities" /><asp:CheckBox ID="ckResetGratuityReminders" runat="server" Text="Reset gratuity reminders too" /></p>
        <p>View donations for: <asp:TextBox ID="txtDonationUser" runat="server"></asp:TextBox><asp:CheckBoxList ID="ckTransactionTypes" runat="server" RepeatColumns="4"><asp:ListItem Selected="True" Text="Payments" Value="0"></asp:ListItem><asp:ListItem Selected="True" Text="Refunds" Value="1"></asp:ListItem><asp:ListItem Text="Adjustments" Value="2"></asp:ListItem><asp:ListItem Text="Test Transactions" Value="3"></asp:ListItem></asp:CheckBoxList><asp:Button ID="btnFindDonations" runat="server" onclick="btnFindDonations_Click" Text="Find" /></p><p>
            <asp:Button ID="btnComputeStats" runat="server" onclick="btnComputeStats_Click" 
                Text="Fix Donation Fees" />
            <asp:PlaceHolder ID="plcPayments" runat="server"></asp:PlaceHolder>
            <asp:SqlDataSource ID="sqlDSTotalPayments" runat="server" DataSourceMode="DataReader"
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="SELECT CAST(CONCAT(YEAR(Date), '-', LPAD(MONTH(Date), 2, '0')) AS CHAR) AS MonthPeriod, SUM(Amount) AS Gross, SUM(Fee) AS Fee, SUM(Amount - Fee) AS Net, SUM(IF(TransactionID='', 0, Amount-FEE)) AS NetPaypal From Payments WHERE TransactionType IN (0, 1) GROUP BY MonthPeriod ORDER BY MonthPeriod ASC">
            </asp:SqlDataSource>
        </p>
    </asp:Panel>
    <cc1:CollapsiblePanelExtender Collapsed="true" ID="CollapsiblePanelExtender1" runat="server" TargetControlID="pnlTestTransaction" CollapseControlID="lblShowHideTestTransaction" ExpandControlID="lblShowHideTestTransaction"
        TextLabelID="lblShowHideTestTransaction" CollapsedText="Click to show" ExpandedText="Click to hide"></cc1:CollapsiblePanelExtender>
    <p><asp:Label ID="lblHeader" runat="server" Font-Bold="true" Text="Test transaction"></asp:Label>&nbsp;<asp:Label ID="lblShowHideTestTransaction" runat="server" Text="show/hide"></asp:Label></p>
    <asp:Panel ID="pnlTestTransaction" Height="0px" style="overflow:hidden;" DefaultButton="btnEnterTestTransaction" runat="server">
        <table>
            <tr>
                <td>Date: </td><td>
                    <uc1:mfbTypeInDate runat="server" ID="dateTestTransaction" DefaultType="Today" />
                </td>
            </tr>
            <tr>
                <td>Username:</td><td>
                    <asp:TextBox ID="txtTestTransactionUsername" runat="server"></asp:TextBox><asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txtTestTransactionUsername" ErrorMessage="Username required"></asp:RequiredFieldValidator></td></tr><tr>
                <td>Amount: </td><td>
                    <uc3:mfbDecimalEdit ID="decTestTransactionAmount" runat="server" />
                </td>
            </tr>
            <tr>
                <td>Fee: </td><td>
                    <uc3:mfbDecimalEdit ID="decTestTransactionFee" runat="server" />
                </td>
            </tr>
            <tr>
                <td>Transaction Type: </td><td>
                    <asp:DropDownList ID="cmbTestTransactionType" runat="server">
                        <asp:ListItem Text="Payment" Value="0"></asp:ListItem><asp:ListItem Text="Refund" Value="1"></asp:ListItem><asp:ListItem Text="Adjustment" Value="2"></asp:ListItem><asp:ListItem Text="TestTransaction" Value="3"  Selected="True"></asp:ListItem></asp:DropDownList></td></tr><tr>
                <td>Notes:</td><td>
                    <asp:TextBox ID="txtTestTransactionNotes" runat="server"></asp:TextBox></td></tr><tr>
                <td></td>
                <td><asp:Button ID="btnEnterTestTransaction" runat="server" Text="Enter" OnClick="btnEnterTestTransaction_Click" /></td>
            </tr>
        </table>
    </asp:Panel>
    <asp:GridView ID="gvDonations" runat="server" GridLines="None" DataSourceID="sqlDSDonations" CellPadding="5" EnableModelValidation="true" 
        ShowHeader="true" AllowPaging="true" OnPageIndexChanging="gvDonations_PageIndexChanging" PageSize="25" AutoGenerateColumns="false" 
        onrowdatabound="gvDonations_RowDataBound">
        <Columns>
            <asp:TemplateField HeaderText="ID" SortExpression="idPayments" ItemStyle-VerticalAlign="Top">
                <ItemTemplate>
                    <asp:Label ID="LabelPayment" runat="server" Text='<%# Bind("idPayments") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Date" SortExpression="Date" ItemStyle-VerticalAlign="Top">
                <ItemTemplate>
                    <asp:Label ID="LabelDate" runat="server" Text='<%# Bind("Date") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Username" SortExpression="Username" ItemStyle-VerticalAlign="Top">
                <ItemTemplate>
                    <asp:Label ID="LabelUser" runat="server" Text='<%# Bind("Username") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Amount" SortExpression="Amount" ItemStyle-VerticalAlign="Top">
                <ItemTemplate>
                    <asp:Label ID="LabelAmount" runat="server" Text='<%# Bind("Amount") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Fee" SortExpression="Fee" ItemStyle-VerticalAlign="Top">
                <ItemTemplate>
                    <asp:Label ID="LabelFee" runat="server" Text='<%# Bind("Fee") %>'></asp:Label>&nbsp;</ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Net" SortExpression="Net" ItemStyle-VerticalAlign="Top">
                <ItemTemplate>
                    <asp:Label ID="LabelNet" runat="server" Text='<%# Bind("Net") %>'></asp:Label>&nbsp;</ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="TransactionType" SortExpression="TYPE">
                <ItemStyle VerticalAlign="Top" />
                <ItemTemplate>
                    <asp:Label ID="lblTransactionType" runat="server" Text=""></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Notes" SortExpression="">
                <ItemStyle VerticalAlign="Top" />
                <ItemTemplate>
                    <asp:Label ID="lblNotes" runat="server" Text='<%# Bind("Notes") %>'></asp:Label></ItemTemplate><EditItemTemplate>
                    <asp:TextBox ID="txtNotes" runat="server" Text='<%# Bind("Notes") %>' TextMode="MultiLine"></asp:TextBox></EditItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="TransactionData" SortExpression="">
                <ItemStyle VerticalAlign="Top" HorizontalAlign="Left" Width="350px" />
                <ItemTemplate>
                    <asp:Label ID="lblTransactionID" runat="server" Font-Bold="true" Text='<%# Bind("TransactionID") %>'></asp:Label><asp:Panel ID="pnlDecoded" runat="server">
                        <asp:PlaceHolder ID="plcDecoded" runat="server"></asp:PlaceHolder>
                        <asp:Label ID="lblTxNotes" runat="server" Text="Decode"></asp:Label><br /></asp:Panel><cc1:CollapsiblePanelExtender ID="cpeNotes" runat="server"
                        CollapseControlID="lblTransactionID" SuppressPostBack="True"
                        ExpandControlID="lblTransactionID" TargetControlID="pnlDecoded"
                        Collapsed="True">
                    </cc1:CollapsiblePanelExtender>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
    <asp:HiddenField ID="hdnLastDonationSortExpr" runat="server" />
    <asp:HiddenField ID="hdnLastDonationSortDir" runat="server" />
</asp:Content>