<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbEditMake.ascx.cs" Inherits="MyFlightbook.AircraftControls.mfbEditMake" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc1" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc2" TagName="mfbTooltip" %>

<asp:Panel ID="Panel2" DefaultButton="btnAddMake" runat="server">
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblManufacturer" runat="server" Text="<%$ Resources:Makes, editMakeManufacturerPrompt %>" />
        </div>
        <div class="vfValue">
            <asp:DropDownList ID="cmbManufacturer" runat="server"
                DataTextField="ManufacturerName" DataValueField="ManufacturerID" 
                ValidationGroup="EditMake" AppendDataBoundItems="True" AutoPostBack="True" 
                onselectedindexchanged="ManufacturerChanged" EnableViewState="False">
                <asp:ListItem Selected="True" Value="-1" Text="<%$ Resources:Makes, editMakeSelectMake %>"  />
            </asp:DropDownList>
            <asp:RangeValidator ID="RangeValidator1" runat="server" ControlToValidate="cmbManufacturer" CssClass="error" Display="Dynamic" ErrorMessage="Please select a manufacturer from the list.  You can add one if needed." MaximumValue="1000000" MinimumValue="0" Type="Integer" ValidationGroup="EditMake" />
            <a href="javascript:showModalById('<% =pnlNewMan.ClientID %>','<%= Resources.Makes.editMakeAddManufacturerPrompt %>', 400);"><% =Resources.Makes.editMakeAddmanufacturer %></a>
            <asp:Panel runat="server" ID="pnlNewMan" style="display:none" DefaultButton="btnManOK">
                <div>
                    <asp:TextBox ID="txtManufacturer" runat="server" autofocus />
                    <asp:Button ID="btnManOK" runat="server" Text="<%$ Resources:LocalizedText, OK %>" onclick="btnManOK_Click" CausesValidation="False" />
                </div>
                <div class="fineprint"><br /><%=Resources.Makes.addManufacturerTip %></div>
            </asp:Panel>
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblModel" runat="server" Text="<%$ Resources:Makes, editMakeModelID %>"  />
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtModel" runat="server" ValidationGroup="EditMake" 
                AutoCompleteType="Disabled" autocomplete="off" placeholder="<%$ Resources:LocalizedText, EditMakeWatermarkModelName %>" />
            <cc1:AutoCompleteExtender ID="txtModel_AutoCompleteExtender" runat="server" 
                DelimiterCharacters="" TargetControlID="txtModel"
                ServiceMethod="SuggestModels" CompletionInterval="100"  
                ServicePath="~/Public/WebService.asmx" MinimumPrefixLength="1"
                CompletionListCssClass="AutoExtender" CompletionListItemCssClass="AutoExtenderList" 
                CompletionListHighlightedItemCssClass="AutoExtenderHighlight" BehaviorID="ctl00_txtModel_AutoCompleteExtender" >
            </cc1:AutoCompleteExtender>
            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" 
                ValidationGroup="EditMake" runat="server" 
                ErrorMessage="<%$ Resources:Makes, editMakeValModelNameRequired %>" ControlToValidate="txtModel" 
                CssClass="error" Display="Dynamic" />
        </div>
        <div class="vfDescription">
            <div><asp:Label ID="lblAboutModel" runat="server" Text="<%$ Resources:Makes, editMakeModelNote %>" /></div>
            <div><asp:Label ID="lblAboutModel2" runat="server" Text="<%$ Resources:Makes, editMakeModelNote2 %>" /></div>
        </div>
    </div>
    <div class="vfSection" id="rowFamily" runat="server">
        <div class="vfPrompt">
            <asp:Label ID="lblFamilyName" runat="server" Text="<%$ Resources:Makes, editMakeICAOCode %>" />
            <div class="vfSubDesc"><asp:HyperLink NavigateUrl="https://www.icao.int/publications/DOC8643/Pages/Search.aspx" Target="_blank" ID="lnkICAO" runat="server" Text="<%$ Resources:Makes, editMakeICAOCodeLookup %>" /></div>
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtFamilyName" runat="server" MaxLength="4" placeholder="<%$ Resources:LocalizedText, EditMakeWatermarkFamily %>" />
            <cc1:FilteredTextBoxExtender runat="server" ID="filteredICAO" FilterType="Numbers, UppercaseLetters, LowercaseLetters" TargetControlID="txtFamilyName" BehaviorID="ctl00_filteredICAO" />
        </div>
        <div class="vfDescription">
            <div class="vfSubDesc"><% =Resources.LocalizedText.OptionalData %></div>
            <asp:Label ID="lblFamilyDesc1" runat="server" Text="<%$ Resources:Makes, editMakeICAOCodeNote %>" /><br />
            <asp:Label ID="lblFamilyDesc2" runat="server" Text="<%$ Resources:Makes, editMakeICAOCodeNote2 %>" />
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblModelName" runat="server" Text="<%$ Resources:Makes, editMakeMarketingName %>" />
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtName" runat="server" ValidationGroup="EditMake" placeholder="<%$ Resources:LocalizedText, EditMakeWatermarkCommonName %>" />
        </div>
        <div class="vfDescription">
            <div class="vfSubDesc"><% =Resources.LocalizedText.OptionalData %></div>
            <asp:Label ID="lblModelName1" runat="server" Text="<%$ Resources:Makes, editMakeMarketingName1 %>" /><br />
            <asp:Label ID="lblModelNameExample" runat="server" Text="<%$ Resources:Makes, editMakeMarketingName2 %>" />
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblType" runat="server" Text="<%$ Resources:Makes, editMakeTypeRatingPrompt %>"  />
            <div class="vfSubDesc"><asp:HyperLink ID="lnkTypeRatings" runat="server" NavigateUrl="https://registry.faa.gov/TypeRatings/" Text="<%$ Resources:Makes, promptLookUpTypes %>" /></div>
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtType" runat="server" ValidationGroup="EditMake" placeholder="<%$ Resources:LocalizedText, EditMakeWatermarkTypeName %>" />
            <div style="max-width: 200px;"><asp:CustomValidator ID="valType" runat="server" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Makes, errYesNotValidType %>" OnServerValidate="valType_ServerValidate" ValidationGroup="EditMake" /></div>
        </div>
        <div class="vfDescription">
            <div><asp:Label ID="lblTypeDesc1" runat="server" Font-Bold="True" Text="<%$ Resources:Makes, editMakeTypeDesc1 %>" /></div>
            <div><asp:Label ID="lblTypeDescExample" runat="server"  Text="<%$ Resources:Makes, editMakeTypeDesc2 %>" /></div>
            <div><asp:Label ID="lblTypeDesc2" runat="server" Text="<%$ Resources:Makes, editMakeTypeDesc3 %>" /></div>
        </div>
    </div>
    <div class="vfSection" id="rowArmyCurrency" runat="server">
        <div class="vfPrompt"><asp:Label ID="lblMDS" runat="server" Text="<%$ Resources:Makes, editMakeMDS %>" Font-Bold="True" /></div>
        <div class="vfValue"><asp:TextBox ID="txtArmyMDS" runat="server" placeholder="<%$ Resources:LocalizedText, EditMakeWatermarkMDS %>" /></div>
        <div class="vfDescription">
            <div class="vfSubDesc"><% =Resources.LocalizedText.OptionalData %></div>
            <asp:Label ID="lblMDS1" runat="server" Text="<%$ Resources:Makes, editMakeMDSNote1 %>" /><br />
            <asp:Label ID="lblMDS2" runat="server" Text="<%$ Resources:Makes, editMakeMDSNote2 %>" />
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt"><asp:Label ID="lblCatClass" runat="server" Text="<%$ Resources:Makes, editMakeCategoryClass %>" /></div>
        <div class="vfValue">
            <asp:DropDownList ID="cmbCatClass" runat="server" DataTextField="CatClass" AutoPostBack="True"
                DataValueField="IdCatClass" ValidationGroup="EditMake" OnSelectedIndexChanged="cmbCatClass_SelectedIndexChanged" />
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt"><asp:Label ID="lblMakeFeatures" runat="server" Text="<%$ Resources:Makes, editMakeFeatures %>" Font-Bold="True" /></div>
        <div>
            <div style="display:inline-block; vertical-align:top" runat="server" id="divComplex">
                <asp:CheckBox ID="ckComplex" runat="server" Text="<%$ Resources:Makes, editMakeComplex %>" /><br />
                <div style="margin-left:20px">
                    <asp:CheckBox ID="ckConstantProp" runat="server" Text="<%$ Resources:Makes, editMakeCSP %>" /><br />
                    <asp:CheckBox ID="ckCowlFlaps" runat="server" Text="<%$ Resources:Makes, editMakeFlaps %>" /><br />
                    <asp:CheckBox ID="ckRetract" runat="server" Text="<%$ Resources:Makes, editMakeRetract %>" /><br />
                </div>
            </div>
            <div style="display:inline-block;vertical-align:top">
                <div id="divTailwheel" runat="server"><asp:CheckBox ID="ckTailwheel" runat="server" Text="<%$ Resources:Makes, editMakeTailwheel %>" /></div>
                <div id="divTMG" runat="server"><asp:CheckBox ID="ckTMG" runat="server" Text="<%$ Resources:Makes, editMakeTMG %>" /></div>
                <div id="divMultiHeli" runat="server"><asp:CheckBox ID="ckMultiHeli" Text="<%$ Resources:Makes, editMakeMultiHeli %>" runat="server" /></div>
                <asp:Panel ID="pnlHighPerfBlock" runat="server">
                    <asp:CheckBox ID="ckHighPerf" runat="server" Text="<%$ Resources:Makes, editMakeHighPerf %>" />
                    <asp:Panel ID="pnlLegacyHighPerf" runat="server" style="display:inline">
                        &nbsp;<asp:CheckBox ID="ckLegacyHighPerf" runat="server" Text="<%$ Resources:Makes, editMakeHighPerf1997 %>" />
                        <uc1:mfbTooltip ID="mfbTooltip1" runat="server">
                            <TooltipBody>
                                <p><% =Resources.Makes.editMakeHighPerf1997Note %></p>
                            </TooltipBody>
                        </uc1:mfbTooltip>
                    </asp:Panel>
                </asp:Panel>
            </div>
            <asp:Panel runat="server" ID="pnlAvionicsType" CssClass="vfSection">
                <div><asp:Label Font-Bold="true" ID="lblAvionicsType" runat="server" Text="<%$ Resources:Makes, avionicsLabel %>"></asp:Label></div>
                <div><asp:RadioButton ID="rbAvionicsAny" runat="server" GroupName="avionics" Checked="true" Text="<%$ Resources:Makes, avionicsAny %>" /></div>
                <div><asp:RadioButton ID="rbAvionicsGlass" runat="server" GroupName="avionics" Text="<%$ Resources:Makes, avionicsGlass %>"/></div>
                <asp:Panel runat="server" ID="pnlTAA">
                    <asp:RadioButton ID="rbAvionicsTAA" runat="server" GroupName="avionics" Text="<%$ Resources:Makes, avionicsTAA %>" />
                    <uc2:mfbTooltip runat="server" ID="mfbTooltip">
                        <TooltipBody>
                            <%=Resources.Makes.TAADefinition %>
                        </TooltipBody>
                    </uc2:mfbTooltip>
                </asp:Panel>
            </asp:Panel>
            <div id="divIsSimOnly" runat="server" visible="False" style="display:inline-block;vertical-align:top">
                <asp:Label ID="lblAdminSimRestrictions" Font-Bold="True" runat="server" Text="<%$ Resources:Makes, editMakeSimOnly %>" />
                <asp:RadioButtonList ID="rblAircraftAllowedTypes" runat="server">
                    <asp:ListItem Selected="True" Text="<%$ Resources:Makes, editMakeSimAny %>" Value="0" />
                    <asp:ListItem Text="<%$ Resources:Makes, editMakeSimSimOnly %>" />
                    <asp:ListItem Text="<%$ Resources:Makes, editMakeSimSimOrGeneric %>" Value="2" />
                </asp:RadioButtonList>
            </div>
        </div>
    </div>
    <div id="rowEngineType" runat="server" class="vfSection">
        <div class="vfPrompt"><asp:Label ID="lblEngineType" runat="server" Text="<%$ Resources:Makes, editMakeEngineType %>" /></div>
        <div>
            <asp:RadioButtonList ID="rblTurbineType" runat="server" RepeatDirection="Horizontal">
                <asp:ListItem Selected="True" Text="<%$ Resources:Makes, editMakeEngineTypePiston %>" Value="0" />
                <asp:ListItem Text="<%$ Resources:Makes, editMakeEngineTypeTurboProp %>" Value="1" />
                <asp:ListItem Text="<%$ Resources:Makes, editMakeEngineTypeJet %>" Value="2" />
                <asp:ListItem Text="<%$ Resources:Makes, editMakeEngineTypeTurbine %>" Value="3" />
                <asp:ListItem Text="<%$ Resources:Makes, editMakeEngineTypeElectric %>" Value="4" />
            </asp:RadioButtonList>
            <asp:Panel ID="pnlSinglePilotOps" runat="server"><asp:CheckBox ID="ckSinglePilot" runat="server" Text="<%$ Resources:Makes, editMakeCertifiedSingle %>" /></asp:Panel>
        </div>
    </div>
    <div><asp:Label ID="lblError" runat="server" CssClass="error" /></div>
    <div><asp:Button ID="btnAddMake" runat="server" Text="<%$ Resources:Makes, editMakeAddMake %>" 
            OnClick="btnAddMake_Click" ValidationGroup="EditMake"  /></div>
</asp:Panel>
<asp:HiddenField ID="hdnID" runat="server" />
<asp:Panel ID="pnlDupesFound" runat="server" style="display:none" Visible="false">
    <h2>
        <asp:Label ID="lblPossibleMatchHeader" runat="server" Text="<%$ Resources:Makes, editMakePossibleMatch %>" /></h2>
    <p>
        <asp:Label ID="lblPossibleMatch" runat="server" Text="<%$ Resources:Makes, editMakePossibleMatchPrompt %>" /></p>
    <asp:Panel ID="pnlConflicts" ScrollBars="Auto" runat="server" Height="200px" 
        BackColor="White" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" 
        Width="90%" style="margin-left:auto; margin-right:auto">
        <asp:GridView ID="gvDupes" runat="server" GridLines="None" CellPadding="10" DataKeyNames="MakeModelID"
        AutoGenerateColumns="False" AllowSorting="True"
        ShowHeader="False" PageSize="20" Width="100%" OnRowCommand="gvDupes_RowCommand">
            <AlternatingRowStyle BackColor="#E0E0E0" />
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:Button ID="btnUseThis" runat="server" CommandName="_Use" CommandArgument='<%# Eval("MakeModelID") %>' Text="<%$ Resources:Makes, editMakePossibleMatchUseThis %>" />
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Top" />
                </asp:TemplateField>
                <asp:TemplateField>
                    <ItemTemplate>
                        <%#: Eval("ManufacturerDisplay") %>
                        <br />
                        <%#: Eval("CategoryClassDisplay") %>
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Top" />
                </asp:TemplateField>
                <asp:TemplateField>
                    <ItemTemplate>
                        <%#: Eval("Model") %>
                        <%#: (String.IsNullOrEmpty(Eval("ModelName").ToString())) ? "" : "\"" + Eval("ModelName") + "\"" %>
                        <%#: (String.IsNullOrEmpty(Eval("TypeName").ToString()) ? "" : "- " + Eval("TypeName")) %>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <br />
    <div style="text-align:center">
        <asp:Button ID="btnCancelDupe" runat="server" Text="<%$ Resources:LocalizedText, Cancel %>" />&nbsp;&nbsp;<asp:Button ID="btnIReallyMeanIt" runat="server" Text="<%$ Resources:Makes, editMakePossibleMatchNoneMatch %>" onclick="btnIReallyMeanIt_Click" />
    </div>
</asp:Panel>  
<script type="text/javascript">
// <![CDATA[  
// These functions from http://www.aspdotnetcodes.com/ModalPopup_Postback.aspx
var clientid;

$(function () {
    if (document.getElementById('<% =pnlDupesFound.ClientID %>'))
            showModalById('<%=pnlDupesFound.ClientID %>', '<%=Resources.Makes.editMakePossibleMatch %>', 640);
    });

function fnClickOK(sender, e) { 
__doPostBack(sender,e); 
}

function FIsSeaplane()
{
    var i = document.getElementById("<% =cmbCatClass.ClientID %>").value;
    return  ((i == "AMES") || (i == "ASES"));
}

function ComplexClicked()
{
    if (document.getElementById("<% =ckComplex.ClientID %>").checked)
        {
        document.getElementById("<% =ckCowlFlaps.ClientID %>").checked = true;
        document.getElementById("<% =ckConstantProp.ClientID %>").checked = true;
        if (!FIsSeaplane())
            document.getElementById("<% =ckRetract.ClientID %>").checked = true;
        } 
    else
        {
        document.getElementById("<% =ckCowlFlaps.ClientID %>").checked = false;
        document.getElementById("<% =ckConstantProp.ClientID %>").checked = false;
        document.getElementById("<% =ckRetract.ClientID %>").checked = false;
        }
}

function ComplexElementClicked()
{
    if (document.getElementById("<% =ckCowlFlaps.ClientID %>").checked && 
        document.getElementById("<% =ckConstantProp.ClientID %>").checked && 
        (FIsSeaplane() || document.getElementById("<% =ckRetract.ClientID %>").checked))
    {
        document.getElementById("<% =ckComplex.ClientID %>").checked = true;
    }
    else
    {
        document.getElementById("<% =ckComplex.ClientID %>").checked = false;
    }
}

function HighPerfClicked() {
    if (!document.getElementById("<% =ckHighPerf.ClientID %>").checked)
        document.getElementById("<% =ckLegacyHighPerf.ClientID %>").checked = false;
}

function LegacyHighPerfClicked() {
    if (document.getElementById("<% =ckLegacyHighPerf.ClientID %>").checked)
        document.getElementById("<% =ckHighPerf.ClientID %>").checked = true;
}

function showSinglePilotCertification() {
    document.getElementById('<% =pnlSinglePilotOps.ClientID %>').style.display = 'block';
}

function hideSinglePilotCertification() {
    document.getElementById('<% =pnlSinglePilotOps.ClientID %>').style.display = 'none';
}
 
// ]]>
</script>