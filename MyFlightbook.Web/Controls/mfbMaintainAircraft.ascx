<%@ Control Language="C#" AutoEventWireup="true"
    Inherits="Controls_mfbMaintainAircraft" Codebehind="mfbMaintainAircraft.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="mfbDecimalEdit.ascx" TagName="mfbDecimalEdit" TagPrefix="uc3" %>
<%@ Register Src="mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc4" %>
<%@ Register Src="~/Controls/mfbDeadlines.ascx" TagPrefix="uc1" TagName="mfbDeadlines" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>


<asp:UpdatePanel ID="UpdatePanel1" runat="server">
    <ContentTemplate>
        <table>
            <tr class="header">
                <td>
                </td>
                <td>
                    <asp:Localize ID="locLastDoneHeader" runat="server" Text="Last done:" 
                        meta:resourcekey="locLastDoneHeaderResource1"></asp:Localize>
                </td>
                <td>
                    <asp:Localize ID="locNextDueHeader" runat="server" Text="Next due:" 
                        meta:resourcekey="locNextDueHeaderResource1"></asp:Localize>    
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locAnnual" runat="server" Text="Annual:" 
                        meta:resourcekey="locAnnualResource1"></asp:Localize>
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastAnnual" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextAnnual" runat="server" 
                        meta:resourcekey="lblNextAnnualResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locXPonder" runat="server" Text="Transponder:" 
                        meta:resourcekey="locXPonderResource1"></asp:Localize>
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastTransponder" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextTransponder" runat="server" 
                        meta:resourcekey="lblNextTransponderResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locPitot" runat="server" Text="Pitot/Static:" 
                        meta:resourcekey="locPitotResource1"></asp:Localize>
            
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastPitotStatic" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextPitot" runat="server" 
                        meta:resourcekey="lblNextPitotResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locAltimeter" runat="server" Text="Altimeter:" 
                        meta:resourcekey="locAltimeterResource1"></asp:Localize>
            
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastAltimeter" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextAltimeter" runat="server" 
                        meta:resourcekey="lblNextAltimeterResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locELT" runat="server" Text="ELT:" 
                        meta:resourcekey="locELTResource1"></asp:Localize>
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastELT" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextELT" runat="server" 
                        meta:resourcekey="lblNextELTResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locVOR" runat="server" Text="VOR:" 
                        meta:resourcekey="locVORResource1"></asp:Localize>
                </td>
                <td>
                    <uc4:mfbTypeInDate ID="mfbLastVOR" runat="server" DefaultType="None" />
                </td>
                <td>
                    <asp:Label ID="lblNextVOR" runat="server" 
                        meta:resourcekey="lblNextVORResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="loc100Hr" runat="server" Text="100-hour (Hours):" 
                        meta:resourcekey="loc100HrResource1"></asp:Localize>
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="mfbLast100" runat="server" />
                </td>
                <td>
                    <asp:Label ID="lblNext100" runat="server" 
                        meta:resourcekey="lblNext100Resource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locLastOil" runat="server" Text="Oil Change (Hours):" 
                        meta:resourcekey="locLastOilResource1"></asp:Localize>
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="mfbLastOil" runat="server" />
                </td>
                <td>
                    <asp:Label ID="lblNextOil" runat="server" Text="Oil change at: 25hrs: {0}, 50hrs: {1}, 100hrs: {2}" meta:resourcekey="lblNextOilResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="locLastEngine" runat="server" Text="Engine time (Hours):" 
                        meta:resourcekey="locLastEngineResource1"></asp:Localize>
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="mfbLastEngine" runat="server" />
                </td>
                <td>
                </td>
            </tr>
            <tr>
                <td class="label">
                    <asp:Localize ID="Localize1" runat="server" Text="Registration Renewal Due:" meta:resourcekey="Localize1Resource1"></asp:Localize>
                </td>
                <td>
                    <uc4:mfbTypeInDate runat="server" ID="mfbRenewalDue" DefaultType="None" />
                </td>
                <td>
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
            <tr>
                <td colspan="3">
                    <uc1:Expando runat="server" ID="expandoMaintHistory">
                        <Header>
                            <asp:Localize ID="locChangeHistoryHeader" runat="server" Text="Change History:" 
                                meta:resourcekey="locChangeHistoryHeaderResource1"></asp:Localize>
                        </Header>
                        <Body>
                            <asp:GridView ID="gvMaintLog" runat="server" Width="100%"
                                AutoGenerateColumns="False" GridLines="None" OnPageIndexChanging="gvMaintLog_PageIndexChanging"
                                CellPadding="5" ShowHeader="False" meta:resourcekey="gvMaintLogResource1">
                                <RowStyle BorderStyle="None" />
                                <Columns>
                                    <asp:BoundField DataField="ChangeDate" DataFormatString="{0:d}" meta:resourcekey="BoundFieldResource1" />
                                    <asp:BoundField DataField="Description" meta:resourcekey="BoundFieldResource2" />
                                    <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
                                        <ItemTemplate>
                                            <asp:Localize ID="locChangedBy" runat="server" Text="By:" 
                                                meta:resourcekey="locChangedByResource1"></asp:Localize>
                                            <%# Eval("FullDisplayName") %>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="Comment" meta:resourcekey="BoundFieldResource3" />
                                </Columns>
                                <AlternatingRowStyle BackColor="#E0E0E0" BorderStyle="None" />
                                <EmptyDataTemplate>
                                    <asp:Label ID="lblNoHistory" runat="server" Text="(No changes)" meta:resourcekey="lblNoHistoryResource1"></asp:Label>
                                </EmptyDataTemplate>
                            </asp:GridView>
                        </Body>
                    </uc1:Expando>
                </td>
            </tr>
        </table>
        <asp:HiddenField ID="hdnIDAircraft" runat="server" Value="-1"></asp:HiddenField>
    </ContentTemplate>
</asp:UpdatePanel>
