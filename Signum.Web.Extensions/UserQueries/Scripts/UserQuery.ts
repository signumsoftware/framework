
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")


once("SF-UserQuery", () => {
    $(document).on("click", ".sf-userquery", function (e) {
        e.preventDefault();
        var findOptionsQueryString = Finder.getFor("").requestDataForSearchInUrl();
        var url = $(this).attr("href") + findOptionsQueryString;

        if (e.ctrlKey || e.which == 2) {
            window.open(url);
        }
        else if (e.which == 1) {
            window.location.href = url;
        }
    });
});

export function attachShowCurrentEntity(el: Lines.EntityLine) {
    var showOnEntity = function () {
        el.element.nextAll("p.messageEntity").toggle(!!el.runtimeInfo().value());
    };

    showOnEntity();

    el.entityChanged = showOnEntity;
}

export function deleteUserQuery(os: Operations.EntityOperationOptions, urlRedirect: string) {

    os.avoidReturnRedirect = true;

    Operations.deleteDefault(os).then(() => {
        if (!os.prefix)
            window.location.href = urlRedirect;
    });
} 

export function saveUserQuery(os: Operations.EntityOperationOptions, url: string) {

    os.controllerUrl = url;

    Operations.executeDefault(os); 
} 

export function createUserQuery(prefix: string, url: string) {
    return SF.submit(url, Finder.getFor(prefix).requestDataForSearch()); 
}

