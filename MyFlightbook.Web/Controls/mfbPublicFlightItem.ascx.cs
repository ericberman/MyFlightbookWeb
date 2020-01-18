using MyFlightbook;
using MyFlightbook.Image;
using System;
using System.Globalization;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbPublicFlightItem : System.Web.UI.UserControl
{
    private LogbookEntry m_le = null;

    public LogbookEntry Entry
    {
        get { return m_le; }
        set
        {
            if (value == null)
                throw new ArgumentNullException("value");

            m_le = value;
            lnkFlight.NavigateUrl = VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/public/ViewPublicFlight.aspx/{0}", value.FlightID));
            lblDate.Text = value.Date.ToShortDateString();
            lblDetails.Text = (value.TotalFlightTime > 0) ? String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.PublicFlightDuration, value.TotalFlightTime) : string.Empty;

            lblroute.Text = HttpUtility.HtmlEncode(value.Route);
            lblComments.Text = HttpUtility.HtmlEncode(value.Comment);
            lblTail.Text = value.TailNumDisplay;

            mfbILAircraft.Key = value.AircraftID.ToString(System.Globalization.CultureInfo.InvariantCulture);
            mfbILAircraft.Refresh();
            lblModel.Text = value.ModelDisplay;
            lblCatClass.Text = value.CatClassDisplay;

            mfbIlFlight.ImageClass = MFBImageInfo.ImageClass.Flight;
            mfbIlFlight.Key = value.FlightID.ToString(System.Globalization.CultureInfo.InvariantCulture);
            mfbIlFlight.AltText = "";
            mfbIlFlight.Columns = 4;
            mfbIlFlight.MaxImage = -1;
            mfbIlFlight.CanEdit = false;
            mfbIlFlight.Refresh();
        }
    }

    protected void Page_Load(object sender, EventArgs e) { }
}