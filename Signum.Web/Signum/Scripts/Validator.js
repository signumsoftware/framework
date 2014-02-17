/// <reference path="globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities"], function(require, exports, Entities) {
    function cleanError($element) {
        $element.removeClass(inputErrorClass);
    }
    exports.cleanError = cleanError;

    var inputErrorClass = "input-validation-error";
    var fieldErrorClass = "sf-field-validation-error";
    var summaryErrorClass = "validation-summary-errors";
    var inlineErrorVal = "inlineVal";
    var globalErrorsKey = "sfGlobalErrors";
    var globalValidationSummary = "sfGlobalValidationSummary";

    function validate(valOptions) {
        SF.log("validate");

        valOptions = $.extend({
            prefix: "",
            controllerUrl: SF.Urls.validate,
            showInlineErrors: true,
            fixedInlineErrorText: "*",
            parentDiv: "",
            requestExtraJsonData: null,
            ajaxError: null,
            errorSummaryId: null
        }, valOptions);

        return SF.ajaxPost({
            url: valOptions.controllerUrl,
            async: false,
            data: constructRequestData(valOptions)
        }).then(function (result) {
            var validatorResult = {
                modelState: result.ModelState,
                isValid: isValid(result.ModelState),
                newToStr: result[Entities.Keys.toStr],
                newLink: result[Entities.Keys.link]
            };
            exports.showErrors(valOptions, validatorResult.modelState);
            return validatorResult;
        });
    }
    exports.validate = validate;

    function constructRequestData(valOptions) {
        SF.log("Validator constructRequestData");

        var formValues = exports.getFormValues(valOptions.prefix);

        formValues["prefix"] = valOptions.prefix;

        if (valOptions.prefix) {
            var staticInfo = Entities.StaticInfo.getFor(valOptions.prefix);

            if (staticInfo.find().length > 0 && staticInfo.isEmbedded()) {
                formValues["rootType"] = staticInfo.rootType();
                formValues["propertyRoute"] = staticInfo.propertyRoute();
            }
        }

        return $.extend(formValues, valOptions.requestExtraJsonData);
    }

    function getFormValues(prefix) {
        if (!prefix)
            return cleanFormInputs($("form :input")).serializeObject();

        var mainControl = $("#{0}_divMainControl".format(prefix));

        var result = cleanFormInputs(mainControl.find(":input")).serializeObject();

        result[SF.compose(prefix, Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

        return $.extend(result, exports.getFormBasics());
    }
    exports.getFormValues = getFormValues;

    function getFormValuesHtml(entityHtml) {
        var mainControl = entityHtml.html.find("#{0}_divMainControl".format(entityHtml.prefix));

        var result = cleanFormInputs(mainControl.find(":input")).serializeObject();

        result[SF.compose(entityHtml.prefix, Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

        return $.extend(result, exports.getFormBasics());
    }
    exports.getFormValuesHtml = getFormValuesHtml;

    function getFormValuesLite(prefix) {
        var result = exports.getFormBasics();

        result[SF.compose(prefix, Entities.Keys.runtimeInfo)] = prefix ? $("#{0}_divMainControl".format(prefix)).data("runtimeinfo") : $('#' + SF.compose(prefix, Entities.Keys.runtimeInfo)).val();

        return result;
    }
    exports.getFormValuesLite = getFormValuesLite;

    function getFormBasics() {
        return $('#' + Entities.Keys.tabId + ", input:hidden[name=" + Entities.Keys.antiForgeryToken + "]").serializeObject();
    }
    exports.getFormBasics = getFormBasics;

    function cleanFormInputs(form) {
        return form.not(".sf-search-control :input");
    }

    function showErrors(valOptions, modelState, showPathErrors) {
        SF.log("Validator showErrors");

        //Remove previous errors
        $('.' + fieldErrorClass).remove();
        $('.' + inputErrorClass).removeClass(inputErrorClass);
        $('.' + summaryErrorClass).remove();

        var allErrors = [];

        var prefix;
        for (prefix in modelState) {
            if (modelState.hasOwnProperty(prefix)) {
                var errorsArray = modelState[prefix];
                var partialErrors = errorsArray.map(function (a) {
                    return "<li>" + a + "</li>";
                });
                allErrors.push(errorsArray);

                if (prefix != globalErrorsKey && prefix != "") {
                    var $control = $('#' + prefix);
                    $control.addClass(inputErrorClass);
                    $('#' + SF.compose(prefix, Entities.Keys.toStr) + ',#' + SF.compose(prefix, Entities.Keys.link)).addClass(inputErrorClass);
                    if (valOptions.showInlineErrors && $control.hasClass(inlineErrorVal)) {
                        var errorMessage = '<span class="' + fieldErrorClass + '">' + (valOptions.fixedInlineErrorText || errorsArray.join('')) + "</span>";

                        if ($control.next().hasClass("ui-datepicker-trigger"))
                            $control.next().after(errorMessage);
                        else
                            $control.after(errorMessage);
                    }
                }
                setPathErrors(valOptions, prefix, partialErrors.join(''), showPathErrors);
            }
        }

        if (allErrors.length) {
            SF.log("(Errors Validator showErrors): " + allErrors.join(''));
            return false;
        }
        return true;
    }
    exports.showErrors = showErrors;

    //This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
    function setPathErrors(valOptions, prefix, partialErrors, showPathErrors) {
        var pathPrefixes = (prefix != globalErrorsKey) ? SF.getPathPrefixes(prefix) : new Array("");
        for (var i = 0, l = pathPrefixes.length; i < l; i++) {
            var currPrefix = pathPrefixes[i];
            if (currPrefix != undefined) {
                var isEqual = (currPrefix === valOptions.prefix);
                var isMoreInner = !isEqual && (currPrefix.indexOf(valOptions.prefix) > -1);
                if (showPathErrors || isMoreInner) {
                    $('#' + SF.compose(currPrefix, Entities.Keys.toStr)).addClass(inputErrorClass);
                    $('#' + SF.compose(currPrefix, Entities.Keys.link)).addClass(inputErrorClass);
                }
                if ((isMoreInner || isEqual) && $('#' + SF.compose(currPrefix, globalValidationSummary)).length > 0 && !SF.isEmpty(partialErrors)) {
                    var currentSummary = valOptions.errorSummaryId ? $('#' + valOptions.errorSummaryId) : $('#' + SF.compose(currPrefix, globalValidationSummary));

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

    function isValid(modelState) {
        SF.log("Validator isValid");
        for (var prefix in modelState) {
            if (modelState.hasOwnProperty(prefix) && modelState[prefix].length) {
                return false;
            }
        }
        return true;
    }

    function entityIsValid(validationOptions) {
        SF.log("Validator EntityIsValid");

        return exports.validate(validationOptions).then(function (result) {
            if (result.isValid)
                return;

            SF.Notify.error(lang.signum.error, 2000);
            alert(lang.signum.popupErrorsStop);
            throw result;
        });
    }
    exports.entityIsValid = entityIsValid;
});
//# sourceMappingURL=Validator.js.map
