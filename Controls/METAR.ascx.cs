using MyFlightbook.Weather.ADDS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_METAR : System.Web.UI.UserControl
{
    #region Properties
    private IEnumerable<METAR> m_METARS = null;

    /// <summary>
    /// Specify the enumerable set of METARs
    /// </summary>
    public IEnumerable<METAR> METARs
    {
        get { return m_METARS; }
        set
        {
            gvMetar.DataSource = m_METARS = value;
            gvMetar.DataBind();
        }
    }
    #endregion

    #region Display Utilities
    protected Color ColorForFlightRules(METAR m)
    {
        if (m == null)
            throw new ArgumentNullException("m");
        switch (m.Category)
        {
            default:
            case METAR.FlightCategory.None:
                return Color.Black;
            case METAR.FlightCategory.VFR:
                return Color.Green;
            case METAR.FlightCategory.MVFR:
                return Color.Blue;
            case METAR.FlightCategory.IFR:
                return Color.Red;
            case METAR.FlightCategory.LIFR:
                return Color.Purple;
        }
    }

    protected string WindVectorInlineStyle(METAR m)
    {
        if (m == null)
            throw new ArgumentNullException("m");
        StringBuilder sb = new StringBuilder();
        sb.Append("display:inline-block; height:20px; width:20px; text-align: center; line-height: 20px; position:relative; vertical-align: middle; ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "transform: rotate({0}deg); -webkit-transform: rotate({0}deg); -ms-transform: rotate({0}deg); ", m.wind_dir_degrees);

        if (m.wind_speed_ktSpecified)
        {
            int fontsize = 14;

            if (m.wind_speed_kt < 5)
                fontsize = 16;
            else if (m.wind_speed_kt > 10)
                fontsize = 18;

            sb.AppendFormat(CultureInfo.InvariantCulture, "font-size: {0}px", fontsize);
        }
        return sb.ToString();
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void gvMetar_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Repeater rpt = (Repeater)e.Row.FindControl("rptSkyConditions");
            rpt.DataSource = ((METAR)e.Row.DataItem).sky_condition;
            rpt.DataBind();
        }
    }
}