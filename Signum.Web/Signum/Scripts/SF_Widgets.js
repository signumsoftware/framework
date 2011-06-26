var SF = SF || {};

SF.Widgets = (function () {
    //var highlightClass = "ui-state-highlight";

    $(".sf-widget").live("mouseover mouseout", function (evt) {
        var $this = $(this);
        if (evt.type == "mouseover") {
            SF.Dropdowns.toggle(evt, this);
            var $content = $this.find(".sf-widget-content");
            $content.css({
                top: $this.height() + 4, /*4 = .sf-widget padding-top + padding-bottom*/
                left: ($this.width() - $content.width())
            });
        }
        else {
            SF.Dropdowns.toggle(evt, this);
        }
    });

    $(".sf-widget").live("click", function (evt) {
        SF.Dropdowns.toggle(evt, this);
    });

    var onNoteCreated = function (url, prefix, successMessage) {
        $.ajax({
            url: url,
            data: { sfRuntimeInfo: new SF.RuntimeInfo(prefix).find().val() },
            success: function (newCount) {
                var $notesWidget = $("#" + SF.compose(prefix, "notesWidget"));
                //$notesWidget.find(".sf-widget-toggler").addClass(highlightClass);
                $notesWidget.find(".sf-widget-count").html(newCount);
                window.alert(successMessage);
            }
        });
    };

    var onAlertCreated = function (url, prefix, successMessage) {
        $.ajax({
            url: url,
            data: { sfRuntimeInfo: new SF.RuntimeInfo(prefix).find().val() },
            success: function (jsonNewCount) {
                var $alertsWidget = $("#" + SF.compose(prefix, "alertsWidget"));
                $alertsWidget.find(".sf-alert-warned").not(".sf-alert-count-label").html(jsonNewCount.warned);
//                if (jsonNewCount.warned > 0) {
//                    $alertsWidget.find(".sf-widget-toggler").addClass(highlightClass);   
//                }
                $alertsWidget.find(".sf-alert-future").not(".sf-alert-count-label").html(jsonNewCount.future);
                $alertsWidget.find(".sf-alert-attended").not(".sf-alert-count-label").html(jsonNewCount.attended);
                window.alert(successMessage);
            }
        });
    };

    return {
        onNoteCreated: onNoteCreated,
        onAlertCreated: onAlertCreated
    };
})();