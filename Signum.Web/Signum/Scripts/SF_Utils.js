﻿"use strict";

var SF = SF || {};

if (!SF.Utils) {

SF.Utils = true;
SF.debug = true;

SF.registerModule = (function () {
    var modules = [];

    return function (module, cb) {
        if ($.inArray(module, modules)===-1) {
            modules.push(module);
            cb();
        }
    }
})();

SF.Loader = {
    loadJs: function (url, cb) {
        $.getScript(url, cb);
    },

    loadCss: function (url, cb) {
        var d = document,
            head = d.getElementsByTagName("head")[0],
            link = d.createElement("link");
        link.type = "text/css";
        link.rel = "stylesheet";
        link.href = url;
        if (cb) {
            if ($.browser.msie) {
                link.onreadystatechange = function () {
                    /loaded|complete/.test(link.readyState) && cb();
                };
            }
            else if ($.browser.opera) {
                link.onload = cb;
            }
            else
            //FF, Safari, Chrome
                (function () {
                    try {
                        link.sheet.cssRule;
                    } catch (e) {
                        setTimeout(arguments.callee, 20);
                        return;
                    };
                    cb();
                })();
        }
        head.appendChild(link);
    }
};

SF.ajax = function (jqueryAjaxOptions) {
    var options = $.extend({
        type: null,
        url: null,
        data: null,
        async: false,
        dataType: null,
        success: null,
        error: null
    }, jqueryAjaxOptions);

    function checkRedirection (ajaxResult) {
        if (SF.isEmpty(ajaxResult))
            return null;
        var json;

        if (typeof ajaxResult !== "object") {
            //suppose that if is already an object it will be a json Object            
            if (!SF.isJSON(ajaxResult))
                return null;
            json = $.parseJSON(ajaxResult);
        } else {
            json = ajaxResult;
        }

        if (json.result == null)
            return null;
        if (json.result == 'url')
            return json.url;
        return null;
    };

    return $.ajax({
        type: options.type,
        url: options.url,
        data: options.data,
        async: options.async,
        dataType: options.dataType,
        success: function (ajaxResult) {
            var url = checkRedirection(ajaxResult);
            if (!SF.isEmpty(url))
                window.location.href = url;
            else {
                if (options.success != null)
                    options.success(ajaxResult);
            }
        },
        error: options.error
    });
};

$(document).ajaxError(function (event, XMLHttpRequest, ajaxOptions, thrownError) {
    //check request status
    //request.abort() has status 0, so we don't show this "error", since we have
    //explicitly aborted the request.
    //this error is documented on http://bugs.jquery.com/ticket/7189
    if (XMLHttpRequest.status === 0) return;
    $("body").trigger("sf-ajax-error", [XMLHttpRequest, ajaxOptions, thrownError]);
});

//Based on jquery-1.4.2 parseJSON function
SF.isJSON = function (data) {
    if (typeof data !== "string" || !data)
        return null;

    // Make sure leading/trailing whitespace is removed (IE can't handle it)
    data = jQuery.trim(data);

    // Make sure the incoming data is actual JSON
    // Logic borrowed from http://json.org/json2.js
    if (/^[\],:{}\s]*$/.test(data.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g, "@")
			.replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g, "]")
			.replace(/(?:^|:|,)(?:\s*\[)+/g, ""))) {

        return true;
    }
    else
        return false;
};

SF.stopPropagation = function (event) {
    if (event.stopPropagation)
        event.stopPropagation();
    else
        event.cancelBubble = true;
}

SF.isFalse = function (value) {
    return value == false || value == "false" || value == "False";
};

SF.isTrue = function (value) {
    return value == true || value == "true" || value == "True";
};

SF.isEmpty = function (value) {
    return (value == undefined || value == null || value === "" || value.toString() == "");
};

String.prototype.hasText = function () { return (this == null || this == undefined || this == '') ? false : true; }

String.prototype.startsWith = function (str) {
    return (this.indexOf(str) === 0);
}

String.prototype.format = function (values) {
    var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    var getValue = function (key) {
        if (values == null || typeof values === 'undefined')
            return null;

        var value = values[key];
        var type = typeof value;

        return type === 'string' || type === 'number' ? value : null;
    };

    return this.replace(regex, function (match) {
        //match will look like {sample-match}
        //key will be 'sample-match';
        var key = match.substr(1, match.length - 2);

        var value = getValue(key);

        return value != null ? value : match;
    });
};

if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/, '');
    }
}

SF.log = function (s) {
    if (SF.debug) {
        if (typeof console != "undefined" && typeof console.debug != "undefined")
            console.log(s);
    }
}

/* show messages on top (info, error...) */
SF.Notify = (function () {
    var $messageArea, timer, css;

    var error = function (s, t) {
        info(s, t, 'error');
    };

    var info = function (s, t, cssClass) {
        $messageArea = $("#message-area"), css = (cssClass != undefined ? cssClass : "info");
        if ($messageArea.length == 0) {
            //create the message container
            $messageArea = $("<div id=\"message-area\"><div class=\"message-area-text-container\"><span></span></div></div>").hide().prependTo($("body"));
        }

        $messageArea.find("span").html(s); 
        $messageArea.children().first().addClass(css); 
        $messageArea.css({ marginLeft: -parseInt($messageArea.outerWidth() / 2), top: 0 }).show();

        if (t != undefined) {
            timer = setTimeout(clear, t);
        }
    };

    var clear = function () {
        if ($messageArea) {
            $messageArea.animate({ top: -30 }, "slow")
                .hide()
                .children().first().removeClass(css);
            clearTimeout(timer);
        timer = null;
        }
    }

    return { error: error, info: info, clear: clear };
})();

SF.InputValidator = {
    isNumber: function (e) {
        var c = e.keyCode;
        return (
			(c >= 48 && c <= 57) || //0-9
			(c >= 96 && c <= 105) ||  //NumPad 0-9
			(c == 8) || //BackSpace
			(c == 9) || //Tab
			(c == 12) || //Clear
			(c == 27) || //Escape
			(c == 37) || //Left
			(c == 39) || //Right
			(c == 46) || //Delete
			(c == 36) || //Home
			(c == 35) || //End
			(c == 109) || //NumPad -
			(c == 189)
		);
    },

    isDecimal: function (e) {
        var c = e.keyCode;
        return (
			this.number(e) ||
			(c == 110) ||  //NumPad Decimal
            (c == 190) || //.
			(c == 188) //,
		);
    }
};

(function ($) {
    $.fn.placeholder = function () {
        if ($.fn.placeholder.supported()) {
            return $(this);
        } else {

            $(this).parent('form').submit(function (e) {
                $('input[placeholder].placeholder, textarea[placeholder].placeholder', this).val('');
            });

            $(this).each(function () {
                $.fn.placeholder.on(this);
            });

            return $(this)

        .focus(function () {
            if ($(this).hasClass('placeholder')) {
                $.fn.placeholder.off(this);
            }
        })

        .blur(function () {
            if ($(this).val() == '') {
                $.fn.placeholder.on(this);
            }
        });
        }
    };

    // Extracted from: http://diveintohtml5.org/detect.html#input-placeholder
    $.fn.placeholder.supported = function () {
        var input = document.createElement('input');
        return !!('placeholder' in input);
    };

    $.fn.placeholder.on = function (el) {
        var $el = $(el);
        if ($el.val() == '') $el.val($el.attr('placeholder')).addClass('placeholder');
    };

    $.fn.placeholder.off = function (el) {
        $(el).val('').removeClass('placeholder');
    };
})(jQuery);

SF.Cookies = {
    read: function (name) {
        var nameEQ = name + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    },

    create: function (name, value, days, domain) {
        var expires = null,
                path = "/";

        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = date;
        }

        document.cookie = name + "=" + escape(value) +
            ((expires) ? ";expires=" + expires.toGMTString() : "") +
            ((path) ? ";path=" + path : "") +
            ((domain) ? ";domain=" + domain : "");
    }
};

SF.LocalStorage = (function () {
    var isSupported = typeof (localStorage) != 'undefined';

    var getItem = function (key) {
        if (isSupported) {
            try { return localStorage.getItem(key); }
            catch (e) { }
        }
        return SF.Cookies.read(key);
    };

    var setItem = function (key, value, days) {
        if (isSupported) {
            try { localStorage.setItem(key, value); return true; }
            catch (e) { }
        }
        SF.Cookies.create(key, value, days ? days : 30);
    };

    return { getItem: getItem, setItem: setItem };
})();

}