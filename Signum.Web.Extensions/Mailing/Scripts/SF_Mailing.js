/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>

var SF;
(function (SF) {
    (function (Mailing) {
        var cssClassActive = "sf-email-inserttoken-targetactive";
        var $lastTokenTarget;
        var onInsertToken;

        function initReplacements() {
            var self = this;

            $(".sf-email-replacements-container").on("focus", ".sf-email-inserttoken-target", function () {
                setTokenTargetFocus($(this));
            });

            $(".sf-email-replacements-container").on("click", ".sf-email-inserttoken", function () {
                var tokenName = SF.FindNavigator.constructTokenName($(this).data("prefix"));
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
                SF.Mailing.newSubTokensComboAdded.call(self, $("#" + idSelectedCombo[0]));
            });
        }
        Mailing.initReplacements = initReplacements;

        function setTokenTargetFocus($element) {
            $("." + cssClassActive).removeClass(cssClassActive);
            onInsertToken = null;

            $element.addClass(cssClassActive);
            $lastTokenTarget = $element;
        }
        Mailing.setTokenTargetFocus = setTokenTargetFocus;
        ;

        function removeTokenTargetFocus($element) {
            $element.removeClass(cssClassActive);
        }
        Mailing.removeTokenTargetFocus = removeTokenTargetFocus;
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
        Mailing.newSubTokensComboAdded = newSubTokensComboAdded;
        ;

        function changeButtonState($button, disablingMessage) {
            var hiddenId = $button.attr("id") + "temp";
            if (typeof disablingMessage != "undefined") {
                $button.addClass("ui-button-disabled").addClass("ui-state-disabled").addClass("sf-disabled").attr("disabled", "disabled").attr("title", disablingMessage);
                $button.unbind('click').bind('click', function (e) {
                    e.preventDefault();
                    return false;
                });
            } else {
                var self = this;
                $button.removeClass("ui-button-disabled").removeClass("ui-state-disabled").removeClass("sf-disabled").prop("disabled", null).attr("title", "");
                $button.unbind('click');
            }
        }

        function updateHtmlEditorTextArea(idTargetTextArea) {
            CKEDITOR.instances[idTargetTextArea].updateElement();
        }
        ;

        function initHtmlEditor(idTargetTextArea) {
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
        }
        Mailing.initHtmlEditor = initHtmlEditor;
        ;

        function initHtmlEditorMasterTemplate(idTargetTextArea) {
            initHtmlEditor(idTargetTextArea);

            var $insertContent = $("#" + idTargetTextArea).closest(".sf-email-template-message").find(".sf-master-template-insert-content");

            $insertContent.on("click", "", function () {
                CKEDITOR.instances[idTargetTextArea].insertText(constructTokenTag("content"));
                updateHtmlEditorTextArea(idTargetTextArea);
            });
        }
        Mailing.initHtmlEditorMasterTemplate = initHtmlEditorMasterTemplate;

        function initHtmlEditorWithTokens(idTargetTextArea) {
            initHtmlEditor(idTargetTextArea);

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
                        ckEditorInstance.setData(cmValue);
                        ckEditorInstance.fire('dataReady');
                    });

                    codeMirrorInstance.on("focus", function () {
                        $("." + cssClassActive).removeClass(cssClassActive);
                        $lastTokenTarget = null;

                        $("#cke_" + idTargetTextArea).addClass(cssClassActive);
                        onInsertToken = ckEditorOnInsertToken;
                    });

                    codeMirrorInstance.on("blur", function () {
                        SF.Mailing.removeTokenTargetFocus($("#cke_" + idTargetTextArea));
                    });
                }
            });

            CKEDITOR.instances[idTargetTextArea].on('blur', function () {
                SF.Mailing.removeTokenTargetFocus($("#cke_" + idTargetTextArea));
            });
        }
        Mailing.initHtmlEditorWithTokens = initHtmlEditorWithTokens;

        function activateIFrame($iframe) {
            var iframe = $iframe[0];

            var doc = iframe.document;
            if (iframe.contentDocument)
                doc = iframe.contentDocument; // For NS6
            else if (iframe.contentWindow)
                doc = iframe.contentWindow.document; // For IE5.5 and IE6

            doc.open();
            doc.writeln($iframe.text());
            doc.close();

            $iframe.height($iframe.contents().find("html").height() + 10);
        }
        Mailing.activateIFrame = activateIFrame;
    })(SF.Mailing || (SF.Mailing = {}));
    var Mailing = SF.Mailing;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Mailing.js.map
