/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Validator"], function(require, exports, Validator) {
    function updateColor(input) {
        var $input = $(input);
        $input.closest("tr").find("div.sf-chart-color-box").css("background-color", "#" + ($input.val() || "FFFFFF"));
    }

    function init() {
        $(function () {
            $("input.sf-chart-color-input").each(function (i, input) {
                updateColor(input);
            }).change(function () {
                updateColor(this);
            });

            $("#sfChartSavePalette").on("click", function (e) {
                e.preventDefault();
            });
        });
    }
    exports.init = init;

    function savePalette(url) {
        SF.ajaxPost({
            url: url,
            data: $("#divMainControl :input").serialize()
        }).then(function (result) {
            if (typeof result.ModelState != "undefined") {
                Validator.showErrors(null, result.ModelState, false);
            }
        });
    }
    exports.savePalette = savePalette;
    ;
});
//# sourceMappingURL=ChartColors.js.map
