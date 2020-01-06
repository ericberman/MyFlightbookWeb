<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_ClubControls_ViewClub" Codebehind="ViewClub.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register src="~/Controls/mfbGoogleMapManager.ascx" tagname="mfbGoogleMapManager" tagprefix="uc1" %>
<%@ Register src="../mfbHtmlEdit.ascx" tagname="mfbHtmlEdit" tagprefix="uc2" %>
<%@ Register src="TimeZone.ascx" tagname="TimeZone" tagprefix="uc3" %>
<asp:FormView ID="fvClub" runat="server" Width="100%" EnableViewState="true" OnItemUpdating="fvClub_ItemUpdating" OnModeChanged="fvClub_ModeChanged" DefaultMode="ReadOnly" OnModeChanging="fvClub_ModeChanging" OnDataBound="fvClub_DataBound">
    <ItemTemplate>
        <div style="width: 100%; max-width: 600px; padding: 3px;">
            <asp:Panel ID="pnlContactDetails" runat="server" CssClass="clubDetailsRight">
                <asp:MultiView ID="mvClubHeader" runat="server">
                    <asp:View ID="vwLink" runat="server">
                        <asp:HyperLink ID="lnkClubDetails" Font-Bold="true" runat="server" NavigateUrl='<%# Eval("EditLink") %>' Text='<%# Eval("Name") %>'></asp:HyperLink>
                    </asp:View>
                    <asp:View ID="vwNoLink" runat="server">
                        <asp:Label ID="lblClubDetails" runat="server" Font-Bold="true" Text='<%# Eval("Name") %>'></asp:Label>
                    </asp:View>
                </asp:MultiView>
                <asp:Panel ID="pnlHomeAirport" runat="server" Visible='<%# !String.IsNullOrEmpty(Eval("HomeAirportCode").ToString()) %>'>
                    <asp:HyperLink ID="lnkViewAirport" NavigateUrl='<%# String.Format("~/Public/MapRoute2.aspx/?sm=1&Airports={0}", Eval("HomeAirport.Code")) %>' runat="server"><%# Eval("HomeAirport.Code") %></asp:HyperLink> - <%# Eval("HomeAirport.Name") %>
                </asp:Panel>
                <asp:Panel ID="pnlLocation" Visible='<%# Eval("HasContactInfo") %>' runat="server">
                    <%# Eval("LocationString") %> <%# Eval("ContactPhone") %></asp:Panel>
                <asp:Panel ID="pnlURL" Visible='<%# !String.IsNullOrEmpty(Eval("URL").ToString()) %>' runat="server">
                    <asp:HyperLink ID="lnkClub" NavigateUrl='<%# Eval("Link") %>' Target="_blank" runat="server" Text='<%$ Resources:Club, LabelClubWebsite %>'></asp:HyperLink>
                </asp:Panel>
            </asp:Panel>
            <div class="clubDetailsLeft">
                <div><%# Eval("Description") %> &nbsp;</div>
                <div><asp:HyperLink ID="HyperLink1" Visible='<%# LinkToDetails %>' Font-Bold="true" runat="server" NavigateUrl='<%# Eval("EditLink") %>' Text='<%$ Resources:Club, LabelViewClubDetails %>'></asp:HyperLink></div>
            </div>
        </div>
    </ItemTemplate>
    <EditItemTemplate>
        <asp:HiddenField ID="hdnClubID" Value='<%# Bind("ID") %>' runat="server" />
        <table>
            <tr>
                <td colspan="2" style="padding:3px">
                    <div><asp:Label ID="Label1" runat="server" Text="<%$ Resources:Club, LabelName %>"></asp:Label></div>
                    <asp:TextBox ID="txtName" runat="server" ValidationGroup="valEditClub" Text='<%# Bind("Name") %>' Width="400px"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="valNameRequired" ValidationGroup="valEditClub" runat="server" ControlToValidate="txtName" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Club, errNameRequired %>"></asp:RequiredFieldValidator>
                </td>
            </tr>
            <tr>
                <td colspan="2" style="padding:3px">
                    <div><asp:Label ID="Label2" runat="server" Text="<%$ Resources:Club, LabelDescription %>"></asp:Label></div>
                    <uc2:mfbHtmlEdit ID="htmlDesc" runat="server" Height="120px" Width="500px" Rows="6" Text='<%# Bind("Description") %>' />
                </td>
            </tr>
            <tr>
                <td style="width:400px; padding:3px" colspan="2">
                    <div><asp:Label ID="Label3" runat="server" Text="<%$ Resources:Club, labelURL %>"></asp:Label></div>
                    http://<asp:TextBox ID="txtURL" runat="server" Text='<%# Bind("URL") %>' Width="400px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td style="padding:3px">
                    <div><asp:Label ID="Label4" runat="server" Text="<%$ Resources:Club, LabelCity %>"></asp:Label></div>
                    <asp:TextBox ID="txtCity" runat="server" Text='<%# Bind("City") %>'></asp:TextBox>
                </td>
                <td style="padding:3px">
                    <div><asp:Label ID="Label5" runat="server" Text="<%$ Resources:Club, LabelStateProvince %>"></asp:Label></div>
                    <asp:TextBox ID="txtState" runat="server" Text='<%# Bind("StateProvince") %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td style="padding:3px">
                    <div><asp:Label ID="Label6" runat="server" Text="<%$ Resources:Club, LabelCountry %>"></asp:Label></div>
                    <asp:DropDownList ID="cmbCountry" runat="server" DataSourceID="sqlDSCountries" DataTextField="Country" DataValueField="Country" SelectedValue='<%# Bind("Country") %>'>
                    </asp:DropDownList>
                    <asp:SqlDataSource ID="sqlDSCountries" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT DISTINCT(TRIM(LEFT(CountryName, LOCATE('(', CountryName) - 1))) AS Country FROM countrycodes ORDER BY IF(Country='', '', IF(Country='United States', '1', CONCAT('2-', Country))) ASC"></asp:SqlDataSource>
                </td>
                <td style="padding:3px">
                    <div><asp:Label ID="Label7" runat="server" Text="<%$ Resources:Club, LabelPhone %>"></asp:Label></div>
                    <asp:TextBox ID="txtPhone" runat="server" Text='<%# Bind("ContactPhone") %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td style="padding:3px">
                    <div><asp:Label ID="Label8" runat="server" Text="<%$ Resources:Club, LabelHomeAirport %>"></asp:Label></div>
                    <asp:TextBox ID="txtHomeAirport" runat="server" Text='<%# Bind("HomeAirportCode") %>'></asp:TextBox>
                </td>
                <td style="padding:3px">
                    <div><asp:Label ID="Label9" runat="server" Text="<%$ Resources:Club, LabelTimeZone %>"></asp:Label></div>
                    <uc3:TimeZone ValidationGroup="valEditClub" SelectedTimeZoneId='<%# Bind("TimeZone.Id") %>' ID="tzClub" runat="server" />
                </td>
            </tr>
            <tr>
                <td style="padding:3px" colspan="2">
                    <hr />
                    <div><asp:Localize ID="locPolicy" Text="<%$ Resources:Club, LabelPolicy %>" runat="server"></asp:Localize></div>
                    <table>
                        <tr>
                            <td><asp:CheckBox ID="ckPolicyRestrictEditing" runat="server" Checked='<%# Bind("RestrictEditingToOwnersAndAdmins") %>' /></td>
                            <td><asp:Label AssociatedControlID="ckPolicyRestrictEditing" ID="locPolicyOpenEdit" Text="<%$ Resources:Club, LabelPolicyOpenEdit %>" runat="server"></asp:Label></td>
                        </tr>
                        <tr>
                            <td><asp:CheckBox ID="ckPolicyNamePrefix" runat="server" Checked='<%# Bind("PrependsScheduleWithOwnerName") %>' /></td>
                            <td><asp:Label AssociatedControlID="ckPolicyNamePrefix" ID="locPolicyOwnerPrefix" Text="<%$ Resources:Club, LabelPolicyOwnerPrefix %>" runat="server"></asp:Label></td>
                        </tr>
                        <tr>
                            <td><asp:CheckBox ID="ckPolicyPrivate" runat="server" Checked='<%# Bind("IsPrivate") %>' /></td>
                            <td><asp:Label AssociatedControlID="ckPolicyPrivate" ID="locPolicyPrivate" Text="<%$ Resources:Club, LabelPolicyClubIsPrivate %>" runat="server"></asp:Label></td>
                        </tr>
                        <tr>
                            <td></td>
                            <td>
                                <asp:Localize ID="locDeletePolicy" Text="<%$ Resources:Club, LabelPolicyDeleteNotification %>" runat="server"></asp:Localize>
                                <asp:DropDownList ID="cmbDeletePolicy" SelectedValue='<%# Bind("DeleteNotifications") %>' runat="server">
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyDeleteNotificationNone %>" Value="None"></asp:ListItem>
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyDeleteNotificationAdmins %>" Value="Admins"></asp:ListItem>
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyDeleteNotificationEveryone %>" Value="WholeClub"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td></td>
                            <td>
                                <asp:Localize ID="Localize1" Text="<%$ Resources:Club, LabelPolicyAddModifyNotification %>" runat="server"></asp:Localize>
                                <asp:DropDownList ID="cmbAddModifyPolicy" SelectedValue='<%# Bind("AddModifyNotifications") %>' runat="server">
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyAddModifyNotificationNone %>" Value="None"></asp:ListItem>
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyAddModifyNotificationAdmins %>" Value="Admins"></asp:ListItem>
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyAddModifyNotificationEveryone %>" Value="WholeClub"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr>
                            <td></td>
                            <td>
                                <asp:Localize ID="locDoubleBookPolicy" Text="<%$ Resources:Club, LabelPolicyDoubleBook %>" runat="server"></asp:Localize>
                                <asp:DropDownList ID="cmbDoubleBookPolicy" SelectedValue='<%# Bind("DoubleBookRoleRestriction") %>' runat="server">
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyDoubleBookNone %>" Value="None"></asp:ListItem>
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyDoubleBookAdmin %>" Value="Admins"></asp:ListItem>
                                    <asp:ListItem Text="<%$ Resources:Club, PolicyDoubleBookAnyone %>" Value="WholeClub"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            <tr>
                <td colspan="2" style="text-align:center;">
                    <asp:Button ID="btnCancel" CausesValidation="false" runat="server" OnClick="btnCancel_Click" Text="<%$ Resources:LocalizedText, Cancel %>" /> &nbsp; &nbsp;
                    <asp:Button ID="btnSave" ValidationGroup="valEditClub" CommandName="Update" runat="server" Text="<%$ Resources:Club, LabelSave %>" /> &nbsp; &nbsp;
                    <asp:Button ID="btnDelete" CausesValidation="false" runat="server" OnClick="btnDelete_Click" Text="<%$ Resources:Club, DeleteClub %>" />
                    <asp:ConfirmButtonExtender ID="ConfirmButtonExtender1" ConfirmText="<%$ Resources:Club, ConfirmClubDelete %>" runat="server" TargetControlID="btnDelete" ConfirmOnFormSubmit="True"></asp:ConfirmButtonExtender>
                </td>
            </tr>
        </table>
    </EditItemTemplate>
</asp:FormView>
<asp:Label ID="lblErr" CssClass="error" runat="server" Text="" EnableViewState="false"></asp:Label>