var SF = SF || {};

SF.Widgets = (function () {
    $(".sf-widget").live("mouseover mouseout", function (evt) {
        var $this = $(this);
        if (evt.type == "mouseover") {
            SF.Dropdowns.toggle(evt, this);
            var $content = $this.find(".sf-widget-content");
            $content.css({
                top: $this.height() + 8, /*8 = .sf-widget padding-top + padding-bottom*/
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
                $("#" + SF.compose(prefix, "notesWidget")).find(".sf-widget-count").html(newCount);
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
                $alertsWidget.find(".sf-alert-warned").html(jsonNewCount.warned);
                $alertsWidget.find(".sf-alert-future").html(jsonNewCount.future);
                $alertsWidget.find(".sf-alert-attended").html(jsonNewCount.attended);
                window.alert(successMessage);
            }
        });
    };

    return {
        onNoteCreated: onNoteCreated,
        onAlertCreated: onAlertCreated
    };
})();