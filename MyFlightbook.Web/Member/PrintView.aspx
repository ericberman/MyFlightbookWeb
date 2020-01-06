<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Async="true" Inherits="Member_PrintView" Title="" culture="auto"  Codebehind="PrintView.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc6" %>
<%@ Register src="../Controls/mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc3" %>
<%@ Register src="../Controls/PrintOptions.ascx" tagname="PrintOptions" tagprefix="uc5" %>
<%@ Register Src="~/Controls/mfbQueryDescriptor.ascx" TagPrefix="uc2" TagName="mfbQueryDescriptor" %>
<%@ Register src="../Controls/mfbLogbookSidebar.ascx" tagname="mfbLogbookSidebar" tagprefix="uc1" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutEASAFCL.ascx" TagPrefix="uc1" TagName="layoutEASAFCL" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutUSA.ascx" TagPrefix="uc1" TagName="layoutUSA" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutNative.ascx" TagPrefix="uc1" TagName="layoutNative" %>
<%@ Register Src="~/Controls/mfbEndorsementList.ascx" TagPrefix="uc1" TagName="mfbEndorsementList" %>
<%@ Register Src="~/Controls/PrintingLayouts/LayoutSACAA.ascx" TagPrefix="uc1" TagName="LayoutSACAA" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutGlider.ascx" TagPrefix="uc1" TagName="layoutGlider" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutNZ.ascx" TagPrefix="uc1" TagName="layoutNZ" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutPortrait.ascx" TagPrefix="uc1" TagName="layoutPortrait" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutCanada.ascx" TagPrefix="uc1" TagName="layoutCanada" %>
<%@ Register Src="~/Controls/PrintingLayouts/layoutCASA.ascx" TagPrefix="uc1" TagName="layoutCASA" %>


<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblUserName" runat="server" ></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <div class="noprint">
        <p><asp:Label ID="lblDescription" runat="server" Text="<%$ Resources:LocalizedText, PrintViewDescription %>" ></asp:Label></p>
        <p><asp:HyperLink ID="lnkReturnToFlights" runat="server" Text="<%$ Resources:LocalizedText, PrintViewReturnLogbook %>"></asp:HyperLink></p>
        <ajaxToolkit:TabContainer ID="TabContainer1" runat="server" CssClass="mfbDefault" ActiveTabIndex="0" >
            <ajaxToolkit:TabPanel ID="tpOptions" runat="server" HeaderText="<%$ Resources:LocalizedText, PrintViewTabOptions %>" >
                <ContentTemplate>
                    <uc5:PrintOptions ID="PrintOptions1" runat="server" OnOptionsChanged="PrintOptions1_OptionsChanged" />
                </ContentTemplate>
            </ajaxToolkit:TabPanel>
            <ajaxToolkit:TabPanel ID="tpFilter" runat="server" HeaderText="<%$ Resources:LocalizedText, PrintViewTabFilter %>" >
                <ContentTemplate>
                    <asp:MultiView ID="mvSearch" runat="server" ActiveViewIndex="0">
                        <asp:View ID="vwDescriptor" runat="server">
                            <table>
                                <tr>
                                    <td>
                                        <asp:CheckBox ID="ckFlights" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="IncludeParamtersChanged" />
                                    </td>
                                    <td>
                                        <asp:Label ID="lblIncludeFlights" AssociatedControlID="ckFlights" runat="server" Text="<%$ Resources:LocalizedText, PrintViewSelectedFlightsLabel %>" ></asp:Label>
                                    </td>
                                </tr>
                                <tr>
                                    <td></td>
                                    <td>
                                        <asp:Button ID="btnChangeQuery" runat="server" Text="<%$ Resources:LocalizedText, ChangeQuery %>" OnClick="btnChangeQuery_Click"  />
                                        <uc2:mfbQueryDescriptor runat="server" ID="mfbQueryDescriptor" ShowEmptyFilter="true" OnQueryUpdated="mfbQueryDescriptor_QueryUpdated" />
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:CheckBox ID="ckTotals" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="IncludeParamtersChanged" />
                                    </td>
                                    <td>
                                        <asp:Label ID="lblIncludeTotals" AssociatedControlID="ckTotals" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeTotals %>"></asp:Label>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:CheckBox ID="ckEndorsements" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="IncludeParamtersChanged" />
                                    </td>
                                    <td>
                                        <asp:Label ID="lblIncludeEndorsements" AssociatedControlID="ckEndorsements" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeEndorsements %>"></asp:Label>
                                    </td>
                                </tr>
                                <tr>
                                    <td></td>
                                    <td>
                                        <div style="float: left">
                                            <asp:CheckBox ID="ckIncludeEndorsementImages" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="IncludeParamtersChanged" /></div>
                                        <div style="float:left">
                                            <asp:Label ID="lblIncludeWhat" AssociatedControlID="ckIncludeEndorsementImages" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeJPEGEndorsements %>"></asp:Label>
                                            <div class="fineprint"><% =Resources.LocalizedText.PrintViewNoEmbeddedPDFsNote %></div>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </asp:View>
                        <asp:View ID="vwSearchForm" runat="server">
                            <uc3:mfbSearchForm ID="mfbSearchForm1" InitialCollapseState="true" runat="server" OnQuerySubmitted="FilterResults" OnReset="mfbSearchForm1_Reset" />
                        </asp:View>
                    </asp:MultiView>
                </ContentTemplate>
            </ajaxToolkit:TabPanel>
            <ajaxToolkit:TabPanel ID="tpPDF" runat="server" HeaderText="<%$ Resources:LocalizedText, DownloadAsPDFTabHeader %>">
                <ContentTemplate>
                    <table>
                        <tr>
                            <td>
                                <% =Resources.LocalizedText.PDFPageSizePrompt %>
                            </td>
                            <td>
                                <asp:DropDownList ID="cmbPageSize" runat="server">
                                    <asp:ListItem Selected="True" Value="Letter" Text="<%$ Resources:LocalizedText, PDFPageSizeLetter %>"></asp:ListItem>
                                    <asp:ListItem Value="Legal" Text="<%$ Resources:LocalizedText, PDFPageSizeLegal %>"></asp:ListItem>
                                    <asp:ListItem Value="A1" Text="<%$ Resources:LocalizedText, PDFPageSizeA1 %>"></asp:ListItem>
                                    <asp:ListItem Value="A2" Text="<%$ Resources:LocalizedText, PDFPageSizeA2 %>"></asp:ListItem>
                                    <asp:ListItem Value="A3" Text="<%$ Resources:LocalizedText, PDFPageSizeA3 %>"></asp:ListItem>
                                    <asp:ListItem Value="A4" Text="<%$ Resources:LocalizedText, PDFPageSizeA4 %>"></asp:ListItem>
                                    <asp:ListItem Value="A5" Text="<%$ Resources:LocalizedText, PDFPageSizeA5 %>"></asp:ListItem>
                                    <asp:ListItem Value="B1" Text="<%$ Resources:LocalizedText, PDFPageSizeB1 %>"></asp:ListItem>
                                    <asp:ListItem Value="B2" Text="<%$ Resources:LocalizedText, PDFPageSizeB2 %>"></asp:ListItem>
                                    <asp:ListItem Value="B3" Text="<%$ Resources:LocalizedText, PDFPageSizeB3 %>"></asp:ListItem>
                                    <asp:ListItem Value="B4" Text="<%$ Resources:LocalizedText, PDFPageSizeB4 %>"></asp:ListItem>
                                    <asp:ListItem Value="B5" Text="<%$ Resources:LocalizedText, PDFPageSizeB5 %>"></asp:ListItem>
                                    <asp:ListItem Value="Tabloid" Text="<%$ Resources:LocalizedText, PDFPageSizeTabloid %>"></asp:ListItem>
                                    <asp:ListItem Value="Executive" Text="<%$ Resources:LocalizedText, PDFPageSizeExecutive %>"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>
                        <tr style="vertical-align:middle">
                            <td>
                                <% =Resources.LocalizedText.PDFPageOrientationPrompt %>
                            </td>
                            <td>
                                <asp:RadioButton ID="rbLandscape" GroupName="pdfOrientation" runat="server" Checked="true" style="vertical-align:middle;" />
                                <label for="<%=rbLandscape.ClientID %>">
                                    <asp:Image ID="imgLandscape" runat="server" AlternateText="<%$ Resources:LocalizedText, PDFOrientationLandscape %>" ToolTip="<%$ Resources:LocalizedText, PDFOrientationLandscape %>" ImageUrl="~/images/landscapeprint.png" ImageAlign="Middle" /></label>
                                <asp:RadioButton ID="rbPortrait" GroupName="pdfOrientation" runat="server" style="vertical-align:middle;" />
                                <label for="<%=rbPortrait.ClientID %>">
                                    <asp:Image ID="imgPortrait" runat="server" AlternateText="<%$ Resources:LocalizedText, PDFOrientationPortrait %>" ToolTip="<%$ Resources:LocalizedText, PDFOrientationPortrait %>" ImageUrl="~/images/portraitprint.png" ImageAlign="Middle" /></label>
                            </td>
                        </tr>
                        <tr>
                            <td><div><% =Resources.LocalizedText.PDFMargin %></div><div class="fineprint"><% =Resources.LocalizedText.PDFMarginNote %></div></td>
                            <td style="padding-left: 20px">
                                <table>
                                    <tr>
                                        <td></td>
                                        <td style="text-align:center"><uc1:mfbDecimalEdit runat="server" ID="decTopMargin" DefaultValueInt="10" Width="40" IntValue="10" EditingMode="Integer" /></td>
                                        <td></td>
                                    </tr>
                                    <tr style="vertical-align:middle">
                                        <td><div><uc1:mfbDecimalEdit runat="server" ID="decLeftMargin" DefaultValueInt="10" Width="40" IntValue="10" EditingMode="Integer" /></div></td>
                                        <td style="border: 1px dotted black; width: 60px; margin: 4px; height:60px">&nbsp;</td>
                                        <td><div><uc1:mfbDecimalEdit runat="server" ID="decRightMargin" DefaultValueInt="10" Width="40" IntValue="10" EditingMode="Integer" /></div></td>
                                    </tr>
                                    <tr>
                                        <td></td>
                                        <td style="text-align:center"><uc1:mfbDecimalEdit runat="server" ID="decBottomMargin" DefaultValueInt="10" Width="40" IntValue="10" EditingMode="Integer" /></td>
                                        <td></td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Image ID="imgPDF" runat="server" ImageUrl="~/images/pdficon_med.png" />
                            </td>
                            <td>
                                <asp:LinkButton ID="lnkDownloadPDF" runat="server" Text="<%$ Resources:LocalizedText, DownloadAsPDF %>" OnClick="lnkDownloadPDF_Click"></asp:LinkButton>
                            </td>
                        </tr>
                    </table>
                    <div class="fineprint"><%=Resources.LocalizedText.Note %> <% =Resources.LocalizedText.wkhtmlnote %></div>
                </ContentTemplate>
            </ajaxToolkit:TabPanel>
        </ajaxToolkit:TabContainer>
        <asp:Label ID="lblErr" CssClass="error" EnableViewState="false" runat="server"></asp:Label>
    </div>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:Panel ID="pnlResults" runat="server">
        <asp:MultiView ID="mvLayouts" runat="server" ActiveViewIndex="0">
            <asp:View ID="vwNative" runat="server">
                <uc1:layoutNative runat="server" ID="layoutNative" />
            </asp:View>
            <asp:View ID="vwPortrait" runat="server">
                <uc1:layoutPortrait runat="server" id="layoutPortrait" />
            </asp:View>
            <asp:View ID="vwEASA" runat="server">
                <uc1:layoutEASAFCL runat="server" id="layoutEASAFCL" />
            </asp:View>
            <asp:View ID="vwUSA" runat="server">
                <uc1:layoutUSA runat="server" ID="layoutUSA" />
            </asp:View>
            <asp:View ID="vwCanada" runat="server">
                <uc1:layoutCanada runat="server" id="layoutCanada" />
            </asp:View>
            <asp:View ID="vwSACAA" runat="server">
                <uc1:LayoutSACAA runat="server" ID="LayoutSACAA" />
            </asp:View>
            <asp:View ID="vwCASA" runat="server">
                <uc1:layoutCASA runat="server" ID="layoutCASA" />
            </asp:View>
            <asp:View ID="vwNZ" runat="server">
                <uc1:layoutNZ runat="server" ID="layoutNZ" />
            </asp:View>
            <asp:View ID="vwGlider" runat="server">
                <uc1:layoutGlider runat="server" id="layoutGlider" />
            </asp:View>
        </asp:MultiView>
        <asp:Panel ID="pnlEndorsements" runat="server" style="page-break-after:always;">
            <uc1:mfbEndorsementList runat="server" ID="mfbEndorsementList" />
            <asp:Repeater ID="rptImages" runat="server">
                <ItemTemplate>
                    <div style="margin-bottom: 5px; page-break-inside:avoid;"><asp:Image ID="imgEndorsement" runat="server" style="max-width:100%" ImageUrl='<%# Eval("URLFullImage") %>' /></div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:Panel>
        <asp:Panel ID="pnlTotals" runat="server">
            <h2><asp:Label ID="lblTotalsHeader" runat="server" Text="<%$ Resources:LocalizedText, PrintViewTotalsHeader %>" ></asp:Label></h2>
            <uc2:mfbTotalSummary ID="mfbTotalSummary1" runat="server" />
        </asp:Panel>
    </asp:Panel>
</asp:Content>