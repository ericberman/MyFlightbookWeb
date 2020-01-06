<%@ Page Language="C#" AutoEventWireup="true" Inherits="Member_Donate" Codebehind="Donate.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link runat="server" id="lnkStyleRef" rel="stylesheet" type="text/css" />
    <link href="" rel="Stylesheet" type="text/css" runat="server" id="cssBranded" />
    <meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1" />
</head>
<body>
    <form id="form1" runat="server" action="https://www.paypal.com/cgi-bin/webscr" method="post" target="_top">
	<div>
        <input type="hidden" name="cmd" value="_s-xclick" />
        <input type="hidden" name="hosted_button_id" value="YH8NJZL5W9HTU" />
        <table>
        <tr><td><input type="hidden" name="on0" value="Donation Levels:" />Donation Levels:</td></tr><tr><td><select name="os0">
            <option value="'Overnight parking fees'">US$10 - Overnight parking fees</option>
            <option value="'Twice around the pattern'">US$15 - Twice around the pattern</option>
            <option value="'Shockingly little avgas'">US$25 - Shockingly little avgas</option>
            <option value="'An hour of instruction'">US$40 - An hour of instruction</option>
            <option value="'Hundred Dollar Hamburger'">US$100 - Hundred Dollar Hamburger</option>
        </select> </td></tr>
        </table>
        <input type="hidden" name="custom" value="<%=Page.User.Identity.Name %>" />
        <input type="hidden" name="on1" value="ProductID" />
        <input type="hidden" name="os1" value="MFBDonation" />
        <input type="hidden" name="currency_code" value="USD" />
        <input type="image" src="https://www.paypalobjects.com/en_US/i/btn/btn_paynowCC_LG.gif" border="0" name="submit" alt="PayPal - The safer, easier way to pay online!" />
        <img alt="" border="0" src="https://www.paypalobjects.com/en_US/i/scr/pixel.gif" width="1" height="1" />
	</div>
    </form>
</body>
</html>
