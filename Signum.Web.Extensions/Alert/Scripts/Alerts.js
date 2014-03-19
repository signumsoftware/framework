/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Entities, Finder, Operations) {
    function exploreAlerts(prefix, column, findOptions) {
        findOptions.filters.push({ columnName: "Entity." + column, operation: 0 /* EqualTo */, value: "true" });

        Finder.explore(findOptions).then(function () {
            return updateAlerts(prefix);
        });
    }
    exports.exploreAlerts = exploreAlerts;

    function createAlert(prefix, operationKey) {
        Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }).then(function () {
            return updateAlerts(prefix);
        });
    }
    exports.createAlert = createAlert;

    function updateAlerts(prefix) {
        var widget = $("#" + SF.compose(prefix, "alertsWidget") + " ul");

        SF.ajaxPost({
            url: widget.data("url"),
            data: { key: Entities.RuntimeInfo.getFromPrefix(prefix).key() }
        }).then(function (jsonNewCount) {
            updateCountAndHighlight(widget.parent(), "attended", jsonNewCount.Attended);
            updateCountAndHighlight(widget.parent(), "alerted", jsonNewCount.Alerted);
            updateCountAndHighlight(widget.parent(), "future", jsonNewCount.Future);
        });
    }

    var highlightClass = "sf-alert-active";
    function updateCountAndHighlight($alertsWidget, key, count) {
        var $current = $alertsWidget.find(".sf-alert-" + key);
        $current.not(".sf-alert-count-label").html(count);
        if (count > 0) {
            $current.addClass(highlightClass);
        }
    }
});
//# sourceMappingURL=Alerts.js.map
