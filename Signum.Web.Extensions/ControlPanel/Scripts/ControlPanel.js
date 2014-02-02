/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Validator"], function(require, exports, Entities, Navigator, Validator) {
    function createNewPart(prefix, url, typesOptions) {
        Navigator.chooser(prefix, lang.signum.chooseAType, typesOptions).then(function (a) {
            if (a == null)
                return;

            SF.ajaxPost({
                url: url,
                data: $.extend({ newPartType: a.value }, Validator.getFormValues(prefix))
            }).then(function (html) {
                return Navigator.reload(Entities.EntityHtml.fromHtml(prefix, html));
            });
        });
    }
});
//# sourceMappingURL=ControlPanel.js.map
