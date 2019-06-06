<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEditPropTemplate.ascx.cs" Inherits="Controls_mfbEditPropTemplate" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc1" TagName="mfbSearchbox" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>

<h3><asp:Localize ID="locTemplateHeader" runat="server" Text="<%$ Resources:LogbookEntry, TemplateHeader %>"></asp:Localize></h3>
<p><asp:Localize ID="locTemplateDescription1" runat="server"></asp:Localize></p>
<p><asp:Localize ID="locTemplateDescription2" runat="server"></asp:Localize></p>
<p><asp:HyperLink ID="lnkBrowseTemplates" runat="server" NavigateUrl="~/Member/BrowseTemplates.aspx" Text="<%$ Resources:LogbookEntry, TemplateBrowseTemplates %>"></asp:HyperLink></p>
<asp:GridView ID="gvPropertyTemplates" runat="server" GridLines="None" CellPadding="3" ShowFooter="false" ShowHeader="false" AutoGenerateColumns="false">
    <Columns>
        <asp:TemplateField>
            <ItemTemplate>
                <div><asp:Label ID="lblGroupName" Font-Size="Larger" Font-Bold="true" runat="server" Text='<%# Eval("GroupName") %>'></asp:Label></div>
                <asp:GridView ID="gvTemplates" runat="server" CellPadding="3" Width="100%" DataSource='<%# Eval("Templates") %>' GridLines="None" ShowFooter="false" ShowHeader="false" AutoGenerateColumns="false" OnRowCommand="gvTemplates_RowCommand">
                    <Columns>
                        <asp:TemplateField ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="25px">
                            <ItemTemplate>
                                <asp:ImageButton ID="imgbtnEdit" runat="server" Visible='<%# Eval("IsMutable") %>'
                                    AlternateText="<%$ Resources:LogbookEntry, TemplateEditTip %>" CommandArgument='<%# Eval("ID") %>'
                                    CommandName="_edit" ImageUrl="~/images/pencilsm.png"
                                    ToolTip="<%$ Resources:LogbookEntry, TemplateEditTip %>" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField ItemStyle-VerticalAlign="Top">
                            <ItemTemplate>
                                <div>
                                    <span style="font-weight:bold"><%#: Eval("Name") %></span>
                                    <span><%# ((string) Eval("Description")).Linkify(true) %></span>
                                </div>
                                <div class="fineprint" style="font-style:italic"><%# String.Join(" ● ", (IEnumerable<string>) Eval("PropertyNames")) %></div>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField ItemStyle-VerticalAlign="Top" ItemStyle-HorizontalAlign="Right">
                            <ItemTemplate>
                                <asp:HiddenField ID="hdnID" runat="server" Value='<%# Eval("ID") %>' />
                                <asp:CheckBox ID="ckIsPublic" Visible='<%# Eval("IsMutable") %>' runat="server" Checked='<%# Eval("IsPublic") %>' AutoPostBack="true" OnCheckedChanged="ckIsPublic_CheckedChanged" Text="<%$ Resources:LogbookEntry, TemplateShare %>" />
                                <div><asp:Label ID="lblPublicErr" runat="server" CssClass="error" EnableViewState="false"></asp:Label></div>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField ItemStyle-VerticalAlign="Top" ItemStyle-Width="30px" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDelete" runat="server"  Visible='<%# Eval("IsMutable") %>'
                                    AlternateText="<%$ Resources:LogbookEntry, TemplateDeleteTip %>" CommandArgument='<%# Eval("ID") %>'
                                    CommandName="_Delete" ImageUrl="~/images/x.gif"
                                    ToolTip="<%$ Resources:LogbookEntry, TemplateDeleteTip %>" />
                                <ajaxToolkit:ConfirmButtonExtender ID="confirmDeleteDeadline" runat="server"
                                    ConfirmOnFormSubmit="True"
                                    ConfirmText="<%$ Resources:LogbookEntry, TemplateDeleteConfirm %>"
                                    TargetControlID="imgDelete" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <ul>
            <li>
                <asp:Label ID="lblNoTemplates" runat="server" Font-Italic="true" Text="<%$ Resources:LogbookEntry, TemplateNoneAvailable %>"></asp:Label>
            </li>
        </ul>
    </EmptyDataTemplate>
</asp:GridView>
<p><asp:Label ID="lblAddTemplate" Font-Bold="true" runat="server"></asp:Label></p>
<ajaxToolkit:CollapsiblePanelExtender ID="cpeNewTemplate" BehaviorID="cpeNewTemplate" runat="server" Collapsed="True" CollapsedSize="0" 
    CollapsedText="<%$ Resources:LogbookEntry, TemplateClickToCreate %>" ExpandControlID="lblAddTemplate" CollapseControlID="lblAddTemplate" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" TargetControlID="pnlNewTemplate" TextLabelID="lblAddTemplate" />
<asp:Panel ID="pnlNewTemplate" runat="server" DefaultButton="btnSaveTemplate" style="overflow:hidden">
    <asp:UpdatePanel runat="server" ID="UpdatePanel2">
        <Triggers>
            <asp:PostBackTrigger ControlID="btnSaveTemplate" />
        </Triggers>
        <ContentTemplate>
            <script type="text/javascript">
                var lstDropTemplate = new listDragger('<% =txtPropID.ClientID %>', '<% =btnAddToTemplate.ClientID %>', '<% =btnRemoveFromTemplate.ClientID %>');
            </script>
            <div style="display:none">
                <asp:TextBox ID="txtPropID" runat="server" EnableViewState="False"></asp:TextBox>
                <asp:Button ID="btnAddToTemplate" runat="server" OnClick="btnAddToTemplate_Click" />
                <asp:Button ID="btnRemoveFromTemplate" runat="server" OnClick="btnRemoveFromTemplate_Click" />
            </div>
            <table>
                <tr>
                    <td><asp:Label ID="lblNamePrompt" runat="server" Text="<%$ Resources:LogbookEntry, TemplateNamePrompt %>"></asp:Label></td>
                    <td colspan="2">
                        <asp:TextBox ID="txtTemplateName" runat="server"></asp:TextBox>
                        <ajaxToolkit:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" TargetControlID="txtTemplateName" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LogbookEntry, TemplateNameWatermark %>" runat="server" />
                        <asp:RequiredFieldValidator ID="reqTemplateName" ValidationGroup="vgPropTemplate" runat="server" ErrorMessage="<%$ Resources:LogbookEntry, errTemplateNoName %>" ControlToValidate="txtTemplateName" CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
                    </td>
                </tr>
                <tr>
                    <td><asp:Label ID="lblDescPrompt" runat="server" Text="<%$ Resources:LogbookEntry, TemplateDescriptionPrompt %>"></asp:Label></td>
                    <td colspan="2"><asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="3"></asp:TextBox></td>
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
                         <div id="divAvailableProps" runat="server" ondrop="javascript:lstDropTemplate.rightListDrop(event)" ondragover="javascript:lstDropTemplate.allowDrop(event)" class="dragTarget">
                            <asp:Repeater ID="rptAvailableProps" runat="server">
                                <ItemTemplate>
                                    <div draggable="true"
                                        id='cpt<%# Eval("PropTypeID") %>'
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
                        <div id="divCurrentProps" ondrop="javascript:lstDropTemplate.leftListDrop(event)" ondragover="javascript:lstDropTemplate.allowDrop(event)" class="dragTarget">
                            <asp:Repeater ID="rptTemplateProps" runat="server">
                                <ItemTemplate>
                                    <div draggable="true" id="cpt<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="javascript:lstDropTemplate.drag(event, <%# Eval("PropTypeID") %>)">
                                        <%# Eval("Title") %>
                                        <script type="text/javascript">
                                            document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { lstDropTemplate.startRightTouch('<%# Eval("PropTypeID") %>'); });
                                            document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { lstDropTemplate.resetTouch(); });
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
