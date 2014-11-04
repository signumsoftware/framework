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
        items: 'all',
        sorter: items => items,
        matcher: item => true,
        highlighter: item => item.label,
        updater: (item, e) => {
            if ((<any>e).keyCode && (<any>e).keyCode == 9) //Tab
                return item.cleanText;

            if (item.url) {
                if ((<MouseEvent>e).ctrlKey || (<MouseEvent>e).which == 2)
                    window.open(item.url);
                else
                    window.location.assign(item.url);
            }
            e.preventDefault();
            return item.cleanText;
        },
    });
}
