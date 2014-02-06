/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function initControlPanel(url : string) {

    var refreshCallback = () => {
        $.get(url, function (data) {
            $("div.processMainDiv").replaceWith(data);
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


export function refreshUpdate(idProcess: string, prefix: string, getProgressUrl: string) {
    setTimeout(function () {
        $.post(getProgressUrl, { id: idProcess }, function (data) {
            $("#progressBar").width(data + '%');
            if (data < 100) {
                refreshUpdate(idProcess, prefix, getProgressUrl);
            }
            else {
                Navigator.requestAndReload(prefix);
            }
        });
    }, 2000);
}

export function processFromMany(options: Operations.EntityOperationOptions ) : Promise<void> {
    options.controllerUrl = SF.Urls.processFromMany; 

    return Operations.constructFromManyDefault(options); 
}
