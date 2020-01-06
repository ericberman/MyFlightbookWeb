<%@ Page Title="Sign an entry" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_SignEntry" Codebehind="SignEntry.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbSignFlight.ascx" tagname="mfbSignFlight" tagprefix="uc3" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:MultiView ID="mvSignFlight" runat="server" ActiveViewIndex="0">
        <asp:View runat="server" ID="vwPickInstructor">
            <div><asp:Label ID="Label2" runat="server" Text="<%$ Resources:SignOff, ChooseInstructorsPrompt %>"></asp:Label></div>
            <asp:Repeater ID="rptInstructors" runat="server">
                <ItemTemplate>
                    <div class="signFlightInstructorChoice"><asp:Image ID="imgAdHoc" runat="server" ImageUrl="~/images/signaturesm.png" style="visibility:hidden;" />
                        <asp:LinkButton ID="lnkExistingInstructor" runat="server" OnCommand="chooseInstructor" CommandName="Existing" CommandArgument='<%# Eval("UserName") %>' Text='<%# Eval("UserFullName") %>'></asp:LinkButton></div>
                </ItemTemplate>
            </asp:Repeater>
            <asp:Repeater ID="rptPriorInstructors" runat="server">
                <ItemTemplate>
                    <div class="signFlightInstructorChoice"><asp:Image ID="imgAdHoc" runat="server" ImageUrl="~/images/signaturesm.png" ToolTip="<%$ Resources:SignOff, AdHocSignatureTooltip %>" AlternateText="<%$ Resources:SignOff, AdHocSignatureTooltip %>" />
                        <asp:LinkButton ID="lnkPriorInstructors" runat="server" OnCommand="chooseInstructor" CommandName="Prior" CommandArgument='<%# Eval("CFIName") %>' Text='<%# Eval("CFIName") %>'></asp:LinkButton></div>
                </ItemTemplate>
            </asp:Repeater>
            <div class="signFlightInstructorChoice"><asp:Image ID="imgAdHoc" runat="server" ImageUrl="~/images/signaturesm.png" ToolTip="<%$ Resources:SignOff, AdHocSignatureTooltip %>" AlternateText="<%$ Resources:SignOff, AdHocSignatureTooltip %>" /> <asp:LinkButton ID="lnkNewInstructor" runat="server" Text='<%$ Resources:SignOff, NewInstructor %>' OnClick="lnkNewInstructor_Click"></asp:LinkButton></div>
        </asp:View>
        <asp:View runat="server" ID="vwAcceptTerms">
            <p><asp:Label ID="lblDisclaimerResponse" runat="server" Text=""></asp:Label></p>
            <p style="font-weight:bold"><asp:Label ID="lblDisclaimerResponse2" runat="server" Text=""></asp:Label></p>
            <p><asp:CheckBox ID="ckAccept" runat="server" 
                Text="<%$ Resources:Signoff, SignAcceptResponsibility %>" /></p>
            <p style="text-align:center"><asp:Button ID="btnAcceptResponsibility" runat="server" 
                Text="<%$ Resources:LocalizedText, NextPrompt %>" 
                onclick="btnAcceptResponsibility_Click" /></p>
        </asp:View>
        <asp:View runat="server" ID="vwSign">
            <uc3:mfbSignFlight ID="mfbSignFlight" runat="server" SigningMode="AdHoc" ShowCancel="false" OnSigningFinished="SigningFinished" />
        </asp:View>
        <asp:View runat="server" ID="vwSuccess">
            <asp:Label ID="lblSuccess" runat="server" CssClass="success" Text="<%$ Resources:SignOff, SigningSuccess %>"></asp:Label>
        </asp:View>
    </asp:MultiView>
    <asp:Label ID="lblErr" CssClass="error" runat="server" Text="" EnableViewState="false"></asp:Label>
    <asp:HiddenField ID="hdnUser" runat="server" />
</asp:Content>

