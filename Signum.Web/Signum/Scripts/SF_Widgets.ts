/// <reference path="SF_Utils.ts"/>
/// <reference path="SF_Globals.ts"/>

export module SF.Widgets
{   
    var highlightClass = "sf-alert-active";

    $(document).on("click", ".sf-widget-toggler", function (evt) {
        SF.Dropdowns.toggle(evt, this, 1);
        return false;
    });

    export function onNoteCreated(url, prefix, successMessage) {
        $.ajax({
            url: url,
            data: { sfRuntimeInfo: new SF.RuntimeInfo(prefix).find().val() },
            success: function (newCount) {
                var $notesWidget = $("#" + SF.compose(prefix, "notesWidget"));
                $notesWidget.find(".sf-widget-count").html(newCount);
                window.alert(successMessage);
            }
        });
    }

    export function onAlertCreated(url, prefix, successMessage) {
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
    }

    function updateCountAndHighlight($alertsWidget, key, count) {
        var $current = $alertsWidget.find(".sf-alert-" + key);
        $current.not(".sf-alert-count-label").html(count);
        if (count > 0) {
            $current.addClass(highlightClass);
        }
    }
}
