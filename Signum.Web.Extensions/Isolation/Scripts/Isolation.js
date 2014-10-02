/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Navigator"], function(require, exports, Navigator) {
    function addIsolationPrefilter(isolationKey) {
        SF.registerAjaxExtraParameters(function (originalParams) {
            $.extend(originalParams, { Isolation: getCurrentIsolation(originalParams["prefix"] || "") });
        });
    }
    exports.addIsolationPrefilter = addIsolationPrefilter;

    function getIsolation(extraJsonData, prefix, title, isolations) {
        var iso = getCurrentIsolation(prefix);

        if (iso != null)
            return Promise.resolve($.extend(extraJsonData, { Isolation: iso }));

        return Navigator.chooser(prefix, title, isolations).then(function (co) {
            if (!co)
                return null;

            return $.extend(extraJsonData, { Isolation: co.value });
        });
    }
    exports.getIsolation = getIsolation;

    function getCurrentIsolation(prefix) {
        while (true) {
            var elem = prefix.child("Isolation").tryGet();

            if (elem.length)
                return elem.val();

            if (prefix)
                prefix = prefix.parent();
            else
                return null;
        }
    }
});
//# sourceMappingURL=Isolation.js.map
