<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="VisitedMap.aspx.cs" Inherits="MyFlightbook.Mapping.VisitedMap" Culture="auto" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title><% =Resources.Airports.visitedAirportTitle %></title>
</head>
<body>
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <form id="form1" runat="server">
    <link rel="stylesheet" type="text/css" href='<% = "~/public/tmap/stylesheets/demo.css".ToAbsoluteURL(Request) %>'>
      <div id="info" class="box hidden time"></div>
        <div id="timeline" class="box" style="height: 80vh;"></div>
        <div id="stat" class="box" style="left: 1em;"></div>
        <script>
            var data = <% =DataToMap %>;
            var marker = [];

            function addMarker(map, latitude, longitude, cityid) {
                var pos = projectAbsolute(latitude, longitude, 1000, 1, -26, 0); //-25, 2.7);
                var markerPath = "<% ="~/public/tmap/airport.svg".ToAbsoluteURL(Request) %> ";
                map.shape('<image href=' + markerPath + ' class="citymarker" height="3" width="4" style="transform:scale(1); transform-box: fill-box; transform-origin: 50% 50%" x="' + pos.x + '" y="' + pos.y + '" id="cm_' + cityid + '"/>');
            }

            function getCountry(iso) {
                for (let c of data.countries) {
                    if (iso3to2(c.id) == iso) {
                        return c;
                    }
                }
            }

            function forEachData(country_cb, state_cb, city_cb) {
                for (let country of data.countries) {
                    if (country_cb) country_cb(country);
                    var states = ("states" in country ? country.states : [{ "id": "", "cities": country.cities }]);
                    for (let state of states) {
                        if (state.id != "" && state_cb) state_cb(state, country);
                        if ("cities" in state) {
                            for (let city of state.cities) {
                                if (city_cb) city_cb(city, state, country);
                            }
                        }
                    }
                }
            }

            function forEachCity(cb) {
                forEachData(null, null, cb);
            }

            function forEachState(cb) {
                forEachData(null, cb, null);
            }

            function forEachCountry(cb) {
                forEachData(cb, null, null);
            }

            function zoomMap(scale) {
                //                 console.log("zoom " + scale);
                for (let m of marker) {
                    m.style.height = 3.0; // / scale;
                    m.style.width = 4.0;// / scale;
                    m.style.transform = "scale(" + (4.0 / scale) + ")";
                    m.style.transformBox = "fill-box";
                    m.style.transformOrigin = "50% 50%";
                }
            }

            function toggleMarker() {
                for (let m of marker) {
                    $(m).toggle();
                }
            }

            var baseColor = '#B9B9B9';
            var highColor = '#008000';
            var stateColor = '#004000';

            var countries = [];
            var count_city = 0;
            forEachCountry(function (country) {
                if (!(country.id in countries)) countries.push(country.id);
            });
            forEachCity(function (city) {
                city.id = count_city;
                count_city++;
            });

            var years = {};
            forEachCity(function (city, state, country) {
                for (let desc of city.description.split("\n")) {
                    var info = desc.split(", ");
                    if (info.length >= 2) {
                        var year = parseInt(info[info.length - 1]);
                        if (!(year in years)) {
                            years[year] = {};
                        }
                        if (!(country.id in years[year])) {
                            years[year][country.id] = [];
                        }
                        if (!(city in years[year][country.id])) {
                            years[year][country.id].push(city);
                        }
                    }
                }
            });
            var timeData = [];
            var accumulated = [];
            document.getElementById("timeline").innerHTML = "<div><input type='checkbox' checked='checked' id='citymarker_switch' name='citymarker_switch' style='width: 20%;' onchange='toggleMarker();'><label for='citymarker_switch'><% =Resources.Airports.airportCode %></label></div><br/>";
            for (var y in years) {
                var clist = [];
                for (var c in years[y]) {
                    if (!(c in accumulated)) accumulated.push(c);
                    if (!(c in clist)) {
                        for (let cty of years[y][c]) {
                            cty.country = c;
                        }
                        clist.push(years[y][c]);
                    }
                }
                var obj = {};
                obj[y] = {};
                for (let c of countries) {
                    obj[y][iso3to2(c)] = baseColor;
                }
                for (let c of accumulated) {
                    obj[y][iso3to2(c)] = highColor;
                }
                timeData.push(obj);
                var html = document.getElementById("timeline").innerHTML;
                html += "<br><b onclick=\"$('#yit_" + y + "').toggle();\" style='cursor: default;'><i class='flaticon-play' style='zoom: 0.7;'></i> " + y + "</b><br><div id='yit_" + y + "' style='display:none; padding-top: 0.4em;'>";
                var flagRoot = "<% ="~/public/tmap".ToAbsoluteURL(Request) %>";
                for (let city of [...new Set(clist.flat())]) {
                    html += "&nbsp;<a href='#' onclick='cityInfo(" + city.id + ");'><img src='" + flagRoot + "/flags/4x3/" + iso3to2(city.country).toLowerCase() + ".svg' width='14'> " + city.name + "</a><br>";
                }
                html += "</div>";
                document.getElementById("timeline").innerHTML = html;
            }



            // Global variables
            var myWorldMap;

            // Load SVG World Map
            loadSVGWorldMap();
            async function loadSVGWorldMap() {
                // Custom options
                var options = {
                    showLabels: false, // Hide country labels
                    showInfoBox: true, // Show info box
                    timeControls: true, // Time data to activate time antimation controls
                    timePause: true, // Set pause to false for autostart
                    timeLoop: false, // Loop time animation
                    libPath : '<% = "~/public/tmap/src/".ToAbsoluteURL(Request) %>',
                    bigMap: true
                };
                // Startup SVG World Map
                myWorldMap = await svgWorldMap(options, false, timeData);
                // SvgPanZoom 
                svgPanZoom(myWorldMap.worldMap, { minZoom: 1, dblClickZoomEnabled: false, onZoom: zoomMap, maxZoom: 20 });

                var count_country = 0, count_state = 0;
                forEachCountry(function (country) {
                    var c = iso3to2(country.id);
                    var cobj = {};
                    cobj[c] = highColor;
                    myWorldMap.update(cobj);
                    count_country++;
                });

                forEachState(function (state, country) {
                    var sobj = {};
                    sobj[iso3to2(country.id) + "-" + state.id] = stateColor;
                    myWorldMap.update(sobj);
                    count_state++;
                });


                forEachCity(function (city) {
                    addMarker(myWorldMap, city.coordinates[0], city.coordinates[1], city.id);
                });

                console.log("<% =Resources.Airports.viewCountryAdminMapTopLevel %>" + count_country + ", <% =Resources.Airports.viewCountryAdminMapAdmin1 %> " + count_state + ", <% =Resources.Airports.viewCountryAdminMapAirports %> " + count_city);
                document.getElementById("stat").innerHTML = "<a href='#' onclick='countryList();'><% =Resources.Airports.viewCountryAdminMapTopLevel %> </a><b>" + count_country + "</b><br /><a href='#' onclick='stateList();'><% =Resources.Airports.viewCountryAdminMapAdmin1 %> </a><b>" + count_state + "</b> <br /> <% =Resources.Airports.viewCountryAdminMapAirports %> <b>" + count_city + "</b>";
                marker = document.getElementById("svg-world-map").contentDocument.getElementsByClassName("citymarker");
                for (let m of marker) {
                    m.addEventListener("click", function () {
                        cityInfo(parseInt(this.id.split("_")[1]));
                    });
                }

                // Fadein with opacity 
                document.getElementById('svg-world-map-container').style.opacity = 1;
            }

            // Custom callback function for map click, defined in 'options.mapClick'
            function mapClick(country) {
                var nation = country.country; // Get parent nation
                if (nation != undefined && country.id != 'Ocean') {
                    var out = '<a class="hide" onclick="document.getElementById(\'info\').classList.add(\'hidden\')">×</a>';
                    out += 'Country: ' + nation.name;
                    out += '<br>';
                    var c = getCountry(nation.id);
                    if (c) {
                        var states = ("states" in c ? c.states : [{ "id": "", "cities": c.cities }]);
                        for (let state of states) {
                            if ("cities" in state) {
                                for (let city of state.cities) {
                                    out += "<br/><b>" + city.name + "</b><br>";
                                    out += "&nbsp;" + city.description.replaceAll("\n", "<br>&nbsp;") + "<br/>";
                                }
                            }
                        }
                    }
                    document.getElementById("info").innerHTML = out;
                    document.getElementById("info").classList.remove("hidden");
                } else {
                    document.getElementById("info").classList.add("hidden");
                }
            }

            function cityInfo(cityid) {
                forEachCity(function (city) {
                    if (city.id == cityid) {
                        var out = '<a class="hide" onclick="document.getElementById(\'info\').classList.add(\'hidden\')">×</a>';
                        out += "<b onclick='$(\"#ci_" + city.id + "\").toggle();'>" + city.name + "</b><br>";
                        out += '<div id="ci_' + city.id + '"><br>&nbsp;';
                        out += city.description.replaceAll("\n", "<br>&nbsp;") + "</div>";
                        document.getElementById("info").innerHTML = out;
                        document.getElementById("info").classList.remove("hidden");
                    }
                });
            }

            function countryList() {
                var list = [];
                forEachCountry(function (country) {
                    list.push({ iso: iso3to2(country.id).toLowerCase(), name: myWorldMap.countryData[iso3to2(country.id)].name });
                });
                list.sort(function (a, b) {
                    var nameA = a.name.toUpperCase();
                    var nameB = b.name.toUpperCase();
                    if (nameA < nameB) {
                        return -1;
                    }
                    if (nameA > nameB) {
                        return 1;
                    }
                    return 0;
                });
                var html = "";
                var flagRoot = "<% = "~/public/tmap/flags/4x3/".ToAbsoluteURL(Request) %>";
                for (let l of list) {
                    html += "<img src='" + flagRoot  + l.iso + ".svg' width='14'> " + l.name + "<br>";
                }
                document.getElementById("info").innerHTML = html;
                document.getElementById("info").classList.remove("hidden");
            }

            function stateList() {
                var list = "";
                forEachState(function (state) {
                    list += state.name + "<br>";
                });
                document.getElementById("info").innerHTML = list;
                document.getElementById("info").classList.remove("hidden");
            }
        </script>
    </form>
</body>
</html>