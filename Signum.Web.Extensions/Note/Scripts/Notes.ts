/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function explore(prefix: string, options: Finder.FindOptions, urlUpdate: string){
    Finder.explore(options)
        .then(() => updateNotes(prefix, urlUpdate));
}

export function createNote(event: MouseEvent, prefix: string, operationKey: string, urlUpdate: string) {
    Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }, event)
        .then(() => updateNotes(prefix, urlUpdate));
}


export function updateNotes(prefix : string, urlUpdate: string) {
    var widget = prefix.child("notesWidget").get()

    SF.ajaxPost({
        url: urlUpdate,
        data: { key: Entities.RuntimeInfo.getFromPrefix(prefix).key() },
    }).then(txt=> widget.parent().find(".sf-widget-count").text(txt));
}
