
/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

function MFBAirportMarker(latitude, longitude, name, code, type, fShowMarker)
{
    this.latitude = latitude;
    this.longitude = longitude;
    this.Name = name;
    this.Code = code;
    this.Type = type;
    this.fShowMarker = fShowMarker;
}

function MFBPhotoMarker(urlThumb, urlFull, latitude, longitude, szcomment, width, height) 
{
    this.urlThumb = urlThumb;
    this.urlFull = urlFull;
    this.latitude = latitude;
    this.longitude = longitude;
    this.comment = szcomment;
    this.width = width;
    this.height = height;
}

function MFBClubMarker(latitude, longitude, name, id, onclickhandler) {
    this.latitude = latitude;
    this.longitude = longitude;
    this.name = name;
    this.clubID = id;
    this.onclickhandler = onclickhandler;
    this.getClickHandler = function (fname, cid) {
        return function () {
            window[fname](cid);
        }
    };
}

function nll(lat, lng)
{
    return new google.maps.LatLng(lat,lng);
}

function MFBMarker()
{
    this.marker = null;
    this.bodyHTML = null;
    this.mfbMap = null;
}

function displayInfoWindow(mfbmarker)
{
    if (mfbmarker.mfbMap.infoWindow != null)
        mfbmarker.mfbMap.infoWindow.close();
        
    mfbmarker.mfbMap.infoWindow = new google.maps.InfoWindow({content: mfbmarker.bodyHTML, maxWidth: 250});
    mfbmarker.mfbMap.infoWindow.open(mfbmarker.mfbMap.gmap, mfbmarker.marker);
}

var MFBMapsOnPage = new Array();

// Legacy function to get the map, if only one is on the page
function getMfbMap() {
    return MFBMapsOnPage.length == 0 ? null : MFBMapsOnPage[0];
}

function getGMap() {
    return MFBMapsOnPage.length == 0 ? null : MFBMapsOnPage[0].gmap;
}

function gmapForContainerID(id) {
    for (var i = 0; i < MFBMapsOnPage.length; i++)
        if (MFBMapsOnPage[i].divContainer == id)
            return MFBMapsOnPage[i].gmap;
    return null;
}

function triggerResize(id) {
    var gmap = gmapForContainerID(id);
    if (gmap != null)
        google.maps.event.trigger(gmap, "resize");
}

function onResizeMapContainer(sender, eventArgs) {
    var e = sender.get_element();
    triggerResize(e.id);
}

var dictRestoreStyle = {};

function toggleZoom(mapID) {
    var d = document.getElementById(mapID);
    if (dictRestoreStyle[mapID] == null) {
        dictRestoreStyle[mapID] = d.style.cssText;
        d.style.position = "fixed";
        d.style.top = "0";
        d.style.left = "0";
        d.style.width = "100%";
        d.style.height = "100%";
        d.style.zIndex = "1000";
    } else {
        d.style.cssText = dictRestoreStyle[mapID];
        dictRestoreStyle[mapID] = null;
        d.style.zIndex = "0";
    }
    triggerResize(mapID);
}

function MFBMap()
{
    this.gmap = null;
    this.oms = null;
    this.divContainer = "";
    this.id = "";
    this.rgAirports = new Array();
    this.rgImages = new Array();
    this.rgClubs = new Array();
    this.zoom = 0;
    this.fShowMap = 0;
    this.fShowMarkers = 1;
    this.fShowRoute = 1;
    this.fDisableUserManip = 0;
    this.defaultZoom = 1;
    this.fAutoZoom = 0;
    this.fAutofillPanZoom = 0;
    this.defaultLat = 47.90634167;
    this.defaultLong = -122.2815639;
    this.minLat = this.defaultLat;
    this.maxLat = this.defaultLat;
    this.minLong = this.defaultLong;
    this.maxLong = this.defaultLong;
    this.center = new google.maps.LatLng(this.defaultLat, this.defaultLong);
    this.pathArray = null;
    this.infoWindow = null;
    this.MapType = google.maps.MapTypeId.HYBRID;
    this.clickPositionMarker = null;
    this.iw = null;
    
    this.NewMap = function()
    {
        this.center = new google.maps.LatLng(this.defaultLat, this.defaultLong);
        var options = {zoom:this.zoom, center:this.center, mapTypeId: this.MapType};
        this.gmap = new google.maps.Map(document.getElementById(this.divContainer), options);
        this.oms = new OverlappingMarkerSpiderfier(this.gmap, { markersWontMove: true, markersWontHide: true, keepSpiderfied: true, legWeight:3 });
        this.iw = new google.maps.InfoWindow();
        this.iw.testProp = "MFBInfoWindow";
        this.oms.addListener('click', function (marker, event) {
            if (marker.clickHandleOverride != null)
                marker.clickHandleOverride();
            this.iw.open(this.gmap, marker);
        });

        this.oms.addListener('spiderfy', function (markers) {
            iw.close();
        });
        this.oms.addListener('unspiderfy', function (markers) {
        });

        if (this.fDisableUserManip != 0)
        {
            options = {draggable:false, 
                disableDoubleClickZoom:true,
                mapTypecontrol: false,
                scrollwheel: false,
                navigationControl: false, 
                disableDefaultUI: true,
                scaleControl:false};
            this.gmap.setOptions(options);
        }
        else
        {
            options = {scrollwheel:true, disableDoubleClickZoom:false};
            this.gmap.setOptions(options);

            var zoomControl = document.createElement('div');
            zoomControl.index = 1;

            // Set CSS for the control border.
            var controlUI = document.createElement('div');
            controlUI.style.width = "20px";
            controlUI.style.height = "20px";
            controlUI.style.padding = "3px";
            controlUI.style.cursor = 'pointer';
            zoomControl.appendChild(controlUI);
            
            var img = document.createElement('img');
            img.src = "/logbook/images/mapzoom.png";
            controlUI.appendChild(img);

            var mapID = this.divContainer;

            controlUI.addEventListener('click', function () { toggleZoom(mapID); });

            this.gmap.controls[google.maps.ControlPosition.TOP_RIGHT].push(zoomControl);
        }
        
        this.ShowOverlays();
        this.ZoomOut();

        google.maps.event.addListener(this.gmap, 'dragend', this.autofillPanZoom);
        google.maps.event.addListener(this.gmap, 'zoom_changed', this.autofillPanZoom);

        return this.gmap;
    }

    this.autofillPanZoom = function () {
        var mfbMap = getMfbMap();
        if (mfbMap.fAutofillPanZoom) {
            if (mfbMap.gmap.getZoom() >= 7) {
                var llb = mfbMap.gmap.getBounds();
                var llbSW = llb.getSouthWest();
                var llbNE = llb.getNorthEast();
                MyFlightbook.MFBWebService.AirportsInBoundingBox(llbSW.lat(), llbSW.lng(), llbNE.lat(), llbNE.lng(),
                    function (result) {
                        mfbMap.clearMarkers();
                        var rgAirports = new Array();
                        for (var i = 0; i < result.length; i++) {
                            rgAirports.push(new MFBAirportMarker(result[i].LatLong.Latitude, result[i].LatLong.Longitude, result[i].Name, result[i].Code, result[i].FacilityType, true));
                        }
                        mfbMap.rgAirports = new Array();
                        mfbMap.rgAirports.push(rgAirports);
                        mfbMap.ShowOverlays();
                    },
                    function (result) {
                        alert(arguments[0].get_message());
                    });
            }
            else {
                mfbMap.clearMarkers();
                mfbMap.rgAirports = new Array();
                mfbMap.ShowOverlays();
            }
        }
    }
    
    this.ZoomOut = function() 
    {
       var gBoundsMap = new google.maps.LatLngBounds(new google.maps.LatLng(this.minLat, this.minLong), 
                                                     new google.maps.LatLng(this.maxLat, this.maxLong));
                                                     
       if (this.fAutoZoom == 0)
            this.gmap.setZoom(this.defaultZoom);
       else
            this.gmap.fitBounds(gBoundsMap);
       this.gmap.setCenter(gBoundsMap.getCenter());
    }
    
    this.showAirport = function(lat, lon)
        {
	        if (this.gmap !== null)
	        {
	            this.gmap.setCenter(new google.maps.LatLng(lat, lon));
	            this.gmap.setZoom(14);
		    }
        }
    
    this.showImage = function(i)
    {
        if (this.gmap != null)
        {
            this.gmap.setCenter(new google.maps.LatLng(this.rgImages[i].latitude, this.rgImages[i].longitude));
            this.gmap.setZoom(14);
        }
    }
   
    this.addEventMarker = function(point, s)
    {
        return this.createNavaidMarker(point, s, null, this.id);
    }
    
    this.addListenerFunction = function(s)
    {
        google.maps.event.addListener(this.gmap, 'click', s);
    }

    this.clearMarkers = function () {
        if (this.rgMarkers == null)
            return;

        for (var i = 0; i < this.rgMarkers.length; i++) {
            this.rgMarkers[i].setMap(null);
        }
        this.rgMarkers.length = 0;  // free up some memory
        this.rgMarkers = new Array();
    }
    
    this.createMarker = function(point, name, icon, szHtml)
    {
      var mfbmarker = new MFBMarker();
      mfbmarker.marker = new google.maps.Marker({position:point, clickable:true, icon:icon, map:this.gmap, title:name});;
      mfbmarker.bodyHTML = '<div style="min-width:250px;">' + szHtml + '</div>';
      mfbmarker.mfbMap = this;
      
      if (this.rgMarkers == null)
          this.rgMarkers = new Array();
      this.rgMarkers.push(mfbmarker.marker);

      // instead of adding the listener to google.maps.event, add the clickhandler to the marker so that the spiderfier will work.
      // google.maps.event.addListener(mfbmarker.marker, 'click', function () { displayInfoWindow(mfbmarker); });
      mfbmarker.marker.clickHandleOverride = function () { displayInfoWindow(mfbmarker); };
      
      return mfbmarker.marker;
    }

    this.iconForType = function (sz) {
        if (sz == "pin")
            return "https://myflightbook.com/logbook/images/pushpin.png";
        else if (sz == "Airport" || sz == "Heliport" || sz == "Seaport" || sz == "A" || sz == "H" || sz == "S")
            return new google.maps.MarkerImage('https://myflightbook.com/logbook/images/airport.png', null, null, new google.maps.Point(6, 6));
        else if (sz == "Photograph")
            return new google.maps.MarkerImage('https://myflightbook.com/logbook/images/cameramarker.png', null, null, new google.maps.Point(19, 19));
        else if (sz.length > 0)
            return new google.maps.MarkerImage('https://myflightbook.com/logbook/images/tower.png', null, null, new google.maps.Point(8, 8));
        else
            return '';
    }
    
    // Creates a marker at the given point with the given number label and adds a listener that pops an info-window.  Designed for airports and navaids.
    this.createNavaidMarker = function (point, title, airport, mapID) {
        var sz;
        sz = "<b>" + title + "</b><br />";
        var icon;
        if (airport == null) {
            sz = title;
            icon = this.iconForType('pin');
        }
        else {
            if (airport.Type == "Airport") {
                sz += "<a href=\"http://www.aopa.org/airports/" + airport.Code + "\" target=\"_blank\">Get Airport Info for " + airport.Code + "</a><br />";
                sz += "<a href=\"http://www.aopa.org/wx/#a=" + airport.Code + "\" target=\"_blank\">Get Current weather at " + airport.Code + "</a><br />";
            }
            else
                sz += airport.Type + "<br />";

            sz += "<a href=\"javascript:" + mapID + ".showAirport(" + airport.latitude + ", " + airport.longitude + ")\">Zoom in</a>";
            icon = this.iconForType(airport.Type);
        }

        return this.createMarker(point, name, icon, sz);
    }
    
    this.createImageMarker = function(point, i, mapID)
    {
        var szImg = "<a href=\"" + this.rgImages[i].urlFull + "\" target=\"_blank\"><img src=\"" + this.rgImages[i].urlThumb + "\"></a>";
        var szZoom = "<a href=\"javascript:" + mapID + ".showImage(" + i + ")\">Zoom in</a>";
        var szDiv = "<div>" + szZoom + "<br />" + szImg + "<br /><p>" + this.rgImages[i].comment + "</p></div>";
        var szName = "Photograph";
        var img = this.rgImages[i];
        var icon = { url: this.rgImages[i].urlThumb, scaledSize:new google.maps.Size(img.width, img.height), anchor:new google.maps.Point(img.width / 2, img.height / 2) };
        return this.createMarker(point, szName, icon, szDiv);
    }
    
    // edit airports functionality    
    this.clickMarker = function(point, name, type, szHtml)
    {
        if (this.clickPositionMarker)
            this.clickPositionMarker.setMap(null);
        if (this.infoWindow)
            this.infoWindow.close();
        
        this.clickPositionMarker = this.createMarker(point, name, this.iconForType(type), szHtml);
    }

    // set up overlays
    this.ShowOverlays = function () {
        var i;
        var j;
        var point;
        if (this.rgAirports.length > 0) {
            if (this.fShowMarkers != 0) {
                for (i = 0; i < this.rgAirports.length; i++) {
                    var airportList = this.rgAirports[i];
                    var points = [];
                    for (j = 0; j < airportList.length; j++) {
                        point = new google.maps.LatLng(airportList[j].latitude, airportList[j].longitude);
                        this.oms.addMarker(this.createNavaidMarker(point, airportList[j].Name + " (" + airportList[j].Code + ")", airportList[j], this.id));
                        points.push(point);
                    }

                    if (this.fShowRoute != 0 && !this.fAutofillPanZoom)
                        var routeMap = new google.maps.Polyline({ path: points, strokeColor: "#0000FF", strokeOpacity: 0.5, strokeWeight: 5, map: this.gmap, geodesic: true });
                }
            }
        }

        if (this.rgClubs != null && this.rgClubs.length > 0)
        {
            for (i = 0; i < this.rgClubs.length; i++) {
                var c = this.rgClubs[i];
                point = new google.maps.LatLng(c.latitude, c.longitude);

                var mfbmarker = new MFBMarker();
                mfbmarker.marker = new google.maps.Marker({ position: point, clickable: true, icon:'', map: this.gmap, title: c.name });;
                mfbmarker.bodyHTML = '';
                mfbmarker.mfbMap = this;

                if (this.rgMarkers == null)
                    this.rgMarkers = new Array();
                this.rgMarkers.push(mfbmarker.marker);

                // Instead of adding the clickhandler to google.maps.event, add it to the spiderfier.
                mfbmarker.marker.clickHandleOverride = c.getClickHandler(c.onclickhandler, c.clubID);
                this.oms.addMarker(mfbmarker.marker);
                // google.maps.event.addListener(mfbmarker.marker, 'click', c.getClickHandler(c.onclickhandler, c.clubID));
            }
        }

        if (this.rgImages.length > 0 && this.fShowMarkers != 0) {
            for (i = 0; i < this.rgImages.length; i++) {
                point = new google.maps.LatLng(this.rgImages[i].latitude, this.rgImages[i].longitude);
                this.oms.addMarker(this.createImageMarker(point, i, this.id));
            }
        }

        if (this.pathArray != null)
            var pathMap = new google.maps.Polyline({ path: this.pathArray, strokeColor: "#FF0000", strokeOpacity: 0.5, strokeWeight: 5, map: this.gmap });
    }
}

function MFBNewMapOptions(mfbMapOptions)
{
    var mfbNewMap = new MFBMap();
    
    if (mfbMapOptions.divContainer != null)
        mfbNewMap.divContainer = mfbMapOptions.divContainer;
    if (mfbMapOptions.rgAirports != null)
        mfbNewMap.rgAirports = mfbMapOptions.rgAirports;
    if (mfbMapOptions.rgImages != null)
        mfbNewMap.rgImages = mfbMapOptions.rgImages;
    if (mfbMapOptions.rgClubs != null)
        mfbNewMap.rgClubs = mfbMapOptions.rgClubs;
    if (mfbMapOptions.fZoom != null)
        mfbNewMap.zoom = mfbMapOptions.fZoom;
    if (mfbMapOptions.fShowMap != null)
        mfbNewMap.fShowMap = mfbMapOptions.fShowMap;
    if (mfbMapOptions.fShowMarkers != null)
        mfbNewMap.fShowMarkers = mfbMapOptions.fShowMarkers;
    if (mfbMapOptions.fShowRoute != null)
        mfbNewMap.fShowRoute = mfbMapOptions.fShowRoute;
    if (mfbMapOptions.fDisableUserManip != null)
        mfbNewMap.fDisableUserManip = mfbMapOptions.fDisableUserManip;
    if (mfbMapOptions.defaultZoom != null)
        mfbNewMap.defaultZoom = mfbMapOptions.defaultZoom;
    if (mfbMapOptions.fAutoZoom != null)
        mfbNewMap.fAutoZoom = mfbMapOptions.fAutoZoom;
    if (mfbMapOptions.fAutofillPanZoom != null)
        mfbNewMap.fAutofillPanZoom = mfbMapOptions.fAutofillPanZoom;
    if (mfbMapOptions.defaultLat != null)
        mfbNewMap.defaultLat = mfbMapOptions.defaultLat;
    if (mfbMapOptions.defaultLong != null)
        mfbNewMap.defaultLong = mfbMapOptions.defaultLong;
    if (mfbMapOptions.defaultLat != null)
        mfbNewMap.defaultLat = mfbMapOptions.defaultLat;
    if (mfbMapOptions.minLat != null)
        mfbNewMap.minLat = mfbMapOptions.minLat;
    if (mfbMapOptions.maxLat != null)
        mfbNewMap.maxLat = mfbMapOptions.maxLat;
    if (mfbMapOptions.minLong != null)
        mfbNewMap.minLong = mfbMapOptions.minLong;
    if (mfbMapOptions.maxLong != null)
        mfbNewMap.maxLong = mfbMapOptions.maxLong;
    if (mfbMapOptions.pathArray != null)
        mfbNewMap.pathArray = mfbMapOptions.pathArray;
    if (mfbMapOptions.MapType != null)
        mfbNewMap.MapType = mfbMapOptions.MapType;
    if (mfbMapOptions.id != null)
        mfbNewMap.id = mfbMapOptions.id;
        
    return mfbNewMap;        
}        

function AddMap(mfbMapOptions) 
{
  if (mfbMapOptions.fShowMap) 
    {
    mfbMapOptions.NewMap();

    return mfbMapOptions;
    }
}

function unload()
{
GUnload();
}
    