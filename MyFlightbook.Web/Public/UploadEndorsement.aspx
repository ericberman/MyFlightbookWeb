<%@ Page Language="C#" AutoEventWireup="true" Codebehind="UploadEndorsement.aspx.cs" Inherits="MyFlightbook.Image.UploadEndorsement" %>
    <form id="form1" runat="server" visible="false">
    <div>
        <asp:TextBox ID="txtAuthToken" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtComment" runat="server"></asp:TextBox>
        <input id="imgPicture" name="imgPicture" runat="server" type="file" />
    </div>
    </form>
