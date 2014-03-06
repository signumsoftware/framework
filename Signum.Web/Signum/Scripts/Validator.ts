/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities");

export interface ValidationOptions {
    prefix?: string;
    controllerUrl?: string;
    requestExtraJsonData?: any;
    rootType?: any;
    propertyRoute?: any;

    showInlineErrors?: boolean;
    fixedInlineErrorText?: string; //Set to "" for it to be populated from ModelState error messages
    errorSummaryId?: string;
    showPathErrors?: boolean;
}

export function cleanError($element: JQuery) {
    $element.removeClass(inputErrorClass)
}

export interface ValidationResult {
    modelState: ModelState;
    isValid: boolean;
    newToStr: string;
    newLink: string;
}

export interface ModelState {
    [prefix: string]: string[];
}

export var inputErrorClass = "input-validation-error";
export var fieldErrorClass = "sf-field-validation-error";
export var summaryErrorClass = "validation-summary-errors";
export var inlineErrorVal = "inlineVal";
export var globalErrorsKey = "sfGlobalErrors";
export var globalValidationSummary = "sfGlobalValidationSummary";

export function validate(valOptions: ValidationOptions): Promise<ValidationResult> {
    SF.log("validate");

    valOptions = $.extend({
        prefix: "",
        controllerUrl: SF.Urls.validate,
        requestExtraJsonData: null,
        ajaxError: null,
        errorSummaryId: null
    }, valOptions);

    return SF.ajaxPost({
        url: valOptions.controllerUrl,
        async: false,
        data: constructRequestData(valOptions),
    }).then(result => {
            var validatorResult: ValidationResult = {
                modelState: result.ModelState,
                isValid: isValid(result.ModelState),
                newToStr: result[Entities.Keys.toStr],
                newLink: result[Entities.Keys.link]
            };
            showErrors(valOptions, validatorResult.modelState);
            return validatorResult
        });
}


function constructRequestData(valOptions: ValidationOptions): FormObject {
    SF.log("Validator constructRequestData");

    var formValues = getFormValues(valOptions.prefix, "prefix");

 

    if (valOptions.rootType)
        formValues["rootType"] = valOptions.rootType

    if (valOptions.propertyRoute)
        formValues["propertyRoute"] = valOptions.propertyRoute;

    return $.extend(formValues, valOptions.requestExtraJsonData);
}

export function getFormValues(prefix: string, prefixRequestKey?: string) : FormObject {

    var result; 
    if (!prefix) {
        result = cleanFormInputs($("form :input")).serializeObject();
    }
    else {
        var mainControl = $("#{0}_divMainControl".format(prefix));

        result = cleanFormInputs(mainControl.find(":input")).serializeObject();

        result[SF.compose(prefix, Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

        result = $.extend(result, getFormBasics());
    }

    if (prefixRequestKey)
        result[prefixRequestKey] = prefix;

    return result;
}

export function getFormValuesLite(prefix: string, prefixRequestKey? : string): FormObject {

    var result = getFormBasics();

    result[SF.compose(prefix, Entities.Keys.runtimeInfo)] = prefix ?
        $("#{0}_divMainControl".format(prefix)).data("runtimeinfo") :
        $('#' + SF.compose(prefix, Entities.Keys.runtimeInfo)).val();

    if (prefixRequestKey)
        result[prefixRequestKey] = prefix;

    return result;
}

export function getFormValuesHtml(entityHtml: Entities.EntityHtml, prefixRequestKey?: string): FormObject {

    var mainControl = entityHtml.html.find("#{0}_divMainControl".format(entityHtml.prefix)); 

    var result = cleanFormInputs(mainControl.find(":input")).serializeObject();

    result[SF.compose(entityHtml.prefix, Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

    if (prefixRequestKey)
        result[prefixRequestKey] = entityHtml.prefix;

    return $.extend(result, getFormBasics());
}


export function getFormBasics(): FormObject {
    return $('#' + Entities.Keys.tabId + ", input:hidden[name=" + Entities.Keys.antiForgeryToken + "]").serializeObject();
}

function cleanFormInputs(form: JQuery): JQuery {
    return form.not(".sf-search-control :input");
}

export function isModelState(result : any) : boolean {
    return typeof result == "Object" && typeof result.ModelState != "undefined";
}

export function showErrors(valOptions: ValidationOptions, modelState: ModelState): boolean {
    valOptions = $.extend({
        prefix: "",
        showInlineErrors: true,
        fixedInlineErrorText: "*", //Set to "" for it to be populated from ModelState error messages
        errorSummaryId: null,
        showPathErrors: false
    }, valOptions);

    SF.log("Validator showErrors");
    //Remove previous errors
    $('.' + fieldErrorClass).remove()
            $('.' + inputErrorClass).removeClass(inputErrorClass);
    $('.' + summaryErrorClass).remove();

    var allErrors: string[][]= [];
    
    var prefix: string;
    for (prefix in modelState) {
        if (modelState.hasOwnProperty(prefix)) {
            var errorsArray = modelState[prefix];
            var partialErrors = errorsArray.map(a=> "<li>" + a + "</li>");
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
            setPathErrors(valOptions, prefix, partialErrors.join(''));
        }
    }

    if (allErrors.length) {
        SF.log("(Errors Validator showErrors): " + allErrors.join(''));
        return false;
    }
    return true;
}


//This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
function setPathErrors(valOptions: ValidationOptions, prefix: string, partialErrors: string) {
    var pathPrefixes = (prefix != globalErrorsKey) ? SF.getPathPrefixes(prefix) : new Array("");
    for (var i = 0, l = pathPrefixes.length; i < l; i++) {
        var currPrefix = pathPrefixes[i];
        if (currPrefix != undefined) {
            var isEqual = (currPrefix === valOptions.prefix);
            var isMoreInner = !isEqual && (currPrefix.indexOf(valOptions.prefix) > -1);
            if (valOptions.showPathErrors || isMoreInner) {
                $('#' + SF.compose(currPrefix, Entities.Keys.toStr)).addClass(inputErrorClass);
                $('#' + SF.compose(currPrefix, Entities.Keys.link)).addClass(inputErrorClass);
            }
            if (valOptions.errorSummaryId || ((isMoreInner || isEqual) && $('#' + SF.compose(currPrefix, globalValidationSummary)).length > 0 && !SF.isEmpty(partialErrors))) {
                var currentSummary = valOptions.errorSummaryId ? $('#' + valOptions.errorSummaryId) : $('#' + SF.compose(currPrefix, globalValidationSummary));

                var summaryUL = currentSummary.children('.' + summaryErrorClass);
                if (summaryUL.length === 0) {
                    currentSummary.append('<ul class="' + summaryErrorClass + '">\n' + partialErrors + '</ul>');
                }
                else {
                    summaryUL.append(partialErrors);
                }
            }
        }
    }
}


function isValid(modelState : ModelState) {
    SF.log("Validator isValid");
    for (var prefix in modelState) {
        if (modelState.hasOwnProperty(prefix) && modelState[prefix].length) {
            return false; //Stop as soon as I find an error
        }
    }
    return true;
}


export function entityIsValid(validationOptions: ValidationOptions): Promise<void> {
    SF.log("Validator EntityIsValid");

    return validate(validationOptions).then(result => {
        if (result.isValid)
            return;

        SF.Notify.error(lang.signum.error, 2000);
        alert(lang.signum.popupErrorsStop);
        throw result; 
    });
}