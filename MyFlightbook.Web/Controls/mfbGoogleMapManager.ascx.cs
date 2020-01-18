using MyFlightbook.Mapping;
using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbGoogleMapMgr : System.Web.UI.UserControl
{

    #region Properties
    private readonly GoogleMap m_Map = new GoogleMap();

    /// <summary>
    /// The map object
    /// </summary>
    public GoogleMap Map { get { return m_Map; } }

    public GMap_Mode Mode
    {
        get { return mvMap.ActiveViewIndex == 0 ? GMap_Mode.Dynamic : GMap_Mode.Static; }
        set { mvMap.SetActiveView(value == GMap_Mode.Dynamic ? vwDynamic : vwStatic); }
    }

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
        set
        {
            if (Mode == GMap_Mode.Dynamic)
                pnlMap.Width = value;
            else
                imgMap.Width = value;
        }
        get { return (Mode == GMap_Mode.Dynamic) ? pnlMap.Width : imgMap.Width; }
    }

    /// <summary>
    /// Height of the map
    /// </summary>
    public Unit Height
    {
        set
        {
            if (Mode == GMap_Mode.Dynamic)
                pnlMap.Height = value;
            else
                imgMap.Height = value;
        }
        get { return (Mode == GMap_Mode.Dynamic) ? pnlMap.Height : imgMap.Height; }
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
        switch (Mode) {
            case GMap_Mode.Dynamic:
                Page.ClientScript.RegisterClientScriptInclude("googleMaps", String.Format(CultureInfo.InvariantCulture, "https://maps.googleapis.com/maps/api/js?key={0}", MyFlightbook.SocialMedia.GooglePlusConstants.MapsKey));
                Page.ClientScript.RegisterClientScriptInclude("MFBMapScript", ResolveClientUrl("~/Public/Scripts/GMapScript.js?v=3"));
                Page.ClientScript.RegisterClientScriptInclude("MFBMapOMS", ResolveClientUrl("~/Public/Scripts/oms.min.js"));
                Page.ClientScript.RegisterStartupScript(GetType(), "MapInit" + UniqueID, Map.MapJScript(MapID, pnlMap.ClientID), true);
                break;
            case GMap_Mode.Static:
                imgMap.ImageUrl = Map.StaticMapHRef(MyFlightbook.SocialMedia.GooglePlusConstants.MapsKey, (int) Height.Value, (int) Width.Value);
                break;
        }
        base.OnPreRender(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}