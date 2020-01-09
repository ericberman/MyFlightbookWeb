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

function containsAll(target, words) {
    for (var i = 0; i < words.length; i++)
        if (target.indexOf(words[i]) === -1)
            return false;
    return true;
}

function FilterItems(src, idDDL, idLBL, txtLbl) {
    var words = src.value.toUpperCase().split(" ");
    var ddl = document.getElementById(idDDL);
    var lbl = document.getElementById(idLBL);
    ddl.options.length = 0;

    for (var i = 0; i < ddlText.length; i++) {
        if (i === 0 || containsAll(ddlText[i].toUpperCase(), words)) {
            var opt = document.createElement("option");
            opt.text = ddlText[i];
            opt.value = ddlValue[i];
            ddl.options.add(opt);
        }
    }

    lbl.innerHTML = ddl.options.length - 1 + txtLbl;
}

function FilterProps(src, idLst, idLbl, txtLbl) {
    var words = src.value.toUpperCase().split(" ");
    var lbl = document.getElementById(idLbl);

    var props = document.getElementById(idLst).children;
    var cFound = 0;
    for (var i = 0; i < props.length; i++) {
        var prop = props[i];
        if (containsAll(prop.innerText.toUpperCase(), words)) {
            cFound++;
            prop.style["display"] = "block";
        }
        else
            prop.style["display"] =  "none";
    }

    lbl.innerHTML = cFound + txtLbl;
}