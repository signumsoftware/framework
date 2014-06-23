/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Navigator, Operations) {
    function initDashboard(url) {
        var refreshCallback = function () {
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
    exports.initDashboard = initDashboard;

    function refreshPage(prefix) {
        setTimeout(function () {
            return Navigator.requestAndReload(prefix);
        }, 500);
    }
    exports.refreshPage = refreshPage;

    function refreshProgress(idProcess, prefix, getProgressUrl) {
        setTimeout(function () {
            return $.post(getProgressUrl, { id: idProcess }, function (data) {
                $("#progressBar").width(data * 100 + '%').attr("aria-valuenow", data * 100);
                if (data < 1) {
                    exports.refreshProgress(idProcess, prefix, getProgressUrl);
                } else {
                    Navigator.requestAndReload(prefix);
                }
            });
        }, 500);
    }
    exports.refreshProgress = refreshProgress;

    function processFromMany(options, event) {
        options.controllerUrl = SF.Urls.processFromMany;

        return Operations.constructFromManyDefault(options, event);
    }
    exports.processFromMany = processFromMany;
});
//# sourceMappingURL=Processes.js.map
