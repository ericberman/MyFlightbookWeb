<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="BulkImportFromTelemetry.aspx.cs" Inherits="BulkImportFromTelemetry" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>
<%@ Register Src="~/Controls/AutofillOptionsChooser.ascx" TagPrefix="uc1" TagName="AutofillOptionsChooser" %>
<%@ Register Src="~/Controls/ClubControls/TimeZone.ascx" TagPrefix="uc1" TagName="TimeZone" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblTitle" runat="server" Text="<%$ Resources:LocalizedText, BulkCreateFlightsFromTelemetryTitle %>"></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <div>
        <p><asp:Label ID="lblBulkImportPrompt1" runat="server" Text="<%$ Resources:LocalizedText, BulkCreateFlightsFromTelemetryTitleDesc1 %>"></asp:Label></p>
        <p><asp:Label ID="lblBulkImportPrompt2" runat="server" Text="<%$ Resources:LocalizedText, BulkCreateFlightsFromTelemetryTitleDesc2 %>"></asp:Label></p>
    </div>
    <asp:Wizard ID="wzFlightsFromTelemetry" Width="80%" FinishCompleteButtonText="<%$ Resources:LocalizedText, BulkCreateFlightsFromTelemetryTitleFinish %>" runat="server" DisplaySideBar="False" OnFinishButtonClick="wzFlightsFromTelemetry_FinishButtonClick" >
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
            <asp:WizardStep ID="WizardStep1" runat="server" Title="Step 1: Choose Settings">
                <div style="margin-left: auto; margin-right:auto; width:400px;">
                    <p><uc1:TimeZone runat="server" ID="TimeZone" /></p>
                    <div><uc1:AutofillOptionsChooser runat="server" id="AutofillOptionsChooser" /></div>
                </div>
            </asp:WizardStep>
            <asp:WizardStep ID="WizardStep2" runat="server" Title="Step 2: Upload telemetry">
                <asp:AjaxFileUpload ID="afuUpload" runat="server" CssClass="mfbDefault" Width="400px" style="margin-left:auto; margin-right:auto;"
                        ThrobberID="myThrobber" MaximumNumberOfFiles="20" OnUploadComplete="afuUpload_UploadComplete" />
                <asp:Image ID="myThrobber" ImageUrl="~/images/ajax-loader.gif" runat="server" style="display:None" />
            </asp:WizardStep>
        </WizardSteps>
    </asp:Wizard>
</asp:Content>
