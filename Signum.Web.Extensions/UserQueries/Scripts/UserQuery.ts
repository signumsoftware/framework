
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")


once("SF-UserQuery", () => {
    $(document).on("click", ".sf-userquery", function (e) {
        e.preventDefault();
        var findOptionsQueryString = Finder.getFor("").requestDataForSearchInUrl();
        var url = $(this).attr("href") + findOptionsQueryString;

        if (e.ctrlKey || e.which == 2) {
            window.open(url);
        }
        else if (e.which == 1) {
            window.location.href = url;
        }
    });
});

export function attachShowCurrentEntity(el: Lines.EntityLine) {
    var showOnEntity = function () {
        el.element.next("p").toggle(el.runtimeInfo().value != null);
    };

    showOnEntity();

    el.entityChanged = showOnEntity;
}
