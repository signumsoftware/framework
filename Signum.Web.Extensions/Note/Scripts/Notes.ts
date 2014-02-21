/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function explore(prefix: string, options: Finder.FindOptions){
    Finder.explore(options).then(() => updateNotes(prefix));
}

export function createNote(prefix: string, operationKey: string) {
    Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }).then(() => updateNotes(prefix));
}


export function updateNotes(prefix) {
    var widget = $("#" + SF.compose(prefix, "notesWidget") + " ul")

    SF.ajaxPost({
        url: widget.data("url"),
        data: { key: Entities.RuntimeInfo.getFromPrefix(prefix).key() },
    }).then(txt=> widget.parent().find(".sf-widget-count").text(txt));
}
