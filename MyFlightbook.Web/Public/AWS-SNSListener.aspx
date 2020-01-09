<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_AWS_SNSListener" Codebehind="AWS-SNSListener.aspx.cs" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <asp:Panel ID="pnlTest" Visible="false" runat="server">
            <h2>Test SNS notifications</h2>
            Enter the JSON data below to test:<br />
            <asp:TextBox ID="txtTestJSONData" Width="800px" TextMode="MultiLine" Rows="25" runat="server"></asp:TextBox>
            <br />
            <asp:Button ID="btnTestSubscribe" runat="server" Text="Test Subscribe" OnClick="btnTestSubscribe_Click" />&nbsp;
            <asp:Button ID="btnTestNotify" runat="server" Text="Test Notify" OnClick="btnTestNotify_Click" />&nbsp;
            <div>
                <asp:Label ID="lblResult" runat="server" Text=""></asp:Label>
            </div>
        </asp:Panel>
    </div>
    </form>
</body>
</html>
