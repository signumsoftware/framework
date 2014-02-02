/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Entities, Navigator, Operations) {
    function saveNew(options, url) {
        options.controllerUrl = url;

        Operations.executeDefault(options);
    }
    exports.saveNew = saveNew;

    function setPassword(options, urlModel, urlSetPassword) {
        var passPrefix = SF.compose(options.prefix, "Pass");

        Navigator.viewPopup(Entities.EntityHtml.withoutType(passPrefix), {
            controllerUrl: urlModel
        }).then(function (eHtml) {
            if (eHtml == null)
                return;

            options.requestExtraJsonData = $.extend({ passPrefix: passPrefix }, eHtml.html.serializeObject());
            options.controllerUrl = urlSetPassword;

            Operations.executeDefault(options);
        });
    }
    exports.setPassword = setPassword;
});
//# sourceMappingURL=Auth.js.map
