<%@ Control Language="C#" AutoEventWireup="true" Codebehind="imageSlider.ascx.cs" Inherits="MyFlightbook.Image.imageSlider" %>
<asp:Panel ID="pnlSlider" runat="server" Visible="false">
    <style type="text/css">
        .bxslider-wrap { visibility: hidden; }
    </style>
    <div class="bxslider-wrap">
        <div id="<% =SliderClientID %>" class="bxslider">
            <asp:Repeater ID="rptImages" runat="server">
                <ItemTemplate>
                    <div style="max-height:480px; max-width: 480px">
                        <asp:MultiView ID="mvImage" runat="server" ActiveViewIndex='<%# (MyFlightbook.Image.MFBImageInfo.ImageFileType)Eval("ImageType") == MyFlightbook.Image.MFBImageInfo.ImageFileType.S3VideoMP4 ? 0 : 1 %>'>
                            <asp:View ID="vwVideo" runat="server">
                                <div><video width="480" height="360" controls><source src='<%# ((MyFlightbook.Image.MFBImageInfo)Container.DataItem).ResolveFullImage() %>' type="video/mp4"></div>
                                <div><%#: Eval("Comment") %></div>
                            </asp:View>
                            <asp:View ID="vwImage" runat="server">
                                <img alt='<%#: Eval("Comment") %>' title='<%#: Eval("Comment") %>' src='<%# Eval("URLFullImage") %>' onmousedown="viewMFBImg(this.src); return false;" style="max-width:480px; max-height:480px" />
                            </asp:View>
                        </asp:MultiView>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </div>
    <script>
        $(document).ready(function () {
            $('<% = "#" + SliderClientID %>').bxSlider({
                onSliderLoad: function () {
                    let firstChild = $(this).children().first();
                    let width = firstChild.width();
                    $(this).css("transform", "translate3d(-" + width + "px,0,0)");
                    $(".bxslider-wrap").css("visibility", "visible");
                },
                adaptiveHeight: true,
                video: true,
                useCSS: true,
                captions: true,
                startSlide: 0,
                touchEnabled: true
            });
        });
    </script>
</asp:Panel>
