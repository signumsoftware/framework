/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Entities, Finder, Operations) {
    once("SF-UserQuery", function () {
        $(document).on("click", ".sf-userquery", function (e) {
            var _this = this;
            e.preventDefault();
            Finder.getFor("").then(function (sc) {
                var findOptionsQueryString = sc.requestDataForSearchInUrl();

                var url = $(_this).attr("href") + findOptionsQueryString;

                if (e.ctrlKey || e.which == 2) {
                    window.open(url);
                } else if (e.which == 1) {
                    window.location.href = url;
                }
            });
        });
    });

    function attachShowCurrentEntity(el) {
        var showOnEntity = function () {
            el.element.nextAll("p.messageEntity").toggle(!!Entities.RuntimeInfo.getFromPrefix(el.options.prefix));
        };

        showOnEntity();

        el.entityChanged = showOnEntity;
    }
    exports.attachShowCurrentEntity = attachShowCurrentEntity;

    function deleteUserQuery(options, urlRedirect) {
        options.avoidReturnRedirect = true;

        if (!Operations.confirmIfNecessary(options))
            return;

        Operations.deleteDefault(options).then(function () {
            if (!options.prefix)
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
        return Finder.getFor(prefix).then(function (sc) {
            return SF.submit(url, sc.requestDataForSearch(0 /* QueryRequest */));
        });
    }
    exports.createUserQuery = createUserQuery;
});
//# sourceMappingURL=UserQuery.js.map
