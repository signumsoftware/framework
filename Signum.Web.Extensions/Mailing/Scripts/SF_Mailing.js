var SF = SF || {};

SF.registerModule("Mailing", function () {

    SF.Mailing = (function () {

        var cssClassActive = "sf-email-inserttoken-targetactive";
        var $lastTokenTarget;
        var onInsertToken;

        var initReplacements = function () {
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

        //var newSubTokensComboAdded = function ($selectedCombo) {
        //    var $btnAddFilter = $(".sf-email-inserttoken-if");
        //    var $btnAddColumn = $(this.pf("btnAddColumn"));

        //    var self = this;
        //    var $selectedOption = $selectedCombo.children("option:selected");
        //    $selectedCombo.attr("title", $selectedOption.attr("title"));
        //    $selectedCombo.attr("style", $selectedOption.attr("style"));
        //    if ($selectedOption.val() == "") {
        //        var $prevSelect = $selectedCombo.prev("select");
        //        if ($prevSelect.length == 0) {
        //            this.changeButtonState($btnAddFilter, lang.signum.selectToken);
        //            this.changeButtonState($btnAddColumn, lang.signum.selectToken);
        //        }
        //        else {
        //            var $prevSelectedOption = $prevSelect.find("option:selected");
        //            this.changeButtonState($btnAddFilter, $prevSelectedOption.attr("data-filter"), function () { self.addFilter(); });
        //            this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () { self.addColumn(); });
        //        }
        //        return;
        //    }

        //    this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () { self.addFilter(); });
        //    this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () { self.addColumn(); });
        //};

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
            removeTokenTargetFocus: removeTokenTargetFocus
        };
    })();
});