<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_UploadEndorsement" Codebehind="UploadEndorsement.aspx.cs" %>

<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc1" %>

    <form id="form1" runat="server" visible="false">
    <div>
        <asp:TextBox ID="txtAuthToken" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtComment" runat="server"></asp:TextBox>
        <input id="imgPicture" name="imgPicture" runat="server" type="file" />
    </div>
    <uc1:mfbImageList ID="mfbImageEndorsement" runat="server" ImageClass="Endorsement" />
    </form>
