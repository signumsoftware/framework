/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")

function createNewPart(prefix: string, url: string, typesOptions: Navigator.ChooserOption[]) {

    Navigator.chooser(prefix, lang.signum.chooseAType, typesOptions).then(a=> {
        if (a == null)
            return;

        SF.ajaxPost({
            url: url,
            data: $.extend({ newPartType: a.value }, Validator.getFormValues(prefix))
        }).then(html=>
                Navigator.reload(Entities.EntityHtml.fromHtml(prefix, html)));

    }); 
}