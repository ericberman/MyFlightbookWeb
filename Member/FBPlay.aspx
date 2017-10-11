<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="FBPlay.aspx.cs" Inherits="Member_FBPlay" Title="Airport Identification Game" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">

<div>

<script src="http://static.ak.connect.facebook.com/js/api_lib/v0.4/FeatureLoader.js.php" type="text/javascript">
</script>
   <fb:login-button onlogin="alert('Authenticated!');"></fb:login-button>  

<div id="info">

</div>
<script type="text/javascript">
// <![CDATA[  
FB.init("db9323a6be7a015c5ebe95898b49bace", "xd_receiver.htm");
// ]]>
</script>

</div>
</asp:Content>

