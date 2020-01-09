<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEditMake" Codebehind="mfbEditMake.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc1" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc2" TagName="mfbTooltip" %>

<asp:Panel ID="Panel2" DefaultButton="btnAddMake" runat="server" 
    meta:resourcekey="Panel2Resource1">
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblManufacturer" runat="server" Text="Manufacturer:" 
                meta:resourcekey="lblManufacturerResource1" ></asp:Label>
        </div>
        <div class="vfValue">
            <asp:DropDownList ID="cmbManufacturer" runat="server"
                DataTextField="ManufacturerName" DataValueField="ManufacturerID" 
                ValidationGroup="EditMake" AppendDataBoundItems="True" AutoPostBack="True" 
                onselectedindexchanged="ManufacturerChanged" EnableViewState="False" 
                meta:resourcekey="cmbManufacturerResource1">
                <asp:ListItem Selected="True" Value="-1" Text="(Please select)" 
                    meta:resourcekey="ListItemResource1"></asp:ListItem>
            </asp:DropDownList>
            <asp:RangeValidator ID="RangeValidator1" runat="server" ControlToValidate="cmbManufacturer" CssClass="error" Display="Dynamic" ErrorMessage="Please select a manufacturer from the list.  You can add one if needed." MaximumValue="1000000" meta:resourceKey="RangeValidator1Resource1" MinimumValue="0" Type="Integer" ValidationGroup="EditMake"></asp:RangeValidator>
            <asp:HyperLink ID="lblAddNewManufacturer" runat="server" meta:resourceKey="lblAddNewManufacturerResource1" NavigateUrl="#" Text="Add a new manufacturer" ValidationGroup="EditMake"></asp:HyperLink>
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblModel" runat="server" 
                Text="Model ID:" 
                meta:resourcekey="lblModelResource1" ></asp:Label>
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtModel" runat="server" ValidationGroup="EditMake" 
                AutoCompleteType="Disabled" autocomplete="off" 
                meta:resourcekey="txtModelResource1"></asp:TextBox>
            <cc1:AutoCompleteExtender ID="txtModel_AutoCompleteExtender" runat="server" 
                DelimiterCharacters="" TargetControlID="txtModel"
                ServiceMethod="SuggestModels" CompletionInterval="100"  
                ServicePath="~/Public/WebService.asmx" MinimumPrefixLength="1"
                CompletionListCssClass="AutoExtender" CompletionListItemCssClass="AutoExtenderList" 
                CompletionListHighlightedItemCssClass="AutoExtenderHighlight" BehaviorID="ctl00_txtModel_AutoCompleteExtender" >
            </cc1:AutoCompleteExtender>
            <cc1:TextBoxWatermarkExtender
                ID="TextBoxWatermarkExtender1" runat="server" 
                WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LocalizedText, EditMakeWatermarkModelName %>" TargetControlID="txtModel" BehaviorID="ctl00_TextBoxWatermarkExtender1">
            </cc1:TextBoxWatermarkExtender>
            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" 
                ValidationGroup="EditMake" runat="server" 
                ErrorMessage="Please enter a model name" ControlToValidate="txtModel" 
                CssClass="error" Display="Dynamic" 
                meta:resourcekey="RequiredFieldValidator1Resource1"></asp:RequiredFieldValidator>
        </div>
        <div class="vfDescription">
                <div><asp:Label ID="lblAboutModel" runat="server" Text="The model identifier.  Be as specific as possible." meta:resourcekey="lblAboutModelResource1"></asp:Label></div>
                <div><asp:Label ID="lblAboutModel2" runat="server" Text="E.g., &quot;B737-700&quot; or &quot;C-172 S&quot; (rather than &quot;B737&quot; or &quot;C-172&quot;)" meta:resourcekey="lblAboutModel2Resource1"></asp:Label></div>
        </div>
    </div>
    <div class="vfSection" id="rowFamily" runat="server">
        <div class="vfPrompt">
            <asp:Label ID="lblFamilyName" runat="server" Text="FAA/ICAO ID:" meta:resourcekey="lblFamilyNameResource1"></asp:Label>
            <div class="vfSubDesc"><asp:HyperLink NavigateUrl="https://www.icao.int/publications/DOC8643/Pages/Search.aspx" Target="_blank" ID="lnkICAO" runat="server" Text="Look up" meta:resourcekey="lnkICAOResource1"></asp:HyperLink></div>
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtFamilyName" runat="server" MaxLength="4" meta:resourcekey="txtFamilyNameResource1"></asp:TextBox>
            <cc1:TextBoxWatermarkExtender
                ID="TextBoxWatermarkExtender5" runat="server" 
                WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LocalizedText, EditMakeWatermarkFamily %>" 
                TargetControlID="txtFamilyName" BehaviorID="ctl00_TextBoxWatermarkExtender5">
            </cc1:TextBoxWatermarkExtender>
            <cc1:FilteredTextBoxExtender runat="server" ID="filteredICAO" FilterType="Numbers, UppercaseLetters, LowercaseLetters" TargetControlID="txtFamilyName" BehaviorID="ctl00_filteredICAO" />
        </div>
        <div class="vfDescription">
            <div class="vfSubDesc"><% =Resources.LocalizedText.OptionalData %></div>
            <asp:Label ID="lblFamilyDesc1" runat="server" Text="The FAA or ICAO designator or the base model." meta:resourcekey="lblFamilyDesc1Resource1"></asp:Label><br />
            <asp:Label ID="lblFamilyDesc2" runat="server" Text="E.g., a C-172 N and a C-172 S are both &quot;C172&quot;." meta:resourcekey="lblFamilyDesc2Resource1"></asp:Label>
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblModelName" runat="server" 
                Text="Marketing Name:" 
                meta:resourcekey="lblModelNameResource1" ></asp:Label>
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtName" runat="server" ValidationGroup="EditMake" 
                meta:resourcekey="txtNameResource1"></asp:TextBox>
            <cc1:TextBoxWatermarkExtender
                ID="TextBoxWatermarkExtender2" runat="server" 
                WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LocalizedText, EditMakeWatermarkCommonName %>" 
                TargetControlID="txtName" BehaviorID="ctl00_TextBoxWatermarkExtender2">
            </cc1:TextBoxWatermarkExtender>
        </div>
        <div class="vfDescription">
            <div class="vfSubDesc"><% =Resources.LocalizedText.OptionalData %></div>
            <asp:Label ID="lblModelName1" runat="server" Text="An informal or market name for an aircraft." meta:resourcekey="lblModelName1"></asp:Label><br />
            <asp:Label ID="lblModelNameExample" runat="server" Text="E.g., &quot;Skyhawk&quot; for a C-172, but blank for a Boeing 737" meta:resourcekey="lblModelNameExampleResource1"></asp:Label>
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblType" runat="server" 
                Text="Is a type rating required?" 
                meta:resourcekey="lblTypeResource1" ></asp:Label>
            <div class="vfSubDesc"><asp:HyperLink ID="lnkTypeRatings" runat="server" NavigateUrl="http://registry.faa.gov/TypeRatings/" Text="Look up Type Ratings" meta:resourceKey="lnkTypeRatingsResource1"></asp:HyperLink></div>
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtType" runat="server" ValidationGroup="EditMake"
                meta:resourcekey="txtTypeResource1"></asp:TextBox>
            <cc1:TextBoxWatermarkExtender
                ID="TextBoxWatermarkExtender3" runat="server" 
                WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LocalizedText, EditMakeWatermarkTypeName %>" 
                TargetControlID="txtType" BehaviorID="ctl00_TextBoxWatermarkExtender3">
            </cc1:TextBoxWatermarkExtender>
        </div>
        <div class="vfDescription">
            <div><asp:Label ID="lblTypeDesc1" runat="server" Font-Bold="True" meta:resourceKey="lblTypeDesc1Resource1" Text="Leave this blank unless the aircraft requires a type-rating (typically jet or over 12,500lbs). "></asp:Label></div>
            <div><asp:Label ID="lblTypeDescExample" runat="server" Text="E.g., &quot;B737&quot; for a Boeing 737, blank for a C-172." meta:resourcekey="lblTypeDescExampleResource1"></asp:Label></div>
            <div>
                <asp:Label ID="lblTypeDesc2" runat="server" meta:resourceKey="lblTypeDesc2Resource1" Text="Models that share a common type rating should use a common value here."></asp:Label>
            </div>
        </div>
    </div>
    <div class="vfSection" id="rowArmyCurrency" runat="server">
        <div class="vfPrompt">
            <asp:Label ID="lblMDS" runat="server" Text="Mission/Design/Series:" Font-Bold="True" meta:resourcekey="lblMDSResource1"></asp:Label>
        </div>
        <div class="vfValue">
            <asp:TextBox ID="txtArmyMDS" runat="server" meta:resourcekey="txtArmyMDSResource1"></asp:TextBox>
            <cc1:TextBoxWatermarkExtender
                ID="TextBoxWatermarkExtender4" runat="server" 
                WatermarkCssClass="watermark" WatermarkText="<%$ Resources:LocalizedText, EditMakeWatermarkMDS %>" 
                TargetControlID="txtArmyMDS" BehaviorID="ctl00_TextBoxWatermarkExtender4">
            </cc1:TextBoxWatermarkExtender>
        </div>
        <div class="vfDescription">
            <div class="vfSubDesc"><% =Resources.LocalizedText.OptionalData %></div>
            <asp:Label ID="lblMDS1" runat="server" Text="All aircraft with a common MDS will contribute to AR 95-1 currency for that group" meta:resourcekey="lblMDS1Resource1"></asp:Label><br />
            <asp:Label ID="lblMDS2" runat="server" Text="If you type an MDS identifier here, all aircraft with that MDS will contribute to AR 95-1 for that identifier." meta:resourcekey="lblMDS2Resource1"></asp:Label>
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblCatClass" runat="server" Text="Category/Class:" 
                meta:resourcekey="lblCatClassResource1" ></asp:Label>
        </div>
        <div class="vfValue">
            <asp:DropDownList ID="cmbCatClass" runat="server" DataTextField="CatClass" AutoPostBack="True"
                DataValueField="IdCatClass" ValidationGroup="EditMake" OnSelectedIndexChanged="cmbCatClass_SelectedIndexChanged"
                meta:resourcekey="cmbCatClassResource1">
            </asp:DropDownList>
        </div>
    </div>
    <div class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblMakeFeatures" runat="server" Text="Features:" meta:resourcekey="lblMakeFeaturesResource1" Font-Bold="True"></asp:Label>
        </div>
        <div>
            <div style="display:inline-block; vertical-align:top" runat="server" id="divComplex">
                <asp:CheckBox ID="ckComplex" runat="server" Text="Complex" 
                    meta:resourcekey="ckComplexResource1" /><br />
                <div style="margin-left:20px">
                    <asp:CheckBox ID="ckConstantProp" runat="server" Text="Constant Speed Prop" 
                    meta:resourcekey="ckConstantPropResource1" /><br />
                    <asp:CheckBox ID="ckCowlFlaps" runat="server" Text="Flaps" 
                    meta:resourcekey="ckCowlFlapsResource1" /><br />
                    <asp:CheckBox ID="ckRetract" runat="server" Text="Retractable Landing Gear" 
                    meta:resourcekey="ckRetractResource1" /><br />
                </div>
            </div>
            <div style="display:inline-block;vertical-align:top">
                <div id="divTailwheel" runat="server">
                    <asp:CheckBox ID="ckTailwheel" runat="server" Text="Tailwheel" 
                        meta:resourcekey="ckTailwheelResource1" />
                </div>
                <div id="divTMG" runat="server">
                    <asp:CheckBox ID="ckTMG" runat="server" Text="Motor Glider/Touring Motor Glider (TMG)" 
                        meta:resourcekey="ckTMGResource1" />
                </div>
                <div id="divMultiHeli" runat="server">
                    <asp:CheckBox ID="ckMultiHeli" Text="Multi-engine" runat="server" meta:resourcekey="ckMultiHeliResource1" />
                </div>
                <asp:Panel ID="pnlHighPerfBlock" runat="server" 
                    meta:resourcekey="pnlHighPerfBlockResource1">
                    <asp:CheckBox ID="ckHighPerf" runat="server" Text="High-Performance" 
                        meta:resourcekey="ckHighPerfResource2" />
                    <asp:Panel ID="pnlLegacyHighPerf" runat="server" style="display:inline" meta:resourcekey="pnlLegacyHighPerfResource1">
                        ...&nbsp;<asp:CheckBox ID="ckLegacyHighPerf" runat="server" 
                            Text="...but only until 1997" meta:resourcekey="ckLegacyHighPerfResource1" />
                        <uc1:mfbTooltip ID="mfbTooltip1" runat="server">
                            <TooltipBody>
                                <p>On Aug 4, 1997, the FAA definition of high-performance changed from an aircraft with more than 200hp to being an aircraft with an engine of more than 200hp.  So a multi-engine aircraft such as a Piper Seneca, with two engines of 200hp each (for 400hp total), went from being high-performance to not high performance as of that date.</p><p>Checking this option will cause flights prior to that date in this model to be treated as high-performance, but not flights after that date.</p>
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
                <asp:Label ID="lblAdminSimRestrictions" Font-Bold="True" runat="server" Text="ADMIN Sim/Anon Restrictions" meta:resourcekey="lblAdminSimRestrictionsResource1"></asp:Label>
                <asp:RadioButtonList ID="rblAircraftAllowedTypes" runat="server" 
                    meta:resourcekey="RadioButtonList1Resource1">
                    <asp:ListItem Selected="True" Text="No Restrictions" Value="0" 
                        meta:resourcekey="ListItemResource6"></asp:ListItem>
                    <asp:ListItem Text="All Aircraft of this make MUST NOT be real aircraft (i.e., sim only)" 
                        Value="1" meta:resourcekey="ListItemResource7"></asp:ListItem>
                    <asp:ListItem Text="Can be sim or anonymous, but not real." Value="2" 
                        meta:resourcekey="ListItemResource8"></asp:ListItem>
                </asp:RadioButtonList>
            </div>
        </div>
    </div>
    <div id="rowEngineType" runat="server" class="vfSection">
        <div class="vfPrompt">
            <asp:Label ID="lblEngineType" runat="server" Text="Engine Type:" meta:resourcekey="lblEngineTypeResource1"></asp:Label>
        </div>
        <div>
            <asp:RadioButtonList ID="rblTurbineType" runat="server" meta:resourcekey="rblTurbineTypeResource1" RepeatDirection="Horizontal">
                <asp:ListItem meta:resourcekey="ListItemResource2" Selected="True" Text="Piston" Value="0"></asp:ListItem>
                <asp:ListItem meta:resourcekey="ListItemResource3" Text="Turboprop" Value="1"></asp:ListItem>
                <asp:ListItem meta:resourcekey="ListItemResource4" Text="Jet" Value="2"></asp:ListItem>
                <asp:ListItem meta:resourcekey="ListItemResource5" Text="Turbine (Unspecified)" Value="3"></asp:ListItem>
                <asp:ListItem Text="Electric" Value="4" meta:resourcekey="ListItemResource13"></asp:ListItem>
            </asp:RadioButtonList>
            <asp:Panel ID="pnlSinglePilotOps" runat="server" meta:resourcekey="pnlSinglePilotOpsResource1">
                <asp:CheckBox ID="ckSinglePilot" runat="server" Text="This model is certified for single-pilot operations" meta:resourcekey="ckSinglePilotResource1" />
            </asp:Panel>
        </div>
    </div>
    <div>
        <asp:Label ID="lblError" runat="server" CssClass="error" 
            meta:resourcekey="lblErrorResource1"></asp:Label>
    </div>
    <div>
        <asp:Button ID="btnAddMake" runat="server" Text="Add" 
            OnClick="btnAddMake_Click" ValidationGroup="EditMake" 
            meta:resourcekey="btnAddMakeResource1" />
    </div>
    <br />
</asp:Panel>
<asp:HiddenField ID="hdnID" runat="server" />
<asp:Panel ID="pnlManufacturer" runat="server" CssClass="modalpopup"
    DefaultButton="btnManOK" meta:resourcekey="pnlManufacturerResource1">
    <div style="text-align:center">
        <div><asp:Label ID="lblAddManufacturer" runat="server" 
                        Text="Add a new manufacturer:" meta:resourcekey="lblAddManufacturerResource1"></asp:Label></div>
        <div><asp:TextBox ID="txtManufacturer" runat="server" 
                        meta:resourcekey="txtManufacturerResource1"></asp:TextBox></div>
        <div style="margin-top: 5px;">
                    <asp:Button ID="btnManOK" runat="server" Text="OK" onclick="btnManOK_Click" 
                        CausesValidation="False" meta:resourcekey="btnManOKResource1" />&nbsp;&nbsp;
                    <asp:Button ID="btnManCancel" runat="server" Text="Cancel" 
                        CausesValidation="False" meta:resourcekey="btnManCancelResource1" /></div>
    </div>
</asp:Panel>
<cc1:ModalPopupExtender ID="ModalPopupExtender1" runat="server" TargetControlID="lblAddNewManufacturer"
    PopupControlID="pnlManufacturer" BackgroundCssClass="modalBackground" 
    CancelControlID="btnManCancel" OnCancelScript="getFlickerSolved();"
    BehaviorID="ctl00_ModalPopupExtender1">
</cc1:ModalPopupExtender>
<asp:Panel ID="pnlDupesFound" runat="server" Width="480px" CssClass="modalpopup"
    style="display:none" meta:resourcekey="pnlDupesFoundResource1">
    <h2>
        <asp:Label ID="lblPossibleMatchHeader" runat="server" Text="Possible match!" 
            meta:resourcekey="lblPossibleMatchHeaderResource1"></asp:Label></h2>
    <p>
        <asp:Label ID="lblPossibleMatch" runat="server" 
            Text="The make/model you are creating looks like it could be one of the following.  Please re-use one of these if it is a match (you may need to scroll):" 
            meta:resourcekey="lblPossibleMatchResource1"></asp:Label></p>
    <asp:Panel ID="pnlConflicts" ScrollBars="Auto" runat="server" Height="200px" 
        BackColor="White" BorderColor="Black" BorderStyle="Solid" BorderWidth="1px" 
        Width="90%" style="margin-left:auto; margin-right:auto" 
        meta:resourcekey="pnlConflictsResource1">
        <asp:GridView ID="gvDupes" runat="server" GridLines="None" CellPadding="10" DataKeyNames="MakeModelID"
        AutoGenerateColumns="False" AllowSorting="True"
        ShowHeader="False" PageSize="20" Width="100%" 
            OnRowCommand="gvDupes_RowCommand" meta:resourcekey="gvDupesResource1">
            <AlternatingRowStyle BackColor="#E0E0E0" />
            <Columns>
                <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
                    <ItemTemplate>
                        <asp:Button ID="btnUseThis" runat="server" CommandName="_Use" CommandArgument='<%# Eval("MakeModelID") %>'
                            meta:resourcekey="btnUseThisResource1" Text="Use this" />
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Top" />
                </asp:TemplateField>
                <asp:TemplateField meta:resourcekey="TemplateFieldResource2">
                    <ItemTemplate>
                        <%# Eval("ManufacturerDisplay") %>
                        <br />
                        <%# Eval("CategoryClassDisplay") %>
                    </ItemTemplate>
                    <ItemStyle VerticalAlign="Top" />
                </asp:TemplateField>
                <asp:TemplateField meta:resourcekey="TemplateFieldResource3">
                    <ItemTemplate>
                        <%# Eval("Model") %>
                        <%# (String.IsNullOrEmpty(Eval("ModelName").ToString())) ? "" : "\"" + Eval("ModelName") + "\"" %>
                        <%# (String.IsNullOrEmpty(Eval("TypeName").ToString()) ? "" : "- " + Eval("TypeName")) %>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>
    <br />
    <div style="text-align:center">
        <asp:Button ID="btnCancelDupe" runat="server" Text="Cancel" 
            meta:resourcekey="btnCancelDupeResource1" />&nbsp;&nbsp;<asp:Button 
            ID="btnIReallyMeanIt" runat="server" 
            Text="None of these match - Create it!" 
            onclick="btnIReallyMeanIt_Click" 
            meta:resourcekey="btnIReallyMeanItResource1" /></div>
</asp:Panel>           
<asp:Label ID="lblDummy" Text="Required for popup" style="display:none" 
    runat="server" meta:resourcekey="lblDummyResource1"></asp:Label>
<cc1:ModalPopupExtender ID="modalPopupDupes" runat="server" 
    PopupControlID="pnlDupesFound" TargetControlID="lblDummy"
    BackgroundCssClass="modalBackground"
    CancelControlID="btnCancelDupe" OnCancelScript="getFlickerSolved();" DynamicServicePath="" BehaviorID="ctl00_modalPopupDupes">
</cc1:ModalPopupExtender>
<script language="javascript">
// <![CDATA[  
// These functions from http://www.aspdotnetcodes.com/ModalPopup_Postback.aspx
var clientid;
function fnSetFocus(txtClientId)
{
	clientid=txtClientId;
	setTimeout("fnFocus()",1000);
    
}

function fnFocus()
{
    eval("document.getElementById('"+clientid+"').focus()");
}


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

function getFlickerSolved() {
    document.getElementById('<%=pnlDupesFound.ClientID%>').style.display = 'none';
    document.getElementById('<%=pnlManufacturer.ClientID%>').style.display = 'none';
}

function showSinglePilotCertification() {
    document.getElementById('<% =pnlSinglePilotOps.ClientID %>').style.display = 'block';
}

function hideSinglePilotCertification() {
    document.getElementById('<% =pnlSinglePilotOps.ClientID %>').style.display = 'none';
}
 
// ]]>
</script>