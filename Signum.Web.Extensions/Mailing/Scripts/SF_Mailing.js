var SF = SF || {};

SF.registerModule("Mailing", function () {

    SF.Mailing = (function () {

        var cssClassActive = "sf-email-inserttoken-targetactive";
        var $lastTokenTarget;
        var onInsertToken;

        var initReplacements = function () {
            var self = this;

            $(".sf-email-replacements-container").on("click", ".sf-email-togglereplacementspanel", function () {
                $(this).siblings(".sf-email-replacements-panel").toggle();
            });

            $(".sf-email-replacements-container").on("focus", ".sf-email-inserttoken-target", function () {
                setTokenTargetFocus($(this));
            });

            $(".sf-email-replacements-container").on("blur", ".sf-email-inserttoken-targetactive", function () {
                removeTokenTargetFocus($(this));
            });

            $(".sf-email-replacements-container").on("click", ".sf-email-inserttoken", function () {
                var tokenName = SF.FindNavigator.constructTokenName($(this).data("prefix"));
                if (SF.isEmpty(tokenName)) {
                    return;
                }

                var tokenTag = constructTokenTag(tokenName, $(this).data("block"));

                if (typeof onInsertToken != "undefined" && onInsertToken != null) {
                    onInsertToken(tokenTag);
                }
                else if (!$lastTokenTarget || $lastTokenTarget.filter(":visible").length == 0) {
                    window.alert("Select the target first");
                }
                else if ($lastTokenTarget.filter(":text").length > 0) {
                    var oldValue = $lastTokenTarget.val();
                    var currentPosition = $lastTokenTarget[0].selectionStart;
                    var newValue = oldValue.substr(0, currentPosition) + tokenTag + oldValue.substring(currentPosition);
                    $lastTokenTarget.val(newValue);
                }
            });

            $(".sf-email-replacements-container").on("sf-new-subtokens-combo", "select", function (event, idSelectedCombo) {
                SF.Mailing.newSubTokensComboAdded.call(self, $("#" + idSelectedCombo));
            });
        };

        var setTokenTargetFocus = function ($element) {
            $element.addClass(cssClassActive);
            $lastTokenTarget = $element;
            onInsertToken = null;
        };

        var removeTokenTargetFocus = function ($element) {
            $element.removeClass(cssClassActive);
        };

        var constructTokenTag = function (tokenName, block) {
            if (typeof block == "undefined") {
                return "@[" + tokenName + "]";
            }
            else if (block === "if") {
                return "@if[" + tokenName + "] @else @endif";
            }
            else if (block === "foreach") {
                return "@foreach[" + tokenName + "] @endforeach";
            }
            else {
                throw "invalid block name";
            }
        };

        var newSubTokensComboAdded = function ($selectedCombo) {
            var $btnInsertBasic = $(".sf-email-inserttoken-basic");
            var $btnInsertIf = $(".sf-email-inserttoken-if");
            var $btnInsertForeach = $(".sf-email-inserttoken-foreach");

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
                }
                else {
                    changeButtonState($btnInsertBasic);
                    var $prevSelectedOption = $prevSelect.find("option:selected");
                    changeButtonState($btnInsertIf, $prevSelectedOption.attr("data-if"));
                    changeButtonState($btnInsertForeach, $prevSelectedOption.attr("data-foreach"));
                }
                return;
            }

            changeButtonState($btnInsertBasic);
            changeButtonState($btnInsertIf, $selectedOption.attr("data-if"));
            changeButtonState($btnInsertForeach, $selectedOption.attr("data-foreach"));
        };

        var changeButtonState = function ($button, disablingMessage) {
            var hiddenId = $button.attr("id") + "temp";
            if (typeof disablingMessage != "undefined") {
                $button.addClass("ui-button-disabled").addClass("ui-state-disabled").addClass("sf-disabled").attr("disabled", "disabled").attr("title", disablingMessage);
                $button.unbind('click').bind('click', function (e) { e.preventDefault(); return false; });
            }
            else {
                var self = this;
                $button.removeClass("ui-button-disabled").removeClass("ui-state-disabled").removeClass("sf-disabled").prop("disabled", null).attr("title", "");
                $button.unbind('click');
            }
        };

        var updateHtmlEditorTextArea = function (idTargetTextArea) {
            CKEDITOR.instances[idTargetTextArea].updateElement();
        };

        var initHtmlEditor = function (idTargetTextArea) {

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
        };

        var initHtmlEditorMasterTemplate = function (idTargetTextArea) {

            initHtmlEditor(idTargetTextArea);

            $("body").off("click").on("click", "#insertMasterTemplateTokenButton", function () {
                CKEDITOR.instances[idTargetTextArea].insertText(constructTokenTag("content"));
                updateHtmlEditorTextArea(idTargetTextArea);
            });
        };

        var initHtmlEditorWithTokens = function (idTargetTextArea) {

            initHtmlEditor(idTargetTextArea);

            CKEDITOR.instances[idTargetTextArea].on('focus', function () {
                $("#cke_" + idTargetTextArea).addClass(cssClassActive);
                $lastTokenTarget = null;
                onInsertToken = function (tokenTag) {
                    if ($("#cke_" + idTargetTextArea + ":visible").length == 0) {
                        window.alert("Select the target first");
                    }
                    else {
                        CKEDITOR.instances[idTargetTextArea].insertText(tokenTag);
                        updateHtmlEditorTextArea(idTargetTextArea);
                    }
                }
            });

            CKEDITOR.instances[idTargetTextArea].on('blur', function () {
                SF.Mailing.removeTokenTargetFocus($("#cke_" + idTargetTextArea));
            });
        };

        return {
            initReplacements: initReplacements,
            initHtmlEditorMasterTemplate: initHtmlEditorMasterTemplate,
            initHtmlEditorWithTokens: initHtmlEditorWithTokens,
            setTokenTargetFocus: setTokenTargetFocus,
            removeTokenTargetFocus: removeTokenTargetFocus,
            newSubTokensComboAdded: newSubTokensComboAdded
        };
    })();
});