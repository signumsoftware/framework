/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
var SF;
(function (SF) {
    (function (Process) {
        var $processEnable = $("#sfProcessEnable");
        var $processDisable = $("#sfProcessDisable");
        var refresh;

        function init(refreshCallback) {
            refresh = refreshCallback;
        }
        Process.init = init;

        once("SF-Process", function () {
            $processEnable.click(function (e) {
                e.preventDefault();
                $.ajax({
                    url: $(this).attr("href"),
                    success: function () {
                        $processEnable.hide();
                        $processDisable.show();
                        refresh();
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
                        refresh();
                    }
                });
            });
        });
    })(SF.Process || (SF.Process = {}));
    var Process = SF.Process;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Process.js.map
