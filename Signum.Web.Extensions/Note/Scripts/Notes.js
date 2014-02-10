/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Entities, Finder, Operations) {
    function explore(prefix, options) {
        Finder.explore(options).then(function () {
            return exports.updateNotes(prefix);
        });
    }
    exports.explore = explore;

    function createNote(prefix, operationKey) {
        Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }).then(function () {
            return exports.updateNotes(prefix);
        });
    }
    exports.createNote = createNote;

    function updateNotes(prefix) {
        var widget = $("#" + SF.compose(prefix, "notesWidget") + " ul");

        SF.ajaxPost({
            url: widget.data("url"),
            data: { sfRuntimeInfo: new Entities.RuntimeInfoElement(prefix).getElem().val() }
        }).then(function (txt) {
            return widget.parent().find(".sf-widget-count").text(txt);
        });
    }
    exports.updateNotes = updateNotes;
});
//# sourceMappingURL=Notes.js.map
