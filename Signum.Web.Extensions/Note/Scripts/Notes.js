/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Operations"], function(require, exports, Entities, Finder, Operations) {
    function explore(prefix, options, urlUpdate) {
        Finder.explore(options).then(function () {
            return exports.updateNotes(prefix, urlUpdate);
        });
    }
    exports.explore = explore;

    function createNote(prefix, operationKey, urlUpdate) {
        Operations.constructFromDefault({ prefix: prefix, operationKey: operationKey, isLite: true }).then(function () {
            return exports.updateNotes(prefix, urlUpdate);
        });
    }
    exports.createNote = createNote;

    function updateNotes(prefix, urlUpdate) {
        var widget = $("#" + SF.compose(prefix, "notesWidget"));

        SF.ajaxPost({
            url: urlUpdate,
            data: { key: Entities.RuntimeInfo.getFromPrefix(prefix).key() }
        }).then(function (txt) {
            return widget.parent().find(".sf-widget-count").text(txt);
        });
    }
    exports.updateNotes = updateNotes;
});
//# sourceMappingURL=Notes.js.map
