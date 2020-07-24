<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    Codebehind="Admin.aspx.cs" Inherits="MyFlightbook.Web.Admin.Member_Admin" Title="Administer MyFlightbook" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbImpersonate.ascx" TagName="mfbImpersonate" TagPrefix="uc1" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:MultiView ID="mvAdmin" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwUsers" runat="server">
            <h2>User Management</h2>
            <asp:GridView ID="gvLockedUsers" runat="server" AutoGenerateColumns="False" 
                DataSourceID="sqlDSLockedUsers" onrowcommand="UnlockUser" >
                <Columns>
                    <asp:ButtonField ButtonType="Button" CommandName="Unlock" Text="Unlock" />
                    <asp:BoundField DataField="Username" HeaderText="Username" 
                        SortExpression="Username" />
                    <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                    <asp:BoundField DataField="LastLockedOutDate" HeaderText="LastLockedOutDate" 
                        SortExpression="LastLockedOutDate" />
                    <asp:BoundField DataField="FailedPasswordAttemptCount" 
                        HeaderText="FailedPasswordAttemptCount" 
                        SortExpression="FailedPasswordAttemptCount" />
                    <asp:BoundField DataField="FailedPasswordAttemptWindowStart" 
                        HeaderText="FailedPasswordAttemptWindowStart" 
                        SortExpression="FailedPasswordAttemptWindowStart" />
                    <asp:BoundField DataField="FailedPasswordAnswerAttemptCount" 
                        HeaderText="FailedPasswordAnswerAttemptCount" 
                        SortExpression="FailedPasswordAnswerAttemptCount" />
                    <asp:BoundField DataField="FailedPasswordAnswerAttemptWindowStart" 
                        HeaderText="FailedPasswordAnswerAttemptWindowStart" 
                        SortExpression="FailedPasswordAnswerAttemptWindowStart" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSLockedUsers" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="SELECT Username, Email, LastLockedOutDate, FailedPasswordAttemptCount, FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerAttemptWindowStart FROM users WHERE IsLockedOut&lt;&gt;0">
            </asp:SqlDataSource>
            <uc1:mfbImpersonate ID="mfbImpersonate1" runat="server" />
            <asp:SqlDataSource ID="sqlDSDupeUsers" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="select count(username) as numaccounts, username from users group by username having numaccounts &gt; 1;">
            </asp:SqlDataSource>
            <h3>Dupe users:</h3>
            <asp:GridView ID="gvDupeUsers" DataSourceID="sqlDSDupeUsers" runat="server">
                <EmptyDataTemplate>
                    <p>No duplicate users found.</p>
                </EmptyDataTemplate>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwModels" runat="server">
        </asp:View>
        <asp:View ID="vwManufacturers" runat="server">
            <h2>
                Manufacturers</h2>
            <asp:UpdatePanel ID="UpdatePanel3" runat="server">
                <ContentTemplate>
                    <p>
                        Possible dupes:
                        <asp:SqlDataSource ID="sqlDataSourceDupeMan" runat="server" 
                            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                            SelectCommand="SELECT m.*
                FROM manufacturers m
                WHERE m.manufacturer IN (SELECT manufacturer FROM (SELECT * FROM manufacturers m2 GROUP BY manufacturer HAVING COUNT(manufacturer) &gt; 1) as dupes);">
                        </asp:SqlDataSource>
                        <asp:SqlDataSource ID="sqlDupeManstoMatch" runat="server" 
                            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                            SelectCommand="SELECT idmanufacturer,
                CAST(CONCAT(idmanufacturer, ' - ', manufacturer) AS CHAR) AS 'DisplayName'
                FROM manufacturers
                WHERE manufacturer IN
                  (SELECT manufacturer FROM manufacturers GROUP BY manufacturer HAVING count(manufacturer) &gt; 1) ORDER BY idmanufacturer;">
                        </asp:SqlDataSource>
                        <asp:SqlDataSource ID="sqlModelsToRemap" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            SelectCommand="SELECT * FROM models WHERE idmanufacturer=?idman"
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" runat="server">
                            <SelectParameters>
                                <asp:ControlParameter ControlID="cmbManToKill" PropertyName="SelectedValue" Name="idman" />
                            </SelectParameters>
                        </asp:SqlDataSource>
                        <asp:GridView ID="gvDupeMan" runat="server" AutoGenerateColumns="False" 
                            DataSourceID="sqlDataSourceDupeMan" EnableModelValidation="True">
                            <Columns>
                                <asp:BoundField DataField="idManufacturer" HeaderText="idManufacturer" 
                                    InsertVisible="False" SortExpression="idManufacturer" />
                                <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" 
                                    SortExpression="manufacturer" />
                            </Columns>
                        </asp:GridView>
                    </p>
                    <p>
                        Keep
                        <asp:DropDownList ID="cmbManToKeep" runat="server" DataSourceID="sqlDupeManstoMatch"
                            DataTextField="DisplayName" DataValueField="idmanufacturer">
                        </asp:DropDownList>
                        And kill
                        <asp:DropDownList ID="cmbManToKill" runat="server" DataSourceID="sqlDupeManstoMatch"
                            DataTextField="DisplayName" DataValueField="idmanufacturer">
                        </asp:DropDownList>
                        (will be deleted)
                        <asp:Button ID="btnPreviewMan" runat="server" OnClick="btnPreviewDupeMans_Click" Text="Preview"
                            ValidationGroup="PreviewDupeMans" />
                        <asp:CustomValidator ID="CustomValidator2" runat="server" ValidationGroup="PreviewDupeMans"
                            ErrorMessage="These don't appear to be duplicates" 
                            OnServerValidate="ValidateDupeMans"></asp:CustomValidator>
                    </p>
                    <asp:Label ID="lblPreviewDupeMan" runat="server" Text=""></asp:Label>
                    <asp:Panel ID="pnlPreviewDupeMan" runat="server" Visible="false">
                        <asp:GridView ID="gvModelsToRemap" runat="server" 
                            DataSourceID="sqlModelsToRemap">
                        </asp:GridView>
                        <asp:Button ID="btnDeleteDupeMan" runat="server" 
                            Text="Delete Duplicate Manufacturer" OnClick="btnDeleteDupeMan_Click"
                            ValidationGroup="PreviewDupeMans" />
                    </asp:Panel>
                </ContentTemplate>
            </asp:UpdatePanel>
            <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label><br />
        </asp:View>
        <asp:View ID="vwMisc" runat="server">
            <h2>Fix duplicate properties on flights</h2><asp:SqlDataSource ID="sqlDSDupeProps" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
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
                    <p class="success">No duplicate properties found.</p></EmptyDataTemplate></asp:GridView><h2>Empty properties</h2><asp:SqlDataSource ID="sqlDSEmptyProps" runat="server"
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
            <h2>Invalid signatures</h2><p>
                        <asp:Button ID="btnRefreshInvalidSigs" runat="server" OnClick="btnRefreshInvalidSigs_Click" Text="Refresh" /></p>
            <p><asp:Label ID="lblSigResults" runat="server" Text=""></asp:Label></p>
            <p><asp:Button ID="btnFlushCache" runat="server" Text="Flush Cache" OnClick="btnFlushCache_Click" /><span class="fineprint">Removes all entries from the cache; will make things slow, but useful for picking up DB changes or debugging</span></p>
        </asp:View>
    </asp:MultiView>
    <asp:HiddenField ID="hdnActiveTab" runat="server" />
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
    <asp:MultiView ID="mvMain" runat="server">
        <asp:View ID="vwMainUsers" runat="server">
        </asp:View>
        <asp:View ID="vwMainModels" runat="server">
            <h2>Makes/models that should be sims:</h2>
            <asp:GridView ID="gvAircraftShouldBeSims" runat="server" AutoGenerateColumns="False" 
                DataSourceID="sqlSimMakes" EnableModelValidation="True">
                <Columns>
                    <asp:HyperLinkField DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}&a=1" DataNavigateUrlFields="idmodel" DataTextFormatString="Edit" DataTextField="model" />
                    <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" 
                        SortExpression="manufacturer" />
                    <asp:BoundField DataField="model" HeaderText="model" SortExpression="model" />
                    <asp:BoundField DataField="typename" HeaderText="typename" 
                        SortExpression="typename" />
                    <asp:BoundField DataField="modelname" HeaderText="modelname" 
                        SortExpression="modelname" />
                    <asp:BoundField DataField="idcategoryclass" HeaderText="idcategoryclass" 
                        SortExpression="idcategoryclass" />
                    <asp:BoundField DataField="fSimOnly" HeaderText="fSimOnly" 
                        SortExpression="fSimOnly" />
                </Columns>
                <EmptyDataTemplate>
                    <div class="success">(No suspect makes/models found)</div>
                </EmptyDataTemplate>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlSimMakes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT man.manufacturer, m.*
FROM models m INNER JOIN manufacturers man ON m.idmanufacturer=man.idManufacturer
WHERE man.DefaultSim &lt;&gt; 0 AND m.fSimOnly= 0"></asp:SqlDataSource>
            <h2>Orphaned makes/models (i.e., no airplanes using them):</h2>
            <asp:GridView ID="gvOrphanMakes" runat="server"
                AutoGenerateDeleteButton="True" DataKeyNames="idmodel,idmanufacturer" 
                DataSourceID="sqlDSOrphanMakes" EnableModelValidation="True" OnRowDeleting="gvOrphanMakes_RowDeleting"
                AutoGenerateColumns="False">
                <Columns>
                    <asp:BoundField DataField="NumberAircraft" HeaderText="NumberAircraft" 
                        ReadOnly="True" SortExpression="NumberAircraft" />
                    <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" 
                        SortExpression="manufacturer" />
                    <asp:BoundField DataField="model" HeaderText="model" SortExpression="model" />
                    <asp:BoundField DataField="typename" HeaderText="typename" 
                        SortExpression="typename" />
                    <asp:BoundField DataField="idmodel" HeaderText="idmodel" ReadOnly="True" 
                        SortExpression="idmodel" />
                    <asp:BoundField DataField="idcategoryclass" HeaderText="idcategoryclass" 
                        SortExpression="idcategoryclass" />
                    <asp:BoundField DataField="idmanufacturer" HeaderText="idmanufacturer" 
                        ReadOnly="True" SortExpression="idmanufacturer" />
                    <asp:BoundField DataField="modelname" HeaderText="modelname" 
                        SortExpression="modelname" />
                    <asp:BoundField DataField="fcomplex" HeaderText="fcomplex" 
                        SortExpression="fcomplex" />
                    <asp:BoundField DataField="fHighPerf" HeaderText="fHighPerf" 
                        SortExpression="fHighPerf" />
                    <asp:BoundField DataField="fTailwheel" HeaderText="fTailwheel" 
                        SortExpression="fTailwheel" />
                    <asp:BoundField DataField="fConstantProp" HeaderText="fConstantProp" 
                        SortExpression="fConstantProp" />
                    <asp:BoundField DataField="fTurbine" HeaderText="fTurbine" 
                        SortExpression="fTurbine" />
                    <asp:BoundField DataField="fRetract" HeaderText="fRetract" 
                        SortExpression="fRetract" />
                    <asp:BoundField DataField="fCowlFlaps" HeaderText="fCowlFlaps" 
                        SortExpression="fCowlFlaps" />
                    <asp:BoundField DataField="fGlassOnly" HeaderText="fGlassOnly" SortExpression="fGlassOnly" />
                    <asp:BoundField DataField="fSimOnly" HeaderText="fSimOnly" SortExpression="fSimOnly" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSOrphanMakes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT COUNT(ac.idaircraft) AS NumberAircraft, man.manufacturer, m.*
        FROM models m
        LEFT JOIN aircraft ac ON m.idmodel=ac.idmodel
        INNER JOIN manufacturers man ON m.idmanufacturer=man.idmanufacturer
        GROUP BY m.idmodel
        HAVING NumberAircraft=0"
                DeleteCommand="DELETE FROM models WHERE idmodel=?idmodel"
                    >
                    <DeleteParameters>
                        <asp:Parameter Type="Int32" Direction="Input" Name="idmodel" />
                    </DeleteParameters>
            </asp:SqlDataSource>    
            <br />
            <h2>Review Type Designations</h2>
            <asp:Button ID="btnRefreshReview" runat="server" Text="Refresh" OnClick="btnRefreshReview_Click" />
            <asp:GridView ID="gvReviewTypes" runat="server" DataKeyNames="idmodel" AutoGenerateColumns="false" AutoGenerateEditButton="True">
                <Columns>
                    <asp:HyperLinkField HeaderText="ModelID" Target="_blank" DataNavigateUrlFields="idmodel" DataTextField="idmodel" DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}" />
                    <asp:BoundField DataField="idmodel" HeaderText="modelid" Visible="false" ReadOnly="true" />
                    <asp:BoundField DataField="catclass" HeaderText="catclass" ReadOnly="true" />
                    <asp:BoundField DataField="manufacturer" ReadOnly="true" HeaderText="Manufacturer" />
                    <asp:BoundField DataField="model" ReadOnly="true" HeaderText="Model Name" />
                    <asp:BoundField DataField="typename" HeaderText="Type name" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSReviewTypes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="select m.idmodel, man.manufacturer, m.model, m.typename, cc.catclass
from models m
inner join manufacturers man on m.idmanufacturer=man.idmanufacturer
inner join categoryclass cc on m.idcategoryclass=cc.idcatclass
where m.typename &lt;&gt; ''
order by cc.idcatclass ASC, man.manufacturer asc, m.model asc, m.typename asc;"
                UpdateCommand="UPDATE Models SET typename=?typename WHERE idmodel=?idmodel">
                <UpdateParameters>
                        <asp:Parameter Type="Int32" Direction="Input" Name="idmodel" />
                        <asp:Parameter Type="String" Direction="Input" Name="typename" />
                </UpdateParameters>
            </asp:SqlDataSource>  
            <h2>Models that are potential dupes:</h2><asp:SqlDataSource ID="SqlDataSourceDupeModels" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" >
            </asp:SqlDataSource>
            <div>
                <p><asp:CheckBox ID="ckExcludeSims" runat="server" Text="Exclude Sims (i.e., real aircraft only)" AutoPostBack="true" OnCheckedChanged="ckExcludeSims_CheckedChanged" /></p>
                <p>
                    Keep <asp:DropDownList ID="cmbModelToMergeInto" runat="server" DataSourceID="SqlDataSourceDupeModels"
                        DataTextField="DisplayName" DataValueField="idmodel">
                    </asp:DropDownList>
                    And kill <asp:DropDownList ID="cmbModelToDelete" runat="server" DataSourceID="SqlDataSourceDupeModels"
                        DataTextField="DisplayName" DataValueField="idmodel">
                    </asp:DropDownList>
                    (will be deleted) <asp:Button ID="btnPreview" runat="server" OnClick="btnPreview_Click" Text="Preview"
                        ValidationGroup="PreviewDupes" />
                    <asp:CustomValidator ID="CustomValidator1" runat="server" ValidationGroup="PreviewDupes"
                        ErrorMessage="These don't appear to be duplicates" OnServerValidate="CustomValidator1_ServerValidate"></asp:CustomValidator></p><asp:Label ID="lblPreview" runat="server" Text=""></asp:Label><asp:Panel ID="pnlPreview" runat="server" Visible="false">
                    <asp:GridView ID="gvAirplanesToRemap" runat="server" DataSourceID="sqlAirplanesToReMap">
                    </asp:GridView>
                    <asp:Button ID="btnDeleteDupeMake" runat="server" Text="Delete Duplicate Make" OnClick="btnDeleteDupeMake_Click"
                        ValidationGroup="PreviewDupes" />
                    <br />
                </asp:Panel>
            </div>
            <div>&nbsp;</div>
            <asp:SqlDataSource ID="sqlAirplanesToReMap" DataSourceMode="DataReader" runat="server"
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
                SelectCommand="SELECT * FROM aircraft WHERE idmodel=?idModel">
                <SelectParameters>
                    <asp:ControlParameter ControlID="cmbModelToDelete" PropertyName="SelectedValue" Name="idModel" />
                </SelectParameters>
            </asp:SqlDataSource>
            <asp:GridView ID="gvDupeModels" DataSourceID="SqlDataSourceDupeModels" runat="server" EnableModelValidation="True" AutoGenerateColumns="false">
                <Columns>
                    <asp:BoundField DataField="idmodel" 
                        DataFormatString="&lt;a href=&quot;EditMake.aspx?id={0}&quot; target=&quot;_blank&quot;&gt;{0}&lt;/a&gt;" 
                        HtmlEncode="False" HtmlEncodeFormatString="False" ReadOnly="True" />
                    <asp:BoundField DataField="idmodel"
                        DataFormatString="&lt;a href=&quot;Aircraft.aspx?a=1&m={0}&quot; target=&quot;_blank&quot;&gt;View Aircraft&lt;/a&gt;" 
                        HtmlEncode="False" HtmlEncodeFormatString="False" ReadOnly="True" />
                    <asp:BoundField DataField="Manufacturer" HeaderText="Manufacturer" SortExpression="Manufacturer" />
                    <asp:BoundField DataField="DisplayName" HeaderText="DisplayName" SortExpression="DisplayName" />
                    <asp:BoundField DataField="Family" HeaderText="Family" SortExpression="FamilyName" />
                    <asp:BoundField DataField="CatClass" HeaderText="CatClass" SortExpression="CatClass" />
                    <asp:TemplateField HeaderText="Model Attributes">
                        <ItemTemplate>
                            <%# Convert.ToBoolean(Eval("fComplex")) ? "Complex " : "" %>
                            <%# Convert.ToBoolean(Eval("fHighPerf")) ? "High Perf " : (Convert.ToBoolean(Eval("f200HP")) ? "200hp" : "") %>
                            <%# Convert.ToBoolean(Eval("fTailWheel")) ? "Tailwheel " : "" %>
                            <%# Convert.ToBoolean(Eval("fConstantProp")) ? "Constant Prop " : "" %>
                            <%# Convert.ToBoolean(Eval("fTurbine")) ? "Turbine " : "" %>
                            <%# Convert.ToBoolean(Eval("fRetract")) ? "Retract " : "" %>
                            <%# Convert.ToBoolean(Eval("fCowlFlaps")) ? "Flaps" : "" %>
                            <%# Eval("ArmyMissionDesignSeries").ToString().Length > 0 ? "AMS = " + Eval("ArmyMissionDesignSeries").ToString() : "" %>
                            <%# Convert.ToBoolean(Eval("fSimOnly")) ? "Sim Only " : "" %>
                            <%# Convert.ToBoolean(Eval("fGlassOnly")) ? "Glass Only " : "" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwMainManufacturers" runat="server">
            <h2>Existing Manufacturers</h2><asp:UpdatePanel runat="server" ID="updatePanelManufacturers">
                <ContentTemplate>
                    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnAdd">
                        Add a new manufacturer: <asp:TextBox ID="txtNewManufacturer" runat="server"></asp:TextBox><asp:Button ID="btnAdd" runat="server" OnClick="btnManAdd_Click" Text="Add" />
                    </asp:Panel>
                    <br />
                    <asp:GridView ID="gvManufacturers" EnableModelValidation="True" runat="server" OnRowDeleting="ManRowDeleting" OnRowUpdating="ManRowUpdating"
                        AllowSorting="True" AutoGenerateEditButton="True" AutoGenerateDeleteButton="True" OnRowDataBound="gvManufacturers_RowDataBound" 
                        DataSourceID="sqlDSManufacturers" DataKeyNames="idManufacturer" BorderStyle="None" 
                        CellPadding="3" AutoGenerateColumns="False">
                            <Columns>
                                <asp:BoundField DataField="idManufacturer" HeaderText="idManufacturer" SortExpression="idManufacturer" ReadOnly="true" />
                                <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" SortExpression="manufacturer" />
                                <asp:TemplateField HeaderText="Restriction" SortExpression="DefaultSim">
                                    <ItemTemplate>
                                        <%# ((AllowedAircraftTypes) ((UInt32)Eval("DefaultSim"))).ToString() %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:RadioButtonList ID="rblDefaultSim" runat="server">
                                            <asp:ListItem Text="No Restrictions" Value="0"></asp:ListItem>
                                            <asp:ListItem Text="Sim Only" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Sim or Generic, but not real" Value="2"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </EditItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="Number of Models" ReadOnly="True" HeaderText="Number of models" SortExpression="Number of Models" />
                            </Columns>
                            <SelectedRowStyle BackColor="#E0E0E0" />
                    </asp:GridView>
                    <asp:SqlDataSource ID="sqlDSManufacturers" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                        SelectCommand="SELECT man.*, COUNT(m.idmodel) AS 'Number of Models'
            FROM manufacturers man
            LEFT JOIN models m ON m.idmanufacturer=man.idmanufacturer
            GROUP BY man.idmanufacturer
            ORDER BY manufacturer ASC"
                        UpdateCommand="UPDATE manufacturers SET Manufacturer=?manufacturer, DefaultSim=?DefaultSim WHERE idManufacturer=?idManufacturer"
                        DeleteCommand="DELETE FROM manufacturers WHERE idManufacturer=?idManufacturer"
                        >
                        <UpdateParameters>
                            <asp:Parameter Name="manufacturer" Type="String" Size="50"  Direction="Input" />
                            <asp:Parameter Name="DefaultSim" Type="Int32" Direction="InputOutput" />
                            <asp:Parameter Name="idManufacturer" Type="Int32" Direction="Input" />
                        </UpdateParameters>
                        <DeleteParameters>
                            <asp:Parameter Name="idManufacturer" Type="Int32" Direction="Input" />
                        </DeleteParameters>
                    </asp:SqlDataSource>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwMainMisc" runat="server">
            <asp:GridView ID="gvInvalidSignatures" runat="server" AutoGenerateColumns="false" OnRowCommand="gvInvalidSignatures_RowCommand">
                <Columns>
                    <asp:HyperLinkField DataNavigateUrlFields="FlightID" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1" DataTextField="FlightID" DataTextFormatString="{0}" Target="_blank" />
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
                            </ItemTemplate></asp:TemplateField></Columns></asp:GridView></asp:View></asp:MultiView></asp:Content>