<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEditPropTemplate" Codebehind="mfbEditPropTemplate.ascx.cs" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc1" TagName="mfbSearchbox" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>

<h3><asp:Localize ID="locTemplateHeader" runat="server" Text="<%$ Resources:LogbookEntry, TemplateHeader %>"></asp:Localize></h3>
<p><asp:Localize ID="locTemplateDescription1" runat="server"></asp:Localize></p>
<p><asp:Localize ID="locTemplateDescription2" runat="server"></asp:Localize></p>
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
                        <asp:TextBox ID="txtTemplateName" runat="server" Width="100%"></asp:TextBox>
                        <ajaxToolkit:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" TargetControlID="txtTemplateName" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LogbookEntry, TemplateNameWatermark %>" runat="server" />
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
                    <td>
                        <div style="display:inline-block"><uc1:mfbSearchbox runat="server"   ID="searchProps" EnableViewState="false" Hint="<%$ Resources:LogbookEntry, TemplateFindPropertiesWatermark %>" OnSearchClicked="searchProps_SearchClicked" /></div>
                    </td>
                    <td><asp:Localize ID="locIncluded" runat="server" Text="<%$ Resources:LogbookEntry, TemplatePropertiesPrompt %>"></asp:Localize></td>
                </tr>
                <tr>
                    <td></td>
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
                    <td><asp:Label ID="lblFilteredLabel" runat="server" CssClass="fineprint" EnableViewState="false"></asp:Label></td>
                    <td><div><asp:Button runat="server" Text="<%$ Resources:LogbookEntry, TemplateCreate %>" ValidationGroup="vgPropTemplate" ID="btnSaveTemplate" OnClick="btnSaveTemplate_Click" /> <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="false"></asp:Label></div></td>
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
                <td></td>
                <td style="padding: 3px; text-align:center">
                    <asp:Label ID="LocIsPublic" runat="server" Font-Bold="true" Text="<%$ Resources:LogbookEntry, TemplateShare %>"></asp:Label>&nbsp;&nbsp;</td>
                <td style="padding: 3px; text-align:center">
                    <asp:Label ID="locDefault" runat="server" Font-Bold="true" Text="<%$ Resources:LogbookEntry, TemplateDefaultHeader %>"></asp:Label></td>
                <td></td>
            </tr>
            <asp:Repeater ID="rptTemplateGroups" runat="server">
                <ItemTemplate>
                    <tr>
                        <td colspan="5" style="padding: 3px;"><asp:Label ID="lblGroupName" Font-Bold="true" Font-Size="Smaller" runat="server" Text='<%# Eval("GroupName") %>'></asp:Label></td>
                    </tr>
                    <asp:Repeater ID="rptTemplates" runat="server" DataSource='<%# Eval("Templates") %>'>
                        <ItemTemplate>
                            <tr>
                                <td style="padding: 3px;">
                                    <asp:ImageButton ID="imgbtnEdit" runat="server" Visible='<%# Eval("IsMutable") %>'
                                        AlternateText="<%$ Resources:LogbookEntry, TemplateEditTip %>"
                                        ImageUrl="~/images/pencilsm.png" OnClick="imgbtnEdit_Click"
                                        ToolTip="<%$ Resources:LogbookEntry, TemplateEditTip %>" />
                                </td>
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
                                    <asp:ImageButton ID="imgDelete" runat="server"  Visible='<%# Eval("IsMutable") %>'
                                        AlternateText="<%$ Resources:LogbookEntry, TemplateDeleteTip %>"
                                        ImageUrl="~/images/x.gif" OnClick="imgDelete_Click"
                                        ToolTip="<%$ Resources:LogbookEntry, TemplateDeleteTip %>" />
                                    <ajaxToolkit:ConfirmButtonExtender ID="confirmDeleteDeadline" runat="server"
                                        ConfirmOnFormSubmit="True"
                                        ConfirmText="<%$ Resources:LogbookEntry, TemplateDeleteConfirm %>"
                                        TargetControlID="imgDelete" />
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
