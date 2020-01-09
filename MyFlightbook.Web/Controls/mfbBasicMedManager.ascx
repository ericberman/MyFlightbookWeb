<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbBasicMedManager" Codebehind="mfbBasicMedManager.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc7" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc8" %>
<%@ Register Src="../Controls/mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc2" %>
<asp:Panel ID="pnlBasicMed" runat="server">
    <h3><% =Resources.Profile.BasicMedHeader %></h3>
    <p><% =Resources.Profile.BasicMedDescription %></p>
    <ol style="list-style-type:lower-alpha">
        <li><%=Resources.Profile.BasicMedDescriptionA %></li>
        <li><%=Resources.Profile.BasicMedDescriptionB %></li>
        <li><%=Resources.Profile.BasicMedDescriptionC %></li>
    </ol>
    <p><a href="javascript:void(0);"><asp:Label ID="lblAddBaiscMedEvent" runat="server" Font-Bold="True" Text="<%$ Resources:Profile, BasicMedAddEventPrompt %>"></asp:Label></a></p>
    <asp:Panel ID="pnlAddBasicMedEvent" runat="server">
        <table>
            <tr>
                <td><% =Resources.Profile.BasicMedEventDate %></td>
                <td><uc2:mfbTypeInDate runat="server" ID="mfbBasicMedEventDate" DefaultType="Today" /></td>
            </tr>
            <tr>
                <td><% =Resources.Profile.BasicMedEventActivity %></td>
                <td>
                    <asp:RadioButtonList ID="rblBasicMedAction" runat="server">
                        <asp:ListItem Text="<%$ Resources:Profile, BasicMedMedicalCourse %>" Selected="True" Value="AeromedicalCourse"></asp:ListItem>
                        <asp:ListItem Text="<%$ Resources:Profile, BasicMedPhysicianVisit %>" Value="PhysicianVisit"></asp:ListItem>
                    </asp:RadioButtonList>
                </td>
            </tr>
            <tr style="vertical-align:top">
                <td><% =Resources.Profile.BasicMedEventDescription %></td>
                <td><asp:TextBox ID="txtBasicMedNotes" TextMode="MultiLine" runat="server"></asp:TextBox></td>
            </tr>
            <tr style="vertical-align:top">
                <td></td>
                <td>
                    <p><% =Resources.Profile.BasicMedAttachDocumentation %></p>
                    <uc7:mfbMultiFileUpload runat="server" ID="mfuBasicMedImages" Mode="Legacy" Class="BasicMed" IncludeDocs="true" IncludeVideos="false" RefreshOnUpload="true" />
                </td>
            </tr>
            <tr style="vertical-align:top">
                <td></td>
                <td>
                    <br />
                    <asp:Button ID="btnAddBasicMedEvent" runat="server" Text="<%$ Resources:Profile, BasicMedAddEvent %>" OnClick="btnAddBasicMedEvent_Click" />
                    <asp:Label ID="lblBasicMedErr" CssClass="error" runat="server" EnableViewState="False"></asp:Label>
                </td>
            </tr>
        </table>
    </asp:Panel>
    <cc1:CollapsiblePanelExtender ID="cpeBasicMedEvents" runat="server" CollapseControlID="lblAddBaiscMedEvent" Collapsed="True" 
            CollapsedSize="0" 
        CollapsedText="<%$ Resources:Profile, BasicMedAddEventPrompt %>" 
            ExpandControlID="lblAddBaiscMedEvent" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" 
            TargetControlID="pnlAddBasicMedEvent" TextLabelID="lblAddBaiscMedEvent" BehaviorID="cpeBasicMed"></cc1:CollapsiblePanelExtender>
    <asp:GridView ID="gvBasicMedEvents" runat="server" OnRowCommand="gvBasicMedEvents_RowCommand" OnRowEditing="gvBasicMedEvents_RowEditing" BorderStyle="None" BorderWidth="0px"
        CellPadding="3" GridLines="None"
        OnRowCancelingEdit="gvBasicMedEvents_RowCancelingEdit" OnRowUpdating="gvBasicMedEvents_RowUpdating" OnRowDataBound="gvBasicMedEvents_RowDataBound" AutoGenerateColumns="False" ShowHeader="False">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:ImageButton ID="imgDelete" runat="server" 
                        AlternateText="<%$ Resources:Profile, BasicMedDeleteTooltip %>" CommandArgument='<%# Bind("ID") %>' 
                        CommandName="_Delete" ImageUrl="~/images/x.gif" 
                        ToolTip="<%$ Resources:Profile, BasicMedDeleteTooltip %>" />
                    <cc1:ConfirmButtonExtender ID="confirmDeleteBasicMed" runat="server" 
                        ConfirmOnFormSubmit="True" 
                        ConfirmText="<%$ Resources:Profile, BasicMedConfirmDelete %>" 
                        TargetControlID="imgDelete"></cc1:ConfirmButtonExtender>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <div style="font-weight:bold"><%# ((DateTime) Eval("EventDate")).ToShortDateString() %> - <%# Eval("EventTypeDescription") %></div>
                    <div><% =Resources.Profile.BasicMedExpiration  %><span style="font-weight:bold"><%# ((DateTime) Eval("ExpirationDate")).ToShortDateString() %></span></div>
                    <div><%#: Eval("Description") %></div>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <uc8:mfbImageList runat="server" ID="ilBasicMed" CanEdit="false" ImageClass="BasicMed" IncludeDocs="true" />
                </ItemTemplate>
                <EditItemTemplate>
                    <div><uc8:mfbImageList runat="server" ID="ilBasicMed" CanEdit="true" ImageClass="BasicMed" IncludeDocs="true" /></div>
                    <div><uc7:mfbMultiFileUpload runat="server" ID="mfuBasicMedImages" Mode="Legacy" Class="BasicMed" IncludeDocs="true" ImageKey='<%# Bind("ID") %>' IncludeVideos="false" RefreshOnUpload="true" /></div>
                </EditItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
            <asp:CommandField ShowEditButton="True" >
            <ItemStyle VerticalAlign="Top" />
            </asp:CommandField>
        </Columns>
        <EmptyDataTemplate>
            <ul>
                <li>
                    <asp:Label ID="lblNoDeadlines" runat="server" Font-Italic="True" 
                        Text="<%$ Resources:Profile, BasicMedNoEvents %>"></asp:Label>
                </li>
            </ul>
        </EmptyDataTemplate>
    </asp:GridView>
</asp:Panel>
