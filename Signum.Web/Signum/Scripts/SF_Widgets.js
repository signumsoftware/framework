var SF = SF || {};

SF.Widgets = (function () {
    $(".sf-widget-li").live("mouseover mouseout", function (evt) {
        if (evt.type == "mouseover") {
            $(this).addClass("sf-widget-li-active");
        }
        else {
            $(this).removeClass("sf-widget-li-active");
        }
    });
})();