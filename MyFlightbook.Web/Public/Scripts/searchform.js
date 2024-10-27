/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

    /*
        function to set up a search form.  Call this on document ready.  
        
        This is HIGHLY dependent on specific element IDs, must be paired with _searchForm

        Options you MUST specify:
         - invalidDateMessage - error message for invalid dates (should be @Resources.LocalizedText.TypeInDateInvalidDate)
         - saveQueryManage - title for the canned query box (should be @Resources.FlightQuery.SaveQueryManage)

         - targetDeleteCanned - RELATIVE url for DeleteCannedQuery (should be '@Url.Action("DeleteCannedQuery", "Search")')
         - targetAddCanned - RELATIVE url for AddCannedQuery (should be @Url.Action("AddCannedQuery", "Search"))

         - onClientReset - name of function to call to perform a reset of the form.
         - onClientSearch - name of function to call when search is clicked
    */
function searchForm(options) {

    this.options = options;

    this.init = function () {
        $('#pnlSearch').keydown(function (e) {
            if (e.keyCode == 13) {
                $('#btnSearch')[0].click();
                return false;
            }
        });
        $('#txtQueryName').keydown(function (e) {
            if (e.keyCode == 13) {
                $('#btnSearchNamed')[0].click();
                return false;
            }
        });

        $("#mfbTIDateFrom").on("change", () => { this.setCustomDates(true); });
        $("#mfbTIDateTo").on("change", () => { this.setCustomDates(true); });

        $.validator.addMethod("shortDatePattern", function (value, element, param) {
            return (this.optional(element) || new RegExp(element.pattern).test(value));
        }, options.invalidDateMessage);

        validateForm($("#frmSearchForm"), { typeInDateField: { shortDatePattern: true } }, { typeInDateField: options.invalidDateMessage });
    }

    this.init();

    this.clutter = function(sender, targetID) {
        $("#" + targetID).find(".decluttered").removeClass("decluttered").addClass("alwaysVisible");
        sender.style.display = "none";
    }

    this.selectAll = function(sender, targetID) {
        $('#' + targetID).find(':checkbox').filter(":visible").prop("checked", sender.checked ? true : false);
    }

    this.clearCustomDates = function() {
        $("#mfbTIDateFrom")[0].value = $("#mfbTIDateTo")[0].value = "";
    }

    this.setCustomDates = function(val) {
        $('#rbCustom')[0].checked = val;
        if (!val) {
            this.clearCustomDates();
        }
    }

    this.reset = function() {
        $('#pnlSearch').find(':checkbox').prop("checked", false);
        $("#hdnTypeNames")[0].value = $('#txtRestrict')[0].value = $('#txtAirports')[0].value = $('#txtModelNameText')[0].value = "";
        $('#rbAllTime')[0].checked = $('#rbEngineAny')[0].checked = $('#rbInstanceAny')[0].checked = $('#rbFlightRangeAll')[0].checked = true;
        $("#flightConjunction").prop("selectedIndex", 0);
        $("#propertyConjunction").prop("selectedIndex", 1); // Any for props
        this.clearCustomDates();
        return false;
    }

    this.buildQuery = function() {
        var fq = new Object();
        fq.UserName = $('#hdnUser').val();
        fq.QueryName = $('#txtQueryName').val();
        fq.DateRange = $('input[name="DateRange"]:checked').val();
        if (fq.DateRange == "Custom") {
            fq.DateMinStr = $('#mfbTIDateFrom').val();
            fq.DateMaxStr = $('#mfbTIDateTo').val();

            if (fq.DateMinStr == "")
                fq.DateMinStr = "0001-01-01";
            if (fq.DateMaxStr == "")
                fq.DateMaxStr = "0001-01-01";
        }
        fq.GeneralText = $('#txtRestrict').val();
        const reAirports = /!?@@?[a-zA-Z0-9]+!?/g;
        fq.AirportList = $('#txtAirports').val().match(reAirports);
        fq.Distance = $('input[name="FlightRange"]:checked').val();

        fq.AircraftIDList = [];
        $("#divACList").find('input[type=checkbox]').each(function () {
            if (this.checked)
                fq.AircraftIDList.push(parseInt(this.value));
        });

        fq.IsTailwheel = $('#ckTailwheel')[0].checked;
        fq.IsHighPerformance = $('#ckHighPerf')[0].checked;
        fq.IsGlass = $('#ckGlass')[0].checked;
        fq.IsTechnicallyAdvanced = $('#ckTAA')[0].checked;
        fq.IsMotorglider = $('#ckMotorGlider')[0].checked;
        fq.IsMultiEngineHeli = $('#ckMultiEngineHeli')[0].checked;
        fq.IsComplex = $('#ckComplex')[0].checked;
        fq.IsRetract = $('#ckRetract')[0].checked;
        fq.IsConstantSpeedProp = $('#ckProp')[0].checked;
        fq.HasFlaps = $('#ckFlaps')[0].checked;
        fq.EngineType = $('input[name="EngineGroup"]:checked').val();
        fq.AircraftInstanceTypes = $('input[name="InstanceGroup"]:checked').val();

        fq.MakeIDList = [];
        $("#divMakeList").find('input[type=checkbox]').each(function () {
            if (this.checked)
                fq.MakeIDList.push(parseInt(this.value));
        });
        fq.ModelName = $("#txtModelNameText").val();


        fq.CatClasses = [];
        $("#divCCList").find('input[type=checkbox]').each(function () {
            if (this.checked) {
                // create a pseudo-catclass
                var cc = new Object();
                cc.CatClass = this.value;
                cc.IdCatClass = this.value;
                fq.CatClasses.push(cc);
            }
        });

        fq.FlightCharacteristicsConjunction = $('#flightConjunction').val();
        fq.HasLandings = $("#ckAnyLandings")[0].checked;
        fq.HasFullStopLandings = $("#ckFSLanding")[0].checked;
        fq.HasNightLandings = $("#ckNightLandings")[0].checked;
        fq.HasApproaches = $("#ckApproaches")[0].checked;
        fq.HasHolds = $("#ckHolds")[0].checked;
        fq.HasXC = $("#ckXC")[0].checked;
        fq.HasIMC = $("#ckIMC")[0].checked;
        fq.HasSimIMCTime = $("#ckSimIMC")[0].checked;
        fq.HasAnyInstrument = $("#ckAnyInstrument")[0].checked;
        fq.HasGroundSim = $("#ckGroundSim")[0].checked;
        fq.HasNight = $("#ckNight")[0].checked;
        fq.HasDual = $("#ckDual")[0].checked;
        fq.HasCFI = $("#ckCFI")[0].checked;
        fq.HasSIC = $("#ckSIC")[0].checked;
        fq.HasPIC = $("#ckPIC")[0].checked;
        fq.HasTotalTime = $("#ckTotal")[0].checked;
        fq.IsPublic = $("#ckFlightIsPublic")[0].checked;
        fq.HasTelemetry = $("#ckHasTelemetry")[0].checked;
        fq.HasImages = $("#ckHasImages")[0].checked;
        fq.IsSigned = $("#ckIsSigned")[0].checked;


        fq.PropertiesConjunction = $('#propertyConjunction').val();
        fq.PropertyTypes = [];
        $("#divPropsList").find('input[type=checkbox]').each(function () {
            if (this.checked) {
                // create a CPT.  It's a pseudo-object - proptypeid is the key piece here.
                var cpt = new Object();
                cpt.PropTypeID = parseInt(this.value);
                cpt.Title = this.title;
                cpt.SortKey = cpt.FormatString = cpt.Description = "";
                cpt.Type = "cfpInteger";
                cpt.IsFavorite = false;
                cpt.Flags = 0;
                fq.PropertyTypes.push(cpt);
            }
        });

        // Preserve any type names.
        var typeNames = $("#hdnTypeNames")[0].value;
        fq.TypeNames = JSON.parse(typeNames == "" ? "[]" : typeNames);

        return fq;
    }
    this.doClientReset = function() {
        this.reset();
        options.onClientReset(this.buildQuery());
        return false;
    }

    this.doClientSearch = function() {
        if (!$("#frmSearchForm").valid())
            return false;
        options.onClientSearch(this.buildQuery());
        return false;
    }

    this.showCanned = function() {
        if (!$("#frmSearchForm").valid())
            return false;
        showModalById('divCanned', options.saveQueryManage, 400);
        $('#txtQueryName').focus();
        return false;
    }

    this.saveAndSearch = function() {
        if (!$("#frmSearchForm").valid())
            return false;
        var queryName = $("#txtQueryName").val();
        var fq = this.buildQuery();
        this.saveQuery(fq, queryName);
        return false;
    }

    this.saveQuery = function(cq, name) {
        if (!$("#frmSearchForm").valid())
            return false;
        if (name == '' || name === undefined)   // no op if not named
            return;

        var params = new Object();
        if (cq.QueryName == '' || cq.QueryName === undefined)
            cq.QueryName = name;
        params.cq = cq;
        var d = JSON.stringify(params);
        $.ajax({
            url: options.targetAddCanned,
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) { window.alert(xhr.responseText); },
            success: function () {
                options.onClientSearch(cq);
            }
        });
    }

    this.deleteQuery = function (sender, cq, name) {
        var params = new Object();
        params.cq = cq;
        cq.QueryName = name;
        var d = JSON.stringify(params);
        $.ajax(
            {
                url: options.targetDeleteCanned,
                type: "POST", data: d, dataType: "text", contentType: "application/json",
                error: function (xhr) {
                    window.alert(xhr.responseText);
                },
                success: function () {
                    sender.parentElement.style.display = "none";
                }
            });
    }

}
