/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

function refresh() {
    location.href = location.href;
};

export function init() {

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
}
