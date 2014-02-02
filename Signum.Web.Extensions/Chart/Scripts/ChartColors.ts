/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
/// <reference path="SF_Chart_Utils.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")



function updateColor(input: Element) {
    var $input = $(input);
    $input.closest("tr").find("div.sf-chart-color-box").css("background-color", "#" + ($input.val() || "FFFFFF"));
}

export function init() {

    $(function () {
        $("input.sf-chart-color-input")
            .each(function (i, input) { updateColor(input); })
            .change(function () { updateColor(this); });

        $("#sfChartSavePalette").on("click", function (e) {
            e.preventDefault();

        });
    });
}

export function savePalette(url: string) {

    SF.ajaxPost({
        url: url,
        data: $("#divMainControl :input").serialize(),
    }).then(result => {
            if (typeof result.ModelState != "undefined") {
                Validator.showErrors(null, result.ModelState, false);
            }
        }); 
};