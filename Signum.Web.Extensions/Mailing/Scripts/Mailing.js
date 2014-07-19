/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Finder", "Framework/Signum.Web/Signum/Scripts/Operations", "ckeditor"], function(require, exports, Finder, Operations, CKEDITOR) {
    var cssClassActive = "sf-email-inserttoken-targetactive";
    var $lastTokenTarget;
    var onInsertToken;

    function initReplacements() {
        var self = this;

        $(".sf-email-replacements-container").on("focus", ".sf-email-inserttoken-target", function () {
            exports.setTokenTargetFocus($(this));
        });

        $(".sf-email-replacements-container").on("click", ".sf-email-inserttoken", function () {
            var tokenName = Finder.QueryTokenBuilder.constructTokenName($(this).data("prefix"));
            if (SF.isEmpty(tokenName)) {
                return;
            }

            var tokenTag = constructTokenTag(tokenName, $(this).data("block"));

            if (typeof onInsertToken != "undefined" && onInsertToken != null) {
                onInsertToken(tokenTag);
            } else if (!$lastTokenTarget || $lastTokenTarget.filter(":visible").length == 0) {
                window.alert("Select the target first");
            } else if ($lastTokenTarget.filter(":text").length > 0) {
                var oldValue = $lastTokenTarget.val();
                var currentPosition = $lastTokenTarget[0].selectionStart;
                var newValue = oldValue.substr(0, currentPosition) + tokenTag + oldValue.substring(currentPosition);
                $lastTokenTarget.val(newValue);
            }
        });

        $(".sf-email-replacements-container").on("sf-new-subtokens-combo", "select", function (event) {
            var idSelectedCombo = [];
            for (var _i = 0; _i < (arguments.length - 1); _i++) {
                idSelectedCombo[_i] = arguments[_i + 1];
            }
            exports.newSubTokensComboAdded.call(self, $("#" + idSelectedCombo[0]));
        });
    }
    exports.initReplacements = initReplacements;

    function setTokenTargetFocus($element) {
        $("." + cssClassActive).removeClass(cssClassActive);
        onInsertToken = null;

        $element.addClass(cssClassActive);
        $lastTokenTarget = $element;
    }
    exports.setTokenTargetFocus = setTokenTargetFocus;
    ;

    function removeTokenTargetFocus($element) {
        $element.removeClass(cssClassActive);
    }
    exports.removeTokenTargetFocus = removeTokenTargetFocus;
    ;

    function constructTokenTag(tokenName, block) {
        if (typeof block == "undefined") {
            return "@[" + tokenName + "]";
        } else if (block === "if") {
            return "<!--@if[" + tokenName + "]--> <!--@else--> <!--@endif-->";
        } else if (block === "foreach") {
            return "<!--@foreach[" + tokenName + "]--> <!--@endforeach-->";
        } else if (block === "any") {
            return "<!--@any[" + tokenName + "=value]--> <!--@notany--> <!--@endany-->";
        } else {
            throw "invalid block name";
        }
    }
    ;

    function newSubTokensComboAdded($selectedCombo) {
        var $btnInsertBasic = $(".sf-email-inserttoken-basic");
        var $btnInsertIf = $(".sf-email-inserttoken-if");
        var $btnInsertForeach = $(".sf-email-inserttoken-foreach");
        var $btnInsertAny = $(".sf-email-inserttoken-any");

        var self = this;
        var $selectedOption = $selectedCombo.children("option:selected");
        $selectedCombo.attr("title", $selectedOption.attr("title"));
        $selectedCombo.attr("style", $selectedOption.attr("style"));
        if ($selectedOption.val() == "") {
            var $prevSelect = $selectedCombo.prev("select");
            if ($prevSelect.length == 0) {
                changeButtonState($btnInsertBasic, lang.signum.selectToken);
                changeButtonState($btnInsertIf, lang.signum.selectToken);
                changeButtonState($btnInsertForeach, lang.signum.selectToken);
                changeButtonState($btnInsertAny, lang.signum.selectToken);
            } else {
                changeButtonState($btnInsertBasic);
                var $prevSelectedOption = $prevSelect.find("option:selected");
                changeButtonState($btnInsertIf, $prevSelectedOption.attr("data-if"));
                changeButtonState($btnInsertForeach, $prevSelectedOption.attr("data-foreach"));
                changeButtonState($btnInsertAny, $prevSelectedOption.attr("data-any"));
            }
            return;
        }

        changeButtonState($btnInsertBasic);
        changeButtonState($btnInsertIf, $selectedOption.attr("data-if"));
        changeButtonState($btnInsertForeach, $selectedOption.attr("data-foreach"));
        changeButtonState($btnInsertAny, $selectedOption.attr("data-any"));
    }
    exports.newSubTokensComboAdded = newSubTokensComboAdded;
    ;

    function changeButtonState($button, disablingMessage) {
        var hiddenId = $button.attr("id") + "temp";
        if (typeof disablingMessage != "undefined") {
            $button.attr("disabled", "disabled").attr("title", disablingMessage);
            $button.unbind('click').bind('click', function (e) {
                e.preventDefault();
                return false;
            });
        } else {
            var self = this;
            $button.attr("disabled", null).attr("title", "");
            $button.unbind('click');
        }
    }

    function updateHtmlEditorTextArea(idTargetTextArea) {
        CKEDITOR.instances[idTargetTextArea].updateElement();
        SF.setHasChanges(idTargetTextArea.get());
    }
    ;

    function initHtmlEditor(idTargetTextArea, culture) {
        CKEDITOR.config.scayt_sLang = culture.replace("-", "_");
        CKEDITOR.replace(idTargetTextArea);

        // Update origin textarea
        // Make this more elegant once http://dev.ckeditor.com/ticket/9794 is fixed.
        var changed = function () {
            window.setTimeout(function () {
                updateHtmlEditorTextArea(idTargetTextArea);
            }, 0);
        };
        CKEDITOR.instances[idTargetTextArea].on('key', changed);
        CKEDITOR.instances[idTargetTextArea].on('paste', changed);
        CKEDITOR.instances[idTargetTextArea].on('afterCommandExec', changed);
        CKEDITOR.instances[idTargetTextArea].on('saveSnapshot', changed);
        CKEDITOR.instances[idTargetTextArea].on('afterUndo', changed);
        CKEDITOR.instances[idTargetTextArea].on('afterRedo', changed);
        CKEDITOR.instances[idTargetTextArea].on('simpleuploads.finishedUpload', changed);
    }
    exports.initHtmlEditor = initHtmlEditor;
    ;

    function initHtmlEditorMasterTemplate(idTargetTextArea, culture) {
        exports.initHtmlEditor(idTargetTextArea, culture);

        var $insertContent = $("#" + idTargetTextArea).closest(".sf-email-template-message").find(".sf-master-template-insert-content");

        $insertContent.on("click", "", function () {
            CKEDITOR.instances[idTargetTextArea].insertText(constructTokenTag("content"));
            updateHtmlEditorTextArea(idTargetTextArea);
        });
    }
    exports.initHtmlEditorMasterTemplate = initHtmlEditorMasterTemplate;

    function initHtmlEditorWithTokens(idTargetTextArea, culture) {
        exports.initHtmlEditor(idTargetTextArea, culture);

        var lastCursorPosition;

        var codeMirrorInstance;

        var ckEditorOnInsertToken = function (tokenTag) {
            if ($("#cke_" + idTargetTextArea + ":visible").length == 0) {
                window.alert("Select the target first");
            } else {
                if (CKEDITOR.instances[idTargetTextArea].mode == "source") {
                    codeMirrorInstance.replaceRange(tokenTag, lastCursorPosition || { line: 0, ch: 0 });
                } else {
                    CKEDITOR.instances[idTargetTextArea].insertHtml(tokenTag);
                    updateHtmlEditorTextArea(idTargetTextArea);
                    if (tokenTag.indexOf("<!--") == 0) {
                        CKEDITOR.instances[idTargetTextArea].setMode("source");
                    }
                }
            }
        };

        CKEDITOR.instances[idTargetTextArea].on('focus', function () {
            $("." + cssClassActive).removeClass(cssClassActive);
            $lastTokenTarget = null;

            $("#cke_" + idTargetTextArea).addClass(cssClassActive);
            onInsertToken = ckEditorOnInsertToken;
        });

        CKEDITOR.instances[idTargetTextArea].on('mode', function () {
            var ckEditorInstance = CKEDITOR.instances[idTargetTextArea];
            if (ckEditorInstance.mode == "source") {
                codeMirrorInstance = window["codemirror_" + ckEditorInstance.id];

                codeMirrorInstance.on("cursorActivity", function (instance) {
                    lastCursorPosition = instance.getCursor();
                });

                codeMirrorInstance.on("change", function (instance, change) {
                    var cmValue = instance.getValue();
                    ckEditorInstance.element.setValue(cmValue);
                    // ckEditorInstance.setData(cmValue);
                    // ckEditorInstance.fire('dataReady');
                });

                codeMirrorInstance.on("focus", function () {
                    $("." + cssClassActive).removeClass(cssClassActive);
                    $lastTokenTarget = null;

                    $("#cke_" + idTargetTextArea).addClass(cssClassActive);
                    onInsertToken = ckEditorOnInsertToken;
                });

                codeMirrorInstance.on("blur", function () {
                    exports.removeTokenTargetFocus($("#cke_" + idTargetTextArea));
                });
            }
        });

        CKEDITOR.instances[idTargetTextArea].on('blur', function () {
            exports.removeTokenTargetFocus($("#cke_" + idTargetTextArea));
        });
    }
    exports.initHtmlEditorWithTokens = initHtmlEditorWithTokens;

    function getDocument(iframe) {
        var doc = iframe.document;

        if (iframe.contentDocument)
            return iframe.contentDocument;
        else if (iframe.contentWindow)
            return iframe.contentWindow.document;

        return doc;
    }

    function activateIFrame($iframe) {
        var iframe = $iframe[0];

        var doc = getDocument(iframe);

        doc.open();
        doc.writeln($iframe.text());
        doc.close();

        //fixHeight();
        $(window).resize(function () {
            //setTimeout(fixHeight, 500);
            iframe.height = doc.body.scrollHeight + "px";
        });
    }
    exports.activateIFrame = activateIFrame;

    function createMailFromTemplate(options, event, findOptions, url) {
        Finder.find(findOptions).then(function (entity) {
            if (entity == null)
                return;

            Operations.constructFromDefault($.extend({
                keys: entity.runtimeInfo.key(),
                controllerUrl: url
            }, options), event);
        });
    }
    exports.createMailFromTemplate = createMailFromTemplate;

    function removeRecipients(options, newsletterDeliveryFindOptions, url) {
        Finder.findMany(newsletterDeliveryFindOptions).then(function (entities) {
            if (entities == null)
                return;

            options.requestExtraJsonData = { liteKeys: Finder.SearchControl.liteKeys(entities) };
            options.controllerUrl = url;

            Operations.executeDefault(options);
        });
    }
    exports.removeRecipients = removeRecipients;
});
//# sourceMappingURL=Mailing.js.map
