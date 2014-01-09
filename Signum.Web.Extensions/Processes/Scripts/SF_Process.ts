/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>

module SF.Process {
    var $processEnable = $("#sfProcessEnable");
    var $processDisable = $("#sfProcessDisable");
    var refresh: () => void;

    export function init(refreshCallback: () => void) {
        refresh = refreshCallback;
    }

    once("SF-Process", () => {
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
}