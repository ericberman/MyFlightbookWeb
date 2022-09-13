<%@ Control Language="C#" AutoEventWireup="true" Codebehind="ClubAircraftSchedule.ascx.cs" Inherits="Controls_ClubControls_ClubAircraftSchedule" %>
<%@ Register src="../mfbResourceSchedule.ascx" tagname="mfbResourceSchedule" tagprefix="uc1" %>
<asp:FormView ID="fvClubaircraft" runat="server" CssClass="mfbDefault">
    <ItemTemplate>
        <uc1:mfbResourceSchedule ID="mfbResourceSchedule1" NavInitClientFunction="InitClubNav" Mode='<%# Mode %>' HideNavContainer="true" ResourceHeader='<%# Eval("DisplayTailnumberWithModel") %>'
            ResourceID='<%# Eval("AircraftID") %>' ClubID='<%# Eval("ClubID") %>' runat="server">
            <ResourceTemplate>
                <asp:Panel ID="pnlSampleImage" Visible='<%# Eval("HasSampleImage") %>' Width="200px" runat="server" style="text-align:center;">
                    <asp:Image ID="imgSample" ImageUrl='<%# ((bool) Eval("HasSampleImage")) ? Eval("SampleImageThumbnail") : string.Empty %>' 
                        onclick='<%# ((bool) Eval("HasSampleImage")) ? String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:viewMFBImg(\"{0}\");", System.Web.VirtualPathUtility.ToAbsolute(Eval("SampleImageFull").ToString())) : string.Empty %>' 
                        ImageAlign="Middle" runat="server" />
                    <div><%#: Eval("SampleImageComment") %></div>
                    <br />
                </asp:Panel>
                <div><%# Eval("ClubDescription") %></div>
                <div><%#: Eval("PublicNotes") %></div>
            </ResourceTemplate>
        </uc1:mfbResourceSchedule>
    </ItemTemplate>
</asp:FormView>


