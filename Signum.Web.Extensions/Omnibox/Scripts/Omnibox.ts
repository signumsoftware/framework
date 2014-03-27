/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

export function initialize(omniboxId: string, url: string) {
    var $omnibox = $("#" + omniboxId);

    var handler: number;
    var lastXhr: JQueryXHR; //To avoid previous requests results to be shown
    $omnibox.typeahead({

        source: function (query, response) {

            if (handler)
                clearTimeout(handler);

            handler = setTimeout(() => {
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
        sorter: items => items,
        matcher: item => true,
        highlighter: item => item.label,
        updater: item  => {
            if (event.keyCode == 9) {
                $omnibox.val(item.cleanText);
            }
            else if (item.url) {
                if (event.ctrlKey)
                    window.open(item.url);
                else
                    window.location.assign(item.url);
            }
            event.preventDefault();
            return item.cleanText;
        },
    });
}
