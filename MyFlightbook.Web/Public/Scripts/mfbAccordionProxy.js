/******************************************************
 *
 * Copyright (c) 2015-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

function mfbAccordionProxy(settings)
{
    this.settings = settings;

    this.proxyClicked = function (idx) {
        var acc = $find(settings.AccordionControlClientID);
        var idxSelected = acc.get_selectedIndex();
        acc.set_selectedIndex(idxSelected === idx ? idxSelected = -1 : idxSelected = idx);
        for (var i = 0; i < settings.HeaderProxyClientIDs.length; i++) {
            var pid = settings.HeaderProxyClientIDs[i];
            var fOpen = i === idxSelected;
            $get(pid).className = fOpen ? settings.OpenCSSClass : settings.CloseCSSClass;
        }

        if (typeof onAccordionPaneShown !== 'undefined')
            onAccordionPaneShown(idxSelected);
    };

    this.proxyPostbackClicked = function (idx) {
        __doPostBack(settings.HeaderProxyPostbackIDs[idx], '');
    };
}