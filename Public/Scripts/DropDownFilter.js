// Javascript below adapted from http://www.aspsnippets.com/Articles/Filter-and-Search-ASP.Net-DropDownList-items-using-JavaScript.aspx
/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

var ddlText, ddlValue;

function CacheItems(ddl) {
    ddlText = new Array();
    ddlValue = new Array();
    for (var i = 0; i < ddl.options.length; i++) {
        ddlText[ddlText.length] = ddl.options[i].text;
        ddlValue[ddlValue.length] = ddl.options[i].value;
    }
}

function FilterItems(src, idDDL, idLBL, txtLbl) {
    var value = src.value.toUpperCase();
    var ddl = document.getElementById(idDDL);
    var lbl = document.getElementById(idLBL);
    ddl.options.length = 0;

    for (var i = 0; i < ddlText.length; i++) {
        if (i === 0 || ddlText[i].toUpperCase().indexOf(value) !== -1) {
            var opt = document.createElement("option");
            opt.text = ddlText[i];
            opt.value = ddlValue[i];
            ddl.options.add(opt);
        }
    }

    lbl.innerHTML = ddl.options.length - 1 + txtLbl;
}
