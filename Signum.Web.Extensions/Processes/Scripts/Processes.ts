/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function initDashboard(url : string) {

    var refreshCallback = () => {
        $.get(url, function (data) {
            $("#processMainDiv").replaceWith(data);
        });
    };


    var $processEnable = $("#sfProcessEnable");
    var $processDisable = $("#sfProcessDisable");

    $processEnable.click(function (e) {
        e.preventDefault();
        $.ajax({
            url: $(this).attr("href"),
            success: function () {
                $processEnable.hide();
                $processDisable.show();
                refreshCallback();
            }
        });
    });

    $processDisable.click(function (e) {
        e.preventDefault();
        $.ajax({
            url: $(this).attr("href"),
            success: function () {
                $processDisable.hide();
                $processEnable.show();
                refreshCallback();
            }
        });
    });
}


export function refreshPage(prefix: string) {
    setTimeout(() => Navigator.requestAndReload(prefix), 500);
}


export function refreshProgress(idProcess: string, prefix: string, getProgressUrl: string) {
    setTimeout(() =>
        $.post(getProgressUrl, { id: idProcess }, function (data) {
            $("#progressBar").width(data * 100 + '%').attr("aria-valuenow", data * 100);
            if (data < 1) {
                refreshProgress(idProcess, prefix, getProgressUrl);
            }
            else {
                Navigator.requestAndReload(prefix);
            }
        })
        , 500);
}

export function processFromMany(options: Operations.EntityOperationOptions, event: MouseEvent) : Promise<void> {
    options.controllerUrl = SF.Urls.processFromMany; 

    return Operations.constructFromManyDefault(options, event); 
}
