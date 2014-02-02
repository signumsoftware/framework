/// <reference path="globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities"], function(require, exports, Entities) {
    function onNoteCreated(url, prefix, successMessage) {
        $.ajax({
            url: url,
            data: { sfRuntimeInfo: new Entities.RuntimeInfoElement(prefix).getElem().val() },
            success: function (newCount) {
                var $notesWidget = $("#" + SF.compose(prefix, "notesWidget"));
                $notesWidget.find(".sf-widget-count").html(newCount);
                window.alert(successMessage);
            }
        });
    }
    exports.onNoteCreated = onNoteCreated;
});
//# sourceMappingURL=Widgets.js.map
