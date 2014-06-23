/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function initDashboard(url: string) {
    $("#sfSchedulerDisable , #sfSchedulerEnable").click(function (e) {
        e.preventDefault();
        $.post($(this).attr("href"));
    });
}

