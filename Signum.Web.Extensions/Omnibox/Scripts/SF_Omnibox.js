/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
var SF;
(function (SF) {
    (function (Omnibox) {
        function initialize($omnibox, autocompleteOptions) {
            autocompleteOptions = autocompleteOptions || {};

            var lastXhr;
            $omnibox.autocomplete($.extend({
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
        Omnibox.initialize = initialize;
    })(SF.Omnibox || (SF.Omnibox = {}));
    var Omnibox = SF.Omnibox;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Omnibox.js.map
