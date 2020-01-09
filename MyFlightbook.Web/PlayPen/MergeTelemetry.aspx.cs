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
    protected void InitPage()
    {
        Coordinates = new List<Position>();
        HasAlt = HasTime = HasSpeed = true;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
            InitPage();
    }

    private string SessionKeyBase { get { return Page.User.Identity.Name + "telemmerge"; } }
    private string SessionTime { get { return SessionKeyBase + "time"; } }
    private string SessionAlt { get { return SessionKeyBase + "Alt"; } }
    private string SessionSpeed { get { return SessionKeyBase + "Speed"; } }

    private List<Position> Coordinates
    {
        get { return (List<Position>)Session[SessionKeyBase]; }
        set { Session[SessionKeyBase] = value; }
    }

    protected bool FromObj(object o)
    {
        return o == null ? true : Convert.ToBoolean(o, CultureInfo.InvariantCulture);
    }

    protected bool HasTime
    {
        get { return FromObj(Session[SessionTime]); }
        set { Session[SessionTime] = value.ToString(); }
    }

    protected bool HasAlt
    {
        get { return FromObj(Session[SessionAlt]); }
        set { Session[SessionAlt] = value.ToString(); }
    }

    protected bool HasSpeed
    {
        get { return FromObj(Session[SessionSpeed]); }
        set { Session[SessionSpeed] = value.ToString(); }
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
            InitPage();
        }
    }
}