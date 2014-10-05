/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function addIsolationPrefilter(isolationKey: string)
{
    SF.registerAjaxExtraParameters((originalParams: FormObject) => {
        $.extend(originalParams, { Isolation: getCurrentIsolation(originalParams["prefix"] || "") });
    });
}

export function getIsolation(extraJsonData: FormObject, prefix: string, title: string, isolations: Navigator.ChooserOption[]): Promise<FormObject> {

    var iso = getCurrentIsolation(prefix);

    if (iso != null)
        return Promise.resolve(<any>$.extend(extraJsonData, { Isolation: iso }));

    return Navigator.chooser(prefix, title, isolations).then(co=> {
        if (!co)
            return null;

        return <any>$.extend(extraJsonData, { Isolation: co.value })
    });
}

function getCurrentIsolation(prefix: string) {

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