/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Entities, Finder, Operations) {
    function exploreAlerts(prefix, findOptions, updateUrl) {
        Finder.explore(findOptions).then(function () {
            return updateAlerts(prefix, updateUrl);
        });
    }
    exports.exploreAlerts = exploreAlerts;

    function createAlert(event, prefix, operationKey, updateUrl) {
        Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }, event).then(function () {
            return updateAlerts(prefix, updateUrl);
        });
    }
    exports.createAlert = createAlert;

    function updateAlerts(prefix, updateUrl) {
        var widget = prefix.child("alertsWidget").get().parent();

        SF.ajaxPost({
            url: updateUrl,
            data: { key: Entities.RuntimeInfo.getFromPrefix(prefix).key() }
        }).then(function (jsonNewCount) {
            updateCountAndHighlight(widget, "attended", jsonNewCount.Attended);
            updateCountAndHighlight(widget, "alerted", jsonNewCount.Alerted);
            updateCountAndHighlight(widget, "future", jsonNewCount.Future);
        });
    }

    var highlightClass = "sf-alert-active";
    function updateCountAndHighlight($alertsWidget, key, count) {
        var $current = $alertsWidget.find(".sf-alert-" + key);
        $current.not(".sf-alert-count-label").html(count.toString());
        $current.toggleClass(highlightClass, count > 0);
    }
});
//# sourceMappingURL=Alerts.js.map
