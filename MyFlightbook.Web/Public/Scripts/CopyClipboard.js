/******************************************************
 *
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

// Credit to https://hackernoon.com/copying-text-to-clipboard-with-javascript-df4d4988697f
function copyClipboard(titleID, bodyID, useBodyValue, feedbacklabelID) {
    const el = document.createElement('textarea');  // Create a <textarea> element
    var title = document.getElementById(titleID);
    var body = document.getElementById(bodyID);
    el.value = (title === null ? "" : title.innerText + '\n') + (body === null ? "" : useBodyValue ? body.value : body.innerText); // Set its value to the string that you want copied
    el.setAttribute('readonly', '');                // Make it readonly to be tamper-proof
    el.style.position = 'absolute';
    el.style.left = '-9999px';                      // Move outside the screen to make it invisible
    document.body.appendChild(el);                  // Append the <textarea> element to the HTML document
    const selected =
        document.getSelection().rangeCount > 0        // Check if there is any content selected previously
            ? document.getSelection().getRangeAt(0)     // Store selection if found
            : false;                                    // Mark as false to know no selection existed before
    el.select();                                    // Select the <textarea> content
    document.execCommand('copy');                   // Copy - only works as a result of a user action (e.g. click events)
    document.body.removeChild(el);                  // Remove the <textarea> element
    if (selected) {                                 // If a selection existed before copying
        document.getSelection().removeAllRanges();    // Unselect everything on the HTML document
        document.getSelection().addRange(selected);   // Restore the original selection
    }
    var fdback = document.getElementById(feedbacklabelID);
    if (fdback) {
        fdback.style.display = "inline";
        setTimeout(function () { jQuery('#' + feedbacklabelID).fadeOut(); }, 2000);
    }
}