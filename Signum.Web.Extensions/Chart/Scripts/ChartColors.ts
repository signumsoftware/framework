/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")


function updateColor(input: Element) {
    var $input = $(input);
    $input.closest("tr").find("div.sf-chart-color-box").css("background-color", "#" + ($input.val() || "FFFFFF"));
}


export function savePalette(url: string) {

    SF.ajaxPost({
        url: url,
        data: Validator.getFormValues(""),
    }).then(result => {
        Validator.assertModelStateErrors(result, "")

        Navigator.reloadMain(Entities.EntityHtml.fromHtml("", result));
    });
};

export function createPalette(url: string, palette : string[], chooseAPalette : string) {

    Navigator.chooser("palette", chooseAPalette, palette).then(p=> {
        if (!p)
            return;

        SF.ajaxPost({
            url: url,
            data: $.extend(Validator.getFormValues(""), {palette: p})
        }).then(result => {
            Navigator.reloadMain(Entities.EntityHtml.fromHtml("", result));
        });
    }); 
};

export function deletePalette(url: string) {
    SF.ajaxPost({ 
        url: url
    }).then(result => {
        Navigator.reloadMain(Entities.EntityHtml.fromHtml("", result));
    });
};