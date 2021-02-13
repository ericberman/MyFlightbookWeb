<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbPublicFlightItem.ascx.cs" Inherits="Controls_mfbPublicFlightItem" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc3" %>
<tr style="vertical-align:top">
    <td colspan="2">
        <asp:Label ID="lblDate" Font-Bold="true" runat="server"></asp:Label> - 
        <asp:Label Font-Bold="true" ID="lblTail" runat="server" CssClass="hintTrigger"></asp:Label>
        <asp:Label ID="lblDetails" runat="server"></asp:Label>
        <asp:HyperLink ID="lnkFlight" runat="server"><asp:Label ID="lblroute" runat="server"></asp:Label></asp:HyperLink>
        <asp:Label ID="lblComments" runat="server" style="white-space:pre-line;"></asp:Label>
    </td>
</tr>
<tr style="vertical-align:top">
    <td>
        <div class="ilItem">
            <asp:HyperLink ID="lnkFullAc" runat="server" Target="_blank">
                <asp:Image ID="imgAc" runat="server" />
            </asp:HyperLink>
        </div>
        <div style="text-align:center">
            <asp:Label ID="lblModel" runat="server" Text=""></asp:Label> <asp:Label ID="lblCatClass" runat="server" Text=""></asp:Label>
        </div>
    </td>
    <td>
        <asp:Repeater ID="rptFlightImages" runat="server" OnItemDataBound="rptFlightImages_ItemDataBound">
            <ItemTemplate>
                <div class="ilItem">
                    <div>
                        <asp:Literal ID="litVideoOpenTag" runat="server"></asp:Literal>
                            <asp:HyperLink ID="lnkFullPicture" runat="server" Target="_blank" NavigateUrl='<%# Eval("URLFullImage") %>'>
                                <asp:Image ID="img" runat="server" ImageUrl='<%# Eval("URLThumbnail") %>' />
                            </asp:HyperLink>
                        <asp:Literal ID="litVideoCloseTag" runat="server"></asp:Literal>
                    </div>
                    <asp:Panel runat="server" ID="pnlStatic" style="max-width: 200px; text-align:center;"><asp:Label ID="lblComments" runat="server" Text='<%#: Eval("Comment") %>'></asp:Label></asp:Panel>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </td>
</tr>