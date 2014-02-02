/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")


export function saveNew(options: Operations.EntityOperationOptions, url: string) {
    options.controllerUrl = url;

    Operations.executeDefault(options); 
}


export function setPassword(options: Operations.EntityOperationOptions, urlModel: string, urlSetPassword: string) {

    var passPrefix = SF.compose(options.prefix, "Pass")

    Navigator.viewPopup(Entities.EntityHtml.withoutType(passPrefix), {
        controllerUrl: urlModel,
    }).then(eHtml => {
            if (eHtml == null)
                return;

            options.requestExtraJsonData = $.extend({ passPrefix: passPrefix }, eHtml.html.serializeObject());
            options.controllerUrl = urlSetPassword;

            Operations.executeDefault(options);
        });
}