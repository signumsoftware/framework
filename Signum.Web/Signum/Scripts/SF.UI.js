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
    $(document).on("change", "select, input, textarea", function () {
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
    (function (ContextMenu) {
        $(document).on("click", function () {
            hideContextMenu();
        });

        function hideContextMenu() {
            $("#sfContextMenu").hide();
        }
        ContextMenu.hideContextMenu = hideContextMenu;

        function createContextMenu(e) {
            var menu = $("#sfContextMenu");

            if (menu.length)
                menu.html("");
            else
                menu = $("<ul id='sfContextMenu' class='dropdown-menu sf-context-menu'></ul>").appendTo("body");

            menu.css({
                left: e.pageX,
                top: e.pageY,
                zIndex: 9999,
                display: "block",
                position: 'absolute'
            });

            menu.on("hidden.bs.dropdown", function () {
                menu.remove();
            });

            return menu;
        }
        ContextMenu.createContextMenu = createContextMenu;
    })(SF.ContextMenu || (SF.ContextMenu = {}));
    var ContextMenu = SF.ContextMenu;

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
            })['catch'](function (err) {
                disable();
                throw err;
                return null;
            });
        }
        Blocker.wrap = wrap;
    })(SF.Blocker || (SF.Blocker = {}));
    var Blocker = SF.Blocker;

    function onVisible(element) {
        if (element.length == 0)
            throw Error("element is empty");

        if (element.closest("[id$=_sfEntity]").length) {
            return Promise.reject("In sfEntity");
        }

        var modal = element.closest(".modal");

        var onModalVisible = modal.length == 0 ? Promise.resolve(element) : onEventOnce(modal, "shown.bs.modal");

        return onModalVisible.then(function () {
            var pane = element.closest(".tab-pane");
            if (!pane.length)
                return element;

            var id = pane[0].id;

            if (pane.hasClass("active") || !id)
                return element;

            var tab = pane.parent().parent().find("a[data-toggle=tab][href=#" + id + "]");

            if (!tab.length)
                return element;

            return onEventOnce(tab, "shown.bs.tab");
        });
    }
    SF.onVisible = onVisible;

    function onEventOnce(element, eventName) {
        return new Promise(function (resolve) {
            var onEvent;

            onEvent = function () {
                element.off(eventName, onEvent);
                resolve(element);
            };

            element.on(eventName, onEvent);
        });
    }
    SF.onEventOnce = onEventOnce;

    function onHidden(element, callbackHidden) {
        element.closest(".modal").on("hide.bs.modal", function () {
            callbackHidden(element);
        });
    }
    SF.onHidden = onHidden;
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
