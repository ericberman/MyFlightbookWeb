<%@ Control Language="C#" AutoEventWireup="true" Codebehind="GoogleAnalytics.ascx.cs" Inherits="Controls_GoogleAnalytics" %>
<!-- Global site tag (gtag.js) - Google Analytics -->
<script async src='<% =String.Format(System.Globalization.CultureInfo.InvariantCulture, "https://www.googletagmanager.com/gtag/js?id={0}", AnalyticsID) %>'></script>
<script>
    window.dataLayer = window.dataLayer || [];
    function gtag() { dataLayer.push(arguments); }
    gtag('js', new Date());

    gtag('config', '<% = AnalyticsID %>');
</script>
