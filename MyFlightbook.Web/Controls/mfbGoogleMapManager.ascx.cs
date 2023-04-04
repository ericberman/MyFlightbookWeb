using MyFlightbook;
using MyFlightbook.Mapping;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2008-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbGoogleMapMgr : UserControl
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
        get { return Map.Options.fShowMarkers; }
        set { Map.Options.fShowMarkers = value; }
    }

    /// <summary>
    /// Whether or not to connect the airports with a blue line
    /// </summary>
    public Boolean ShowRoute
    {
        get { return Map.Options.fShowRoute; }
        set { Map.Options.fShowRoute = value; }
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
        get { return "gm" + UniqueID; }
    }

    /// <summary>
    /// Javascript that will zoom to fit the map.
    /// </summary>
    public string ZoomToFitScript
    {
        get { return String.Format(CultureInfo.InvariantCulture, "javascript:gmapForMapID('{0}').ZoomOut();", MapID); }
    }
    #endregion

    protected override void OnPreRender(EventArgs e)
    {
        if (Page.User.Identity.IsAuthenticated)
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            Map.Options.PathColor = pf.GetPreferenceForKey<string>(MFBConstants.keyPathColor, MFBGoogleMapOptions.DefaultPathColor);
            Map.Options.RouteColor = pf.GetPreferenceForKey<string>(MFBConstants.keyRouteColor, MFBGoogleMapOptions.DefaultRouteColor);
        }

        switch (Mode)
        {
            case GMap_Mode.Dynamic:
                {
                    // Generate a name for the callback function that is unique to this map.
                    string szFuncName = "initMap" + Regex.Replace(UniqueID, "[^a-zA-Z0-9]", string.Empty);
                    Page.ClientScript.RegisterClientScriptInclude("MFBMapScript", ResolveClientUrl("~/Public/Scripts/GMapScript.js?v=10"));
                    Page.ClientScript.RegisterClientScriptBlock(GetType(), "MapInit" + UniqueID, Map.MapJScript(MapID, szFuncName, pnlMap.ClientID), true);

                    // We put this in-line because, as far as I can tell, there's no way to do a registerclientscriptinclude that includes the async or the defer attribute AND because it MUST be after the <div> is loaded; 
                    // if we register these as an include, they get placed at the top, before the <div>
                    // Load googlemaps and then the spiderfier, and use the same callback for both so we can tell when both have loaded
                    litScript.Text = String.Format(CultureInfo.InvariantCulture, "<script defer src=\"https://maps.googleapis.com/maps/api/js?key={0}&callback={1}\"></script><script defer src=\"{2}?v=9&?spiderfier_callback={1}\"></script>", MyFlightbook.SocialMedia.GooglePlusConstants.MapsKey, szFuncName, ResolveClientUrl("~/Public/Scripts/oms.min.js"));
                }
                break;
            case GMap_Mode.Static:
                imgMap.ImageUrl = Map.StaticMapHRef(MyFlightbook.SocialMedia.GooglePlusConstants.MapsKey, (int)Height.Value, (int)Width.Value);
                break;
        }
        base.OnPreRender(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}
