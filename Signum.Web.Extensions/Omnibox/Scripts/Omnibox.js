/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function initialize(omniboxId, url) {
        var $omnibox = $("#" + omniboxId);

        var handler;
        var lastXhr;
        $omnibox.typeahead({
            source: function (query, response) {
                if (handler)
                    clearTimeout(handler);

                handler = setTimeout(function () {
                    if (lastXhr)
                        lastXhr.abort();
                    lastXhr = $.ajax({
                        url: url,
                        data: { text: query },
                        sfNotify: false,
                        success: function (data) {
                            lastXhr = null;
                            response(data);
                        }
                    });
                });
            },
            sorter: function (items) {
                return items;
            },
            matcher: function (item) {
                return true;
            },
            highlighter: function (item) {
                return item.label;
            },
            updater: function (item) {
                if (event.keyCode == 9) {
                    $omnibox.val(item.cleanText);
                } else if (item.url) {
                    if (event.ctrlKey)
                        window.open(item.url);
                    else
                        window.location.assign(item.url);
                }
                event.preventDefault();
                return item.cleanText;
            }
        });
    }
    exports.initialize = initialize;
});
//# sourceMappingURL=Omnibox.js.map
