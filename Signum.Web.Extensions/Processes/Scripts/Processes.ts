/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")

export function initControlPanel(refreshCallback: () => void) {

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
