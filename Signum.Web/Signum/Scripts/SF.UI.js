/// <reference path="SF.ts"/>
/// <reference path="../Headers/bootstrap/bootstrap.d.ts"/>
/// <reference path="../Headers/bootstrap/bootstrap.datepicker.d.ts"/>
/// <reference path="../Headers/bootstrap/bootstrap.timepicker.d.ts"/>

var SF;
(function (SF) {
    once("SF-control", function () {
        jQuery.fn.SFControl = function () {
            return getPromise(this);
        };

        function getPromise(jq) {
            if (jq.length == 0)
                throw new Error("impossible to get SFControl from no elements");

            if (jq.length > 1)
                throw new Error("impossible to get SFControl from more than one element");

            var result = jq.data("SF-control");

            if (result)
                return Promise.resolve(result);

            if (!jq.hasClass("SF-control-container"))
                throw Error("this element has not SF-control");

            var queue = jq.data("SF-queue");

            if (!queue) {
                queue = [];

                jq.data("SF-queue", queue);
            }

            return new Promise(function (resolve) {
                queue.push(resolve);
            });
        }

        jQuery.fn.SFControlFullfill = function (val) {
            fulllFill(this, val);
        };

        function fulllFill(jq, control) {
            if (jq.length == 0)
                throw new Error("impossible to fulfill SFControl from no elements");

            if (jq.length > 1)
                throw new Error("impossible to fulfill SFControl from more than one element");

            var queue = jq.data("SF-queue");

            if (queue) {
                queue.forEach(function (action) {
                    return action(control);
                });

                jq.data("SF-queue", null);
            }
        }
    });

    once("setupAjaxNotifyPrefilter", function () {
        return setupAjaxNotifyPrefilters();
    });

    function setupAjaxNotifyPrefilters() {
        var pendingRequests = 0;

        $.ajaxSetup({
            type: "POST",
            sfNotify: true
        });

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

                var originalComplete = options.complete;

                options.complete = function (jqXHR, textStatus) {
                    pendingRequests--;
                    if (pendingRequests <= 0) {
                        pendingRequests = 0;
                        Notify.clear();
                    }

                    if (originalComplete != null) {
                        originalComplete(jqXHR, textStatus);
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

    function setHasChanges(element) {
        if (element.closest(".sf-search-control").length)
            return;

        element.closest(".sf-main-control").addClass("sf-changed");
    }
    SF.setHasChanges = setHasChanges;
})(SF || (SF = {}));

$(function () {
    $(document).on("change", "select, input", function () {
        SF.setHasChanges($(this));
    });
});

once("disableTextSelect", function () {
    return $.extend($.fn.disableTextSelect = function () {
        return this.each(function () {
            var $this = $(this);
            $this.bind('selectstart', function () {
                return false;
            });
        });
    });
});

var SF;
(function (SF) {
    (function (Dropdowns) {
        function toggle(event, elem, topFix) {
            var $elem = $(elem), clss = "sf-open";

            if (!$elem.hasClass("sf-dropdown")) {
                $elem = $elem.closest(".sf-dropdown");
            }

            var opened = $elem.hasClass(clss);
            if (opened) {
                $elem.removeClass(clss);
            } else {
                //topFix is used to correct top when the toggler element is inside another panel with borders or anything
                if (typeof topFix == "undefined") {
                    topFix = 0;
                }

                $(".sf-dropdown").removeClass(clss);
                var $content = $elem.find(".sf-menu-button");
                var left = $elem.width() - $content.width();
                $content.css({
                    top: $elem.outerHeight() + topFix,
                    left: ($elem.position().left - $elem.parents("div").first().position().left) < Math.abs(left) ? 0 : left
                });
                $elem.addClass(clss);
            }

            event.stopPropagation();
        }
        Dropdowns.toggle = toggle;

        once("closeDropDowns", function () {
            return $(function () {
                $(document).on("click", function (e) {
                    $(".sf-dropdown").removeClass("sf-open");
                });
            });
        });
    })(SF.Dropdowns || (SF.Dropdowns = {}));
    var Dropdowns = SF.Dropdowns;

    (function (Blocker) {
        var blocked = false;
        var $elem;

        function isEnabled() {
            return blocked;
        }
        Blocker.isEnabled = isEnabled;

        function enable() {
            blocked = true;
            $elem = $("<div/>", {
                "class": "sf-ui-blocker",
                "width": "300%",
                "height": "300%"
            }).appendTo($("body"));
        }
        Blocker.enable = enable;

        function disable() {
            blocked = false;
            $elem.remove();
        }
        Blocker.disable = disable;

        function wrap(promise) {
            if (blocked)
                return promise();

            enable();

            return promise().then(function (val) {
                disable();
                return val;
            }).catch(function (err) {
                disable();
                throw err;
                return null;
            });
        }
        Blocker.wrap = wrap;
    })(SF.Blocker || (SF.Blocker = {}));
    var Blocker = SF.Blocker;
})(SF || (SF = {}));

once("removeKeyPress", function () {
    return $(function () {
        $('#form input[type=text]').keypress(function (e) {
            return e.which != 13;
        });
    });
});

once("ajaxError", function () {
    return $(function () {
        $("body").bind("sf-ajax-error", function (event, XMLHttpRequest, textStatus, thrownError) {
            var error = XMLHttpRequest.responseText;
            if (!error) {
                error = textStatus;
            }

            var message = error.length > 50 ? error.substring(0, 49) + "..." : error;
            SF.Notify.error(lang.signum.error + ": " + message, 2000);

            SF.log(error);
            SF.log(XMLHttpRequest);
            SF.log(thrownError);

            alert(lang.signum.error + ": " + error);
            if (SF.Blocker.isEnabled()) {
                SF.Blocker.disable();
            }
        });
    });
});
//# sourceMappingURL=SF.UI.js.map
