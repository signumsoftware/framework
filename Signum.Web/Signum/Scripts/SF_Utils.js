var SF;
(function (SF) {
    SF.Urls;
    SF.Locale;

    SF.debug = true;

    function log(s) {
        if (SF.debug) {
            if (typeof console != "undefined" && typeof console.debug != "undefined")
                console.log(s);
        }
    }
    SF.log = log;

    setupAjaxPrefilters();

    function setupAjaxPrefilters() {
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
                    if (typeof (lang) != "undefined") {
                        Notify.info(lang.signum.loading);
                    }
                }
            }
            if (options.sfCheckRedirection) {
                var originalSuccess = options.success;

                options.success = function (result) {
                    pendingRequests--;
                    if (pendingRequests <= 0) {
                        pendingRequests = 0;
                        Notify.clear();
                    }
                    if (typeof result === "string") {
                        result = result ? result.trim() : "";
                    }
                    var url = checkRedirection(result);
                    if (!SF.isEmpty(url)) {
                        window.location.href = url;
                    } else {
                        if (originalSuccess != null) {
                            originalSuccess(result);
                        }
                    }
                };
            }
        });

        $(document).ajaxError(function (event, XMLHttpRequest, ajaxOptions, thrownError) {
            if (XMLHttpRequest.status !== 0) {
                $("body").trigger("sf-ajax-error", [XMLHttpRequest, ajaxOptions, thrownError]);
                pendingRequests = 0;
            }
        });
    }

    function stopPropagation(event) {
        if (event.stopPropagation)
            event.stopPropagation();
else
            event.cancelBubble = true;
    }
    SF.stopPropagation = stopPropagation;

    function isFalse(value) {
        return value == false || value == "false" || value == "False";
    }
    SF.isFalse = isFalse;
    ;

    function isTrue(value) {
        return value == true || value == "true" || value == "True";
    }
    SF.isTrue = isTrue;
    ;
    0;

    function isEmpty(value) {
        return (value == undefined || value == null || value === "" || value.toString() == "");
    }
    SF.isEmpty = isEmpty;
    ;

    (function (Notify) {
        var $messageArea;
        var timer;
        var css;

        function error(message, timeout) {
            info(message, timeout, 'sf-error');
        }
        Notify.error = error;
        ;

        function info(message, timeout, cssClass) {
            clear();
            css = (cssClass != undefined ? cssClass : "sf-info");
            $messageArea = $("#sfMessageArea");
            if ($messageArea.length == 0) {
                $messageArea = $("<div id=\"sfMessageArea\"><div id=\"sfMessageAreaTextContainer\"><span></span></div></div>").hide().prependTo($("body"));
            }

            $messageArea.find("span").html(message);
            $messageArea.children().first().addClass(css);
            $messageArea.css({
                marginLeft: -$messageArea.outerWidth() / 2
            }).show();

            if (timeout != undefined) {
                timer = setTimeout(clear, timeout);
            }
        }
        Notify.info = info;

        function clear() {
            if ($messageArea) {
                $messageArea.hide().children().first().removeClass(css);
                if (timer != null) {
                    clearTimeout(timer);
                    timer = null;
                }
            }
        }
        Notify.clear = clear;
    })(SF.Notify || (SF.Notify = {}));
    var Notify = SF.Notify;

    (function (InputValidator) {
        function isNumber(e) {
            var c = e.keyCode;
            return ((c >= 48 && c <= 57) || (c >= 96 && c <= 105) || (c == 8) || (c == 9) || (c == 12) || (c == 27) || (c == 37) || (c == 39) || (c == 46) || (c == 36) || (c == 35) || (c == 109) || (c == 189) || (e.ctrlKey && c == 86) || (e.ctrlKey && c == 67));
        }
        InputValidator.isNumber = isNumber;

        function isDecimal(e) {
            var c = e.keyCode;
            return (this.isNumber(e) || (c == 110) || (c == 190) || (c == 188));
        }
        InputValidator.isDecimal = isDecimal;
    })(SF.InputValidator || (SF.InputValidator = {}));
    var InputValidator = SF.InputValidator;

    (function (Cookies) {
        function read(name) {
            var nameEQ = name + "=";
            var ca = document.cookie.split(';');
            for (var i = 0; i < ca.length; i++) {
                var c = ca[i];
                while (c.charAt(0) == ' ')
                    c = c.substring(1, c.length);
                if (c.indexOf(nameEQ) == 0)
                    return c.substring(nameEQ.length, c.length);
            }
            return null;
        }
        Cookies.read = read;

        function create(name, value, days, domain) {
            var expires = null, path = "/";

            if (days) {
                var date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = date;
            }

            document.cookie = name + "=" + encodeURI(value) + ((expires) ? ";expires=" + expires.toGMTString() : "") + ((path) ? ";path=" + path : "") + ((domain) ? ";domain=" + domain : "");
        }
        Cookies.create = create;
    })(SF.Cookies || (SF.Cookies = {}));
    var Cookies = SF.Cookies;

    (function (LocalStorage) {
        var isSupported = typeof (localStorage) != 'undefined';

        function getItem(key) {
            if (isSupported) {
                try  {
                    return localStorage.getItem(key);
                } catch (e) {
                }
            }
            return Cookies.read(key);
        }
        LocalStorage.getItem = getItem;
        ;

        function setItem(key, value, days) {
            if (isSupported) {
                try  {
                    localStorage.setItem(key, value);
                    return true;
                } catch (e) {
                }
            } else
                Cookies.create(key, value, days ? days : 30);
        }
        LocalStorage.setItem = setItem;
        ;

        return {
            getItem: getItem,
            setItem: setItem
        };
    })(SF.LocalStorage || (SF.LocalStorage = {}));
    var LocalStorage = SF.LocalStorage;

    function triggerNewContent($source) {
        $source.trigger("sf-new-content");
    }
    SF.triggerNewContent = triggerNewContent;
})(SF || (SF = {}));

var SF;
(function (SF) {
    (function (NewContentProcessor) {
        function defaultButtons($newContent) {
            $newContent.find(".sf-entity-button, .sf-query-button, .sf-line-button, .sf-chooser-button, .sf-button").each(function (i, val) {
                var $txt = $(val);
                if (!$txt.hasClass("ui-button") && !($txt.closest(".sf-menu-button").length > 0)) {
                    var data = $txt.data();
                    $txt.button({
                        text: (!("text" in data) || SF.isTrue(data.text)),
                        icons: { primary: data.icon, secondary: data.iconSecondary },
                        disabled: $txt.hasClass("sf-disabled")
                    });
                }
            });
        }
        NewContentProcessor.defaultButtons = defaultButtons;

        function defaultDatepicker($newContent) {
            $newContent.find(".sf-datepicker").each(function (i, val) {
                var $txt = $(val);
                $txt.datepicker(jQuery.extend({}, SF.Locale.defaultDatepickerOptions, { dateFormat: $txt.attr("data-format") }));
            });
        }
        NewContentProcessor.defaultDatepicker = defaultDatepicker;

        function defaultDropdown($newContent) {
            $newContent.find(".sf-dropdown .sf-menu-button").addClass("ui-autocomplete ui-menu ui-widget ui-widget-content ui-corner-all").find("li").addClass("ui-menu-item").find("a").addClass("ui-corner-all");
        }
        NewContentProcessor.defaultDropdown = defaultDropdown;

        function defaultPlaceholder($newContent) {
            $newContent.find('input[placeholder], textarea[placeholder]').placeholder();
        }
        NewContentProcessor.defaultPlaceholder = defaultPlaceholder;

        function defaultTabs($newContent) {
            var $tabContainers = $newContent.find(".sf-tabs:not(.ui-tabs)").prepend($("<ul></ul>"));

            $tabContainers.tabs();
            $tabContainers.each(function () {
                var $tabContainer = $(this);

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
                    } else {
                        $("<li><a href='#" + id + "'>" + legend + "</a></li>").appendTo($($tabContainer.find(".ui-tabs-nav").first()));
                        $legend.remove();
                    }
                });

                $tabContainer.tabs("refresh");
                $tabContainer.tabs("option", "active", 0);
            });
        }
        NewContentProcessor.defaultTabs = defaultTabs;

        function defaultSlider($newContent) {
            $newContent.find(".sf-search-results-container").each(function (i, val) {
                new SF.slider(jQuery(val));
            });
        }
        NewContentProcessor.defaultSlider = defaultSlider;

        function defaultModifiedChecker($newContent) {
            $newContent.find(":input").on("change", function () {
                var $mainControl = $(this).closest(".sf-main-control");
                if ($mainControl.length > 0) {
                    $mainControl.addClass("sf-changed");
                }
            });
        }
        NewContentProcessor.defaultModifiedChecker = defaultModifiedChecker;
    })(SF.NewContentProcessor || (SF.NewContentProcessor = {}));
    var NewContentProcessor = SF.NewContentProcessor;
})(SF || (SF = {}));

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

            return $(this).focus(function () {
                if ($(this).hasClass('sf-placeholder')) {
                    $.fn.placeholder.off(this);
                }
            }).blur(function () {
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
        if ($el.val() == '')
            $el.val($el.attr('placeholder')).addClass('sf-placeholder');
    };

    $.fn.placeholder.off = function (el) {
        $(el).val('').removeClass('sf-placeholder');
    };
})(jQuery);

String.prototype.hasText = function () {
    return (this == null || this == undefined || this == '') ? false : true;
};

String.prototype.startsWith = function (str) {
    return (this.indexOf(str) === 0);
};

String.prototype.format = function () {
    var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    var args = arguments;

    var getValue = function (key) {
        if (args == null || typeof args === 'undefined')
            return null;

        var value = args[key];
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

String.prototype.replaceAll = function (from, to) {
    return this.split(from).join(to);
};

if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/, '');
    };
}
