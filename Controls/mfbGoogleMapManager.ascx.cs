using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.Mapping;

/******************************************************
 * 
 * Copyright (c) 2008-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbGoogleMapMgr : System.Web.UI.UserControl
{

    #region Properties
    private GoogleMap m_Map = new GoogleMap();

    /// <summary>
    /// The map object
    /// </summary>
    public GoogleMap Map { get { return m_Map; } }

    /// <summary>
    /// Whether or not to highlight the specified airports with a marker that enables zooming and other services
    /// </summary>
    public Boolean ShowMarkers
    {
        get { return Map.ShowMarkers; }
        set { Map.ShowMarkers = value; }
    }

    /// <summary>
    /// Whether or not to connect the airports with a blue line
    /// </summary>
    public Boolean ShowRoute
    {
        get { return Map.ShowRoute; }
        set { Map.ShowRoute = value; }
    }

    /// <summary>
    /// Enables/disables user control of zoom or panning.
    /// </summary>
    public Boolean AllowUserManipulation
    {
        get { return Map.ZoomAndPan; }
        set { Map.ZoomAndPan = value; ResizableControlExtender1.Enabled = value; }
    }

    public Boolean AllowResize
    {
        get { return ResizableControlExtender1.Enabled; }
        set { ResizableControlExtender1.Enabled = value; }
    }
    
    /// <summary>
    /// Width of the map
    /// </summary>
    public Unit Width
    {
        set { pnlMap.Width = value; }
        get { return pnlMap.Width; }
    }

    /// <summary>
    /// Height of the map
    /// </summary>
    public Unit Height
    {
        set { pnlMap.Height = value; }
        get { return pnlMap.Height; }
    }

    /// <summary>
    /// ID of the variable containing the map object
    /// </summary>
    public string MapID
    {
        get {return "gm" + UniqueID;}
    }

    /// <summary>
    /// Javascript that will zoom to fit the map.
    /// </summary>
    public string ZoomToFitScript
    {
        get { return String.Format(CultureInfo.InvariantCulture, "javascript:{0}.ZoomOut();", MapID); }
    }
    #endregion

    protected override void OnPreRender(EventArgs e)
    {
        Page.ClientScript.RegisterStartupScript(GetType(), "MapInit" + UniqueID, Map.MapJScript(MapID, pnlMap.ClientID), true);
        base.OnPreRender(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptInclude("googleMaps", String.Format(CultureInfo.InvariantCulture, "https://maps.googleapis.com/maps/api/js?key={0}", MyFlightbook.SocialMedia.GooglePlusConstants.MapsKey));
        Page.ClientScript.RegisterClientScriptInclude("MFBMapScript", ResolveClientUrl("~/Public/GMapScript.js"));
        Page.ClientScript.RegisterClientScriptInclude("MFBMapOMS", ResolveClientUrl("~/Public/oms.min.js"));
    }
}