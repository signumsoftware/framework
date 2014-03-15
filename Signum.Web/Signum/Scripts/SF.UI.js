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
})(SF || (SF = {}));


(function ($) {
    // $.fn is the object we add our custom functions to
    $.fn.popup = function (val) {
        //    var $this = $(this);
        //    if (!val)
        //        return $this.data("SF-popupOptions");
        //    if (typeof val == "string") {
        //        if (val == "destroy") {
        //            this.dialog('destroy');
        //            return;
        //        }
        //        if (val == "restoreTitle") {
        //            $this.data("SF-popupOptions").restoreTitle();
        //            return;
        //        }
        //        throw Error("unknown command");
        //    }
        //    /*
        //    prefix, onOk, onClose
        //    */
        //    var options: JQueryUI.PopupOptions = options = $.extend({
        //        modal: true
        //    }, val);
        //    var canClose = function ($popupDialog) {
        //        var $mainControl = $popupDialog.find(".sf-main-control");
        //        if ($mainControl.length > 0) {
        //            if ($mainControl.hasClass("sf-changed")) {
        //                if (!confirm(lang.signum.loseChanges)) {
        //                    return false;
        //                }
        //            }
        //        }
        //        return true;
        //    };
        //    $this.data("SF-popupOptions", options);
        //    var htmlTitle = $this.find("span.sf-popup-title").first();
        //    var titleParent = htmlTitle.parent();
        //    var o = {
        //        dialogClass: 'sf-popup-dialog',
        //        modal: options.modal,
        //        title: htmlTitle.length == 0 ? $this.attr("data-title") || $this.children().attr("data-title") : "",
        //        width: 'auto',
        //        beforeClose: function (evt, ui) {
        //            return canClose($(this));
        //        },
        //        close: options.onCancel,
        //        dragStop: function (event, ui) {
        //            var $dialog = $(event.target).closest(".ui-dialog");
        //            var w = $dialog.width();
        //            $dialog.width(w + 1);    //auto -> xxx width
        //            setTimeout(function () {
        //                $dialog.css({ width: "auto" });
        //            }, 500);
        //        }
        //    };
        //    if (typeof options.onOk != "undefined") {
        //        $this.find(".sf-ok-button").off('click').click(function (e) {
        //            e.preventDefault();
        //            var $this = $(this);
        //            if ($this.hasClass("sf-save-protected")) {
        //                var $popupDialog = $this.closest(".sf-popup-dialog");
        //                var $mainControl = $popupDialog.find(".sf-main-control");
        //                if ($mainControl.length < 1) {
        //                    options.onOk();
        //                }
        //                else if (!$mainControl.hasClass("sf-changed")) {
        //                    options.onOk();
        //                }
        //                else if (canClose($popupDialog)) {
        //                    if (typeof options.onCancel != "undefined") {
        //                        if (options.onCancel()) {
        //                            $popupDialog.remove();
        //                        }
        //                    }
        //                }
        //            }
        //            else {
        //                options.onOk();
        //            }
        //        });
        //    }
        //    var dialog = $this.dialog(o);
        //    if (htmlTitle.length > 0) {
        //        dialog.data("ui-dialog")._title = function (title) {
        //            title.append(this.options.title);
        //        };
        //        dialog.dialog('option', 'title', htmlTitle);
        //        (<any>options).restoreTitle = () => {
        //            titleParent.append(htmlTitle);
        //        };
        //    }
    };
})(jQuery);

var SF;
(function (SF) {
    function slider($container) {
        var w = $container.width(), $target = $container.children("table").first(), mw = $target.width(), containerLeft = $container.offset().left;

        $container.css({ "overflow-x": "hidden" });
        var $track = $("<div class='sf-search-track'></div>").css({ width: w });

        var sliderWidth = Math.max(100, (2 * w - mw));
        var $slider = $("<div class='sf-search-slider' title='Arrastrar para hacer scroll'></div>").css({ width: sliderWidth }).appendTo($track);

        var proportion = (mw - w) / (w - sliderWidth);
        if (mw <= w)
            $track.hide();

        $target.before($track);
        $target.after($track.clone());

        var mouseDown = false, left = 0, prevLeft;

        $container.find(".sf-search-slider").bind("mousedown", function (e) {
            mouseDown = true;
            left = getMousePosition(e).x - containerLeft;
            prevLeft = $(this).position().left;
        });

        $container.find(".sf-search-track").bind("click", function (e) {
            if ($(e.target).hasClass("sf-search-slider"))
                return;

            var $track = $(this), clicked = getMousePosition(e).x - containerLeft, $slider = $track.find(".sf-search-slider"), sliderPosLeft = $slider.position().left, sliderWidth = $slider.width();

            var isLeft = sliderPosLeft > clicked;

            var left = 0;
            if (isLeft) {
                //move sliders left
                left = Math.max(sliderPosLeft - sliderWidth, 0);
            } else {
                left = Math.min(sliderPosLeft + sliderWidth, w - sliderWidth);
            }

            $track.parent().find(".sf-search-slider").css({ left: left });

            $container.children("table").first().css({ marginLeft: -left * proportion });
        });

        $(document).bind("mousemove", function (e) {
            if (mouseDown) {
                var currentLeft = prevLeft + (getMousePosition(e).x - containerLeft - left);
                currentLeft = Math.min(currentLeft, w - (Math.max(100, 2 * w - mw)));
                currentLeft = Math.max(0, currentLeft);

                $container.find(".sf-search-slider").css({ left: currentLeft });
                $target.css({ marginLeft: -currentLeft * proportion });
            }
        }).bind("mouseup", function () {
            mouseDown = false;
        });

        var resize = function ($c, $t) {
            if (!mouseDown) {
                var _w = $c.width(), _mw = $c.children("table").first().width();
                if ((w != _w || mw != _mw)) {
                    if (_mw > _w) {
                        w = _w;
                        mw = _mw;
                        $t.css({ width: w, left: 0 }).show();

                        var sliderWidth = Math.max(100, (2 * w - mw));

                        proportion = (mw - w) / (w - sliderWidth);

                        $t.find(".sf-search-slider").css({ width: sliderWidth });
                        $container.children("table").first().css({ marginLeft: 0 });
                    } else {
                        $t.hide();
                    }
                }
            }
            setTimeout(function () {
                resize($c, $t);
            }, 1000);
        };
        resize($container, $container.find(".sf-search-track"));

        $(function () {
            $container.find(".sf-search-slider").disableTextSelect();
        });
    }
    SF.slider = slider;

    var getMousePosition = function (e) {
        var posx = 0, posy = 0;

        if (window.event) {
            posx = window.event.clientX + document.documentElement.scrollLeft + document.body.scrollLeft;
            posy = window.event.clientY + document.documentElement.scrollTop + document.body.scrollTop;
        } else {
            posx = e.clientX + document.body.scrollLeft;
            posy = e.clientY + document.body.scrollTop;
        }

        return {
            'x': posx,
            'y': posy
        };
    };
})(SF || (SF = {}));

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
