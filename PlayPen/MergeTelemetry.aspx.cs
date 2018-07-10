using MyFlightbook.Geography;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class PlayPen_MergeTelemetry : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            Coordinates = new List<Position>();
    }

    private string SessionKeyBase { get { return Page.User.Identity.Name + "telemmerge"; } }

    private List<Position> Coordinates
    {
        get { return (List<Position>)Session[SessionKeyBase]; }
        set { Session[SessionKeyBase] = value; }
    }

    protected bool HasTime
    {
        get { return Convert.ToBoolean(hdnHasTime.Value, CultureInfo.InvariantCulture); }
        set { hdnHasTime.Value = value.ToString(); }
    }

    protected bool HasAlt
    {
        get { return Convert.ToBoolean(hdnHasAlt.Value, CultureInfo.InvariantCulture); }
        set { hdnHasAlt.Value = value.ToString(); }
    }

    protected bool HasSpeed
    {
        get { return Convert.ToBoolean(hdnHasSpeed.Value, CultureInfo.InvariantCulture); }
        set { hdnHasSpeed.Value = value.ToString(); }
    }

    protected void AjaxFileUpload1_UploadComplete(object sender, AjaxControlToolkit.AjaxFileUploadEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.State != AjaxControlToolkit.AjaxFileUploadState.Success)
            return;

        using (FlightData fd = new FlightData())
        {
            if (fd.ParseFlightData(System.Text.Encoding.UTF8.GetString(e.GetContents())))
            {

                if (fd.HasLatLongInfo)
                {
                    Coordinates.AddRange(fd.GetTrajectory());
                    HasTime = HasTime && fd.HasDateTime;
                    HasAlt = HasAlt && fd.HasAltitude;
                    HasSpeed = HasSpeed && fd.HasSpeed;
                }
            }
            else
                lblErr.Text = fd.ErrorString;
        }

        e.DeleteTemporaryData();
    }

    protected void btnMerge_Click(object sender, EventArgs e)
    {
        string szResult = string.Empty;
        if (Coordinates != null)
        {
            Coordinates.Sort();
            var dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.GPX);
            Response.Clear();
            Response.ContentType = dst.Mimetype;
            Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename=MergedData.{0}", dst.DefaultExtension));
            using (FlightData fd = new FlightData())
                fd.WriteGPXData(Response.OutputStream, Coordinates, HasTime, HasAlt, HasSpeed);
            Response.End();
        }
    }
}