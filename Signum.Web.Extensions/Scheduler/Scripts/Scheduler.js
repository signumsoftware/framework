/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function initDashboard(url) {
        $("#sfSchedulerDisable , #sfSchedulerEnable").click(function (e) {
            e.preventDefault();
            $.post($(this).attr("href"));
        });
    }
    exports.initDashboard = initDashboard;
});
//# sourceMappingURL=Scheduler.js.map
