<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEndorsement.ascx.cs" Inherits="Controls_mfbEndorsement" %>
<asp:FormView ID="FormView1" runat="server">
    <ItemTemplate>
        <div style="padding: 5px">
            <table class="endorsement">
                <tr id="rowExternalEndorsement" runat="server" visible='<%# (bool) Eval("IsExternalEndorsement") %>'><td colspan="2" style="font-weight:bold; background-color:darkgray; color:white">
                    <%= MyFlightbook.Branding.ReBrand(Resources.SignOff.ExternalEndorsementDisclaimer) %></td></tr>
                <tr><td colspan="2" style="font-weight:bold"><%# Eval("FullTitleAndFar") %></td></tr>
                <tr><td colspan="2"><hr /><%# Eval("EndorsementText") %><hr /></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal1" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDatePrompt %>"></asp:Label></td><td><%# Convert.ToDateTime(Eval("Date")).ToShortDateString()  %></td></tr>
                <tr id="rowCreationDate" runat="server"><td><asp:Label Font-Bold="true" ID="Literal6" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDateCreatedPrompt %>"></asp:Label></td><td><%# Convert.ToDateTime(Eval("CreationDate")).ToShortDateString()  %></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal2" runat="server" Text="<%$ Resources:SignOff, EditEndorsementStudentPrompt %>"></asp:Label></td><td><%# Eval("StudentDisplayName") %></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal3" runat="server" Text="<%$ Resources:SignOff, EditEndorsementInstructorPrompt %>"></asp:Label></td><td><%# Eval("CFIDisplayName") %></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal4" runat="server" Text="<%$ Resources:SignOff, EditEndorsementCFIPrompt %>"></asp:Label></td><td><%# Eval("CFICertificate") %></td></tr>
                <tr><td><asp:Label Font-Bold="true" ID="Literal5" runat="server" Text="<%$ Resources:SignOff, EditEndorsementExpirationPrompt %>"></asp:Label></td><td><%# Convert.ToDateTime(Eval("CFIExpirationDate")).ToShortDateString() %></td></tr>
            </table>
        </div>
    </ItemTemplate>
</asp:FormView>
