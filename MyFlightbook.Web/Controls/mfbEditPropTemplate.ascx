﻿<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbEditPropTemplate.ascx.cs" Inherits="MyFlightbook.Templates.Controls.Controls_mfbEditPropTemplate" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc1" TagName="mfbSearchbox" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>

<h2><asp:Localize ID="locTemplateHeader" runat="server" Text="<%$ Resources:LogbookEntry, TemplateHeader %>" /></h2>
<p><asp:Localize ID="locTemplateDescription1" runat="server" /> <asp:HyperLink ID="lnkLearnMore" Font-Bold="true" Target="_blank" runat="server" Text="<%$ Resources:LogbookEntry, TemplatesLearnMore %>" NavigateUrl="https://myflightbookblog.blogspot.com/2019/06/flight-templates.html" /></p>
<p><asp:Localize ID="locTemplateDescription2" runat="server" /></p>
<p>
    <asp:HyperLink ID="lnkBrowseTemplates" runat="server" NavigateUrl="~/Member/BrowseTemplates.aspx" Font-Bold="true" Text="<%$ Resources:LogbookEntry, TemplateBrowseTemplates %>"></asp:HyperLink>
    <asp:Localize ID="OrAdd" runat="server" Text="<%$ Resources:LocalizedText, ORSeparator %>"></asp:Localize>
    <asp:Label ID="lblAddTemplate" Font-Bold="true" runat="server"></asp:Label>
</p>
<ajaxToolkit:CollapsiblePanelExtender ID="cpeNewTemplate" BehaviorID="cpeNewTemplate" runat="server" Collapsed="True" CollapsedSize="0" 
    CollapsedText="<%$ Resources:LogbookEntry, TemplateClickToCreate %>" ExpandControlID="lblAddTemplate" CollapseControlID="lblAddTemplate" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" TargetControlID="pnlNewTemplate" TextLabelID="lblAddTemplate" />
<asp:Panel ID="pnlNewTemplate" runat="server" DefaultButton="btnSaveTemplate" style="overflow:hidden">
    <asp:UpdatePanel runat="server" ID="UpdatePanel2">
        <Triggers>
            <asp:PostBackTrigger ControlID="btnSaveTemplate" />
        </Triggers>
        <ContentTemplate>
            <script>
                var lstDropTemplate = new listDragger('', '', '');
            </script>
            <div style="display:none">
                <script>
                    const setAdded = new Set();
                    const setRemoved = new Set();
                </script>
                <asp:HiddenField ID="hdnAvailableProps" runat="server" />
                <asp:HiddenField ID="hdnUsedProps" runat="server" />
            </div>
            <table>
                <tr>
                    <td><asp:Label ID="lblNamePrompt" runat="server" Text="<%$ Resources:LogbookEntry, TemplateNamePrompt %>"></asp:Label></td>
                    <td colspan="2">
                        <asp:TextBox ID="txtTemplateName" runat="server" Width="100%" placeholder="<%$ Resources:LogbookEntry, TemplateNameWatermark %>" />
                        <asp:RequiredFieldValidator ID="reqTemplateName" ValidationGroup="vgPropTemplate" runat="server" ErrorMessage="<%$ Resources:LogbookEntry, errTemplateNoName %>" ControlToValidate="txtTemplateName" CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
                    </td>
                </tr>
                <tr>
                    <td><asp:Label ID="lblDescPrompt" runat="server" Text="<%$ Resources:LogbookEntry, TemplateDescriptionPrompt %>"></asp:Label></td>
                    <td colspan="2"><asp:TextBox ID="txtDescription" runat="server" Width="100%" TextMode="MultiLine" Rows="3"></asp:TextBox></td>
                </tr>
                <tr>
                    <td><asp:Label ID="lblCategoryPrompt" runat="server" Text="<%$ Resources:LogbookEntry, TemplateCategoryPrompt %>"></asp:Label></td>
                    <td colspan="2"><asp:DropDownList ID="cmbCategories" runat="server"></asp:DropDownList></td>
                </tr>
                <tr>
                    <td></td>
                    <td><% =Resources.LogbookEntry.TemplatePropertiesPromptSource %></td>
                    <td><% =Resources.LogbookEntry.TemplatePropertiesPromptTarget %></td>
                </tr>
                <tr>
                    <td style="vertical-align:top;">
                        <asp:Localize ID="locIncluded" runat="server" Text="<%$ Resources:LogbookEntry, TemplatePropertiesPrompt %>" />
                        <div class="fineprint"><% =Resources.LogbookEntry.TemplatePropertiesPromptHint %></div>
                        <div><uc1:mfbSearchbox runat="server" ID="searchProps" EnableViewState="false" Hint="<%$ Resources:LogbookEntry, TemplateFindPropertiesWatermark %>" OnSearchClicked="searchProps_SearchClicked" /></div>
                        <div><asp:Label ID="lblFilteredLabel" runat="server" CssClass="fineprint" EnableViewState="false" /></div>
                    </td>
                    <td>
                         <div id="divAvailableProps" runat="server" ondragover="javascript:lstDropTemplate.allowDrop(event)" class="dragTarget">
                            <asp:Repeater ID="rptAvailableProps" runat="server">
                                <ItemTemplate>
                                    <div draggable="true"
                                        id='cptT<%# Eval("PropTypeID") %>'
                                        class="draggableItem" style='<%# StyleForTitle((string) Eval("Title")) %>'
                                        ontouchstart="javascript:function () { lstDropTemplate.startLeftTouch('<%# Eval("PropTypeID") %>'); }"
                                        ontouchend="javascript:function () { lstDropTemplate.resetTouch(); }"
                                        ondragstart="javascript:lstDropTemplate.drag(event, <%# Eval("PropTypeID") %>)" >
                                        <%# Eval("Title") %>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </td>
                    <td>
                        <div id="divCurrentProps" runat="server" ondragover="javascript:lstDropTemplate.allowDrop(event)" class="dragTarget">
                            <asp:Repeater ID="rptTemplateProps" runat="server">
                                <ItemTemplate>
                                    <div draggable="true" id="cptT<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="javascript:lstDropTemplate.drag(event, <%# Eval("PropTypeID") %>)">
                                        <%# Eval("Title") %>
                                        <script>
                                            document.getElementById('cptT<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { lstDropTemplate.startRightTouch('<%# Eval("PropTypeID") %>'); });
                                            document.getElementById('cptT<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { lstDropTemplate.resetTouch(); });
                                        </script>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </td>
                </tr>
                <tr>
                    <td></td>
                    <td></td>
                    <td><div>
                        <asp:Button runat="server" Text="<%$ Resources:LocalizedText, Cancel %>" ValidationGroup="vgPropTemplate" Visible="false" ID="btnCancelEditTemplate" OnClick="btnCancelEditTemplate_Click" /> 
                        <asp:Button runat="server" Text="<%$ Resources:LogbookEntry, TemplateCreate %>" ValidationGroup="vgPropTemplate" ID="btnSaveTemplate" OnClick="btnSaveTemplate_Click" /> 
                        <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="false"></asp:Label>

                        </div></td>
                </tr>
            </table>
        </ContentTemplate>
    </asp:UpdatePanel>    
</asp:Panel>
<asp:MultiView ID="mvOwnedTemplates" runat="server">
    <asp:View ID="vwTemplates" runat="server">
        <table>
            <tr style="vertical-align:bottom">
                <td></td>
                <td style="padding: 3px; text-align:center; -webkit-transform: rotate(315deg); -moz-transform: rotate(315deg);-o-transform: rotate(315deg);-ms-transform: rotate(315deg);transform: rotate(315deg);">
                    <asp:Label ID="LocIsPublic" runat="server" Font-Bold="true" Text="<%$ Resources:LogbookEntry, TemplateShare %>"></asp:Label>&nbsp;&nbsp;</td>
                <td style="padding: 3px; text-align:center; -webkit-transform: rotate(315deg); -moz-transform: rotate(315deg);-o-transform: rotate(315deg);-ms-transform: rotate(315deg);transform: rotate(315deg);">
                    <asp:Label ID="locDefault" runat="server" Font-Bold="true" Text="<%$ Resources:LogbookEntry, TemplateDefaultHeader %>"></asp:Label></td>
                <td></td>
                <td></td>
            </tr>
            <asp:Repeater ID="rptTemplateGroups" runat="server">
                <ItemTemplate>
                    <tr>
                        <td colspan="5" style="border-bottom: 1px solid gray; margin-top: 3px; text-align:center;"><asp:Label ID="lblGroupName" Font-Bold="true" runat="server" Font-Size="Larger" Text='<%# Eval("GroupName") %>'></asp:Label></td>
                    </tr>
                    <asp:Repeater ID="rptTemplates" runat="server" DataSource='<%# Eval("Templates") %>' OnItemDataBound="rptTemplates_ItemDataBound">
                        <ItemTemplate>
                            <tr>
                                <td style="padding: 3px;">
                                    <div>
                                        <span style="font-weight:bold; font-size:larger"><%#: Eval("Name") %></span>
                                        <span><%# ((string) Eval("Description")).Linkify(true) %></span>
                                    </div>
                                    <div class="fineprint" style="font-style:italic; color: #555555; margin-left: 2em"><%# String.Join(" ● ", (IEnumerable<string>) Eval("PropertyNames")) %></div>
                                    <div><asp:Label ID="lblPublicErr" runat="server" CssClass="error" EnableViewState="false"></asp:Label></div>
                                </td>
                                <td style="padding: 3px; text-align:center;">
                                    <asp:HiddenField ID="hdnID" runat="server" Value='<%# Eval("ID") %>' />
                                    <asp:CheckBox ID="ckIsPublic" Visible='<%# Eval("IsMutable") %>' runat="server" Checked='<%# Eval("IsPublic") %>' AutoPostBack="true" OnCheckedChanged="ckIsPublic_CheckedChanged" ToolTip="<%$ Resources:LogbookEntry, TemplateShare %>" />
                                </td>
                                <td style="padding: 3px; text-align:center;">
                                    <asp:CheckBox ID="ckDefault" Visible='<%# Eval("IsMutable") %>' runat="server" Checked='<%# Eval("IsDefault") %>' AutoPostBack="true" OnCheckedChanged="ckDefault_CheckedChanged" ToolTip="<%$ Resources:LogbookEntry, TemplateDefaultTooltip %>" />
                                </td>
                                <td style="padding: 3px;">
                                    <uc1:popmenu runat="server" ID="popmenu" OffsetX="-110">
                                        <MenuContent>
                                            <div style="line-height: 26px;">
                                                <asp:LinkButton ID="imgbtnEdit" runat="server" OnClick="imgbtnEdit_Click">
                                                    <asp:Image ID="imgEditTemplate" runat="server" AlternateText="<%$ Resources:LogbookEntry, TemplateEditTip %>"
                                                    ImageUrl="~/images/pencilsm.png" style="padding-right:4px;" />
                                                    <asp:Label ID="lblEditTemplate" runat="server" Text="<%$ Resources:LogbookEntry, TemplateEditTip %>"></asp:Label>
                                                </asp:LinkButton>
                                            </div>
                                            <div style="line-height: 26px;">
                                                <asp:LinkButton ID="lnkEditTemplate" runat="server" OnClick="imgDelete_Click">
                                                    <asp:Image ID="imgDelete" runat="server" style="padding-right: 10px"
                                                        AlternateText="<%$ Resources:LogbookEntry, TemplateDeleteTip %>"
                                                        ImageUrl="~/images/x.gif" />
                                                    <asp:Label ID="lblDeleteTemplate" runat="server" Text="<%$ Resources:LogbookEntry, TemplateDeleteTip %>"></asp:Label>
                                                </asp:LinkButton>
                                                <ajaxToolkit:ConfirmButtonExtender ID="confirmDeleteTemplate" runat="server"
                                                    ConfirmOnFormSubmit="True"
                                                    TargetControlID="lnkEditTemplate" />
                                            </div>
                                            <div style="line-height: 26px;">
                                                <asp:LinkButton ID="imgCopy" runat="server" OnClick="imgCopy_Click">
                                                    <asp:Image ID="imgCopyTemplate" runat="server" style="padding-right:4px;"  
                                                    AlternateText="<%$ Resources:LogbookEntry, TemplateCopy %>"
                                                    ImageUrl="~/images/copyflight.png"/>
                                                    <asp:Label ID="lblCopyTemplate" runat="server" Text="<%$ Resources:LogbookEntry, TemplateCopy %>"></asp:Label>
                                                </asp:LinkButton>
                                            </div>
                                        </MenuContent>
                                    </uc1:popmenu>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </asp:Repeater>
                </ItemTemplate>
            </asp:Repeater>
        </table>
    </asp:View>
    <asp:View ID="vwNoTemplates" runat="server">
        <ul>
            <li>
                <asp:Label ID="lblNoTemplates" runat="server" Font-Italic="true" Text="<%$ Resources:LogbookEntry, TemplateNoneAvailable %>"></asp:Label>
            </li>
        </ul>
    </asp:View>
</asp:MultiView>
