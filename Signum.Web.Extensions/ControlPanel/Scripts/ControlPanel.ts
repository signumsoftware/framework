/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import GridRepeater = require("Extensions/Signum.Web.Extensions/ControlPanel/Scripts/GridRepeater")


export function attachGridControl(gridRepeater: GridRepeater.GridRepeater, url: string, typesOptions: Navigator.ChooserOption[]) {

    gridRepeater.creating = prefix => {
        return Navigator.typeChooser(prefix.child("New"), typesOptions).then(type=> {
            if (type == null)
                return null;

            return SF.ajaxPost({
                url: url,
                data: $.extend({
                    prefix: prefix,
                    rootType: gridRepeater.options.rootType,
                    propertyRoute: gridRepeater.options.propertyRoute,
                    partialViewName: gridRepeater.options.partialViewName,
                    newPartType: type,
                }, Validator.getFormValues(prefix))
            }).then(html=> {
                var result = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(gridRepeater.singleType(), 0, true));
                result.loadHtml(html)
                return result;
            });
        });
    }; 


    gridRepeater.element.on("change", "select[name$=_Style]", e => {
        var select = $(e.currentTarget);
        var panel = select.closest(".panel");
        panel.removeClass("panel-default panel-primary panel-success panel-info panel-warning panel-danger"); 
        panel.addClass("panel-" + (<string>select.val()).toLowerCase());
    }); 
}