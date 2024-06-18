/******************************************************
 * 
 * Copyright (c) 2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
  Creates an accordion proxy - external "proxy" buttons to operate an accordion-like interface.

  Two parameters:
   Container - the ID of the div that contains the buttons.
   Options - contains set up options, including:
   * (optional) openCSSClass - the CSS class for when the button is in an active state (the corresponding pane is open)
   * (optional) closeCSSClass - the CSS class for when the button is in an inactive state (the corresponding pane is closed)
   * (optional) enhancedCSSClass - the CSS class for emphasizing one button
   * (optional) contentCSSClass - the CSS class for the target div that contains the pane to be opened/closed
   * (optional) defaultPane - the ID of the button (div) that should be expanded by default
   * (required) proxies: An array of "proxies", where each proxy consists of:
      * (Required): idButton - the id of the div inside the container that represents the button
      * (Required): idTarget - the id of the div (outside the container) that is operated by the button
      * onclick: if present, a function that is called (no arguments) when the button is *opened*.  (Not called on collapse)
      * isEnhanced: if true, the button gets a shadown and extra emphasis
*/
function accordionProxy(container, options) {
    this.openCSSClass = options.openCSSClass || "accordionMenuControllerSelected";
    this.closeCSSClass = options.closeCSSClass || "accordionMenuControllerUnselected";
    this.enhancedCSSClass = options.enhancedCSSClass || "accordionEnhanced";
    this.contentCSSClass = options.contentCSSClass || "accordionMenuContent";

    container.addClass("accordionMenuContainer");
    container.addClass("noprint");

    this.closeAll = function(outerThis) {
        options.proxies.forEach((proxy) => {
            var proxyButton = $("#" + proxy.idButton);
            proxyButton.removeClass(outerThis.openCSSClass);
            proxyButton.addClass(outerThis.closeCSSClass);
            $("#" + proxy.idTarget).hide("slide", "up");
        });
    }

    this.toggleProxy = function (p, outerThis) {
        options.proxies.forEach((proxy) => {
            var proxyButton = $("#" + proxy.idButton);
            var target = $("#" + proxy.idTarget);
            var isHidden = target.is(":hidden");
            if (p.idButton == proxy.idButton && isHidden) {
                proxyButton.removeClass(outerThis.closeCSSClass);
                proxyButton.addClass(outerThis.openCSSClass);
                target.slideDown();
                if (proxy.onclick)
                    proxy.onclick();
            } else {
                proxyButton.removeClass(outerThis.openCSSClass);
                proxyButton.addClass(outerThis.closeCSSClass);
                if (!isHidden)
                    target.slideUp();
            }
        });
    }

    this.setEnhanced = function (id, enhanced) {
        if (enhanced)
            $("#" + id).addClass(this.enhancedCSSClass);
        else
            $("#" + id).removeClass(this.enhancedCSSClass);
    }

    options.proxies.forEach((proxy) => {
        var proxyButton = $("#" + proxy.idButton);
        proxyButton.addClass(this.closeCSSClass);   // start out closed

        var thisRef = this;
        proxyButton.click(function (e) {
            thisRef.toggleProxy(proxy, thisRef);
        });
        $("#" + proxy.idTarget).addClass(this.contentCSSClass);
        if (proxy.isEnhanced || false)
            proxyButton.addClass(this.enhancedCSSClass);
        else
            proxyButton.removeClass(this.enhancedCSSClass);

        if (options.defaultPane == proxy.idButton)
            this.toggleProxy(proxy, this);
    });

    container.show();
}