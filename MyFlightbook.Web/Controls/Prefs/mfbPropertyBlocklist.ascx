<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbPropertyBlocklist.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbPropertyBlocklist" %>
<h2><asp:Localize ID="locPropHeader" runat="server" Text="<%$ Resources:LogbookEntry, PropertiesHeader %>" /></h2>
<p><asp:Localize ID="lblPropertyDesc" runat="server" Text="<%$ Resources:Preferences, PrefBlockListDesc %>" /></p>
<p><asp:Localize ID="locInstructions" runat="server" Text="<%$ Resources:Preferences, PrefBlockListInstructions %>" /></p>
<script>
    var listDrop = new listDragger('<% =txtPropID.ClientID %>', '<% =btnAllowList.ClientID %>', '<% =btnBlockList.ClientID %>');
</script>
<div style="display:none">
    <asp:TextBox ID="txtPropID" runat="server" EnableViewState="False" />
    <asp:Button ID="btnBlockList" runat="server" OnClick="btnBlockList_Click" />
    <asp:Button ID="btnAllowList" runat="server" OnClick="btnAllowList_Click" />
</div>
<table style="margin-left: auto; margin-right: auto;">
    <tr>
        <td style="width:50%"><asp:Localize ID="locPrevUsed" runat="server" Text="<%$ Resources:Preferences, PrefBlockListShow %>" /></td>
        <td style="width:50%"><asp:Localize ID="locBlockListed" runat="server" Text="<%$ Resources:Preferences, PrefBlockListHide %>" /></td>
    </tr>
    <tr>
        <td style="width:50%">
            <div id="divPropsToShow" ondrop="javascript:listDrop.leftListDrop(event)" ondragover="javascript:listDrop.allowDrop(event)" class="dragTarget">
                <asp:Repeater ID="rptUsedProps" runat="server">
                    <ItemTemplate>
                        <div draggable="true" id="cpt<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="javascript:listDrop.drag(event, <%# Eval("PropTypeID") %>)" >
                            <%# Eval("Title") %>
                            <script>
                                document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { listDrop.startLeftTouch('<%# Eval("PropTypeID") %>'); });
                                document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { listDrop.resetTouch(); });
                            </script>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </td>
        <td style="width:50%">
            <div id="divPropsToBlocklist" ondrop="javascript:listDrop.rightListDrop(event)" ondragover="javascript:listDrop.allowDrop(event)" class="dragTarget">
                <asp:Repeater ID="rptBlockList" runat="server">
                    <ItemTemplate>
                        <div draggable="true" id="cpt<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="javascript:listDrop.drag(event, <%# Eval("PropTypeID") %>)">
                            <%# Eval("Title") %>
                            <script>
                                document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { listDrop.startRightTouch('<%# Eval("PropTypeID") %>'); });
                                document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { listDrop.resetTouch(); });
                            </script>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </td>
    </tr>
</table>