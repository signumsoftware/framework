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
            updater: function (item, e) {
                if (e.keyCode && e.keyCode == 9)
                    return item.cleanText;

                if (item.url) {
                    if (e.ctrlKey)
                        window.open(item.url);
                    else
                        window.location.assign(item.url);
                }
                e.preventDefault();
                return item.cleanText;
            }
        });
    }
    exports.initialize = initialize;
});
//# sourceMappingURL=Omnibox.js.map
