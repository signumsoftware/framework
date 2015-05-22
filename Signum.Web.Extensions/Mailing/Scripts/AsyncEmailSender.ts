 /// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function initDashboard(url: string) {

    var refreshCallback = () => {
        $.get(url, function (data) {
            $("div#emailAsyncMainDiv").replaceWith(data);
        });
    };

    var $processEnable = $("#sfEmailAsyncProcessEnable");
    var $processDisable = $("#sfEmailAsyncProcessDisable");

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