/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    var refresh;

    function init(refreshCallback) {
        refresh = refreshCallback;
    }
    exports.init = init;
    ;

    function initStats() {
        $(document).on("click", "table.sf-stats-table a.sf-stats-show", function (e) {
            e.preventDefault();
            $(this).closest("tr").next().toggle();
        });
    }
    exports.initStats = initStats;
    ;

    once("SF-Cache", function () {
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
});
//# sourceMappingURL=Cache.js.map
