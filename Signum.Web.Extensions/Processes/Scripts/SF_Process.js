/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
var SF;
(function (SF) {
    (function (Process) {
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
        Process.initControlPanel = initControlPanel;

        function refreshUpdate(idProcess, prefix, getProgressUrl) {
            setTimeout(function () {
                $.post(getProgressUrl, { id: idProcess }, function (data) {
                    $("#progressBar").width(data + '%');
                    if (data < 100) {
                        refreshUpdate(idProcess, prefix, getProgressUrl);
                    } else {
                        if (SF.isEmpty(prefix)) {
                            window.location.reload();
                        } else {
                            var oldViewNav = new SF.ViewNavigator({ prefix: prefix });
                            var tempDivId = oldViewNav.tempDivId();

                            SF.closePopup(prefix);

                            new SF.ViewNavigator({
                                type: "Process",
                                id: idProcess,
                                prefix: prefix,
                                containerDiv: tempDivId
                            }).viewSave();
                        }
                    }
                });
            }, 2000);
        }
        Process.refreshUpdate = refreshUpdate;
    })(SF.Process || (SF.Process = {}));
    var Process = SF.Process;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Process.js.map
