/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function exploreAlerts(prefix : string, column : string) {
    var findOptions: Finder.FindOptions = JSON.parse($("#" + SF.compose(prefix, "alertsWidget") + " ul").data("findOptions"));

    findOptions.filters.push({ columnName: "Entity." + column, operation: Finder.FilterOperation.EqualTo, value: "true" }); 

    Finder.explore(findOptions).then(() => updateAlerts(prefix)); 
}

export function createAlert(prefix: string, operationKey: string) {

    Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }).then(() => updateAlerts(prefix));
}

function updateAlerts(prefix: string) {
    var widget = $("#" + SF.compose(prefix, "alertsWidget") + " ul"); 

    $.ajax({
        url: widget.data("url"),
        data: { sfRuntimeInfo: new Entities.RuntimeInfoElement(prefix).getElem().val() },
        success: function (jsonNewCount) {
            updateCountAndHighlight(widget, "warned", jsonNewCount.warned);
            updateCountAndHighlight(widget, "future", jsonNewCount.future);
            updateCountAndHighlight(widget, "attended", jsonNewCount.attended);
        }
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
