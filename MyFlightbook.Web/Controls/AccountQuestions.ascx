<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_AccountQuestions" Codebehind="AccountQuestions.ascx.cs" %>
<div>
    <ajaxToolkit:ComboBox ID="cmbQuestions" runat="server" DropDownStyle="DropDown" Width="400px" OnTextChanged="cmbQuestions_TextChanged">
    </ajaxToolkit:ComboBox>
    <asp:Label ID="lblRequired" runat="server" Text="*" Visible="false"></asp:Label>
</div>
<div>
    <asp:RequiredFieldValidator ID="valQuestionRequired" runat="server" ControlToValidate="cmbQuestions"
            CssClass="error"
        ErrorMessage="<%$ Resources:LocalizedText, AccountQuestionRequired %>"
        Display="Dynamic"></asp:RequiredFieldValidator>
    <asp:RegularExpressionValidator ID="valQuestionLength" runat="server" 
        ControlToValidate="cmbQuestions" Display="Dynamic"  CssClass="error"
        ErrorMessage="<%$ Resources:LocalizedText, AccountQuestionLengthTooLong %>" 
        ValidationExpression=".{0,80}"></asp:RegularExpressionValidator>
</div>