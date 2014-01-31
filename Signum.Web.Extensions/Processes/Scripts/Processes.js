/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Navigator"], function(require, exports, Navigator) {
    function initControlPanel(refreshCallback) {
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
    exports.initControlPanel = initControlPanel;

    function refreshUpdate(idProcess, prefix, getProgressUrl) {
        setTimeout(function () {
            $.post(getProgressUrl, { id: idProcess }, function (data) {
                $("#progressBar").width(data + '%');
                if (data < 100) {
                    exports.refreshUpdate(idProcess, prefix, getProgressUrl);
                } else {
                    Navigator.requestAndReload(prefix);
                }
            });
        }, 2000);
    }
    exports.refreshUpdate = refreshUpdate;
});
//# sourceMappingURL=Processes.js.map
