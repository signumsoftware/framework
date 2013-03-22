var SF = SF || {};

SF.Omnibox = (function () {
    var $omnibox = $("#sfOmnibox");
    var lastXhr; //To avoid previous requests results to be shown
    $omnibox.autocomplete({
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
                        }
                    }));
                }
            });
        },
        select: function (event, ui) {
            if (event.keyCode == 9) {
                $omnibox.val(ui.item.cleanText);
            }
            else {
                var url = $(ui.item.label).attr("href");
                if (event.ctrlKey || event.which == 2) {
                    window.open(url);
                }
                else {
                    window.location = url;
                }
            }
            event.preventDefault();
            return false;
        },
        focus: function (event, ui) {
            return false;
        }
    })
        .data("autocomplete")._renderItem = function (ul, item) {
            return $("<li></li>")
				.data("item.autocomplete", item)
				.append(item.label)
				.appendTo(ul);
        };
})();