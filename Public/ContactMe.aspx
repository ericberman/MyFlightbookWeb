<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" Title="Contact Us" AutoEventWireup="true" CodeFile="ContactMe.aspx.cs" Inherits="Public_ContactMe" ValidateRequest="false" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentTitle" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% = Resources.LocalizedText.ContactUsTitle %>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnSend">
            <div id="entry" runat="server">
                <table>
                    <tr>
                        <td valign="top">
                            <asp:HyperLink ID="lnkFAQ" runat="server" NavigateUrl="~/Public/FAQ.aspx" Target="_blank" Text="<%$ Resources:LocalizedText, ContactUsReadFAQ %>"></asp:HyperLink>
                        </td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:LocalizedText, ContactUsName %>"></asp:Literal>
                            </td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:TextBox ID="txtName" runat="server" Width="280px"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" CssClass="error"
                            ErrorMessage="<%$ Resources:LocalizedText, ValidationNameRequired %>" ControlToValidate="txtName" Display="Dynamic"></asp:RequiredFieldValidator>
                        </td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:Literal ID="Literal3" Text="<%$ Resources:LocalizedText, ContactUsEmail %>" runat="server"></asp:Literal>
                            </td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:TextBox ID="txtEmail" runat="server" Width="280px"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" CssClass="error"
            ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailRequired %>" ControlToValidate="txtEmail" 
                                SetFocusOnError="True" Display="Dynamic"></asp:RequiredFieldValidator>
                             <asp:RegularExpressionValidator ID="RegularExpressionValidator1" 
                                runat="server" CssClass="error"
            ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailFormat %>" 
                                ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" 
                                ControlToValidate="txtEmail" SetFocusOnError="True" Display="Dynamic"></asp:RegularExpressionValidator>
                        </td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:Literal ID="Literal4" runat="server" Text="<%$ Resources:LocalizedText, ContactUsSubject %>"></asp:Literal>
                            </td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:TextBox ID="txtSubject" runat="server" Width="280px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:Literal ID="Literal5" runat="server" Text="<%$ Resources:LocalizedText, ContactUsMessage %>"></asp:Literal>
                            </td>
                    </tr>
                    <tr>
                        <td valign="top">
                            <asp:TextBox ID="txtComments" runat="server" Rows="5" TextMode="MultiLine" 
                                Width="280px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td align="right" valign="top">
                            <asp:Button ID="btnSend" runat="server" Text="<%$ Resources:LocalizedText, ContactUsSend %>" OnClick="btnSend_Click" /></td>
                    </tr>
                </table>
            </div>
            <div id="Thanks" runat="server">
                <p>
                    <asp:Literal ID="Literal6" runat="server" Text="<%$ Resources:LocalizedText, ContactUsThankYou %>"></asp:Literal><br />
                <asp:HyperLink ID="lnkReturn" runat="server" NavigateUrl="~/Default.aspx" Text="<%$ Resources:LocalizedText, ContactUsReturnHome %>"></asp:HyperLink></p>
            </div>
            <cc1:NoBot ID="NoBot1" runat="server" />
    </asp:Panel>
</asp:content>

