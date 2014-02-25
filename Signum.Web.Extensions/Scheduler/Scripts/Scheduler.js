/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function initControlPanel(url) {
        $("#sfSchedulerDisable , #sfSchedulerEnable").click(function (e) {
            e.preventDefault();
            $.post($(this).attr("href"));
        });
    }
    exports.initControlPanel = initControlPanel;
});
//# sourceMappingURL=Scheduler.js.map
