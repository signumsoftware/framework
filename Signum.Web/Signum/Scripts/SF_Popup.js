/// <reference path="references.ts"/>
var SF;
(function (SF) {
    (function (Popup) {
        function serialize(prefix) {
            var id = SF.compose(prefix, "panelPopup");
            var $formChildren = $("#" + id + " :input");
            var data = $formChildren.serialize();

            var myRuntimeInfoKey = SF.compose(prefix, SF.Keys.runtimeInfo);
            if ($formChildren.filter("#" + myRuntimeInfoKey).length == 0) {
                var $mainControl = $(".sf-main-control[data-prefix=" + prefix + "]");
                data += "&" + myRuntimeInfoKey + "=" + $mainControl.data("runtimeinfo");
            }
            return data;
        }
        Popup.serialize = serialize;

        function serializeJson(prefix) {
            var id = SF.compose(prefix, "panelPopup");
            var arr = $("#" + id + " :input").serializeArray();
            var data = {};
            for (var index = 0; index < arr.length; index++) {
                if (data[arr[index].name] != null) {
                    data[arr[index].name] += "," + arr[index].value;
                } else {
                    data[arr[index].name] = arr[index].value;
                }
            }

            var myRuntimeInfoKey = SF.compose(prefix, SF.Keys.runtimeInfo);
            if (typeof data[myRuntimeInfoKey] == "undefined") {
                var $mainControl = $(".sf-main-control[data-prefix=" + prefix + "]");
                data[myRuntimeInfoKey] = $mainControl.data("runtimeinfo");
            }
            return data;
        }
        Popup.serializeJson = serializeJson;
        ;
    })(SF.Popup || (SF.Popup = {}));
    var Popup = SF.Popup;
})(SF || (SF = {}));

(function ($) {
    // $.fn is the object we add our custom functions to
    $.fn.popup = function (opt) {
        /*
        prefix, onOk, onClose
        */
        var options = {
            modal: true
        };

        if (opt) {
            options = $.extend(options, opt);
        }

        var canClose = function ($popupDialog) {
            var $mainControl = $popupDialog.find(".sf-main-control");
            if ($mainControl.length > 0) {
                if ($mainControl.hasClass("sf-changed")) {
                    if (!confirm(lang.signum.loseChanges)) {
                        return false;
                    }
                }
            }
            return true;
        };

        return this.each(function () {
            var $this = $(this);

            if (typeof opt == "string") {
                if (opt == "destroy") {
                    $this.dialog('destroy');
                }
            } else {
                var $htmlTitle = $this.find("span.sf-popup-title").first();

                var o = {
                    dialogClass: 'sf-popup-dialog',
                    modal: options.modal,
                    title: $htmlTitle.length == 0 ? $this.attr("data-title") || $this.children().attr("data-title") : "",
                    width: 'auto',
                    beforeClose: function (evt, ui) {
                        return canClose($(this));
                    },
                    close: options.onCancel,
                    dragStop: function (event, ui) {
                        var $dialog = $(event.target).closest(".ui-dialog");
                        var w = $dialog.width();
                        $dialog.width(w + 1); //auto -> xxx width
                        setTimeout(function () {
                            $dialog.css({ width: "auto" });
                        }, 500);
                    }
                };

                if (typeof options.onOk != "undefined") {
                    $this.find(".sf-ok-button").off('click').click(function () {
                        var $this = $(this);
                        if ($this.hasClass("sf-save-protected")) {
                            var $popupDialog = $this.closest(".sf-popup-dialog");
                            var $mainControl = $popupDialog.find(".sf-main-control");
                            if ($mainControl < 1) {
                                options.onOk();
                            } else if (!$mainControl.hasClass("sf-changed")) {
                                options.onOk();
                            } else if (canClose($popupDialog)) {
                                if (typeof options.onCancel != "undefined") {
                                    if (options.onCancel()) {
                                        $popupDialog.remove();
                                    }
                                }
                            }
                        } else {
                            options.onOk();
                        }
                    });
                }

                if (typeof options.onSave != "undefined") {
                    $this.find(".sf-save").off('click').click(options.onSave);
                }

                var dialog = $this.dialog(o);

                if ($htmlTitle.length > 0) {
                    dialog.data("ui-dialog")._title = function (title) {
                        title.html(this.options.title);
                    };

                    dialog.dialog('option', 'title', $htmlTitle.html());
                    //$htmlTitle.remove();
                }
            }
        });
    };
})(jQuery);
