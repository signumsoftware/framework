var SF = SF || {};

SF.Cache = (function () {
    var $cacheEnable = $("#sfCacheEnable");
    var $cacheDisable = $("#sfCacheDisable");
    var refresh;

    var init = function(refreshCallback){
      refresh= refreshCallback;
    };

    var initStats = function () {
        $(document).on("click", "table.sf-stats-table a.sf-stats-show", function (e) {
            e.preventDefault();
            $(this).closest("tr").next().toggle();
        });
    };

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

    return {
        initStats: initStats,
        init: init 
    };
})(); 