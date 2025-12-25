/******************************************************
 *
 * Copyright (c) 2020-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/* this code is adapted from https://www.w3schools.com/howto/howto_js_autocomplete.asp */
function replaceRangeInInput(input, start, end, replacement) {
    const before = input.value.slice(0, start);
    const after = input.value.slice(end);

    const newValue = before + replacement + after;
    const newCursorPos = before.length + replacement.length;

    input.value = newValue;

    // Critical for <input>: restore cursor position *after* value update
    input.setSelectionRange(newCursorPos, newCursorPos);
}

function autoInsert(inp, uri, triggerChar) {
    /*the autocomplete function takes two arguments,
    the text field element and an array of possible autocompleted values:*/
    var currentFocus;
    /*execute a function when someone writes in the text field:*/
    inp.addEventListener("input", function (e) {
        var a, b, i, val = this.value;
        /*close any already open lists of autocompleted values*/
        closeAllLists();
        if (!val) { return false; }
        currentFocus = -1;

        // Get the selection start/end and see if we are completing something that begins with "["
        if (this.selectionStart !== this.selectionEnd)
            return;

        // Back up to find the start of what we're typing.
        var sz = this.value.substring(0, this.selectionStart);
        var prefix = "";

        for (ich = sz.length; ich >= 0; ich--) {
            if (sz.charAt(ich) === triggerChar) {
                prefix = sz.substring(ich);
            } else if (sz.charAt(ich) === " ")
                break;
        }

        /// Not starting an autocomplete, if at least 2 characters typed, including the opening bracket.
        if (prefix.length < 2)
            return;

        /*create a DIV element that will contain the items (values):*/
        a = document.createElement("ul");
        a.setAttribute("id", this.id + "autoInsertList");
        a.setAttribute("class", "autoInsertItems");
        a.style.display = "none";   // avoid flashing if no matches
        a.style.width = "120%";
        /*append the DIV element as a child of the autocomplete container:*/
        this.parentNode.appendChild(a);

        // Remember the area to replace.
        var endPos = this.selectionStart;
        var startPos = this.selectionStart - prefix.length;

        var params = new Object();
        params.prefixText = prefix;
        params.count = 20;
        var d = JSON.stringify(params);

        $.ajax(
            {
                url: uri,
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr) {
                    window.alert(xhr.responseText);
                },
                complete: function () { },
                success: function (arr) {
                    if (arr.length === 0)
                        closeAllLists();

                    // for each item in the array...
                    for (i = 0; i < arr.length; i++) {
                        // create a DIV element for each matching element:
                        b = document.createElement("li");
                        b.setAttribute("class", "autoInsertItem")
                        // make the matching letters bold:
                        b.innerHTML = arr[i];
                        // b.innerHTML = "<strong>" + arr[i].substr(0, prefix.length) + "</strong>";
                        // b.innerHTML += arr[i].substr(prefix.length);
                        // insert a input field that will hold the current array item's value:
                        b.innerHTML += "<input type='hidden' value='" + arr[i] + "'>";
                        // execute a function when someone clicks on the item value (DIV element):
                        b.addEventListener("click", function (e) {
                            var val = this.getElementsByTagName("input")[0].value;
                            replaceRangeInInput(inp, startPos, endPos, val);
                            closeAllLists();
                            inp.focus();
                        });
                        a.appendChild(b);
                        a.style.display = "inline-block";
                    }
                }
            });
    });

    /*execute a function presses a key on the keyboard:*/
    inp.addEventListener("keydown", function (e) {
        var x = document.getElementById(this.id + "autoInsertList");
        if (x) x = x.getElementsByTagName("li");
        if (e.keyCode === 40) {
            /*If the arrow DOWN key is pressed,
            increase the currentFocus variable:*/
            currentFocus++;
            /*and and make the current item more visible:*/
            addActive(x);
            if (x && currentFocus > -1)
                e.preventDefault();
        } else if (e.keyCode === 38) { //up
            /*If the arrow UP key is pressed,
            decrease the currentFocus variable:*/
            currentFocus--;
            if (x && currentFocus > -1)
                e.preventDefault();
            /*and and make the current item more visible:*/
            addActive(x);
        } else if (e.keyCode === 13) {
            if (currentFocus > -1 && x) {
                /*If the ENTER key is pressed, prevent the form from being submitted,*/
                e.preventDefault();
                /*and simulate a click on the "active" item:*/
                x[currentFocus].click();
            }
        } else if (e.keyCode === 27)
            closeAllLists();
    });
    function addActive(x) {
        /*a function to classify an item as "active":*/
        if (!x) return false;
        /*start by removing the "active" class on all items:*/
        removeActive(x);
        if (currentFocus >= x.length) currentFocus = 0;
        if (currentFocus < 0) currentFocus = (x.length - 1);
        /*add class "autocomplete-active":*/
        x[currentFocus].classList.add("AutoExtenderHighlight");
    }
    function removeActive(x) {
        /*a function to remove the "active" class from all autocomplete items:*/
        for (var i = 0; i < x.length; i++) {
            x[i].classList.remove("AutoExtenderHighlight");
        }
    }
    function closeAllLists(elmnt) {
        /*close all autocomplete lists in the document,
        except the one passed as an argument:*/
        var x = document.getElementsByClassName("autoInsertItems");
        for (var i = 0; i < x.length; i++) {
            if (elmnt !== x[i] && elmnt !== inp) {
                x[i].parentNode.removeChild(x[i]);
            }
        }
        currentFocus = -1;
    }
    /*execute a function when someone clicks in the document:*/
    document.addEventListener("click", function (e) {
        closeAllLists(e.target);
    });
} 