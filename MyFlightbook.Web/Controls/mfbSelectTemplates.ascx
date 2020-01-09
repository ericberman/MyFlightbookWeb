<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSelectTemplates" Codebehind="mfbSelectTemplates.ascx.cs" %>
<asp:Repeater ID="rptGroups" runat="server" EnableViewState="false">
    <ItemTemplate>
        <div><asp:Label ID="lblGroupName" Font-Bold="true" Font-Size="Smaller" runat="server" Text='<%# Eval("GroupName") %>'></asp:Label></div>
        <asp:Repeater ID="rptTemplates" runat="server" DataSource='<%# Eval("Templates") %>' >
            <ItemTemplate>
                <div>
                    <asp:HiddenField ID="hdnID" runat="server" Value='<%# Eval("ID") %>' />
                    <asp:CheckBox ID="ckActive" runat="server" Checked='<%# ActiveTemplates.Contains((int) Eval("ID")) %>' AutoPostBack="true" OnCheckedChanged="ckActive_CheckedChanged" Text='<%# Eval("Name") %>' />
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </ItemTemplate>
</asp:Repeater>
