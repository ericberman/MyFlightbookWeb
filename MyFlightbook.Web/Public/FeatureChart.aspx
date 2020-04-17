<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FeatureChart.aspx.cs" MasterPageFile="~/MasterPage.master" Inherits="MyFlightbook.Web.Public.FeatureChart" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="server">
    <% =Branding.ReBrand(Resources.LocalizedText.FeaturesHeader) %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" runat="Server">
    <style>
        table.featureTable {
            max-width: 80%;
            margin-left:auto;
            margin-right:auto;
            vertical-align:top;
            border-spacing: 0px;
            border-collapse: collapse;
        }

        .featureTable tr {
            vertical-align:middle;
        }

        .featureTable tr td {
            padding: 3px;
            vertical-align:middle;
        }

        .featureTable .featureGroup {
            font-weight:bold;
            font-size: larger;
        }

        .featureTable .appHeader {
            font-weight:bold;
            font-size: larger;
            border: none;
        }

        .featureTable tr.appHeader td {
            border: none;
        }

        .featureTable .spacerColumn {
            width: 2em;
        }

        .featureTable .feature {
            text-align:center;
            max-width: 20em;
            border: 1px solid gray;
        }

        .featureName {
            max-width: 20em;
            border: 1px solid gray;
        }

        .highlightedFeature {
            background-color: #eeeeee;
        }

    </style>
    <% =Branding.ReBrand(Resources.LocalizedText.FeatureTable) %>
</asp:Content>
