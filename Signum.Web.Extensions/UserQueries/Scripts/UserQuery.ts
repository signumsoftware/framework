
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")


once("SF-UserQuery", () => {
    $(document).on("click", ".sf-userquery", function (e) {
        e.preventDefault();
        Finder.getFor("").then(sc=> {
            var findOptionsQueryString = sc.requestDataForSearchInUrl();

            var url = $(this).attr("href") + findOptionsQueryString;

            if (e.ctrlKey || e.which == 2) {
                window.open(url);
            }
            else if (e.which == 1) {
                window.location.href = url;
            }
        });
    });
});

export function attachShowCurrentEntity(el: Lines.EntityLine) {
    var showOnEntity = function () {
        el.element.nextAll("p.messageEntity").toggle(!!Entities.RuntimeInfo.getFromPrefix(el.options.prefix));
    };

    showOnEntity();

    el.entityChanged = showOnEntity;
}

export function deleteUserQuery(options: Operations.EntityOperationOptions, urlRedirect: string) {

    options.avoidReturnRedirect = true;

    if (!Operations.confirmIfNecessary(options))
        return;

    Operations.deleteDefault(options).then(() => {
        if (!options.prefix)
            window.location.href = urlRedirect;
    });
} 

export function saveUserQuery(os: Operations.EntityOperationOptions, url: string) {

    os.controllerUrl = url;

    Operations.executeDefault(os); 
} 

export function createUserQuery(prefix: string, url: string) {
    return Finder.getFor(prefix).then(sc=>
        SF.submit(url, sc.requestDataForSearch())); 
}

