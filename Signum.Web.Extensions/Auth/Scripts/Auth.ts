/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")


export function saveNew(options: Operations.EntityOperationOptions, url: string) {
    options.controllerUrl = url;
    options.avoidValidate = true;
    Operations.executeDefault(options);
}


export function setPassword(options: Operations.EntityOperationOptions, urlModel: string, urlSetPassword: string) {

    var passPrefix = options.prefix.child("Pass")

    Navigator.viewPopup(Entities.EntityHtml.withoutType(passPrefix), {
        controllerUrl: urlModel,
        allowErrors: Navigator.AllowErrors.No
    }).then(eHtml => {
        if (eHtml == null)
            return;

        options.requestExtraJsonData = $.extend({ passPrefix: passPrefix }, Validator.getFormValuesHtml(eHtml));
        options.controllerUrl = urlSetPassword;

        Operations.executeDefault(options);
    });
}