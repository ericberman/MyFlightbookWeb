<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Async="true" AsyncTimeout="300"
    Codebehind="Admin.aspx.cs" Inherits="MyFlightbook.Web.Admin.Member_Admin" Title="Administer MyFlightbook" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
        <h2>Fix duplicate properties on flights</h2><asp:SqlDataSource ID="sqlDSDupeProps" runat="server" 
            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" OnSelecting="sql_SelectingLongTimeout" 
            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="select fp.idflight, fp.idproptype, count(fp.idProp) AS numProps, cast(group_concat(fp.idprop) as char) AS PropIDs
from flightproperties fp
group by fp.idflight, fp.idproptype
having numProps &gt; 1;"></asp:SqlDataSource>
        <asp:GridView ID="gvDupeProps" runat="server" AutoGenerateColumns="False" 
            EnableModelValidation="True">
            <Columns>
                <asp:HyperLinkField DataTextField="idflight" DataNavigateUrlFields="idflight" DataTextFormatString="{0}" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1&oldProps=1" Target="_blank" HeaderText="Flight" SortExpression="idflight" />
                <asp:BoundField DataField="idproptype" HeaderText="idproptype" 
                    SortExpression="idproptype" />
                <asp:BoundField DataField="numProps" HeaderText="numProps" 
                    SortExpression="numProps" />
                <asp:BoundField DataField="PropIDs" 
                    HeaderText="PropIDs" 
                    SortExpression="PropIDs" />
            </Columns>
            <EmptyDataTemplate>
                <p class="success">No duplicate properties found.</p></EmptyDataTemplate></asp:GridView><h2>Empty properties</h2><asp:SqlDataSource ID="sqlDSEmptyProps" runat="server" OnSelecting="sql_SelectingLongTimeout"
        ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
        SelectCommand="SELECT * FROM flightproperties WHERE intvalue = 0 AND decvalue = 0.0 AND (datevalue IS NULL OR YEAR(datevalue) &lt; 10) AND (stringvalue = '' OR stringvalue IS NULL);" >
        </asp:SqlDataSource>
        <asp:GridView ID="gvEmptyProps" runat="server" AutoGenerateColumns="False" 
            EnableModelValidation="True">
            <Columns>
                <asp:HyperLinkField DataTextField="idflight" DataNavigateUrlFields="idflight" DataTextFormatString="{0}" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1&oldProps=1" Target="_blank" HeaderText="Flight" SortExpression="idflight" />
                <asp:BoundField DataField="idproptype" HeaderText="idproptype" 
                    SortExpression="idproptype" />
            </Columns>
            <EmptyDataTemplate>
                <p class="success">No empty properties found.</p>
            </EmptyDataTemplate>
        </asp:GridView>
        <div><asp:Button ID="btnRefreshProps" runat="server" Text="Refresh empty/dupe props" OnClick="btnRefreshProps_Click" /></div>
        <h2>Invalid signatures</h2>
        <script type="text/javascript">
            function startScan() {
                document.getElementById('<% =btnRefreshInvalidSigs.ClientID %>').click();
            }
        </script>
        <asp:Button ID="btnRefreshInvalidSigs" runat="server" OnClick="btnRefreshInvalidSigs_Click" Text="Refresh" />
        <asp:HiddenField ID="hdnSigOffset" runat="server" Value="0" />
        <p><asp:Label ID="lblSigResults" runat="server" Text=""></asp:Label></p>
        <asp:Label ID="lblSigProgress" runat="server" />
        <asp:MultiView ID="mvCheckSigs" runat="server" ActiveViewIndex="1">
            <asp:View ID="vwSigProgress" runat="server">
                <asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" />
                <script type="text/javascript">
                    $(document).ready(function () { startScan(); });
                </script>
            </asp:View>
            <asp:View ID="vwInvalidSigs" runat="server">
                <p>Flights with signatures to fix:</p>
                <asp:GridView ID="gvInvalidSignatures" runat="server" AutoGenerateColumns="false" OnRowCommand="gvInvalidSignatures_RowCommand">
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <a href='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}/logbook/member/LogbookNew.aspx/{1}?a=1", Branding.CurrentBrand.HostName, Eval("FlightID")) %>' target="_blank"><%# Eval("FlightID").ToString() %></a>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <%# Eval("User") %><br />
                                <%# ((DateTime) Eval("Date")).ToShortDateString() %><br />
                                Saved State: <%# Eval("CFISignatureState") %><br /><%# Eval("AdminSignatureSanityCheckState").ToString() %></ItemTemplate></asp:TemplateField><asp:TemplateField>
                            <ItemTemplate>
                                <asp:Label ID="Label5" runat="server" Width="60px" Text="Saved:"></asp:Label><%# Eval("DecryptedFlightHash") %><br /><asp:Label ID="Label6" runat="server" Width="60px" Text="Current:"></asp:Label><%# Eval("DecryptedCurrentHash") %></ItemTemplate></asp:TemplateField><asp:TemplateField>
                            <ItemTemplate>
                                <asp:Button ID="btnSetValidity" runat="server" Text="Fix" CommandArgument='<%# Bind("FlightID") %>' CommandName="FixValidity" /><br />
                                <asp:Button ID="btnForceValidSig" runat="server" Text="Force Valid" CommandArgument='<%# Bind("FlightID") %>' CommandName="ForceValidity" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <p>No invalid signatures found!</p>
                    </EmptyDataTemplate>
                </asp:GridView>
                <p>Auto-fixed flights:</p>
                <asp:GridView ID="gvAutoFixed" runat="server" AutoGenerateColumns="false">
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <a href='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}/logbook/member/LogbookNew.aspx/{1}?a=1", Branding.CurrentBrand.HostName, Eval("FlightID")) %>' target="_blank"><%# Eval("FlightID").ToString() %></a>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <%# Eval("User") %> <%# ((DateTime) Eval("Date")).ToShortDateString() %>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <p>No autofixed signatures found!</p>
                    </EmptyDataTemplate>
                </asp:GridView>
            </asp:View>
        </asp:MultiView>
        <h2>Cache management</h2>
        <div><asp:Label ID="lblMemStats" runat="server" /></div>
        <uc1:expando runat="server" id="Expando">
            <Header>Object Summary</Header>
            <Body>
                <asp:GridView ID="gvCacheData" runat="server" />
            </Body>
        </uc1:expando>
        <p><asp:Button ID="btnFlushCache" runat="server" Text="Flush Cache" OnClick="btnFlushCache_Click" /> <span class="fineprint">Removes all entries from the cache; will make things slow, but useful for picking up DB changes or debugging</span></p>
        <div><asp:Label ID="lblCacheFlushResults" runat="server" EnableViewState="false" /></div>
        <h2>Nightly Run</h2>
        <p><asp:Button ID="btnNightlyRun" runat="server" Text="Kick Off Nightly Run" OnClick="btnNightlyRun_Click" /> <span class="fineprint">BE CAREFUL! This can spam users.  Only click once, and only if it DIDN'T run last night.</span></p>
        <div><asp:Label ID="lblNightlyRunResult" runat="server"></asp:Label></div>
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
</asp:Content>