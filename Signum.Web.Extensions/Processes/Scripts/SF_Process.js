var SF = SF || {};

SF.Process = (function () {
    var $processEnable = $("#sfProcessEnable");
    var $processDisable = $("#sfProcessDisable");
    var refresh;

    var init = function(refreshCallback){
      refresh= refreshCallback;
    };


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

    return {
        init: init 
    };
})(); 