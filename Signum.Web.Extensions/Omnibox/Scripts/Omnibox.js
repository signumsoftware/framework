/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function initialize(omniboxId, autocompleteOptions) {
        autocompleteOptions = autocompleteOptions || {};

        var $omnibox = $("#" + omniboxId);

        var lastXhr;
        $omnibox.typeahead($.extend({
            delay: 0,
            minLength: 1,
            source: function (request, response) {
                if (lastXhr)
                    lastXhr.abort();
                lastXhr = $.ajax({
                    url: $omnibox.attr("data-url"),
                    data: { text: request.term },
                    sfNotify: false,
                    success: function (data) {
                        lastXhr = null;
                        response($.map(data, function (item) {
                            return {
                                label: item.label,
                                cleanText: item.cleanText,
                                value: item
                            };
                        }));
                    }
                });
            },
            select: function (event, ui) {
                if (event.keyCode == 9) {
                    $omnibox.val(ui.item.cleanText);
                } else {
                    var url = $(ui.item.label).attr("href");
                    if (event.ctrlKey || event.which == 2) {
                        window.open(url);
                    } else {
                        window.location.assign(url);
                    }
                }
                event.preventDefault();
                return false;
            },
            focus: function (event, ui) {
                return false;
            }
        }, autocompleteOptions));

        $omnibox.data("ui-autocomplete")._renderItem = function (ul, item) {
            return $("<li></li>").data("ui-autocomplete-item", item).append(item.label).appendTo(ul);
        };
    }
    exports.initialize = initialize;
});
//# sourceMappingURL=Omnibox.js.map
