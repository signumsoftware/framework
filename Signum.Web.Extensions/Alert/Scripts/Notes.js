/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities"], function(require, exports, Entities) {
    function onAlertCreated(url, prefix, successMessage) {
        $.ajax({
            url: url,
            data: { sfRuntimeInfo: new Entities.RuntimeInfoElement(prefix).getElem().val() },
            success: function (jsonNewCount) {
                var $alertsWidget = $("#" + SF.compose(prefix, "alertsWidget"));
                updateCountAndHighlight($alertsWidget, "warned", jsonNewCount.warned);
                updateCountAndHighlight($alertsWidget, "future", jsonNewCount.future);
                updateCountAndHighlight($alertsWidget, "attended", jsonNewCount.attended);
                window.alert(successMessage);
            }
        });
    }
    exports.onAlertCreated = onAlertCreated;

    function updateCountAndHighlight($alertsWidget, key, count) {
        var $current = $alertsWidget.find(".sf-alert-" + key);
        $current.not(".sf-alert-count-label").html(count);
        if (count > 0) {
            $current.addClass(highlightClass);
        }
    }
});
//# sourceMappingURL=Notes.js.map
