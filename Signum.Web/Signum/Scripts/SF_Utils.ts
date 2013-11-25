/// <reference path="references.ts"/>


declare var lang: any;

module SF {
    export var Urls: any;
    export var Locale: any;

    export var debug = true;

    export function log(s : string) {
        if (debug) {
            if (typeof console != "undefined" && typeof console.debug != "undefined") console.log(s);
        }
    }

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
    }

    export function stopPropagation(event) {
        if (event.stopPropagation)
            event.stopPropagation();
        else
            event.cancelBubble = true;
    }

    export function isFalse(value): boolean {
        return value == false || value == "false" || value == "False";
    };

    export function isTrue(value): boolean {
        return value == true || value == "true" || value == "True";
    };0

    export function isEmpty(value): boolean {
        return (value == undefined || value == null || value === "" || value.toString() == "");
    };

    export module Notify {
        var $messageArea: JQuery;
        var timer: number;
        var css: string;

        export function error(message: string, timeout?: number) {
            info(message, timeout, 'sf-error');
        };

        export function info(message: string, timeout?: number, cssClass?: string) {
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

        export function clear() {
            if ($messageArea) {
                $messageArea.hide().children().first().removeClass(css);
                if (timer != null) {
                    clearTimeout(timer);
                    timer = null;
                }
            }
        }
    }

    export module InputValidator {
        export function isNumber(e: KeyboardEvent): boolean {
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
        }

        export function isDecimal(e: KeyboardEvent): boolean {
            var c = e.keyCode;
            return (
                this.isNumber(e) ||
                (c == 110) /*NumPad Decimal*/ ||
                (c == 190) /*.*/ ||
                (c == 188) /*,*/
                );
        }
    }

    export module Cookies {
        export function read(name: string) {
            var nameEQ = name + "=";
            var ca = document.cookie.split(';');
            for (var i = 0; i < ca.length; i++) {
                var c = ca[i];
                while (c.charAt(0) == ' ') c = c.substring(1, c.length);
                if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
            }
            return null;
        }

        export function create(name: string, value: string, days: number, domain?: string) {
            var expires = null,
                path = "/";

            if (days) {
                var date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = date;
            }

            document.cookie = name + "=" + encodeURI(value) + ((expires) ? ";expires=" + expires.toGMTString() : "") + ((path) ? ";path=" + path : "") + ((domain) ? ";domain=" + domain : "");
        }
    }


    export module LocalStorage {
        var isSupported = typeof (localStorage) != 'undefined';

        export function getItem(key : string) {
            if (isSupported) {
                try {
                    return localStorage.getItem(key);
                } catch (e) { }
            }
            return Cookies.read(key);
        };

        export function setItem(key: string, value: string, days?: number) {
            if (isSupported) {
                try {
                    localStorage.setItem(key, value);
                    return true;
                } catch (e) { }
            } else
                Cookies.create(key, value, days ? days : 30);
        };

        return {
            getItem: getItem,
            setItem: setItem
        };
    }

    export function triggerNewContent($source: JQuery) {
        $source.trigger("sf-new-content");
    }
}

module SF.NewContentProcessor {
    export function defaultButtons($newContent: JQuery) {
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

    export function defaultDatepicker($newContent) {
        $newContent.find(".sf-datepicker").each(function (i, val) {
            var $txt = $(val);
            $txt.datepicker(jQuery.extend({}, SF.Locale.defaultDatepickerOptions, { dateFormat: $txt.attr("data-format") }));
        });
    }

    export function defaultDropdown($newContent) {
        $newContent.find(".sf-dropdown .sf-menu-button")
            .addClass("ui-autocomplete ui-menu ui-widget ui-widget-content ui-corner-all")
            .find("li")
            .addClass("ui-menu-item")
            .find("a")
            .addClass("ui-corner-all");
    }

    export function defaultPlaceholder($newContent) {
        $newContent.find('input[placeholder], textarea[placeholder]').placeholder();
    }

    export function defaultTabs($newContent) {
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
                }
                else {
                    $("<li><a href='#" + id + "'>" + legend + "</a></li>")
                        .appendTo($($tabContainer.find(".ui-tabs-nav").first()));
                    $legend.remove();
                }
            });

            $tabContainer.tabs("refresh");
            $tabContainer.tabs("option", "active", 0);
        });
    }

    export function defaultSlider($newContent) {
        $newContent.find(".sf-search-results-container").each(function (i, val) {
            new SF.slider(jQuery(val));
        });
    }

    export function defaultModifiedChecker($newContent) {
        $newContent.find(":input").on("change", function () {
            var $mainControl = $(this).closest(".sf-main-control");
            if ($mainControl.length > 0) {
                $mainControl.addClass("sf-changed");
            }
        });
    }
}


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

interface String {
    hasText(): boolean;
    startsWith(str: string): boolean;
    format(...parameters: any[]): string;
    replaceAll(from: string, to: string);
}

String.prototype.hasText = function () {
    return (this == null || this == undefined || this == '') ? false : true;
}

String.prototype.startsWith = function (str) {
    return (this.indexOf(str) === 0);
}

String.prototype.format = function () {
    var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    var args = arguments;

    var getValue = function (key) {
        if (args == null || typeof args === 'undefined') return null;

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
    return this.split(from).join(to)
};

if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/, '');
    }
}




