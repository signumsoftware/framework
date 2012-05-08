var SF = SF || {};

SF.Widgets = (function () {
    var highlightClass = "sf-alert-active";

    $(".sf-widget").live("mouseover mouseout", function (evt) {
        var $this = $(this);
        if (evt.type == "mouseover") {
            SF.Dropdowns.toggle(evt, this);
            var $content = $this.find(".sf-widget-content");
            $content.css({
                top: $this.outerHeight(),
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
                updateCountAndHighlight($alertsWidget, "warned", jsonNewCount.warned);
                updateCountAndHighlight($alertsWidget, "future", jsonNewCount.future);
                updateCountAndHighlight($alertsWidget, "attended", jsonNewCount.attended);
                window.alert(successMessage);
            }
        });
    };

    var updateCountAndHighlight = function ($alertsWidget, key, count) {
        var $current = $alertsWidget.find(".sf-alert-" + key);
        $current.not(".sf-alert-count-label").html(count);
        if (count > 0) {
            $current.addClass(highlightClass);
        }
    };

    return {
        onNoteCreated: onNoteCreated,
        onAlertCreated: onAlertCreated
    };
})();