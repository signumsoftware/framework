/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Finder, Operations) {
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
            el.element.nextAll("p.messageEntity").toggle(!!el.runtimeInfo().value());
        };

        showOnEntity();

        el.entityChanged = showOnEntity;
    }
    exports.attachShowCurrentEntity = attachShowCurrentEntity;

    function deleteUserQuery(os, urlRedirect) {
        os.avoidReturnRedirect = true;

        Operations.deleteDefault(os).then(function () {
            if (!os.prefix)
                window.location.href = urlRedirect;
        });
    }
    exports.deleteUserQuery = deleteUserQuery;

    function saveUserQuery(os, url) {
        os.controllerUrl = url;

        Operations.executeDefault(os);
    }
    exports.saveUserQuery = saveUserQuery;

    function createUserQuery(prefix, url) {
        return SF.submit(url, Finder.getFor(prefix).requestDataForSearch());
    }
    exports.createUserQuery = createUserQuery;
});
//# sourceMappingURL=UserQuery.js.map
