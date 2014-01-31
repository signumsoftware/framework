/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Finder"], function(require, exports, Finder) {
    once("SF-UserQuery", function () {
        $(document).on("click", ".sf-userquery", function (e) {
            e.preventDefault();
            var findOptionsQueryString = Finder.getFor("").requestDataForSearchInUrl();
            var url = $(this).attr("href") + findOptionsQueryString;

            if (e.ctrlKey || e.which == 2) {
                window.open(url);
            } else if (e.which == 1) {
                window.location.href = url;
            }
        });
    });

    function attachShowCurrentEntity(el) {
        var showOnEntity = function () {
            el.element.next("p").toggle(el.runtimeInfo().value != null);
        };

        showOnEntity();

        el.entityChanged = showOnEntity;
    }
    exports.attachShowCurrentEntity = attachShowCurrentEntity;
});
//# sourceMappingURL=UserQuery.js.map
