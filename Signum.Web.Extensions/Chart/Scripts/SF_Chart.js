var SF = SF || {};

SF.Chart = (function () {
    $(".sf-chart-img").live("click", function () {
        var $this = $(this);
        var $chartControl = $this.closest(".sf-chart-control");
        var $chartType = $this.closest(".sf-chart-type");
        $chartType.find(".ui-widget-header hidden").val($this.attr("data-related"));
        $.ajax({
            url: $chartType.attr("data-url"),
            data: $chartControl.find("input").serialize(),
            success: function (result) {
                $this.closest(".sf-chart-builder").replaceWith(result);
            }
        });
    });
})();