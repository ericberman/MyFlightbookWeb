/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/*
    This code is adapted from https://stackoverflow.com/questions/931207/is-there-a-way-to-make-an-input-field-grow-as-you-type
    Grows a single-line input field to fit its contents, up to the right edge of the window.  When the field loses focus, it shrinks back down to its original size.
    To use: call initGrowInputs(selector) with a jQuery selector for the input fields you want to grow.  The input fields should be contained in a positioned container (e.g. position: relative) so that they can be brought above their siblings when focused.
*/
function initGrowInputs(selector) {
    const $ruler = $('<span></span>').css({
        position: 'absolute',
        visibility: 'hidden',
        whiteSpace: 'pre',
        top: '-9999px',
        left: '-9999px'
    }).appendTo('body');

    function measureTextWidth($input) {
        $ruler.css('font', $input.css('font'));
        $ruler.text($input.val() || $input.attr('placeholder') || '');
        return $ruler.width();
    }

    function growToFit($input) {
        let restWidth = $input.data('rest-width');
        if (!restWidth) {
            restWidth = $input.outerWidth();
            $input.data('rest-width', restWidth);
        }

        const textWidth = measureTextWidth($input);
        const padding = 24;
        const desired = Math.max(restWidth, textWidth + padding);

        const left = $input.offset().left;
        const maxAllowed = $(window).width() - left - 16;

        $input.css('width', Math.min(desired, maxAllowed) + 'px');
    }

    function shrinkBack($input) {
        const restWidth = $input.data('rest-width');
        if (restWidth) {
            $input.css('width', restWidth + 'px');
        }
    }

    $(selector)
        .css({
            'max-width': '100%',
            'box-sizing': 'border-box',
            'transition': 'width 0.15s ease'
        })
        .on('focus', function () {
            const $input = $(this);
            // bring this field's positioned container above its siblings
            $input.closest('.grow-input-container').css('z-index', 1000);
            growToFit($input);
        })
        .on('input', function () {
            growToFit($(this));
        })
        .on('blur', function () {
            const $input = $(this);
            shrinkBack($input);
            // reset z-index once it's shrunk back down
            $input.closest('.grow-input-container').css('z-index', '');
        });
}