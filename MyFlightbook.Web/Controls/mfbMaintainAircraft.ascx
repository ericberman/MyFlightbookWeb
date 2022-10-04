<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbMaintainAircraft.ascx.cs" Inherits="MyFlightbook.AircraftControls.mfbMaintainAircraft" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="mfbDecimalEdit.ascx" TagName="mfbDecimalEdit" TagPrefix="uc3" %>
<%@ Register Src="mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc4" %>
<%@ Register Src="~/Controls/mfbDeadlines.ascx" TagPrefix="uc1" TagName="mfbDeadlines" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<asp:UpdatePanel ID="UpdatePanel1" runat="server">
    <ContentTemplate>
        <script type="text/javascript">
            $(document).ready(function () {
                $.ajax


                var params = new Object();
                params.idAircraft = <% =AircraftID %>;
                var d = JSON.stringify(params);
                $.ajax(
                    {
                        url: '<% =ResolveUrl("~/Member/EditAircraft.aspx/GetHighWaterMarks") %>',
                        type: "POST", data: d, dataType: "json", contentType: "application/json",
                        error: function (xhr, status, error) {
                            window.alert(xhr.responseJSON.Message);
                        },
                        complete: function (response) { },
                        success: function (response) { $(<% =pnlHighWater.ClientID %>)[0].innerText = response.d; }
                    });
            });
        </script>
        <asp:Panel ID="pnlHighWater" runat="server" />
        <table>
            <tr class="header">
                <td>
                </td>
                <td>
                    <asp:Localize ID="locLastDoneHeader" runat="server" Text="<%$ Resources:Aircraft, MaintenanceLastDone %>" />
                </td>
                <td>
                    <asp:Localize ID="locNextDueHeader" runat="server" Text="<%$ Resources:Aircraft, MaintenanceNextDue %>" />    
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locAnnual" runat="server" Text="<%$ Resources:Aircraft, MaintenanceAnnualPrompt %>" />
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastAnnual" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextAnnual" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locXPonder" runat="server" Text="<%$ Resources:Aircraft, MaintenanceXPonderPrompt %>" />
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastTransponder" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextTransponder" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locPitot" runat="server" Text="<%$ Resources:Aircraft, MaintenancePitotPrompt %>" />
            
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastPitotStatic" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextPitot" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locAltimeter" runat="server" Text="<%$ Resources:Aircraft, MaintenanceAltimeterPrompt %>" />
            
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastAltimeter" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextAltimeter" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locELT" runat="server" Text="<%$ Resources:Aircraft, MaintenanceELTPrompt %>"/>
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastELT" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextELT" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locVOR" runat="server" Text="<%$ Resources:Aircraft, MaintenanceVORPrompt %>" />
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastVOR" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextVOR" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="loc100Hr" runat="server" Text="<%$ Resources:Aircraft, Maintenance100HrPrompt %>" />
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="mfbLast100" runat="server" />
                </td>
                <td>
                    <asp:Label ID="lblNext100" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locLastOil" runat="server" Text="<%$ Resources:Aircraft, MaintenanceOilPrompt %>" />
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="mfbLastOil" runat="server" />
                </td>
                <td>
                    <asp:Label ID="lblNextOil" runat="server" />
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locLastEngine" runat="server" Text="<%$ Resources:Aircraft, MaintenanceEnginePrompt %>" />
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="mfbLastEngine" runat="server" />
                </td>
                <td>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locRegistration" runat="server" Text="<%$ Resources:Aircraft, MaintenanceRegistrationPrompt %>" />
                </td>
                <td>
                    <uc4:mfbTypeInDate runat="server" ID="mfbRenewalDue" DefaultType="None" />
                </td>
                <td>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locComments" runat="server" Text="<%$ Resources:Aircraft, MaintenanceNotesPrompt %>"></asp:Localize>
                    <uc1:mfbTooltip runat="server" id="ttNotes">
                        <TooltipBody>
                            <% =Resources.Aircraft.MaintenanceNotesWatermark %>
                        </TooltipBody>
                    </uc1:mfbTooltip>
                </td>
                <td colspan="2">
                    <asp:TextBox ID="txtNotes" runat="server" Width="280px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td class="label" style="vertical-align:top;">
                    <% =Resources.Currency.deadlinesHeaderDeadlines %>
                    <uc1:mfbTooltip runat="server" id="mfbTooltip">
                        <TooltipBody>
                            <% =Resources.Currency.DeadlineDescription %>
                        </TooltipBody>
                    </uc1:mfbTooltip>
                </td>
                <td colspan="2">
                    <uc1:mfbDeadlines runat="server" ID="mfbDeadlines1" OnDeadlineUpdated="mfbDeadlines1_DeadlineUpdated" CreateShared="true" OnDeadlineAdded="mfbDeadlines1_DeadlineAdded" OnDeadlineDeleted="mfbDeadlines1_DeadlineDeleted" />
                </td>
            </tr>
        </table>
        <uc1:Expando runat="server" ID="expandoMaintHistory">
            <Header>
                <span style="font-weight:bold"><asp:Localize ID="locChangeHistoryHeader" runat="server" Text="<%$ Resources:Aircraft, MaintenanceChangeHistoryPrompt %>" /></span>
            </Header>
            <Body>
                <asp:GridView ID="gvMaintLog" runat="server" Width="100%"
                    RowStyle-CssClass="logbookRow" AlternatingRowStyle-CssClass="logbookAlternateRow"
                    AutoGenerateColumns="False" GridLines="None" OnPageIndexChanging="gvMaintLog_PageIndexChanging"
                    CellPadding="5" ShowHeader="False" >
                    <RowStyle BorderStyle="None" />
                    <Columns>
                        <asp:BoundField DataField="ChangeDate" DataFormatString="{0:d}" ControlStyle-Font-Size="Larger" />
                        <asp:TemplateField >
                            <ItemTemplate>
                                <div><span style="font-weight:bold"><%#: Eval("FullDisplayName") %></span> <%#: Eval("Description") %></div>                                    
                                <div><%#: Eval("Comment") %></div>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <asp:Label ID="lblNoHistory" runat="server" Text="(No changes)" ></asp:Label>
                    </EmptyDataTemplate>
                </asp:GridView>
            </Body>
        </uc1:Expando>
        <asp:HiddenField ID="hdnIDAircraft" runat="server" Value="-1"></asp:HiddenField>
    </ContentTemplate>
</asp:UpdatePanel>