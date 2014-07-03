/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Validator", "Framework/Signum.Web/Signum/Scripts/Navigator"], function(require, exports, Entities, Validator, Navigator) {
    function updateColor(input) {
        var $input = $(input);
        $input.closest("tr").find("div.sf-chart-color-box").css("background-color", "#" + ($input.val() || "FFFFFF"));
    }

    function savePalette(url) {
        SF.ajaxPost({
            url: url,
            data: Validator.getFormValues("")
        }).then(function (result) {
            Validator.assertModelStateErrors(result, "");

            Navigator.reloadMain(Entities.EntityHtml.fromHtml("", result));
        });
    }
    exports.savePalette = savePalette;
    ;

    function createPalette(url, palette, chooseAPalette) {
        Navigator.chooser("palette", chooseAPalette, palette).then(function (p) {
            if (!p)
                return;

            SF.ajaxPost({
                url: url,
                data: $.extend(Validator.getFormValues(""), { palette: p })
            }).then(function (result) {
                Navigator.reloadMain(Entities.EntityHtml.fromHtml("", result));
            });
        });
    }
    exports.createPalette = createPalette;
    ;

    function deletePalette(url) {
        SF.ajaxPost({
            url: url
        }).then(function (result) {
            Navigator.reloadMain(Entities.EntityHtml.fromHtml("", result));
        });
    }
    exports.deletePalette = deletePalette;
    ;
});
//# sourceMappingURL=ChartColors.js.map
