/******************************************************
 * 
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
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

function MFBPhotoMarker(hrefThumb, hrefFull, latitude, longitude, szcomment, width, height) 
{
    this.hrefThumb = hrefThumb;
    this.hrefFull = hrefFull;
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

    this.handleClick = function () {
        eval(onclickhandler)(id);
    }
}

function nll(lat, lng) {
    return new google.maps.LatLng(lat,lng);
}

function MFBMarker() {
    this.marker = null;
    this.bodyHTML = null;
    this.mfbMap = null;
}

function displayInfoWindow(mfbmarker)
{
    if (mfbmarker.mfbMap.infoWindow)
        mfbmarker.mfbMap.infoWindow.close();
        
    mfbmarker.mfbMap.infoWindow = new google.maps.InfoWindow({content: mfbmarker.bodyHTML, maxWidth: 250});
    mfbmarker.mfbMap.infoWindow.open(mfbmarker.mfbMap.gmap, mfbmarker.marker);
}

var MFBMapsOnPage = new Array();

// Legacy function to get the map, if only one is on the page
function getMfbMap() {
    return MFBMapsOnPage.length === 0 ? null : MFBMapsOnPage[0];
}

function getGMap() {
    return MFBMapsOnPage.length === 0 ? null : MFBMapsOnPage[0].gmap;
}

function gmapForContainerID(id) {
    for (var i = 0; i < MFBMapsOnPage.length; i++)
        if (MFBMapsOnPage[i].divContainer === id)
            return MFBMapsOnPage[i].gmap;
    return null;
}

function triggerResize(id) {
    var gmap = gmapForContainerID(id);
    if (gmap)
        google.maps.event.trigger(gmap, "resize");
}

function onResizeMapContainer(sender, _eventArgs) {
    var e = sender.get_element();
    triggerResize(e.id);
}

var dictRestoreStyle = {};

function toggleZoom(mapID) {
    var d = document.getElementById(mapID);
    if (!dictRestoreStyle[mapID]) {
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
    this.fShowMap = false;
    this.fShowMarkers = true;
    this.fShowRoute = true;
    this.defaultZoom = true;
    this.fAutoZoom = false;
    this.fAutofillPanZoom = true;
    this.defaultLat = 47.90634167;
    this.defaultLong = -122.2815639;
    this.minLat = this.defaultLat;
    this.maxLat = this.defaultLat;
    this.minLong = this.defaultLong;
    this.maxLong = this.defaultLong;
    this.center = new google.maps.LatLng(this.defaultLat, this.defaultLong);
    this.pathArray = null;
    this.infoWindow = null;
    this.mapType = google.maps.MapTypeId.HYBRID;
    this.clickPositionMarker = null;
    
    this.NewMap = function () {
        this.center = new google.maps.LatLng(this.defaultLat, this.defaultLong);
        var options = { zoom: this.zoom, center: this.center, mapTypeId: this.mapType };
        this.gmap = new google.maps.Map(document.getElementById(this.divContainer), options);
        this.oms = new OverlappingMarkerSpiderfier(this.gmap, { markersWontMove: true, markersWontHide: true, keepSpiderfied: true, legWeight: 3 });
        this.oms.addListener('click', function (marker, _event) {
            if (marker.clickHandleOverride)
                marker.clickHandleOverride();
        });

        this.oms.addListener('spiderfy', function (_markers) {
        });
        this.oms.addListener('unspiderfy', function (_markers) {
        });

        options = { scrollwheel: true, disableDoubleClickZoom: false };
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
        img.src = "https://myflightbook.com/logbook/images/mapzoom.png";
        controlUI.appendChild(img);

        var mapID = this.divContainer;

        controlUI.addEventListener('click', function () { toggleZoom(mapID); });

        this.gmap.controls[google.maps.ControlPosition.TOP_RIGHT].push(zoomControl);

        this.ShowOverlays();
        this.ZoomOut();

        google.maps.event.addListener(this.gmap, 'dragend', this.autofillPanZoom);
        google.maps.event.addListener(this.gmap, 'zoom_changed', this.autofillPanZoom);

        return this.gmap;
    };

    this.autofillPanZoom = function () {
        var mfbMap = getMfbMap();
        if (mfbMap.fAutofillPanZoom) {
            if (mfbMap.gmap.getZoom() >= 6) {
                var llb = mfbMap.gmap.getBounds();
                var llbSW = llb.getSouthWest();
                var llbNE = llb.getNorthEast();
                MyFlightbook.MFBWebService.AirportsInBoundingBox(llbSW.lat(), llbSW.lng(), llbNE.lat(), llbNE.lng(), mfbMap.fAutofillHeliports,
                    function (result) {
                        mfbMap.clearMarkers();
                        var rgAirports = new Array();
                        for (var i = 0; i < result.length; i++) {
                            rgAirports.push(new MFBAirportMarker(result[i].LatLong.Latitude, result[i].LatLong.Longitude, result[i].NameWithGeoRegion, result[i].Code, result[i].FacilityType, true));
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
    };
    
    this.ZoomOut = function () {
        var gBoundsMap = new google.maps.LatLngBounds(new google.maps.LatLng(this.minLat, this.minLong),
            new google.maps.LatLng(this.maxLat, this.maxLong));

        if (!this.fAutoZoom)
            this.gmap.setZoom(this.defaultZoom);
        else
            this.gmap.fitBounds(gBoundsMap);
        this.gmap.setCenter(gBoundsMap.getCenter());
    };
    
    this.showAirport = function (lat, lon) {
        if (this.gmap) {
            this.gmap.setCenter(new google.maps.LatLng(lat, lon));
            this.gmap.setZoom(14);
        }
    };
    
    this.showImage = function (i) {
        if (this.gmap) {
            this.gmap.setCenter(new google.maps.LatLng(this.rgImages[i].latitude, this.rgImages[i].longitude));
            this.gmap.setZoom(14);
        }
    };
   
    this.addEventMarker = function (point, s) {
        return this.createNavaidMarker(point, s, null, this.id);
    };
    
    this.addListenerFunction = function (s) {
        google.maps.event.addListener(this.gmap, 'click', s);
    };

    this.clearMarkers = function () {
        if (!this.rgMarkers)
            return;

        for (var i = 0; i < this.rgMarkers.length; i++) {
            this.rgMarkers[i].setMap(null);
        }
        this.rgMarkers.length = 0;  // free up some memory
        this.rgMarkers = new Array();
    };
    
    this.createMarker = function (point, name, icon, szHtml) {
        var mfbmarker = new MFBMarker();
        mfbmarker.marker = new google.maps.Marker({ position: point, clickable: true, icon: icon, map: this.gmap, title: name });
        mfbmarker.bodyHTML = '<div style="min-width:250px;">' + szHtml + '</div>';
        mfbmarker.mfbMap = this;

        if (!this.rgMarkers)
            this.rgMarkers = new Array();
        this.rgMarkers.push(mfbmarker.marker);

        // instead of adding the listener to google.maps.event, add the clickhandler to the marker so that the spiderfier will work.
        mfbmarker.marker.clickHandleOverride = function () { displayInfoWindow(mfbmarker); };

        return mfbmarker.marker;
    };

    this.iconForType = function (sz) {
        if (sz === "pin")
            return "https://myflightbook.com/logbook/images/pushpin.png";
        else if (sz === "Airport" || sz === "Heliport" || sz === "Seaport" || sz === "A" || sz === "H" || sz === "S")
            return new google.maps.MarkerImage('https://myflightbook.com/logbook/images/airport.png', null, null, new google.maps.Point(6, 6));
        else if (sz === "Photograph")
            return new google.maps.MarkerImage('https://myflightbook.com/logbook/images/cameramarker.png', null, null, new google.maps.Point(19, 19));
        else if (sz.length > 0)
            return new google.maps.MarkerImage('https://myflightbook.com/logbook/images/tower.png', null, null, new google.maps.Point(8, 8));
        else
            return '';
    };
    
    // Creates a marker at the given point with the given number label and adds a listener that pops an info-window.  Designed for airports and navaids.
    this.createNavaidMarker = function (point, title, airport, mapID) {
        var sz;
        sz = "<b>" + title + "</b><br />";
        var icon;
        if (!airport) {
            sz = title;
            icon = this.iconForType('pin');
        }
        else {
            if (airport.Type === "Airport") {
                sz += "<a href=\"http://www.aopa.org/airports/" + airport.Code + "\" target=\"_blank\">Get Airport Info for " + airport.Code + "</a><br />";
                sz += "<a href=\"http://www.aopa.org/wx/#a=" + airport.Code + "\" target=\"_blank\">Get Current weather at " + airport.Code + "</a><br />";
            }
            else
                sz += airport.Type + "<br />";

            sz += "<a href=\"javascript:" + mapID + ".showAirport(" + airport.latitude + ", " + airport.longitude + ")\">Zoom in</a>";
            icon = this.iconForType(airport.Type);
        }

        return this.createMarker(point, name, icon, sz);
    };
    
    this.createImageMarker = function (point, i, mapID) {
        var szImg = "<a href=\"" + this.rgImages[i].hrefFull + "\" target=\"_blank\"><img src=\"" + this.rgImages[i].hrefThumb + "\"></a>";
        var szZoom = "<a href=\"javascript:" + mapID + ".showImage(" + i + ")\">Zoom in</a>";
        var szDiv = "<div>" + szZoom + "<br />" + szImg + "<br /><p>" + this.rgImages[i].comment + "</p></div>";
        var szName = "Photograph";
        var img = this.rgImages[i];
        var icon = { url: this.rgImages[i].hrefThumb, scaledSize: new google.maps.Size(img.width, img.height), anchor: new google.maps.Point(img.width / 2, img.height / 2) };
        return this.createMarker(point, szName, icon, szDiv);
    };
    
    // edit airports functionality    
    this.clickMarker = function (point, name, type, szHtml) {
        if (this.clickPositionMarker)
            this.clickPositionMarker.setMap(null);
        if (this.infoWindow)
            this.infoWindow.close();

        this.clickPositionMarker = this.createMarker(point, name, this.iconForType(type), szHtml);
    };

    // set up overlays
    this.ShowOverlays = function () {
        var i;
        var j;
        var point;
        if (this.rgAirports.length > 0) {
            if (this.fShowMarkers) {
                for (i = 0; i < this.rgAirports.length; i++) {
                    var airportList = this.rgAirports[i];
                    var points = [];
                    var airports = {};

                    for (j = 0; j < airportList.length; j++) {
                        point = new google.maps.LatLng(airportList[j].latitude, airportList[j].longitude);
                        var key = airportList[j].Code + airportList[j].Type;
                        if (!airports[key]) {
                            airports[key] = point;
                            this.oms.addMarker(this.createNavaidMarker(point, airportList[j].Code + " - " + airportList[j].Name, airportList[j], this.id));
                        }
                        points.push(point);
                    }

                    if (this.fShowRoute && !this.fAutofillPanZoom)
                        var _ = new google.maps.Polyline({ path: points, strokeColor: "#0000FF", strokeOpacity: 0.5, strokeWeight: 5, map: this.gmap, geodesic: true });
                }
            }
        }

        if (this.rgClubs && this.rgClubs.length > 0) {
            for (i = 0; i < this.rgClubs.length; i++) {
                var c = this.rgClubs[i];
                point = new google.maps.LatLng(c.latitude, c.longitude);

                var mfbmarker = new MFBMarker();
                mfbmarker.marker = new google.maps.Marker({ position: point, clickable: true, icon: '', map: this.gmap, title: c.name });
                mfbmarker.bodyHTML = '';
                mfbmarker.mfbMap = this;

                if (!this.rgMarkers)
                    this.rgMarkers = new Array();
                this.rgMarkers.push(mfbmarker.marker);

                // Instead of adding the clickhandler to google.maps.event, add it to the spiderfier.
                mfbmarker.marker.clickHandleOverride = c.handleClick;
                this.oms.addMarker(mfbmarker.marker);
            }
        }

        if (this.rgImages.length > 0 && this.fShowMarkers) {
            for (i = 0; i < this.rgImages.length; i++) {
                point = new google.maps.LatLng(this.rgImages[i].latitude, this.rgImages[i].longitude);
                this.oms.addMarker(this.createImageMarker(point, i, this.id));
            }
        }

        if (this.pathArray)
            var __ = new google.maps.Polyline({ path: this.pathArray, strokeColor: "#FF0000", strokeOpacity: 0.5, strokeWeight: 5, map: this.gmap });
    };
}

function MFBNewMapOptions(mfbMapOptions, rgPath)
{
    var mfbNewMap = new MFBMap();
    
    if (mfbMapOptions.divContainer)
        mfbNewMap.divContainer = mfbMapOptions.divContainer;
    if (mfbMapOptions.rgAirports) {
        mfbNewMap.rgAirports = [];
        // this is an array of arrays
        mfbMapOptions.rgAirports.forEach((rgap) => {
            var arr = [];
            rgap.forEach((ap) => {
                arr.push(new MFBAirportMarker(ap.latitude, ap.longitude, ap.Name, ap.Code, ap.Type, ap.fShowMarker));
            });
            mfbNewMap.rgAirports.push(arr);
        });
    }
    if (mfbMapOptions.rgImages) {
        mfbNewMap.rgImages = [];
        mfbMapOptions.rgImages.forEach((img) => {
            mfbNewMap.rgImages.push(new MFBPhotoMarker(img.hrefThumb, img.hrefFull, img.latitude, img.longitude, img.comment, img.width, img.height));
        });
    }
    if (mfbMapOptions.rgClubs) {
        mfbNewMap.rgClubs = [];
        mfbMapOptions.rgClubs.forEach((c) => {
            mfbNewMap.rgClubs.push(new MFBClubMarker(c.latitude, c.longitude, c.name, c.clubID, c.onclickhandler));
        });
    }
    // for size efficiency, patharray is an array of 2-element arrays, latitude followed by longitude - no point sending down data labels over the wire
    // it's stored in an external array with the variable name passed down; this allows it to be used by other javascript as well.
    if (rgPath) {
        mfbNewMap.pathArray = [];
        rgPath.forEach((ll, _) => {
            mfbNewMap.pathArray.push(nll(ll[0], ll[1]));
        });
    }
    if (mfbMapOptions.defaultZoom)
        mfbNewMap.defaultZoom = mfbMapOptions.defaultZoom;
    if (mfbMapOptions.defaultLat)
        mfbNewMap.defaultLat = mfbMapOptions.defaultLat;
    if (mfbMapOptions.defaultLong)
        mfbNewMap.defaultLong = mfbMapOptions.defaultLong;
    if (mfbMapOptions.defaultLat)
        mfbNewMap.defaultLat = mfbMapOptions.defaultLat;
    if (mfbMapOptions.minLat !== null)
        mfbNewMap.minLat = mfbMapOptions.minLat;
    if (mfbMapOptions.maxLat !== null)
        mfbNewMap.maxLat = mfbMapOptions.maxLat;
    if (mfbMapOptions.minLong !== null)
        mfbNewMap.minLong = mfbMapOptions.minLong;
    if (mfbMapOptions.maxLong !== null)
        mfbNewMap.maxLong = mfbMapOptions.maxLong;
    if (mfbMapOptions.mapType)
        mfbNewMap.mapType = eval(mfbMapOptions.mapType);
    if (mfbMapOptions.id)
        mfbNewMap.id = mfbMapOptions.id;

    mfbNewMap.fShowMap = mfbMapOptions.fShowMap;
    mfbNewMap.fShowMarkers = mfbMapOptions.fShowMarkers;
    mfbNewMap.fShowRoute = mfbMapOptions.fShowRoute;
    mfbNewMap.fAutoZoom = mfbMapOptions.fAutoZoom;
    mfbNewMap.fAutofillPanZoom = mfbMapOptions.fAutofillPanZoom;
    mfbNewMap.fAutofillHeliports = mfbMapOptions.fAutofillHeliports;
        
    return mfbNewMap;        
}        

function AddMap(mfbMapOptions) {
    if (mfbMapOptions.fShowMap) {
        mfbMapOptions.NewMap();
        return mfbMapOptions;
    }
}

function unload() {
    GUnload();
}
    