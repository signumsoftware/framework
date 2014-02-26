/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>

declare var CodeMirror: any;

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")
import Files = require("Extensions/Signum.Web.Extensions/Files/Scripts/Files")

export function init($textArea: JQuery) {

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
                if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false);
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
    }

    function getSelectedRange() {
        return { from: editor.getCursor(true), to: editor.getCursor(false) };
    }

    CodeMirror.commands.commentSelection = function (cm) {
        var range = getSelectedRange();
        editor.commentRange(true, range.from, range.to);
    }

    CodeMirror.commands.uncommentSelection = function (cm) {
        var range = getSelectedRange();
        editor.commentRange(false, range.from, range.to);
    }

    CodeMirror.commands.autoFormatSelection = function (cm) {
        var range = getSelectedRange();
        editor.autoFormatRange(range.from, range.to);
    }
}

export function refreshIcon(fileLine: Files.FileLine,imageId:string)
{
    fileLine.entityChanged = () => {
        $('#' + imageId).attr("src", fileLine.getLink());
    }
}