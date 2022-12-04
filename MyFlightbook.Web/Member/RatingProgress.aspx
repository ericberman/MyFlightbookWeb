<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="RatingProgress.aspx.cs" Inherits="MyFlightbook.RatingsProgress.RatingProgressPage" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblTitle" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="false" Text=""></asp:Label>
    <div class="noprint">
        <p>
            <asp:DropDownList ID="cmbMilestoneGroup" runat="server" 
                AppendDataBoundItems="true" AutoPostBack="True" DataTextField="GroupName" DataValueField="GroupName" 
                onselectedindexchanged="cmbMilestoneGroup_SelectedIndexChanged">
                <asp:ListItem Selected="True" Value="" Text="<%$ Resources:MilestoneProgress, PromptRatingGroup %>"></asp:ListItem>
            </asp:DropDownList>
        </p>
        <p>
            <asp:DropDownList ID="cmbMilestones" runat="server" AutoPostBack="True" AppendDataBoundItems="true" 
                DataTextField="Title" DataValueField="Title" 
                onselectedindexchanged="cmbMilestones_SelectedIndexChanged">
                <asp:ListItem Selected="True" Value="" Text="<%$ Resources:MilestoneProgress, PromptRatingItem %>"></asp:ListItem>
            </asp:DropDownList>
        </p>
    </div>
    <asp:Panel ID="pnlAddEdit" runat="server" CssClass="noprint" Visible="false" style="background-color:lightgray; border: 1px solid black; border-radius:5px; padding:5px; margin: 3px;">
        <uc1:Expando runat="server" ID="expandoEditRatings">
            <Header>
                <asp:Label ID="lblAddEdit" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressCreate %>" />
            </Header>
            <Body>
                <h2><asp:Label ID="lblAddNewRating" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressAddNewRating %>" /></h2>
                <asp:Panel ID="pnlAddRating" runat="server" DefaultButton="btnAddNew">
                    <asp:TextBox ID="txtNewTitle" runat="server" ValidationGroup="vgAddRating" placeholder="<%$ Resources:MilestoneProgress, CustomProgressTitlePrompt %>" />
                    <asp:TextBox ID="txtNewDisclaimer" runat="server" ValidationGroup="vgAddRating" placeholder="<%$ Resources:MilestoneProgress, CustomProgressGenDisclaimerPrompt %>" />
                    <asp:Button ID="btnAddNew" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressAddNewRating %>" OnClick="btnAddNew_Click" ValidationGroup="vgAddRating" />
                    <div><asp:RequiredFieldValidator ID="reqNewTitle" runat="server" ControlToValidate="txtNewTitle" CssClass="error" ErrorMessage="<%$ Resources:MilestoneProgress, CustomProgressTitleRequired %>" Display="Dynamic" ValidationGroup="vgAddRating" /></div>
                </asp:Panel>
                <asp:GridView ID="gvCustomRatings" runat="server" AutoGenerateColumns="false" GridLines="None" AutoGenerateEditButton="true" ShowHeader="false" CellPadding="5" OnRowUpdating="gvCustomRatings_RowUpdating"
                    OnRowEditing="gvCustomRatings_RowEditing" OnRowCancelingEdit="gvCustomRatings_RowCancelingEdit" OnRowCommand="gvCustomRatings_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="Title" HtmlEncode="true" />
                        <asp:BoundField DataField="GeneralDisclaimer" HtmlEncode="true" />
                        <asp:ButtonField Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneEditMilestones %>" ButtonType="Link" CommandName="_EditMilestones" />
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDelete" runat="server" 
                                    AlternateText="<%$ Resources:MilestoneProgress, CustomProgressDelete %>" CommandArgument='<%# Bind("Title") %>' 
                                    CommandName="_DeleteRating" ImageUrl="~/images/x.gif" 
                                    ToolTip="<%$ Resources:MilestoneProgress, CustomProgressDelete %>" />
                                <ajaxToolkit:ConfirmButtonExtender ID="cbeDelete" runat="server" 
                                    ConfirmOnFormSubmit="True" 
                                    ConfirmText="<%$ Resources:MilestoneProgress, CustomProgressDeleteConfirm %>" 
                                    TargetControlID="imgDelete">
                                </ajaxToolkit:ConfirmButtonExtender>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <asp:Label ID="lblNoRatings" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNoneFound %>" />
                    </EmptyDataTemplate>
                </asp:GridView>
                <asp:Panel ID="pnlAddMilestones" runat="server" DefaultButton="btnAddMilestone" style="display:none;" Visible="false">
                    <asp:HiddenField ID="hdnCpeIndex" runat="server" />
                    <asp:Label ID="lblEditMilestonesForProgress" runat="server" style="display:none;" />
                    <table>
                        <tr>
                            <td><asp:Label ID="lblFarRefPrmopt" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneFARRef %>" /></td>
                            <td>
                                <asp:TextBox ID="txtMiFARRef" runat="server" ValidationGroup="vgMilestone" />
                                <div><asp:RequiredFieldValidator ID="reqMilestoneFAR" runat="server" ValidationGroup="vgMilestone" ControlToValidate="txtMiFARRef" Display="Dynamic" ErrorMessage="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneFARRefRequired %>" CssClass="error" /></div>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2"><asp:Label ID="lblFarRefNote" CssClass="fineprint" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneFARRefNote %>" /><br />&nbsp;</td>
                        </tr>
                        <tr>
                            <td><asp:Label ID="lblTitlePrompt" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneTitle %>" /></td>
                            <td>
                                <asp:TextBox ID="txtMiTitle" runat="server" ValidationGroup="vgMilestone" />
                                <div><asp:RequiredFieldValidator ID="reqMilestoneTitle" ValidationGroup="vgMilestone" ControlToValidate="txtMiTitle" runat="server" Display="Dynamic" ErrorMessage="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneTitleRequired %>" CssClass="error" /></div>
                            </td>
                        </tr>
                        <tr><td colspan="2"><asp:Label ID="lblTitleTip" CssClass="fineprint" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneTitleTip %>" /><br />&nbsp;</td></tr>
                        <tr>
                            <td><asp:Label ID="lblMiNote" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressItemNewMilestoneNote %>" /></td>
                            <td><asp:TextBox ID="txtMiNote" runat="server" /></td>
                        </tr>
                        <tr><td colspan="2"><asp:Label ID="lblNoteTip" CssClass="fineprint" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressItemNewMilestoneNoteTip %>" /><br />&nbsp;</td></tr>
                        <tr>
                            <td><asp:Label ID="lblFieldToSumPrompt" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneValue %>" /></td>
                            <td><asp:DropDownList ID="cmbFields" runat="server" DataTextField="DataName" DataValueField="DataField" /></td>
                        </tr>
                        <tr>
                            <td><asp:Label ID="lblThreshold" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneThreshold %>" /></td>
                            <td>
                                <uc1:mfbDecimalEdit runat="server" ID="decThreshold" />
                                <div><asp:Label ID="lblErrThreshold" runat="server" EnableViewState="false" CssClass="error" Visible="false" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneThresholdRequired %>" /></div>
                            </td>
                        </tr>
                        <tr>
                            <td><asp:Label ID="lblQuery" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneQuery %>" /></td>
                            <td>
                                <asp:DropDownList ID="cmbQueries" runat="server" AppendDataBoundItems="true" DataTextField="QueryName" DataValueField="QueryName">
                                    <asp:ListItem Enabled="true" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneAllFlightsQuery %>" Value="" />
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td></td>
                            <td><asp:Button ID="btnAddMilestone" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressNewMilestoneAdd %>" ValidationGroup="vgMilestone" OnClick="btnAddMilestone_Click" /></td>
                        </tr>
                    </table>
                    <hr />
                    <h3><asp:Label ID="lblExistingMilestones" runat="server" Text="<%$ Resources:MilestoneProgress, CustomProgressExistingMilestones %>" /></h3>
                    <asp:GridView ID="gvCustomRatingItems" OnRowCommand="gvCustomRatings_RowCommand" runat="server" AutoGenerateColumns="false" GridLines="None" ShowHeader="false" ShowFooter="false">
                        <Columns>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <%# Container.DataItem.ToString().Linkify(true) %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <asp:ImageButton ID="imgDelete" runat="server" 
                                        AlternateText="<%$ Resources:MilestoneProgress, CustomProgressDeleteMilestone %>" CommandArgument='<%# Container.DataItemIndex %>'
                                        CommandName="_DeleteMilestone" ImageUrl="~/images/x.gif" 
                                        ToolTip="<%$ Resources:MilestoneProgress, CustomProgressDeleteMilestone %>" />
                                    <ajaxToolkit:ConfirmButtonExtender ID="cbeDelete" runat="server" 
                                        ConfirmOnFormSubmit="True" 
                                        ConfirmText="<%$ Resources:MilestoneProgress, CustomProgressDeleteMilestone %>" 
                                        TargetControlID="imgDelete">
                                    </ajaxToolkit:ConfirmButtonExtender>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <asp:Label ID="lblNoMilestones" runat="server" Font-Italic="true" Text="<%$ Resources:MilestoneProgress, CustomProgressNoItemsFound %>" />
                        </EmptyDataTemplate>
                    </asp:GridView>
                </asp:Panel>
                <script type="text/javascript">
                    function showCustom() {
                        showModalById('<% =pnlAddMilestones.ClientID %>', $("#" + '<%=lblEditMilestonesForProgress.ClientID %>')[0].innerText, 550);
                        document.getElementById('<% =txtMiFARRef.ClientID %>').focus();
                    }

                    $(function () {
                        if (document.getElementById('<% =pnlAddMilestones.ClientID %>'))
                            showCustom();
                    })
                </script>
            </Body>
        </uc1:Expando>
    </asp:Panel>
    <div class="printonly" style="text-align:center;">
        <h2><asp:Label ID="lblPrintHeader" runat="server" /></h2>
    </div>
    <asp:Panel ID="pnlOverallProgress" runat="server" Visible="false">
        <asp:Label Font-Bold="true" ID="lblOverallProgress" runat="server" Text=""></asp:Label><br />
        <asp:Label ID="lblNoteProgress" runat="server" Font-Bold="true" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
        <asp:Label ID="lblOverallProgressDisclaimer" CssClass="fineprint" runat="server" Text=""></asp:Label>
        <asp:Panel ID="pnlRatingDisclaimer" runat="server">
            <p class="fineprint"><asp:Label ID="lblRatingOverallDisclaimer" runat="server" Text=""></asp:Label></p>
        </asp:Panel>
    </asp:Panel>
    <asp:GridView ID="gvMilestoneProgress" runat="server" ShowHeader="false" ShowFooter="false" 
        onrowdatabound="gvMilestoneProgress_RowDataBound" CellPadding="3" CellSpacing="3" RowStyle-CssClass="progressRow" AlternatingRowStyle-CssClass="progressRow"
        AutoGenerateColumns="False" GridLines="None">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div class="checkedBox">
                        <asp:Label ID="lblDone" Text="✔" runat="server" Visible='<%# Eval("IsSatisfied") %>'></asp:Label>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <div>
                        <span style="font-weight:bold"><%# ((string) Eval("FARRef")).Linkify(false) %></span> - <%# ((string) Eval("Title")).Linkify(true) %> <asp:Label ID="lblExpiration" runat="server" Font-Bold="true" Text='<%#: Eval("ExpirationNote") %>' />
                        <asp:Image ID="imgDetails" runat="server" Visible='<%# Eval("HasDetails") %>' ImageUrl="~/images/expand.png" />
                    </div>
                    <asp:Panel ID="pnlNote" CssClass="fineprint" runat="server">
                        <asp:Label ID="lblNoteHeader" runat="server" Font-Bold="true" Text="<%$ Resources:MilestoneProgress, NoteHeader %>" />
                        <span style="font-style:italic"><%# ((string) Eval("Note")).Linkify(true) %></span>
                    </asp:Panel>
                    <ajaxtoolkit:CollapsiblePanelExtender ID="cpeDisplayMode" runat="server" Collapsed='true' Enabled='<%# Eval("HasDetails") %>'
                        TargetControlID="pnlDetails" CollapsedImage="~/images/expand.png" ExpandedImage="~/images/collapse.png" CollapseControlID="imgDetails" ExpandControlID="imgDetails" ImageControlID="imgDetails" />
                    <asp:Panel ID="pnlDetails" runat="server"><span style="white-space:pre-line""><%# ((string) Eval("Details")).Linkify() %></span></asp:Panel>
                    <asp:MultiView ID="mvProgress" runat="server">
                        <asp:View ID="vwPercentage" runat="server">
                            <div class="progress">
                                <div class="percent"><asp:Label ID="lblProgress" runat="server" Text='<%# Eval("ProgressDisplay") %>'></asp:Label></div>
                                <div class="bar" id="divPercent" runat="server">&nbsp;</div>
                            </div>
                        </asp:View>
                        <asp:View ID="vwAchievement" runat="server">
                            <asp:MultiView ID="mvAchievement" runat="server">
                                <asp:View ID="vwAchieved" runat="server">
                                    <asp:Label ID="lblCompleted" runat="server" Text="<%$ Resources:MilestoneProgress, CompletedHeader %>" Font-Bold="true"></asp:Label>
                                    <asp:HyperLink runat="server" ID="lnkFlight" Target="_blank" Text='<%#: Eval("MatchingEventText") %>'></asp:HyperLink>
                                </asp:View>
                                <asp:View ID="vwNotAchieved" runat="server">
                                    <asp:Label ID="lblNotDone" Font-Bold="true" runat="server" Text="<%$ Resources:MilestoneProgress, NotMet %>"></asp:Label>
                                </asp:View>
                            </asp:MultiView>
                        </asp:View>
                    </asp:MultiView>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

