"use strict";

SF.registerModule("Popup", function () {

    (function ($) {
        // $.fn is the object we add our custom functions to
        $.fn.popup = function (opt) {

            /*
            prefix, onOk, onClose
            */

            var options = {
                modal: true
            };

            if (opt) options = $.extend(options, opt);

            return this.each(function () {
                var $this = $(this);

                if (typeof opt == "string") {
                    if (opt == "destroy")
                        $this.dialog('destroy');
                }
                else {
                    var o = {
                        title: $this.attr("data-title") || $this.children().attr("data-title"), //title causes that it is shown when mouseovering popup
                        modal: options.modal,
                        width: 'auto',
                        close: options.onCancel,
                        dragStop: function (event, ui) {
                           var $dialog = $(event.target).closest(".ui-dialog");
                            var w = $dialog.width();
                            $dialog.width(w + 1);    //auto -> xxx width
                            setTimeout(function () {
                                $dialog.css({ width: "auto" });
                            }, 500);
                        }
                    };

                    if (options.onOk)
                        $this.find(".sf-ok-button").click(options.onOk);

                    $this.dialog(o);
                }
            });
        }
    })(jQuery);
});