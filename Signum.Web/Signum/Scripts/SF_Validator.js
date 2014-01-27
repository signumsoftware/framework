/// <reference path="references.ts"/>
var SF;
(function (SF) {
    (function (Validation) {
        function cleanError($element) {
            $element.removeClass(inputErrorClass);
        }
        Validation.cleanError = cleanError;

        var inputErrorClass = "input-validation-error";
        var fieldErrorClass = "sf-field-validation-error";
        var summaryErrorClass = "validation-summary-errors";
        var inlineErrorVal = "inlineVal";
        var globalErrorsKey = "sfGlobalErrors";
        var globalValidationSummary = "sfGlobalValidationSummary";

        function completeOptions(valOptions) {
            return $.extend({
                prefix: "",
                controllerUrl: null,
                showInlineErrors: true,
                fixedInlineErrorText: "*",
                parentDiv: "",
                requestExtraJsonData: null,
                ajaxError: null,
                errorSummaryId: null
            }, valOptions);
        }

        function constructRequestData(valOptions) {
            SF.log("Validator constructRequestData");
            var formChildren = SF.isEmpty(valOptions.parentDiv) ? $("form :input") : $("#" + valOptions.parentDiv + " :input").add("#" + SF.Keys.tabId).add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");

            var searchControlInputs = $(".sf-search-control :input");
            formChildren = formChildren.not(searchControlInputs);

            var serializer = new SF.Serializer();
            serializer.add(formChildren.serialize()).add("prefix", valOptions.prefix).add(valOptions.requestExtraJsonData);

            return serializer.serialize();
        }

        function trySave(valOptions) {
            valOptions = completeOptions(valOptions);

            SF.log("Validator trySave");
            var returnValue = false;
            $.ajax({
                url: valOptions.controllerUrl || SF.Urls.trySave,
                async: false,
                data: constructRequestData(valOptions),
                success: function (msg) {
                    if (typeof msg === "object") {
                        if (msg.result != "ModelState") {
                            throw "Validator trySave: Incorrect result type " + msg.result;
                        }
                        var modelState = msg.ModelState;
                        returnValue = showErrors(modelState, true);
                        SF.Notify.error(lang.signum.error, 2000);
                    } else {
                        if (SF.isEmpty(valOptions.parentDiv)) {
                            $("#content").html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                            SF.triggerNewContent($("#content"));
                        } else {
                            $("#" + valOptions.parentDiv).html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                            SF.triggerNewContent($("#" + valOptions.parentDiv));
                        }
                        returnValue = true;
                        SF.Notify.info(lang.signum.saved, 2000);
                    }
                }
            });
            return returnValue;
        }
        Validation.trySave = trySave;

        function validate(valOptions) {
            valOptions = completeOptions(valOptions);

            SF.log("Validator validate");
            var returnValue = false;
            $.ajax({
                url: valOptions.controllerUrl || SF.Urls.validate,
                async: false,
                data: constructRequestData(valOptions),
                success: function (msg) {
                    if (typeof msg === "object") {
                        if (msg.result != "ModelState") {
                            throw "Validator validate: Incorrect result type " + msg.result;
                        }
                        var modelState = msg.ModelState;
                        returnValue = showErrors(modelState, true);
                    } else {
                        returnValue = true;
                    }
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    if (valOptions.ajaxError != null) {
                        valOptions.ajaxError(xhr, ajaxOptions, thrownError);
                    }
                }
            });
            return returnValue;
        }
        Validation.validate = validate;

        function isValid(modelState) {
            SF.log("Validator isValid");
            var controlID;
            for (controlID in modelState) {
                if (modelState.hasOwnProperty(controlID) && modelState[controlID].length) {
                    return false;
                }
            }
            return true;
        }

        function showErrors(valOptions, modelState, showPathErrors) {
            SF.log("Validator showErrors");

            //Remove previous errors
            $('.' + fieldErrorClass).remove();
            $('.' + inputErrorClass).removeClass(inputErrorClass);
            $('.' + summaryErrorClass).remove();

            var allErrors = [];
            var inlineErrorStart = '<span class="' + fieldErrorClass + '">';
            var inlineErrorEnd = "</span>";

            var controlID;
            for (controlID in modelState) {
                if (modelState.hasOwnProperty(controlID)) {
                    var errorsArray = modelState[controlID], errorMessage = [], partialErrors = [], j;

                    for (j = 0; j < errorsArray.length; j++) {
                        errorMessage.push(errorsArray[j]);
                        partialErrors.push("<li>" + errorsArray[j] + "</li>");
                        allErrors.push(partialErrors);
                    }
                    var fixedInlineErrorText = valOptions.fixedInlineErrorText;

                    if (controlID != globalErrorsKey && controlID != "") {
                        var $control = $('#' + controlID);
                        $control.addClass(inputErrorClass);
                        $('#' + SF.compose(controlID, SF.Keys.toStr) + ',#' + SF.compose(controlID, SF.Keys.link)).addClass(inputErrorClass);
                        if (valOptions.showInlineErrors && $control.hasClass(inlineErrorVal)) {
                            if ($control.next().hasClass("ui-datepicker-trigger")) {
                                if (SF.isEmpty(fixedInlineErrorText)) {
                                    $control.next().after(inlineErrorStart + errorMessage.join('') + inlineErrorEnd);
                                } else {
                                    $control.next().after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                                }
                            } else {
                                if (SF.isEmpty(fixedInlineErrorText)) {
                                    $control.after(inlineErrorStart + errorMessage.join('') + inlineErrorEnd);
                                } else {
                                    $control.after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                                }
                            }
                        }
                    }
                    setPathErrors(valOptions, controlID, partialErrors.join(''), showPathErrors);
                }
            }

            if (allErrors.length) {
                SF.log("(Errors Validator showErrors): " + allErrors.join(''));
                return false;
            }
            return true;
        }
        Validation.showErrors = showErrors;

        //This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
        function setPathErrors(valOptions, controlID, partialErrors, showPathErrors) {
            var pathPrefixes = (controlID != globalErrorsKey) ? SF.getPathPrefixes(controlID) : new Array("");
            for (var i = 0, l = pathPrefixes.length; i < l; i++) {
                var currPrefix = pathPrefixes[i];
                if (currPrefix != undefined) {
                    var isEqual = (currPrefix === valOptions.prefix);
                    var isMoreInner = !isEqual && (currPrefix.indexOf(valOptions.prefix) > -1);
                    if (showPathErrors || isMoreInner) {
                        $('#' + SF.compose(currPrefix, SF.Keys.toStr)).addClass(inputErrorClass);
                        $('#' + SF.compose(currPrefix, SF.Keys.link)).addClass(inputErrorClass);
                    }
                    if ((isMoreInner || isEqual) && $('#' + SF.compose(currPrefix, globalValidationSummary)).length > 0 && !SF.isEmpty(partialErrors)) {
                        var currentSummary = !SF.isEmpty(valOptions.errorSummaryId) ? $('#' + valOptions.errorSummaryId) : SF.isEmpty(valOptions.parentDiv) ? $('#' + SF.compose(currPrefix, globalValidationSummary)) : $('#' + valOptions.parentDiv + " #" + SF.compose(currPrefix, globalValidationSummary));

                        var summaryUL = currentSummary.children('.' + summaryErrorClass);
                        if (summaryUL.length === 0) {
                            currentSummary.append('<ul class="' + summaryErrorClass + '">\n' + partialErrors + '</ul>');
                        } else {
                            summaryUL.append(partialErrors);
                        }
                    }
                }
            }
        }

        function completePartialOptions(valOptions) {
            return $.extend({
                parentDiv: SF.compose(valOptions.prefix, "panelPopup"),
                type: null,
                id: null
            }, completeOptions(valOptions));
        }

        function checkOrAddRuntimeInfo(valOptions, $formChildren, serializer) {
            //Check runtimeInfo present => if it's a popup from a LineControl it will not be
            var myRuntimeInfoKey = SF.compose(valOptions.prefix, SF.Keys.runtimeInfo);
            if ($formChildren.filter("#" + myRuntimeInfoKey).length == 0) {
                var $mainControl = $(".sf-main-control[data-prefix=" + valOptions.prefix + "]");
                serializer.add(myRuntimeInfoKey, $mainControl.data("runtimeinfo"));
            }
        }

        function constructRequestDataForSaving(valOptions) {
            SF.log("PartialValidator constructRequestDataForSaving");
            var prefix = valOptions.prefix;
            var formChildren = $("#" + valOptions.parentDiv + " :input").add("#" + SF.Keys.tabId).add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]").add(getInfoParams(prefix));
            formChildren = formChildren.not(".sf-search-control *");

            var serializer = new SF.Serializer();
            serializer.add(formChildren.serialize());

            if (!SF.isEmpty(prefix)) {
                serializer.add("prefix", prefix);
            }

            checkOrAddRuntimeInfo(valOptions, formChildren, serializer);

            serializer.add(valOptions.requestExtraJsonData);

            return serializer.serialize();
        }

        function getInfoParams(prefix) {
            return $("#" + SF.compose(prefix, SF.Keys.runtimeInfo));
        }
        ;

        function createValidatorResult(r) {
            var validatorResult = {
                modelState: r["ModelState"],
                isValid: isValid(r["ModelState"]),
                newToStr: r[SF.Keys.toStr],
                newLink: r[SF.Keys.link]
            };
            return validatorResult;
        }

        function trySavePartial(valOptions) {
            valOptions = completePartialOptions(valOptions);
            SF.log("PartialValidator trySave");
            var validatorResult = null;
            $.ajax({
                url: valOptions.controllerUrl || SF.Urls.trySavePartial,
                async: false,
                data: constructRequestDataForSaving(valOptions),
                success: function (result) {
                    validatorResult = createValidatorResult(result);
                    showErrors(valOptions, validatorResult.modelState);
                }
            });
            if (validatorResult != null && validatorResult.isValid) {
                SF.Notify.info(lang.signum.saved, 2000);
            } else
                SF.Notify.error(lang.signum.error, 2000);
            return validatorResult;
        }
        Validation.trySavePartial = trySavePartial;

        function constructRequestDataForValidating(valOptions) {
            SF.log("PartialValidator constructRequestDataForValidating");

            //Send main form (or parent popup) to be able to construct a typecontext if EmbeddedEntity
            var staticInfo = new SF.StaticInfo(valOptions.prefix);
            if (staticInfo.find().length == 0 && !SF.isEmpty(valOptions.prefix)) {
                var lastPrefix = valOptions.prefix.substr(0, valOptions.prefix.lastIndexOf(SF.Keys.separator));
                staticInfo = new SF.StaticInfo(lastPrefix);
            }

            var formChildren = null;
            var parentPrefix;

            if (!SF.isEmpty(valOptions.parentDiv)) {
                if (formChildren == null) {
                    formChildren = $("#" + valOptions.parentDiv + " :input").add("#" + SF.Keys.tabId).add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");
                } else {
                    formChildren = formChildren.add($("#" + valOptions.parentDiv + " :input"));
                }
            }
            formChildren = formChildren.not(".sf-search-control :input");

            var serializer = new SF.Serializer().add(formChildren.serialize());

            checkOrAddRuntimeInfo(valOptions, formChildren, serializer);

            if (staticInfo.find().length > 0 && staticInfo.isEmbedded()) {
                serializer.add("rootType", staticInfo.rootType());
                serializer.add("propertyRoute", staticInfo.propertyRoute());
            }

            serializer.add("prefix", valOptions.prefix);
            serializer.add(valOptions.requestExtraJsonData);

            if (typeof (parentPrefix) != "undefined") {
                serializer.add("parentPrefix", parentPrefix);

                if (formChildren.filter("#" + SF.compose(parentPrefix, "sfRuntimeInfo")).length == 0) {
                    var $parentMainControl = $(".sf-main-control[data-prefix=" + parentPrefix + "]");
                    serializer.add(SF.compose(parentPrefix, "sfRuntimeInfo"), $parentMainControl.data("runtimeinfo"));
                }
            }

            return serializer.serialize();
        }

        function validatePartial(valOptions) {
            SF.log("PartialValidator validate");
            var validatorResult = null;
            $.ajax({
                url: valOptions.controllerUrl || SF.Urls.validatePartial,
                async: false,
                data: constructRequestDataForValidating(valOptions),
                success: function (result) {
                    validatorResult = createValidatorResult(result);
                    showErrors(valOptions, validatorResult.modelState);
                }
            });
            return validatorResult;
        }
        Validation.validatePartial = validatePartial;

        function entityIsValid(validationOptions) {
            SF.log("Validator EntityIsValid");

            var isValid;

            if (validationOptions.prefix)
                isValid = validate(validationOptions);
            else
                isValid = validatePartial(validationOptions).isValid;

            if (isValid)
                return true;

            SF.Notify.error(lang.signum.error, 2000);
            alert(lang.signum.popupErrorsStop);

            return false;
        }
        Validation.entityIsValid = entityIsValid;
    })(SF.Validation || (SF.Validation = {}));
    var Validation = SF.Validation;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Validator.js.map
