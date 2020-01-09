<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" Title="Contact Us" AutoEventWireup="true" Inherits="Public_ContactMe" ValidateRequest="false" Codebehind="ContactMe.aspx.cs" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentTitle" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% = Resources.LocalizedText.ContactUsTitle %>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:MultiView ID="mvContact" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwContact" runat="server">
            <asp:Panel ID="Panel1" runat="server" DefaultButton="btnSend">
                <p>
                    <asp:HyperLink ID="lnkFAQ" runat="server" Font-Bold="true" NavigateUrl="~/Public/FAQ.aspx" Target="_blank" Text="<%$ Resources:LocalizedText, ContactUsReadFAQ %>"></asp:HyperLink>
                </p>
                <div>
                    <asp:Label ID="Literal1" runat="server" Text="<%$ Resources:LocalizedText, ContactUsName %>"></asp:Label>
                </div>
                <div>
                    <asp:TextBox ID="txtName" runat="server" Width="280px"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" CssClass="error"
                        ErrorMessage="<%$ Resources:LocalizedText, ValidationNameRequired %>" ControlToValidate="txtName" Display="Dynamic"></asp:RequiredFieldValidator>
                </div>
                <div>&nbsp;</div>
                <div>
                    <asp:Label ID="Literal3" Text="<%$ Resources:LocalizedText, ContactUsEmail %>" runat="server"></asp:Label>
                </div>
                <div>
                    <asp:TextBox ID="txtEmail" runat="server" Width="280px"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" CssClass="error"
                        ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailRequired %>" ControlToValidate="txtEmail"
                        SetFocusOnError="True" Display="Dynamic"></asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="RegularExpressionValidator1"
                        runat="server" CssClass="error"
                        ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailFormat %>"
                        ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                        ControlToValidate="txtEmail" SetFocusOnError="True" Display="Dynamic"></asp:RegularExpressionValidator>
                </div>
                <div>&nbsp;</div>
                <div>
                    <asp:Label ID="lblSubjectPrompt" runat="server" Text="<%$ Resources:LocalizedText, ContactUsSubject %>"></asp:Label>
                </div>
                <div>
                    <asp:TextBox ID="txtSubject" runat="server" Width="280px"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="reqSubject" runat="server" ErrorMessage="*" Display="Dynamic" CssClass="error" ControlToValidate="txtSubject"></asp:RequiredFieldValidator>
                </div>
                <div>&nbsp;</div>
                <div>
                    <asp:Label ID="lblMessagePrompt" runat="server" Text="<%$ Resources:LocalizedText, ContactUsMessage %>"></asp:Label>
                </div>
                <div>
                    <asp:TextBox ID="txtComments" runat="server" Rows="5" TextMode="MultiLine"
                        Width="280px"></asp:TextBox>
                </div>
                <div id="rowAttach" runat="server">
                    <div>&nbsp;</div>
                    <asp:FileUpload ID="fuFile" runat="server" AllowMultiple="true" />
                </div>
                <div style="width:280px; text-align:right;">
                    <asp:Button ID="btnSend" runat="server" Text="<%$ Resources:LocalizedText, ContactUsSend %>" OnClick="btnSend_Click" />
                </div>
                <cc1:NoBot ID="NoBot1" runat="server" />
            </asp:Panel>
        </asp:View>
        <asp:View ID="vwThanks" runat="server">
            <p>
                <asp:Label ID="Literal6" runat="server" Text="<%$ Resources:LocalizedText, ContactUsThankYou %>"></asp:Label></p>
            <p>
                <asp:HyperLink ID="lnkReturn" runat="server" NavigateUrl="~/Default.aspx" Text="<%$ Resources:LocalizedText, ContactUsReturnHome %>"></asp:HyperLink>
            </p>
        </asp:View>
    </asp:MultiView>
</asp:Content>

