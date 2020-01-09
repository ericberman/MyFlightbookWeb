<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Inherits="PlayPen_MergeFlights" Codebehind="MergeFlights.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbLogbook.ascx" TagPrefix="uc1" TagName="mfbLogbook" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    Merge Flights
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Wizard ID="wzMerge" runat="server" DisplaySideBar="false" finishcompletebuttonText="Merge" Width="100%" OnNextButtonClick="wzMerge_NextButtonClick" OnFinishButtonClick="wzMerge_FinishButtonClick">
        <HeaderTemplate>
            <div style="width:100%">
                <asp:Repeater ID="SideBarList" runat="server">
                    <ItemTemplate>
                        <span class="<%# GetClassForWizardStep(Container.DataItem) %>">
                            &nbsp;
                            <%# DataBinder.Eval(Container, "DataItem.Title") %>
                        </span>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </HeaderTemplate>
        <WizardSteps>
            <asp:WizardStep ID="wsSelectFlights" runat="server" Title="Select Flights">
                <p>Select the flights to merge</p>
                <p><strong>NOTE:</strong></p>
                <ul>
                    <li>All flights will be merged into the earliest one, and the other flights <strong>will be deleted</strong></li>
                    <li>Flights can only be merged if they are in the same aircraft</li>
                    <li>Images will be preserved.</li>
                    <li>Telemetry be preserved, but it will be a "least common denominator". <strong>This could be destructive if they do not share a common format!</strong></li>
                    <li>Values from later flights <strong>will overwrite values</strong> from earlier flights where they cannot be added.</li>
                </ul>
                <div>
                    <asp:Label ID="lblNeed2Flights" runat="server" CssClass="error" EnableViewState="false" Text="Please select at least 2 flights to merge" Visible="false"></asp:Label>
                    <asp:Label ID="lblHeterogeneousAircraft" runat="server" CssClass="error" EnableViewState="false" Text="You can only merge flights that have the same aircraft" Visible="false"></asp:Label>
                </div>
                <asp:Panel ID="pnlFlights" ScrollBars="Auto" Height="200" runat="server">
                    <table cellpadding="4">
                        <asp:Repeater ID="rptSelectedFlights" runat="server" >
                            <ItemTemplate>
                                <tr style="vertical-align:top">
                                    <td>
                                        <asp:CheckBox ID="ckFlight" runat="server" />
                                        <asp:HiddenField ID="hdnFlightID" runat="server" Value='<%# Eval("FlightID") %>' />
                                    </td>
                                    <td>
                                        <div style="font-weight:bold"><%# ((DateTime) Eval("Date")).ToShortDateString() %></div>
                                        <div><%# Eval("TailNumDisplay") %></div>
                                    </td>
                                    <td>
                                        <div><span style="font-style:italic"><%# Eval("Route") %></span> <%# Eval("Comment") %></div>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </table>
                </asp:Panel>
            </asp:WizardStep>
            <asp:WizardStep ID="wsPreview" runat="server" Title="Preview and finish">
                <p>The following flights will be merged into one flight:</p>
                <uc1:mfbLogbook runat="server" ID="mfbLogbookPreview" />
            </asp:WizardStep>
        </WizardSteps>
        <StepNavigationTemplate>
            <asp:Button CommandName="MovePrevious" Runat="server" Text="Previous" />
            <asp:Button CommandName="MoveNext" Runat="server" Text="Next" />
        </StepNavigationTemplate>
        <FinishNavigationTemplate>
            <asp:Button CommandName="MovePrevious" Runat="server" Text="Previous" />
            <asp:Button CommandName="MoveComplete" Runat="server" Text="Merge" />
        </FinishNavigationTemplate>
    </asp:Wizard>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
</asp:Content>