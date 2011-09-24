var SF = SF || {};

SF.Chart = SF.Chart || {};

SF.Chart.Builder = (function () {

    var updateChartBuilder = function ($chartControl) {
        var $chartBuilder = $chartControl.find(".sf-chart-builder");
        $.ajax({
            url: $chartBuilder.attr("data-url"),
            data: $chartControl.find(":input").serialize(),
            success: function (result) {
                $chartBuilder.replaceWith(result);
                SF.triggerNewContent($chartControl.find(".sf-chart-builder"));
            }
        });
    };

    $(".sf-chart-img").live("click", function () {
        var $this = $(this);
        $this.closest(".sf-chart-type").find(".ui-widget-header :hidden").val($this.attr("data-related"));
        var $chartControl = $this.closest(".sf-chart-control");
        updateChartBuilder($chartControl);

        var $resultsContainer = $chartControl.find(".sf-search-results-container");
        if ($this.hasClass("sf-chart-img-equiv")) {
            if ($resultsContainer.find("svg").length > 0) {
                //TODO do not call server again to regenerate chart
                $chartControl.find(".sf-chart-draw").click();
            }
        }
        else {
            $resultsContainer.html("");
        }
    });

    $(".sf-chart-group-trigger").live("change", function () {
        var $this = $(this);
        $this.closest(".sf-chart-builder").find(".sf-chart-group-results").val($this.is(":checked"));
        updateChartBuilder($this.closest(".sf-chart-control"));
    });

    $(".sf-chart-draw").live("click", function (e) {
        e.preventDefault();
        var $this = $(this);
        var $chartControl = $this.closest(".sf-chart-control");
        $.ajax({
            url: $this.attr("data-url"),
            data: $chartControl.find(":input").serialize(),
            success: function (result) {
                if (typeof result === "object") {
                    if (typeof result.ModelState != "undefined") {
                        var modelState = result.ModelState;
                        returnValue = new SF.Validator().showErrors(modelState, true);
                        SF.Notify.error(lang.signum.error, 2000);
                    }
                }
                else {
                    $chartControl.find(".sf-search-results-container").html(result);
                    SF.triggerNewContent($chartControl.find(".sf-search-results-container"));
                }
            }
        });
    });
})();