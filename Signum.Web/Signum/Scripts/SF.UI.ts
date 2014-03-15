/// <reference path="SF.ts"/>
/// <reference path="../Headers/bootstrap/bootstrap.d.ts"/>
/// <reference path="../Headers/bootstrap/bootstrap.datepicker.d.ts"/>
/// <reference path="../Headers/bootstrap/bootstrap.timepicker.d.ts"/>

interface JQuery {
    SFControl<T>(): Promise<T>;
    SFControlFullfill<T>(control: T) : void
}

interface JQueryAjaxSettings {
    sfNotify? : boolean
}


module SF {

    once("SF-control", () => {
        jQuery.fn.SFControl = function () {
            return getPromise(this);
        };

        function getPromise<T>(jq: JQuery): Promise<T> {

            if (jq.length == 0)
                throw new Error("impossible to get SFControl from no elements");

            if (jq.length > 1)
                throw new Error("impossible to get SFControl from more than one element");

            var result = <T>jq.data("SF-control");

            if (result)
                return Promise.resolve(result);

            if (!jq.hasClass("SF-control-container"))
                throw Error("this element has not SF-control");

            var queue: { (value: T): void }[] = jq.data("SF-queue");

            if (!queue) {
                queue = [];

                jq.data("SF-queue", queue);
            }

            return new Promise<T>((resolve) => {
                queue.push(resolve);
            });
        }

        jQuery.fn.SFControlFullfill = function (val : any) {
            fulllFill<any>(this, val);
        };


        function fulllFill<T>(jq: JQuery, control: T){
         
            if (jq.length == 0)
                throw new Error("impossible to fulfill SFControl from no elements");

            if (jq.length > 1)
                throw new Error("impossible to fulfill SFControl from more than one element");

            var queue: { (value: T): void }[] = jq.data("SF-queue");

            if (queue) {
                queue.forEach(action=> action(control));

                jq.data("SF-queue", null);
            }
        }
    });

    once("setupAjaxNotifyPrefilter", () =>
        setupAjaxNotifyPrefilters());

    function setupAjaxNotifyPrefilters() {
        var pendingRequests = 0;

        $.ajaxSetup({
            type: "POST",
            sfNotify: true
        });


        $.ajaxPrefilter(function (options: JQueryAjaxSettings, originalOptions: JQueryAjaxSettings, jqXHR) {
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
}



//// Overrides jquery calendar in (jquery-ui-1.7.2.js) to format dates in .net dateformat that can be found here:
//// http://msdn.microsoft.com/en-us/library/8kb3ddd4%28v=VS.71%29.aspx

//once("datePickerFormat", () =>
//    (function ($) {
//        $.datepicker.formatDate = function (format, date, settings) {
//            if (!date)
//                return '';
//            var dayNamesShort = (settings ? settings.dayNamesShort : null) || this._defaults.dayNamesShort;
//            var dayNames = (settings ? settings.dayNames : null) || this._defaults.dayNames;
//            var monthNamesShort = (settings ? settings.monthNamesShort : null) || this._defaults.monthNamesShort;
//            var monthNames = (settings ? settings.monthNames : null) || this._defaults.monthNames;

//            // Get patternChar number of repetitions
//            var getAdvanceChars = function (pos, patternChar) {
//                var repeatPattern = pos + 1;
//                while ((repeatPattern < format.length) && (format.charAt(repeatPattern) == patternChar)) {
//                    repeatPattern++;
//                }
//                return (repeatPattern - pos);
//            };

//            // Format a number, with leading zero if necessary
//            var formatNumber = function (value, len) {
//                var num = '' + value;
//                while (num.length < len)
//                    num = '0' + num;
//                return num;
//            };

//            var output = '';
//            var advanceChars;
//            if (date) {
//                for (var iFormat = 0; iFormat < format.length; iFormat += advanceChars) {
//                    var patternChar = format.charAt(iFormat);
//                    advanceChars = getAdvanceChars(iFormat, patternChar);

//                    switch (patternChar) {
//                        case 'y':
//                            if (advanceChars > 2)
//                                output += date.getFullYear();
//                            else {
//                                var year = '' + date.getFullYear();
//                                if (advanceChars == 2)
//                                    output += year.charAt(2);
//                                output += year.charAt(3);
//                            }
//                            break;
//                        case 'M':
//                            if (advanceChars == 1)
//                                output += formatNumber(date.getMonth() + 1, 1);
//                            else if (advanceChars == 2)
//                                output += formatNumber(date.getMonth() + 1, 2);
//                            else if (advanceChars == 3)
//                                output += monthNamesShort[date.getMonth()];
//                            else if (advanceChars == 4)
//                                output += monthNames[date.getMonth()];
//                            break;
//                        case 'd':
//                            if (advanceChars == 1)
//                                output += formatNumber(date.getDate(), 1);
//                            else if (advanceChars == 2)
//                                output += formatNumber(date.getDate(), 2);
//                            else if (advanceChars == 3)
//                                output += dayNamesShort[date.getDay()];
//                            else if (advanceChars == 4)
//                                output += dayNames[date.getDay()];
//                            break;
//                        case 'H':
//                        case 'h':
//                            output += formatNumber(0, advanceChars); //Always set hours to 0 when selecting a date from the picker
//                            break;
//                        case 'm':
//                            output += formatNumber(0, advanceChars); //Always set minutes to 0 when selecting a date from the picker
//                            break;
//                        case 's':
//                            output += formatNumber(0, advanceChars); //Always set seconds to 0 when selecting a date from the picker
//                            break;
//                        case 't':
//                            output += 'a'; //Always set am/pm to am when selecting a date from the picker
//                            if (advanceChars == 2)
//                                output += 'm';
//                            break;
//                        case 'f': //Seconds fractions
//                            break;
//                        default:
//                            output += format.charAt(iFormat);
//                    }
//                }
//            }
//            return output;
//        };

//        $.datepicker.parseDate = function (format, value, settings) {
//            if (format == null || value == null)
//                throw 'Invalid arguments';

//            value = (typeof value == 'object' ? value.toString() : value + '');
//            if (value == '')
//                return null;

//            var shortYearCutoff = (settings ? settings.shortYearCutoff : null) || this._defaults.shortYearCutoff;
//            var dayNamesShort = (settings ? settings.dayNamesShort : null) || this._defaults.dayNamesShort;
//            var dayNames = (settings ? settings.dayNames : null) || this._defaults.dayNames;
//            var monthNamesShort = (settings ? settings.monthNamesShort : null) || this._defaults.monthNamesShort;
//            var monthNames = (settings ? settings.monthNames : null) || this._defaults.monthNames;

//            var year = -1;
//            var month = -1;
//            var day = -1;
//            var doy = -1;

//            // Get patternChar number of repetitions
//            var getAdvanceChars = function (pos, patternChar) {
//                var repeatPattern = pos + 1;
//                while ((repeatPattern < format.length) && (format.charAt(repeatPattern) == patternChar)) {
//                    repeatPattern++;
//                }
//                return (repeatPattern - pos);
//            };

//            var getValueAdvanceChars = function (iFormat, advanceChars, valueCurrentIndex) {
//                if (format.length == iFormat + advanceChars)
//                    return value.length - valueCurrentIndex;
//                var templateNextChar = format.charAt(iFormat + advanceChars);
//                return value.indexOf(templateNextChar, valueCurrentIndex) - valueCurrentIndex;
//            }

//            // Extract a number from the string value
//            var getNumber = function (pos, len) {
//                var num = 0;
//                var index;
//                for (index = 0; index < len; index++) {
//                    var currChar = value.charAt(pos + index);
//                    if (currChar < '0' || currChar > '9')
//                        throw 'Missing number at position ' + pos + index;
//                    num = num * 10 + parseInt(currChar, 10);
//                }
//                return num;
//            };

//            // Extract a name from the string value and convert to an index
//            var getNameIndex = function (name, arrayNames) {
//                for (var i = 0; i < arrayNames.length; i++) {
//                    if (name == arrayNames[i])
//                        return i + 1;
//                }
//                throw 'Unknown name ' + name;
//            };

//            // Confirm that a literal character matches the string value
//            var checkLiteral = function (pos, valueCurrentIndex) {
//                if (value.charAt(valueCurrentIndex) != format.charAt(pos))
//                    throw 'Unexpected literal at position ' + iValue;
//                iValue++;
//            };

//            var valueCurrentIndex = 0;
//            var valueAdvanceChars = 0;
//            var iValue = 0;
//            var advanceChars;
//            for (var iFormat = 0; iFormat < format.length; iFormat += advanceChars) {
//                var patternChar = format.charAt(iFormat);
//                advanceChars = getAdvanceChars(iFormat, patternChar);
//                valueAdvanceChars = getValueAdvanceChars(iFormat, advanceChars, valueCurrentIndex);
//                switch (patternChar) {
//                    case 'y':
//                        year = getNumber(valueCurrentIndex, valueAdvanceChars);
//                        if (advanceChars > 2)
//                            break;
//                        else {
//                            var currYear = '' + new Date().getFullYear();
//                            if (advanceChars == 2)
//                                year = parseInt(currYear.charAt(0) + currYear.charAt(1) + year);
//                            else
//                                year = parseInt(currYear.charAt(0) + currYear.charAt(1) + currYear.charAt(2) + year);
//                        }
//                        break;
//                    case 'M':
//                        if (advanceChars == 1 || advanceChars == 2)
//                            month = getNumber(valueCurrentIndex, valueAdvanceChars);
//                        else {
//                            var monthStr = value.substr(valueCurrentIndex, valueAdvanceChars);
//                            if (advanceChars == 3)
//                                month = getNameIndex(monthStr, monthNamesShort);
//                            else if (advanceChars == 4)
//                                month = getNameIndex(monthStr, monthNames);
//                        }

//                        break;
//                    case 'd':
//                        if (advanceChars == 1 || advanceChars == 2)
//                            day = getNumber(valueCurrentIndex, valueAdvanceChars);
//                        else {
//                            var dayStr = value.substr(valueCurrentIndex, valueAdvanceChars);
//                            if (advanceChars == 3)
//                                day = getNameIndex(dayStr, dayNamesShort);
//                            else if (advanceChars == 4)
//                                day = getNameIndex(dayStr, dayNames);
//                        }
//                        break;
//                    case 'D':
//                        throw new Error("not implemented");
//                        //getName('D', dayNamesShort, dayNames);
//                        break;
//                    case 'H':
//                    case 'h':
//                    case 'm':
//                    case 's':
//                    case 't':
//                    case 'f':
//                        break;
//                    default:
//                        {
//                            checkLiteral(iFormat, valueCurrentIndex);
//                            advanceChars = 1; //Check only one literal at a time
//                            valueAdvanceChars = 1;
//                        }
//                }
//                valueCurrentIndex += valueAdvanceChars;
//            }

//            if (year == -1)
//                year = new Date().getFullYear();

//            var date = this._daylightSavingAdjust(new Date(year, month - 1, day));
//            if (date.getFullYear() != year || date.getMonth() + 1 != month || date.getDate() != day)
//                throw 'Invalid date'; // E.g. 31/02/*
//            return date;
//        };
//    })(jQuery));



declare module JQueryUI {
    interface PopupOptions {
        modal?: boolean;
        onOk?: () => void;
        onCancel?: () => void;
    }
}

interface JQuery {
    popup(opt: JQueryUI.PopupOptions): void;
    popup(command: string);
    popup(): JQueryUI.PopupOptions;
}


(function ($: JQueryStatic) {
    // $.fn is the object we add our custom functions to
    $.fn.popup = function (val: any) {

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
    }
})(jQuery);

module SF {

    export function slider($container: JQuery) {
        var w = $container.width(),
            $target = $container.children("table").first(),
            mw = $target.width(),
            containerLeft = $container.offset().left;

        $container.css({ "overflow-x": "hidden" });
        var $track = $("<div class='sf-search-track'></div>")
            .css({ width: w });

        var sliderWidth = Math.max(100, (2 * w - mw));
        var $slider = $("<div class='sf-search-slider' title='Arrastrar para hacer scroll'></div>")
            .css({ width: sliderWidth })
            .appendTo($track);

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
            if ($(e.target).hasClass("sf-search-slider")) return;

            var $track = $(this),
                clicked = getMousePosition(e).x - containerLeft,
                $slider = $track.find(".sf-search-slider"),
                sliderPosLeft = $slider.position().left,
                sliderWidth = $slider.width();

            var isLeft = sliderPosLeft > clicked;

            var left = 0;
            if (isLeft) {
                //move sliders left
                left = Math.max(sliderPosLeft - sliderWidth, 0);
            }
            else {
                left = Math.min(sliderPosLeft + sliderWidth, w - sliderWidth);
            }

            $track.parent()
                .find(".sf-search-slider")
                .css({ left: left });

            $container.children("table")
                .first()
                .css({ marginLeft: -left * proportion });

        });

        $(document)
            .bind("mousemove", function (e) {
                if (mouseDown) {
                    var currentLeft = prevLeft + (getMousePosition(e).x - containerLeft - left);
                    currentLeft = Math.min(currentLeft, w - (Math.max(100, 2 * w - mw)));
                    currentLeft = Math.max(0, currentLeft);

                    $container.find(".sf-search-slider").css({ left: currentLeft });
                    $target.css({ marginLeft: -currentLeft * proportion });
                }
            })
            .bind("mouseup", function () {
                mouseDown = false;
            });

        var resize = function ($c, $t) {
            if (!mouseDown) {
                var _w = $c.width(),
                    _mw = $c.children("table").first().width();
                if ((w != _w || mw != _mw)) {
                    if (_mw > _w) {
                        w = _w;
                        mw = _mw;
                        $t.css({ width: w, left: 0 }).show();

                        var sliderWidth = Math.max(100, (2 * w - mw));

                        proportion = (mw - w) / (w - sliderWidth);

                        $t.find(".sf-search-slider").css({ width: sliderWidth });
                        $container.children("table").first().css({ marginLeft: 0 });
                    }
                    else {
                        $t.hide();
                    }
                }
            }
            setTimeout(function () { resize($c, $t); }, 1000);
        };
        resize($container, $container.find(".sf-search-track"));

        $(function () {
            $container.find(".sf-search-slider").disableTextSelect();
        });
    }

    var getMousePosition = function (e) {
        var posx = 0,
            posy = 0;

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
    }
}

interface JQuery {
    disableTextSelect();
}
once("disableTextSelect", () =>
    $.extend($.fn.disableTextSelect = function () {
        return this.each(function () {
            var $this = $(this);
            $this.bind('selectstart', function () { return false; });
        });
    }));


module SF {
    export module Dropdowns {
        export function toggle(event, elem, topFix) {
            var $elem = $(elem),
                clss = "sf-open";

            if (!$elem.hasClass("sf-dropdown")) {
                $elem = $elem.closest(".sf-dropdown");
            }

            var opened = $elem.hasClass(clss);
            if (opened) {
                $elem.removeClass(clss);
            }
            else {
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

        once("closeDropDowns", () =>
            $(function () {
                $(document).on("click", function (e) {
                    $(".sf-dropdown").removeClass("sf-open");
                });
            }));
    }

    export module Blocker {
        var blocked = false;
        var $elem: JQuery;

        export function isEnabled() {
            return blocked;
        }

        export function enable() {
            blocked = true;
            $elem =
            $("<div/>", {
                "class": "sf-ui-blocker",
                "width": "300%",
                "height": "300%"
            }).appendTo($("body"));
        }

        export function disable() {
            blocked = false;
            $elem.remove();
        }

        export function wrap<T>(promise: () => Promise<T>): Promise<T> {
            if (blocked)
                return promise();

            enable();

            return promise()
                .then(val=> { disable(); return val; })
                .catch(err=> { disable(); throw err; return <T>null; }); //Typescript bug?
        }
    }

   
}


once("removeKeyPress", () =>
    $(function () { $('#form input[type=text]').keypress(function (e) { return e.which != 13 }) }));


once("ajaxError", () =>
    $(function () {
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
    }));