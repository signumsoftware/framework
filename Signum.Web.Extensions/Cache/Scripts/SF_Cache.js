/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
var SF;
(function (SF) {
    (function (Cache) {
        var refresh;

        function init(refreshCallback) {
            refresh = refreshCallback;
        }
        Cache.init = init;
        ;

        function initStats() {
            $(document).on("click", "table.sf-stats-table a.sf-stats-show", function (e) {
                e.preventDefault();
                $(this).closest("tr").next().toggle();
            });
        }
        Cache.initStats = initStats;
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
    })(SF.Cache || (SF.Cache = {}));
    var Cache = SF.Cache;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Cache.js.map
