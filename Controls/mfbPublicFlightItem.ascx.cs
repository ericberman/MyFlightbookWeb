using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Image;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbPublicFlightItem : System.Web.UI.UserControl
{
    /// <summary>
    /// The ID to use for the FB Div
    /// </summary>
    public string FBDivID { get; set; }

    private LogbookEntry m_le = null;

    public LogbookEntry Entry
    {
        get { return m_le; }
        set
        {
            if (value == null)
                throw new ArgumentNullException("value");

            m_le = value;
            lnkFlight.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/public/ViewPublicFlight.aspx/{0}", value.FlightID);
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

            fbComment1.URI = Branding.PublicFlightURL(value.FlightID);

        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}