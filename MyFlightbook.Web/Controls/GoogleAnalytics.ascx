<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_GoogleAnalytics" Codebehind="GoogleAnalytics.ascx.cs" %>
<!-- Google Analytics -->
<script>
    window.ga=window.ga||function(){(ga.q=ga.q||[]).push(arguments)};ga.l=+new Date;
    ga('create', 'UA-1545617-1', 'auto');
    ga('send', 'pageview' <% =RedirJScript %>);
</script>
<script async src='https://www.google-analytics.com/analytics.js'></script>
<!-- End Google Analytics -->
