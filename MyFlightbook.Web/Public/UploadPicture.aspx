<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_UploadPicture" Codebehind="UploadPicture.aspx.cs" %>

<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc1" %>

    <form id="form1" runat="server" visible="false">
    <div>
        <asp:TextBox ID="txtAuthToken" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtFlight" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtComment" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtLat" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtLon" runat="server"></asp:TextBox>
        <input id="imgPicture" name="imgPicture" runat="server" type="file" />
    </div>
    <uc1:mfbImageList ID="mfbImageFlight" runat="server" ImageClass="Flight" />
    </form>
