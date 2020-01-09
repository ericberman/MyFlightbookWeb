<%@ Page Language="C#" AutoEventWireup="true" Inherits="Shunt" Codebehind="Shunt.aspx.cs" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>MyFlightbook Down for Maintenance</title>
    <link href="Public/stylesheet.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <script>(function (d, s, id) {
            var js, fjs = d.getElementsByTagName(s)[0];
            if (d.getElementById(id)) return;
            js = d.createElement(s); js.id = id;
            js.src = "//connect.facebook.net/en_US/sdk.js#xfbml=1&version=v2.9&appId=9433051282";
            fjs.parentNode.insertBefore(js, fjs);
        }(document, 'script', 'facebook-jssdk'));
    </script>
        <div>
        <a href="Default.aspx"><img src="Public/mfblogonew.png" alt="MyFlightbook Logo" /></a>
        <h1>Scheduled Maintenance Underway</h1>
        <p><asp:label ID="lblShuntMsg" runat="server" text=""></asp:label></p>
        <div class="fb-page" data-href="https://www.facebook.com/MyFlightbook/" data-tabs="timeline" data-height="600" data-small-header="true" data-adapt-container-width="true" data-hide-cover="true" data-show-facepile="true">
            <blockquote cite="https://www.facebook.com/MyFlightbook/" class="fb-xfbml-parse-ignore"><a href="https://www.facebook.com/MyFlightbook/">MyFlightbook</a></blockquote>
        </div>
    </div>
    </form>
</body>
</html>

