<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_imageSlider" Codebehind="imageSlider.ascx.cs" %>
<asp:Panel ID="pnlSlider" runat="server" Visible="false">
    <ul id="<% =SliderClientID %>" class="bxslider">
        <asp:Repeater ID="rptImages" runat="server">
            <ItemTemplate>
                <li style="max-height:480px; max-width: 480px">
                    <asp:MultiView ID="mvImage" runat="server" ActiveViewIndex='<%# (MyFlightbook.Image.MFBImageInfo.ImageFileType)Eval("ImageType") == MyFlightbook.Image.MFBImageInfo.ImageFileType.S3VideoMP4 ? 0 : 1 %>'>
                        <asp:View ID="vwVideo" runat="server">
                            <div><video width="480" height="360" controls><source src='<%# ((MyFlightbook.Image.MFBImageInfo)Container.DataItem).ResolveFullImage() %>' type="video/mp4"></div>
                            <div><%# Eval("Comment") %></div>
                        </asp:View>
                        <asp:View ID="vwImage" runat="server">
                            <asp:HyperLink ID="lnkImage" runat="server" Target="_blank" NavigateUrl='<%# Eval("URLFullImage") %>'>
                                <asp:Image ID="img" runat="server" AlternateText='<%# Eval("Comment") %>' title='<%# Eval("Comment") %>' ImageUrl='<%# Eval("URLFullImage") %>' />
                            </asp:HyperLink>
                        </asp:View>
                    </asp:MultiView>
                </li>
            </ItemTemplate>
        </asp:Repeater>
    </ul>
    <script>
        $(document).ready(function () {
            $('<% = "#" + SliderClientID %>').bxSlider({
                adaptiveHeight: true,
                video: true,
                useCSS: false,
                captions: true
            });
        });
    </script>
</asp:Panel>
