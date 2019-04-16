function mfbCalendar(rootPath, resourceID, clubID, displayMode, divCalContainer, newApptFunc, editApptFunc, getEditedApptFunc) {
    this.rootPath = rootPath;
    this.resourceID = resourceID;
    this.clubID = clubID;
    this.displayMode = displayMode;
    this.dpCalendarContainer = divCalContainer;
    this.newAppointment = newApptFunc;
    this.editApptFunc = editApptFunc;
    this.getApptFunc = getEditedApptFunc;
    var base = this;

    this.initCal = function () {
        var dp;
        if (base.displayMode === 'Month')
            dp = new DayPilot.Month(this.dpCalendarContainer);
        else {
            dp = new DayPilot.Calendar(this.dpCalendarContainer);
            dp.viewType = base.displayMode === 'Week' ? "Week" : "days";
        }
        dp.init();
        return dp;
    };

    this.dpCalendar = this.initCal();

    this.initNav = function (divNavContainer) {
        var nav = new DayPilot.Navigator(divNavContainer);
        nav.showMonths = 1;
        nav.skipMonths = 1;
        nav.selectMode = base.displayMode === "Week" ? "week" : "day";
        nav.init();
        nav.onTimeRangeSelected = function (args) {
            base.dpCalendar.startDate = args.day;
            base.dpCalendar.update();
            base.refreshEvents();
        };

        return nav;
    };

    this.removeEvent = function (id) {
        var row = null;

        if (typeof this.dpCalendar.events.list === "undefined")
            return;

        for (var j = this.dpCalendar.events.list.length - 1; j >= 0; j--)
            if (this.dpCalendar.events.list[j].id === id)
                row = this.dpCalendar.events.list[j];

        if (row)
            this.dpCalendar.events.remove(row);
    };

    this.addPrefix = function (szText, szOwnerName) {
        if (!szOwnerName || szOwnerName.length === 0 || szText.indexOf(szOwnerName) === 0)
            return szText;
        else
            return szOwnerName + ': ' + szText;
    };

    this.removePrefix = function (szText, szOwnerName) {
        if (!szOwnerName || szOwnerName.length === 0 || szText.indexOf(szOwnerName) === -1)
            return szText;

        var szSubject = szText.substring(szOwnerName.length).trim();
        if (szSubject.indexOf(':') === 0)
            szSubject = szSubject.substring(1).trim();
        if (szSubject.indexOf('-') === 0)
            szSubject = szSubject.substring(1).trim();
        return szSubject;
    };

    // load the initial events.
    this.refreshEvents = function () {
        var params = new Object();
        if (base.displayMode === "Month") {
            params.dtStart = this.dpCalendar.startDate;
            params.dtEnd = this.dpCalendar.startDate.addDays(43);
        }
        else {
            params.dtStart = this.dpCalendar.visibleStart();
            params.dtEnd = this.dpCalendar.visibleEnd();
        }
        params.resourceName = this.resourceID;
        params.clubID = this.clubID;
        var d = $.toJSON(params);

        $.ajax(
            {
                url: base.rootPath + "/ReadEvents",
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                async: true,
                error: function (xhr, status, error) { window.alert(xhr.responseJSON.Message); },
                complete: function (response) { },
                success: function (response) {
                    if (typeof base.dpCalendar.events.list !== "undefined") {
                        while (base.dpCalendar.events.list.length > 0)
                            base.dpCalendar.events.remove(base.dpCalendar.events.list[0]);
                    }
                    var eventRows = response.d;
                    for (var i = 0; i < eventRows.length; i++) {
                        var er = eventRows[i];
                        var e = new DayPilot.Event({
                            start: new DayPilot.Date(er.start, false),
                            end: new DayPilot.Date(er.end, false),
                            id: er.ID,
                            resource: er.ResourceID,
                            text: base.addPrefix(er.Body, er.OwnerName),
                            readonly: er.ReadOnly,
                            userdisplayname: er.OwnerName
                        });
                        if (er.ReadOnly) {
                            e.data.clickDisabled = true;
                            e.data.moveDisabled = true;
                        }
                        try {
                            base.dpCalendar.events.add(e);
                        }
                        catch (err) {
                            var s = err.message;
                        }
                    }
                }
            });
    };

    this.JSONparamsFromEvent = function (e) {
        var p = new Object({ start: e.data.start, end: e.data.end, id: e.data.id, resource: e.data.resource, text: e.data.text, userdisplayname: e.data.userdisplayname, readonly: e.data.ReadOnly, clubID: this.clubID });
        return $.toJSON(p);
    };

    this.dpCalendar.onTimeRangeSelected = function (args) {
        this.clearSelection();
        e = new DayPilot.Event({ start: args.start, end: args.end, id: DayPilot.guid(), resource:base.resourceID, text: '' });
        base.newAppointment(e, function (args, onSuccess) {
            args.preventDefault();
            var e = base.getApptFunc();
            var d = base.JSONparamsFromEvent(e);
            $.ajax(
            {
                url: base.rootPath + "/CreateEvent",
                async: true,
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function (response) { },
                success: function (response) {
                    if (response.d !== "")
                        window.alert(response.d);
                    else {
                        base.dpCalendar.events.add(e);
                        base.dpCalendar.update();
                        if (onSuccess)
                            onSuccess();
                    }
                }
            });
        });
        return false;
    };

    this.updateEvent = function (d, onSuccess, onError) {
        $.ajax(
            {
                url: base.rootPath + "/UpdateEvent",
                async: true,
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) { window.alert(xhr.responseJSON.Message); onError(); },
                complete: function (response) { },
                success: function (response) {
                    if (response.d !== "") {
                        window.alert(response.d);
                        onError();
                    }
                    else if (onSuccess)
                        onSuccess();
                }
            });
    };

    this.dpCalendar.onEventClicked = function (args) {
        args.preventDefault();
        if (args.e.data.readonly)
            return;

        var szDisplay = args.e.data.userdisplayname;
        var params = new DayPilot.Event({ id: args.e.data.id, start: args.e.data.start, end: args.e.data.end, resource: args.e.data.resource, text: base.removePrefix(args.e.data.text, args.e.data.userdisplayname), readonly: args.e.data.readonly, userdisplayname: args.e.data.userdisplayname });
        params.onSave = function (args, onSuccess) {
            args.preventDefault();
            var e = base.getApptFunc();
            var d = base.JSONparamsFromEvent(e);
            base.updateEvent(d, onSuccess, new function () { });
            // a bit of a hack, but don't require the refresh - edit the actual appointment
            base.removeEvent(e.data.id);

            // Fix up the user's display name
            e.data.text = base.addPrefix(e.data.text, szDisplay);
            base.dpCalendar.events.add(e);
            base.refreshEvents();
        };
        params.onDelete = function (args, onSuccess) {
            args.preventDefault();
            var e = base.getApptFunc();
            var d = base.JSONparamsFromEvent(e);
            $.ajax(
                {
                    url: base.rootPath + "/DeleteEvent",
                    async: true,
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) { window.alert(xhr.responseJSON.Message); },
                    complete: function (response) { },
                    success: function (response) {
                        if (response.d !== "")
                            window.alert(response.d);
                        else {
                            base.removeEvent(e.data.id);
                            base.dpCalendar.update();
                            if (onSuccess) onSuccess();
                            base.refreshEvents();
                        }
                    }
                });
        };
        base.editApptFunc(params, false);
        return false;
    };

    this.dpCalendar.onEventMove = function (args) {
        if (args.e.data.readonly) {
            args.preventDefault();
        }
    };

    this.dpCalendar.onEventMoved = this.dpCalendar.onEventResized = function (args) {
        args.preventDefault();
        var params = new DayPilot.Event({ id: args.e.data.id, start: args.newStart, end: args.newEnd, resource: args.e.data.resource, text: base.removePrefix(args.e.data.text, args.e.data.userdisplayname) });
        var d = base.JSONparamsFromEvent(params);
        base.updateEvent(d, function () { }, function () { base.refreshEvents(); });
    };
}