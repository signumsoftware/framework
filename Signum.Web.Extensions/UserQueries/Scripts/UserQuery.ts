
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")


once("SF-UserQuery",() => {
    $(document).on("click", ".sf-userquery", function (e) {
        e.preventDefault();
        Finder.getFor("").then(sc=> {
            sc.requestDataForSearchInUrl().then(foUrl=> {
                var url = $(this).attr("href") + foUrl;

                if (e.ctrlKey || e.which == 2) {
                    window.open(url);
                }
                else if (e.which == 1) {
                    window.location.href = url;
                }
            });
        });
    });
});

export function deleteUserQuery(options: Operations.EntityOperationOptions, urlRedirect: string) {

    options.avoidReturnRedirect = true;

    Operations.deleteDefault(options).then(() => {
        if (!options.prefix)
            window.location.href = urlRedirect;
    });
}

export function createUserQuery(prefix: string, url: string) {
    return Finder.getFor(prefix)
        .then(sc=> sc.requestDataForSearch(Finder.RequestType.QueryRequest))
        .then(data=> SF.submit(url, data));
}

