var SF = SF || {};

SF.Profiler = (function () {
    var $profileEnable = $("#sfProfileEnable");
    var $profileDisable = $("#sfProfileDisable");
    var refresh;

    var init = function(refreshCallback){
      refresh= refreshCallback;
    };

    var initStats = function () {
        $("table.sf-stats-table a.sf-stats-show").live("click", function (e) {
            e.preventDefault();
            $(this).closest("tr").next().toggle();
        });
    };

    $("#sfProfileClear").click(function (e) {
        e.preventDefault();
        $.ajax({
            url: $(this).attr("href"),
            success: function () {
                refresh();
            }
        });
    });

    $profileEnable.click(function (e) {
        e.preventDefault();
        $.ajax({
            url: $(this).attr("href"),
            success: function () {
                $profileEnable.hide();
                $profileDisable.show();
                refresh();
            }
        });
    });

    $profileDisable.click(function (e) {
        e.preventDefault();
        $.ajax({
            url: $(this).attr("href"),
            success: function () {
                $profileDisable.hide();
                $profileEnable.show();
                refresh();
            }
        });
    });

    return {
        initStats: initStats,
        init: init 
    };
})(); 