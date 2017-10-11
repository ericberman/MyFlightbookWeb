<%@ Page Title="Sign an entry" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="SignEntry.aspx.cs" Inherits="Public_SignEntry" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbSignFlight.ascx" tagname="mfbSignFlight" tagprefix="uc3" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <h2><asp:Label ID="lblHeader" runat="server" Text=""></asp:Label></h2>
    <asp:MultiView ID="mvSignFlight" runat="server" ActiveViewIndex="0">
        <asp:View runat="server" ID="vwPickInstructor">
            <asp:Label ID="Label2" runat="server" Text="<%$ Resources:SignOff, ChooseInstructorsPrompt %>"></asp:Label>
            <asp:DropDownList ID="cmbInstructors" runat="server" AutoPostBack="true" AppendDataBoundItems="true" DataValueField="UserName" DataTextField="UserFullName" 
                onselectedindexchanged="cmbInstructors_SelectedIndexChanged">
                <asp:ListItem Selected="True" Text="<%$ Resources:SignOff, NewInstructor %>" Value=""></asp:ListItem>
            </asp:DropDownList>
            <asp:Button ID="btnNewInstructor" runat="server" 
                Text="<%$ Resources:LocalizedText, NextPrompt %>" onclick="btnNewInstructor_Click" 
                />
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
</asp:Content>

