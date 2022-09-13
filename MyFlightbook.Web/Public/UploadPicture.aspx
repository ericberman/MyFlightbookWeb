<%@ Page Language="C#" AutoEventWireup="true" Codebehind="UploadPicture.aspx.cs" Inherits="MyFlightbook.Image.UploadPicture" %>

    <form id="form1" runat="server" visible="false">
    <div>
        <asp:TextBox ID="txtAuthToken" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtFlight" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtComment" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtLat" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtLon" runat="server"></asp:TextBox>
        <input id="imgPicture" name="imgPicture" runat="server" type="file" />
    </div>
    </form>
