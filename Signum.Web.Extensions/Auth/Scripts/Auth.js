/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Operations", "Framework/Signum.Web/Signum/Scripts/Validator"], function(require, exports, Entities, Navigator, Operations, Validator) {
    function saveNew(options, url) {
        options.controllerUrl = url;
        options.avoidValidate = true;
        Operations.executeDefault(options);
    }
    exports.saveNew = saveNew;

    function setPassword(options, urlModel, urlSetPassword) {
        var passPrefix = options.prefix.child("Pass");

        Navigator.viewPopup(Entities.EntityHtml.withoutType(passPrefix), {
            controllerUrl: urlModel,
            allowErrors: 2 /* No */
        }).then(function (eHtml) {
            if (eHtml == null)
                return;

            options.requestExtraJsonData = $.extend({ passPrefix: passPrefix }, Validator.getFormValuesHtml(eHtml));
            options.controllerUrl = urlSetPassword;

            Operations.executeDefault(options);
        });
    }
    exports.setPassword = setPassword;
});
//# sourceMappingURL=Auth.js.map
