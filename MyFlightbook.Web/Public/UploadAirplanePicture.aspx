<%@ Page Language="C#" AutoEventWireup="true" Codebehind="UploadAirplanePicture.aspx.cs" Inherits="MyFlightbook.Image.UploadAirplanePicture" %>

    <form id="form1" runat="server" visible="false">
    <div>
        <asp:TextBox ID="txtAuthToken" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtAircraft" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtComment" runat="server"></asp:TextBox>
        <input id="imgPicture" name="imgPicture" runat="server" type="file" />
    </div>
    </form>
