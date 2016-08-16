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
            fullFill<any>(this, val);
        };


        function fullFill<T>(jq: JQuery, control: T){
         
            if (jq.length == 0)
                throw new Error("impossible to fulfill SFControl from no elements");

            if (jq.length > 1)
                throw new Error("impossible to fulfill SFControl from more than one element");

            if (!jq.hasClass("SF-control-container"))
                throw Error("this element has not SF-control");

            if (!jq.data("SF-control"))
                throw Error("SF-control not set yet");

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

    export function setHasChanges(element: JQuery) {
        if (element.closest(".sf-search-control").length)
            return;

        element.closest(".sf-main-control").addClass("sf-changed");
    }

    export function getDateTime(dateTimePickerId: string): string {
        var date = dateTimePickerId.child("Date").get();
        var time = dateTimePickerId.child("Time").get();

        return date.val() + " " + time.val();
    }

    export function setDateTime(dateTimePickerId: string, value: string): void {

        var date = dateTimePickerId.child("Date").get();
        var time = dateTimePickerId.child("Time").get();

        date.val(SF.isEmpty(value) ? "" : value.before(" "));
        time.val(SF.isEmpty(value) ? "" : value.after(" "));
    }



}


$(function () {
    $(document).on("change", "select, input, textarea", function () {
        SF.setHasChanges($(this));
    });
});

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

    export module ContextMenu {
        $(document).on("click", function () {
            hideContextMenu();
        });

        export function hideContextMenu() {
            $("#sfContextMenu").hide();
        }

        export function createContextMenu(e: { pageX: number; pageY: number }) {

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
                .then(val => { disable(); return val; })
            ['catch']((err): any => { disable(); throw err; }); //Typescript bug?
        }
    }

    export function onVisible(element: JQuery) : Promise<JQuery> {

        if (element.length == 0)
            throw Error("element is empty");

        if (element.closest("[id$=_sfEntity]").length) {
            return Promise.reject("In sfEntity"); // will be called again? 
        }

        var modal = element.closest(".modal"); 

        var onModalVisible = modal.length == 0 || modal.is(":visible") ? Promise.resolve(element) :
            onEventOnce(modal, "shown.bs.modal"); 

        return onModalVisible.then(() => {
            var pane = element.closest(".tab-pane");
            if (!pane.length)
                return element;

            var id = (<HTMLElement>pane[0]).id;

            if (pane.hasClass("active") || !id)
                return element;

            var tab = pane.parent().parent().find("a[data-toggle=tab][href='#" + id + "']");

            if (!tab.length)
                return element;

            return <any>onEventOnce(tab, "shown.bs.tab");
        });
    }

    export function onEventOnce(element: JQuery, eventName: string): Promise<JQuery> {
        return new Promise((resolve) => {
            var onEvent: () => void;

            onEvent = () => {
                element.off(eventName, onEvent);
                resolve(element);
            };

            element.on(eventName, onEvent);
        });

    }

    export function onHidden(element: JQuery): Promise<JQuery> {
        return new Promise((resolve) => {
            element.closest(".modal")
                .on("hide.bs.modal", () => {
                    resolve(element);
                });

        });
    }
}


once("removeKeyPress", () =>
    $(function () { $('#form input[type=text]').keypress(function (e) { return e.which != 13 }) }));


once("ajaxError", () =>
    $(function () {
        $("body").bind("sf-ajax-error", <any>function (event, XMLHttpRequest, textStatus, thrownError) {

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

once("dateTimePickerSync", () => {
    $(function () {
        $(document).on("paste", 'div.date-time div.date input', function (e: JQueryEventObject) {
            setTimeout(function () {
                var dateTime: string = $(e.currentTarget).val();

                var hour = dateTime.tryAfterLast(" ");
                var date = dateTime.tryBeforeLast(" ");

                if (hour && date) {
                    var timePicker = $(e.currentTarget).closest("div.date-time").find("div.time");
                    timePicker.timepicker("setTime", hour);
                    $(e.currentTarget).val(date.tryAfterLast(" ") || date)
                }
            }, 100);             
        });

        $(document).on("changeDate clearDate", 'div.date-time div.date', function (e : any) {
            var time = $(this).closest("div.date-time").find("div.time");

            var input = time.is("input") ? time : time.find("input");
            if (SF.isEmpty(input.val()) != SF.isEmpty(e.date))
                time.timepicker("setTime", e.date);
        });

        $(document).on("show.timepicker", 'div.date-time div.time', function (e: any) {
            var time = $(this).closest("div.date-time").find("div.date")
            var date = time.datepicker("getDate");
            if (isNaN(date.getTime()))
                e.time.cancel = true;
        });
    });
});

