/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator"], function(require, exports, Entities, Navigator) {
    function attachGridControl(gridRepeater, url, types) {
        gridRepeater.creating = function (prefix) {
            return Navigator.typeChooser(prefix.child("New"), types).then(function (type) {
                if (type == null)
                    return null;

                return SF.ajaxPost({
                    url: url,
                    data: {
                        prefix: prefix,
                        rootType: gridRepeater.options.rootType,
                        propertyRoute: gridRepeater.options.propertyRoute,
                        partialViewName: gridRepeater.options.partialViewName,
                        newPartType: type.name
                    }
                }).then(function (html) {
                    var result = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(gridRepeater.singleType(), 0, true));
                    result.loadHtml(html);
                    return result;
                });
            });
        };

        gridRepeater.element.on("change", "select[name$=_Style]", function (e) {
            var select = $(e.currentTarget);
            var panel = select.closest(".panel");
            panel.removeClass("panel-default panel-primary panel-success panel-info panel-warning panel-danger");
            panel.addClass("panel-" + select.val().toLowerCase());
        });
    }
    exports.attachGridControl = attachGridControl;
});
//# sourceMappingURL=Dashboard.js.map
