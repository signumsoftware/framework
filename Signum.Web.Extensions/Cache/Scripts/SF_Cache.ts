/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/> 


module SF.Cache {

    var refresh: () => void;

    export function init (refreshCallback : ()=> void) {
        refresh = refreshCallback;
    };

    export function initStats() {
        $(document).on("click", "table.sf-stats-table a.sf-stats-show", function (e) {
            e.preventDefault();
            $(this).closest("tr").next().toggle();
        });
    };

    once("SF-Cache", () => {

        var $cacheEnable = $("#sfCacheEnable");
        var $cacheDisable = $("#sfCacheDisable");


        $("#sfCacheClear").click(function (e) {
            e.preventDefault();
            $.ajax({
                url: $(this).attr("href"),
                success: function () {
                    refresh();
                }
            });
        });

        $cacheEnable.click(function (e) {
            e.preventDefault();
            $.ajax({
                url: $(this).attr("href"),
                success: function () {
                    $cacheEnable.hide();
                    $cacheDisable.show();
                    refresh();
                }
            });
        });

        $cacheDisable.click(function (e) {
            e.preventDefault();
            $.ajax({
                url: $(this).attr("href"),
                success: function () {
                    $cacheDisable.hide();
                    $cacheEnable.show();
                    refresh();
                }
            });
        });
    });
}