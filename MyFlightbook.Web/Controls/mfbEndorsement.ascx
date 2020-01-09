<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEndorsement" Codebehind="mfbEndorsement.ascx.cs" %>
<asp:FormView ID="FormView1" runat="server">
    <ItemTemplate>
        <div style="padding: 5px">
            <table class="endorsement">
                <tr id="rowExternalEndorsement" runat="server" visible='<%# (bool) Eval("IsExternalEndorsement") %>'><td colspan="2" style="font-weight:bold; background-color:darkgray; color:white">
                    <%= MyFlightbook.Branding.ReBrand(Resources.SignOff.ExternalEndorsementDisclaimer) %></td></tr>
                <tr><td colspan="2" style="font-weight:bold"><%# Eval("FullTitleAndFar") %></td></tr>
                <tr><td colspan="2"><hr /><%# Eval("EndorsementText") %><hr /></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal1" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDatePrompt %>"></asp:Label></td><td><%# Convert.ToDateTime(Eval("Date")).ToShortDateString()  %></td></tr>
                <tr id="rowCreationDate" runat="server" visible='<%# ((DateTime) Eval("CreationDate")).Date.CompareTo(((DateTime) Eval("Date")).Date) != 0 %>'><td><asp:Label Font-Bold="true" ID="Literal6" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDateCreatedPrompt %>"></asp:Label></td><td>
                    <asp:Label ID="lblCreationDate" runat="server" Font-Bold='<%# ((DateTime) Eval("CreationDate")).Date.Subtract(((DateTime) Eval("Date"))).Days > 10 %>' Text='<%# Convert.ToDateTime(Eval("CreationDate")).ToShortDateString() %>'></asp:Label></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal2" runat="server" Text="<%$ Resources:SignOff, EditEndorsementStudentPrompt %>"></asp:Label></td><td><%# Eval("StudentDisplayName") %></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal3" runat="server" Text="<%$ Resources:SignOff, EditEndorsementInstructorPrompt %>"></asp:Label></td><td><%# Eval("CFIDisplayName") %></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal4" runat="server" Text="<%$ Resources:SignOff, EditEndorsementCFIPrompt %>"></asp:Label></td><td><%# Eval("CFICertificate") %></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal5" runat="server" Text="<%$ Resources:SignOff, EditEndorsementExpirationPrompt %>"></asp:Label></td><td><%# ((DateTime) Eval("CFIExpirationDate")).HasValue() ? ((DateTime) Eval("CFIExpirationDate")).ToShortDateString() : Resources.SignOff.EndorsementNoDate %></td></tr>
                <tr runat="server" visible='<%# Eval("IsAdHocEndorsement") %>'>
                    <td colspan="2">
                        <img src='<%# Eval("DigitizedSigLink") %>' />
                    </td>
                </tr>
            </table>
        </div>
    </ItemTemplate>
</asp:FormView>
