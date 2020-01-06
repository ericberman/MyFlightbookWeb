<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_UploadAirplanePicture" Codebehind="UploadAirplanePicture.aspx.cs" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc1" %>

    <form id="form1" runat="server" visible="false">
    <div>
        <asp:TextBox ID="txtAuthToken" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtAircraft" runat="server"></asp:TextBox>
        <asp:TextBox ID="txtComment" runat="server"></asp:TextBox>
        <input id="imgPicture" name="imgPicture" runat="server" type="file" />
    </div>
    <uc1:mfbImageList ID="mfbImageAircraft" runat="server" ImageClass="Aircraft" />
    </form>
