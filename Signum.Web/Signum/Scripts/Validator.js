/// <reference path="globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities"], function(require, exports, Entities) {
    exports.validationSummary = "validaton-summary";
    exports.globalErrorsKey = "sfGlobalErrors";
    exports.globalValidationSummary = "sfGlobalValidationSummary";

    function validate(valOptions) {
        SF.log("validate");

        var options = $.extend({
            prefix: "",
            controllerUrl: SF.Urls.validate,
            requestExtraJsonData: null,
            ajaxError: null,
            errorSummaryId: null
        }, valOptions);

        return SF.ajaxPost({
            url: options.controllerUrl,
            async: false,
            data: constructRequestData(options)
        }).then(function (result) {
            var validatorResult = {
                modelState: result.ModelState,
                isValid: isValid(result.ModelState),
                newToStr: result[Entities.Keys.toStr],
                newLink: result[Entities.Keys.link]
            };
            exports.showErrors(options, validatorResult.modelState);
            return validatorResult;
        });
    }
    exports.validate = validate;

    function constructRequestData(valOptions) {
        SF.log("Validator constructRequestData");

        var formValues = exports.getFormValues(valOptions.prefix, "prefix");

        if (valOptions.rootType)
            formValues["rootType"] = valOptions.rootType;

        if (valOptions.propertyRoute)
            formValues["propertyRoute"] = valOptions.propertyRoute;

        return $.extend(formValues, valOptions.requestExtraJsonData);
    }

    function getFormValues(prefix, prefixRequestKey) {
        var result;
        if (!prefix) {
            result = cleanFormInputs($("form :input")).serializeObject();
        } else {
            var mainControl = prefix.child("divMainControl").get();

            result = cleanFormInputs(mainControl.find(":input")).serializeObject();

            result[prefix.child(Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

            result = $.extend(result, exports.getFormBasics());
        }

        if (prefixRequestKey)
            result[prefixRequestKey] = prefix;

        return result;
    }
    exports.getFormValues = getFormValues;

    function getFormValuesLite(prefix, prefixRequestKey) {
        var result = exports.getFormBasics();

        result[prefix.child(Entities.Keys.runtimeInfo)] = prefix ? prefix.child("divMainControl").get().data("runtimeinfo") : prefix.child(Entities.Keys.runtimeInfo).get().val();

        if (prefixRequestKey)
            result[prefixRequestKey] = prefix;

        return result;
    }
    exports.getFormValuesLite = getFormValuesLite;

    function getFormValuesHtml(entityHtml, prefixRequestKey) {
        var mainControl = entityHtml.getChild("divMainControl");

        var result = cleanFormInputs(mainControl.find(":input")).serializeObject();

        result[entityHtml.prefix.child(Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

        if (prefixRequestKey)
            result[prefixRequestKey] = entityHtml.prefix;

        return $.extend(result, exports.getFormBasics());
    }
    exports.getFormValuesHtml = getFormValuesHtml;

    function getFormBasics() {
        return $('#' + Entities.Keys.tabId + ", input:hidden[name=" + Entities.Keys.antiForgeryToken + "]").serializeObject();
    }
    exports.getFormBasics = getFormBasics;

    function cleanFormInputs(form) {
        return form.not(".sf-search-control :input");
    }

    function isModelState(result) {
        return typeof result == "Object" && typeof result.ModelState != "undefined";
    }
    exports.isModelState = isModelState;

    function showErrors(valOptions, modelState) {
        valOptions = $.extend({
            prefix: "",
            showInlineErrors: true,
            fixedInlineErrorText: "*",
            errorSummaryId: null,
            showPathErrors: false
        }, valOptions);

        SF.log("Validator showErrors");

        //Remove previous errors
        $('.' + exports.hasError).removeClass(exports.hasError);
        $('.' + exports.validationSummary).remove();

        var allErrors = [];

        var prefix;
        for (prefix in modelState) {
            if (modelState.hasOwnProperty(prefix)) {
                var errorsArray = modelState[prefix];
                allErrors.push(errorsArray);
                if (prefix != exports.globalErrorsKey) {
                    exports.setHasError($('#' + prefix));
                    setPathErrors(valOptions, prefix, errorsArray);
                } else {
                    setPathErrors(valOptions, valOptions.prefix, errorsArray);
                }
            }
        }

        if (allErrors.length) {
            SF.log("(Errors Validator showErrors): " + allErrors.join(''));
            return false;
        }
        return true;
    }
    exports.showErrors = showErrors;

    exports.hasError = "has-error";
    function cleanHasError($element) {
        exports.errorElement($element).removeClass(exports.hasError);
    }
    exports.cleanHasError = cleanHasError;

    function setHasError($element) {
        exports.errorElement($element).addClass(exports.hasError);
    }
    exports.setHasError = setHasError;

    function errorElement($element) {
        var formGroup = $element.closest(".form-group");
        if (formGroup.length)
            return formGroup;

        return $element;
    }
    exports.errorElement = errorElement;

    //This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
    function setPathErrors(valOptions, prefix, errorsArray) {
        var partialErrors = errorsArray.map(function (a) {
            return "<li>" + a + "</li>";
        }).join('');

        exports.getPathPrefixes(prefix).forEach(function (currPrefix) {
<<<<<<< HEAD
            var summary = currPrefix.child(exports.globalValidationSummary).tryGet();
=======
            var summary = valOptions["errorSummaryId"] ? $('#' + valOptions["errorSummaryId"]) : $('#' + SF.compose(currPrefix, exports.globalValidationSummary));
>>>>>>> ValidatorErrorSummaryId

            if (summary.length) {
                var ul = summary.children("ul." + exports.validationSummary);
                if (!ul.length)
                    ul = $('<ul class="' + exports.validationSummary + ' alert alert-danger"></ul>').appendTo(summary);

                ul.append(partialErrors);
            }
            if (currPrefix.length < valOptions.prefix.length) {
                var element = $('#' + currPrefix);

                if (element.length > 0 && !element.hasClass("SF-avoid-child-errors"))
                    exports.setHasError(element);
            }
        });
    }

    function getPathPrefixes(prefix) {
        var path = [], pathSplit = prefix.split("_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[i] = pathSplit.slice(0, i).join("_");

        return path;
    }
    exports.getPathPrefixes = getPathPrefixes;

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
