/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function exploreAlerts(prefix : string, findOptions : Finder.FindOptions, updateUrl: string) {

    Finder.explore(findOptions).then(() => updateAlerts(prefix, updateUrl)); 
}

export function createAlert(event: MouseEvent, prefix: string, operationKey: string, updateUrl: string) {

    Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }, event)
        .then(() => updateAlerts(prefix, updateUrl));
}

function updateAlerts(prefix: string, updateUrl: string) {
    var widget = prefix.child("alertsWidget").get().parent(); 

    SF.ajaxPost({
        url: updateUrl,
        data: { key: Entities.RuntimeInfo.getFromPrefix(prefix).key() },
    }).then(function (jsonNewCount) {
            updateCountAndHighlight(widget, "attended", jsonNewCount.Attended);
            updateCountAndHighlight(widget, "alerted", jsonNewCount.Alerted);
            updateCountAndHighlight(widget, "future", jsonNewCount.Future);
        });
}

var highlightClass = "sf-alert-active";
function updateCountAndHighlight($alertsWidget : JQuery, key: string, count : number) {
    var $current = $alertsWidget.find(".sf-alert-" + key);
    $current.not(".sf-alert-count-label").html(count.toString());
    $current.toggleClass(highlightClass, count > 0);
}
