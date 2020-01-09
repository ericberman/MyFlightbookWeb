using MyFlightbook.Airports;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbAirportServices : System.Web.UI.UserControl
{
    /// <summary>
    /// The client ID for the google map
    /// </summary>
    public string GoogleMapID { get; set; }

    public bool ShowZoom { get; set; }
    public bool ShowInfo { get; set; }
    public bool ShowFBO { get; set; }
    public bool ShowHotels { get; set; }
    public bool ShowMetar { get; set; }

    public bool AddZoomLink { get; set; }

    /// <summary>
    /// The airports to display
    /// </summary>
    public void SetAirports(IEnumerable<airport> rgAirports)
    {
        if (rgAirports == null)
            throw new ArgumentNullException("rgAirports");

        // dedupe and sort the airports; eliminate anything that isn't an airport
        List<airport> lst = new List<airport>(rgAirports);
        lst.RemoveAll(ap => !ap.IsPort);

        Dictionary<string, airport> dict = new Dictionary<string, airport>();
        foreach (airport ap in lst)
            dict[ap.Code] = ap;

        lst = new List<airport>(dict.Values);
        lst.Sort();

        gvAirports.DataSource = lst;
        gvAirports.DataBind();

        gvAirports.Columns[0].Visible = ShowZoom;
        gvAirports.Columns[1].Visible = ShowInfo;
        gvAirports.Columns[2].Visible = ShowFBO;
        gvAirports.Columns[3].Visible = ShowMetar;
        gvAirports.Columns[4].Visible = ShowHotels;
    }

    protected string ZoomLink(airport ap)
    {
        if (ap == null)
            throw new ArgumentNullException("ap");
        return String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:{0}.gmap.setCenter(new google.maps.LatLng({1}, {2}));{0}.gmap.setZoom(14);", GoogleMapID, ap.LatLong.LatitudeString, ap.LatLong.LongitudeString);
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            HyperLink lnkZoom = (HyperLink)e.Row.FindControl("lnkZoom");
            Image imgAirport = (Image)e.Row.FindControl("imgAirport");
            airport ap = (airport)e.Row.DataItem;

            ((MultiView)e.Row.FindControl("mvAirportName")).ActiveViewIndex = AddZoomLink ? 0 : 1;

            imgAirport.Attributes["onclick"] = lnkZoom.NavigateUrl = ZoomLink(ap);

            HyperLink lnkHotels = (HyperLink)e.Row.FindControl("lnkHotels");
            lnkHotels.NavigateUrl = String.Format(System.Globalization.CultureInfo.InvariantCulture, "http://www.expedia.com/pubspec/scripts/eap.asp?goto=hotsearch&Map=1&lat={0}&long={1}&CityName={2}", ap.LatLong.LatitudeString, ap.LatLong.LongitudeString, HttpUtility.UrlEncode(ap.Name));
            lnkHotels.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.AirportServiceHotels, ap.Name);
        }
    }
}