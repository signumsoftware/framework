/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
define(["require", "exports", "codemirror"], function(require, exports, CodeMirror) {
    function init($textArea) {
        var changedDelay;

        var editor = CodeMirror.fromTextArea($textArea[0], {
            lineNumbers: true,
            matchBrackets: true,
            mode: "javascript",
            extraKeys: {
                "Ctrl-Space": "autocomplete",
                "Ctrl-K": "commentSelection",
                "Ctrl-U": "uncommentSelection",
                "Ctrl-I": "autoFormatSelection",
                "F11": function (cm) {
                    cm.setOption("fullScreen", !cm.getOption("fullScreen"));
                },
                "Esc": function (cm) {
                    if (cm.getOption("fullScreen"))
                        cm.setOption("fullScreen", false);
                }
            },
            onCursorActivity: function () {
                editor.matchHighlight("CodeMirror-matchhighlight");
            },
            onChange: function () {
                editor.save();
                if (opener != null && opener != undefined) {
                    clearTimeout(changedDelay);
                    changedDelay = setTimeout(updatePreview, 150);
                }
            }
        });

        function updatePreview() {
            opener.changeTextArea($textArea.val(), $("#sfRuntimeInfo").val());
            exceptionDelay = setTimeout(getException, 100);
        }

        var exceptionDelay;
        var hlLine;
        function getException() {
            var number = opener.getExceptionNumber();
            if (number != null) {
                clearTimeout(exceptionDelay);
                if (hlLine != null)
                    editor.setLineClass(hlLine, null, null);
                if (number != -1)
                    hlLine = editor.setLineClass(number - 1, null, "exceptionLine");
            }
        }

        CodeMirror.commands.autocomplete = function (cm) {
            CodeMirror.simpleHint(cm, CodeMirror.javascriptHint);
        };

        function getSelectedRange() {
            return { from: editor.getCursor(true), to: editor.getCursor(false) };
        }

        CodeMirror.commands.commentSelection = function (cm) {
            var range = getSelectedRange();
            editor.commentRange(true, range.from, range.to);
        };

        CodeMirror.commands.uncommentSelection = function (cm) {
            var range = getSelectedRange();
            editor.commentRange(false, range.from, range.to);
        };

        CodeMirror.commands.autoFormatSelection = function (cm) {
            var range = getSelectedRange();
            editor.autoFormatRange(range.from, range.to);
        };
    }
    exports.init = init;
});
//# sourceMappingURL=ChartScript.js.map
