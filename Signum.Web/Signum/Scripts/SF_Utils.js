"use strict";

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

(function (options) {
    var pendingRequests = 0;

    $.ajaxSetup({
        type: "POST",
        sfCheckRedirection: true,
        sfNotify: true
    });

    var checkRedirection = function (ajaxResult) {
        if (SF.isEmpty(ajaxResult)) {
            return null;
        }
        if (typeof ajaxResult !== "object") {
            return null;
        }
        if (ajaxResult.result == null) {
            return null;
        }
        if (ajaxResult.result == 'url') {
            return ajaxResult.url;
        }
        return null;
    };

    $.ajaxPrefilter(function (options, originalOptions, jqXHR) {
        if (options.dataType == "script" && (typeof originalOptions.type == "undefined")) {
            options.type = "GET";
        }
        if (options.sfNotify) {
            pendingRequests++;
            if (pendingRequests == 1) {
                SF.Notify.info(lang.signum.loading);
            }
        }
        if (options.sfCheckRedirection) {
            var originalSuccess = options.success;

            options.success = function (result) {
                pendingRequests--;
                if (pendingRequests <= 0) {
                    pendingRequests = 0;
                    SF.Notify.clear();
                }
                if (typeof result === "string") {
                    result = result ? result.trim() : "";
                }
                var url = checkRedirection(result);
                if (!SF.isEmpty(url)) {
                    window.location.href = url;
                }
                else {
                    if (originalSuccess != null) {
                        originalSuccess(result);
                    }
                }
            };
        }
    });

    $(document).ajaxError(function (event, XMLHttpRequest, ajaxOptions, thrownError) {
        //check request status
        //request.abort() has status 0, so we don't show this "error", since we have
        //explicitly aborted the request.
        //this error is documented on http://bugs.jquery.com/ticket/7189
        if (XMLHttpRequest.status !== 0) {
            $("body").trigger("sf-ajax-error", [XMLHttpRequest, ajaxOptions, thrownError]);
            pendingRequests = 0;
        }
    });
})();

SF.stopPropagation = function (event) {
        if (event.stopPropagation) event.stopPropagation();
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

    String.prototype.hasText = function () {
        return (this == null || this == undefined || this == '') ? false : true;
    }

String.prototype.startsWith = function (str) {
    return (this.indexOf(str) === 0);
}

String.prototype.format = function (values) {
    var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    var getValue = function (key) {
            if (values == null || typeof values === 'undefined') return null;

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

    String.prototype.replaceAll = function (s1, s2) {
        return this.split(s1).join(s2)
    };

if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/, '');
    }
}

SF.log = function (s) {
    if (SF.debug) {
            if (typeof console != "undefined" && typeof console.debug != "undefined") console.log(s);
    }
}

/* show messages on top (info, error...) */
SF.Notify = (function () {
    var $messageArea, timer, css;

    var error = function (s, t) {
        info(s, t, 'sf-error');
    };

    var info = function (s, t, cssClass) {
        SF.Notify.clear();
        css = (cssClass != undefined ? cssClass : "sf-info");
        $messageArea = $("#sfMessageArea");
        if ($messageArea.length == 0) {
            $messageArea = $("<div id=\"sfMessageArea\"><div id=\"sfMessageAreaTextContainer\"><span></span></div></div>").hide().prependTo($("body"));
        }

        $messageArea.find("span").html(s);
        $messageArea.children().first().addClass(css);
        $messageArea.css({
            marginLeft: -parseInt($messageArea.outerWidth() / 2)
        }).show();

        if (t != undefined) {
            timer = setTimeout(clear, t);
        }
    };

    var clear = function () {
        if ($messageArea) {
            $messageArea.hide().children().first().removeClass(css);
            if (timer != null) {
                clearTimeout(timer);
                timer = null;
            }
        }
    }

    return {
        error: error,
        info: info,
        clear: clear
    };
})();

SF.InputValidator = {
    isNumber: function (e) {
        var c = e.keyCode;
            return ((c >= 48 && c <= 57) /*0-9*/ || 
			(c >= 96 && c <= 105) /*NumPad 0-9*/ ||
			(c == 8) /*BackSpace*/ ||
			(c == 9) /*Tab*/ || 
			(c == 12) /*Clear*/ || 
			(c == 27) /*Escape*/ || 
			(c == 37) /*Left*/ || 
			(c == 39) /*Right*/ || 
			(c == 46) /*Delete*/ || 
			(c == 36) /*Home*/ || 
			(c == 35) /*End*/ || 
			(c == 109) /*NumPad -*/ ||
            (c == 189) /*-*/ ||
            (e.ctrlKey && c == 86) /*Ctrl + v*/ ||
            (e.ctrlKey && c == 67) /*Ctrl + v*/
        );
    },

    isDecimal: function (e) {
        var c = e.keyCode;
        return (
            this.isNumber(e) || 
            (c == 110) /*NumPad Decimal*/ ||
            (c == 190) /*.*/ ||
			(c == 188) /*,*/
		);
    }
};

(function ($) {
    $.fn.placeholder = function () {
        if ($.fn.placeholder.supported()) {
            return $(this);
        } else {

            $(this).parent('form').submit(function (e) {
                $('input[placeholder].sf-placeholder, textarea[placeholder].sf-placeholder', this).val('');
            });

            $(this).each(function () {
                $.fn.placeholder.on(this);
            });

            return $(this)

        .focus(function () {
            if ($(this).hasClass('sf-placeholder')) {
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
        if ($el.val() == '') $el.val($el.attr('placeholder')).addClass('sf-placeholder');
    };

    $.fn.placeholder.off = function (el) {
        $(el).val('').removeClass('sf-placeholder');
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

            document.cookie = name + "=" + escape(value) + ((expires) ? ";expires=" + expires.toGMTString() : "") + ((path) ? ";path=" + path : "") + ((domain) ? ";domain=" + domain : "");
    }
};

SF.LocalStorage = (function () {
    var isSupported = typeof (localStorage) != 'undefined';

    var getItem = function (key) {
        if (isSupported) {
                try {
                    return localStorage.getItem(key);
                } catch (e) { }
        }
        return SF.Cookies.read(key);
    };

    var setItem = function (key, value, days) {
        if (isSupported) {
                try {
                    localStorage.setItem(key, value);
                    return true;
                } catch (e) { }
        }
        SF.Cookies.create(key, value, days ? days : 30);
    };

        return {
            getItem: getItem,
            setItem: setItem
        };
    })();

    SF.triggerNewContent = function ($source) {
        $source.trigger("sf-new-content");
    }
}

SF.NewContentProcessor = {
    defaultButtons: function ($newContent) {
        $newContent.find(".sf-entity-button, .sf-query-button, .sf-line-button, .sf-chooser-button, .sf-button").each(function (i, val) {
            var $txt = $(val);
            if (!$txt.hasClass("ui-button") && !$txt.closest(".sf-menu-button").length > 0) {
                var data = $txt.data();
                $txt.button({
                    text: (!("text" in data) || SF.isTrue(data.text)),
                    icons: { primary: data.icon, secondary: data.iconSecondary },
                    disabled: $txt.hasClass("sf-disabled")
                });
            }
        });
    },

    defaultDatepicker: function ($newContent) {
        $newContent.find(".sf-datepicker").each(function (i, val) {
            var $txt = $(val);
            $txt.datepicker(jQuery.extend({}, SF.Locale.defaultDatepickerOptions, { dateFormat: $txt.attr("data-format") }));
        });
    },

    defaultDropdown: function ($newContent) {
        $newContent.find(".sf-dropdown .sf-menu-button")
            .addClass("ui-autocomplete ui-menu ui-widget ui-widget-content ui-corner-all")
            .find("li")
            .addClass("ui-menu-item")
            .find("a")
            .addClass("ui-corner-all");
    },

    defaultAutocomplete: function ($newContent) {
        $newContent.find(".sf-entity-autocomplete").each(function (i, val) {
            var $txt = $(val);
            var data = $txt.data();
            $.SF.entityLine.prototype.entityAutocomplete($txt, { delay: 200, types: data.types, url: data.url || SF.Urls.autocomplete, count: 5 });
        });
    },

    defaultPlaceholder: function ($newContent) {
        $newContent.find('input[placeholder], textarea[placeholder]').placeholder();
    },

    defaultTabs: function ($newContent) {
        var $tabContainer = $newContent.find(".sf-tabs:not(.ui-tabs)").prepend($("<ul></ul>"));

        $tabContainer.tabs();

        var $tabs = $tabContainer.children("fieldset");
        $tabs.each(function () {
            var $this = $(this);
            var $legend = $this.children("legend");
            if ($legend.length == 0) {
                $this.prepend("<strong>¡¡¡NO LEGEND SPECIFIED!!!</strong>");
                throw "No legend specified for tab";
            }
            var legend = $legend.html();

            var id = $this.attr("id");
            if (SF.isEmpty(id)) {
                $legend.html(legend + " <strong>¡¡¡NO TAB ID SET!!!</strong>");
                throw "No id set for tab with legend: " + legend;
            }
            else {
                $tabContainer.tabs("add", "#" + id, legend);
                $legend.remove();
            }
        });
    },

    defaultSlider: function ($newContent) {
        $newContent.find(".sf-search-results-container").each(function (i, val) {
            new SF.slider(jQuery(val));
        });
    },

    defaultModifiedChecker: function ($newContent) { 
        $newContent.find(":input").on("change", function() {
            var $mainControl = $(this).closest(".sf-main-control"); 
            if ($mainControl.length > 0) {
                $mainControl.addClass("sf-changed");
            }
        });
    }
};