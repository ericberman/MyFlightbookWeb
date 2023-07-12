using MyFlightbook.Airports;
using Newtonsoft.Json;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Mapping
{
    public partial class VisitedMap : Page
    {
        protected string DataToMap { get; set; } = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            Page.ClientScript.RegisterClientScriptInclude("tmapsvg", ResolveClientUrl("~/public/tmap/scripts/svg-world-map.js?v=1"));
            Page.ClientScript.RegisterClientScriptInclude("robinson", ResolveClientUrl("~/public/tmap/scripts/robinson.js"));
            Page.ClientScript.RegisterClientScriptInclude("tmapsvgpan", ResolveClientUrl("~/public/tmap/scripts/svg-pan-zoom.js"));
            Page.ClientScript.RegisterClientScriptInclude("tmapiso", ResolveClientUrl("~/public/tmap/scripts/iso.js"));

            if (!IsPostBack)
            {
                string fqs = util.GetStringParam(Request, "fq");
                FlightQuery fq = String.IsNullOrEmpty(fqs) ? new FlightQuery(User.Identity.Name) : FlightQuery.FromBase64CompressedJSON(fqs);
                if (fq.UserName.CompareCurrentCultureIgnoreCase(User.Identity.Name) != 0)
                    throw new UnauthorizedAccessException();

                VisitedLocations locations = new VisitedLocations(VisitedAirport.VisitedAirportsForQuery(fq));
                DataToMap = JsonConvert.SerializeObject(locations, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });
            }
        }
    }
}