/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities");

export interface ValidationOptions {
    prefix?: string;
    controllerUrl?: string;
    requestExtraJsonData?: any;
    rootType?: any;
    propertyRoute?: any;
    errorSummaryId?: string;
}

export interface ValidationResult {
    modelState: ModelState;
    isValid: boolean;
    newToStr: string;
}

export interface ModelState {
    [prefix: string]: string[];
}


export var validationSummary = "validaton-summary";
export var globalErrorsKey = "sfGlobalErrors";
export var globalValidationSummary = "sfGlobalValidationSummary";

export function validate(valOptions: ValidationOptions): Promise<ValidationResult> {
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
        data: constructRequestData(options),
    }).then(result => {
            var validatorResult: ValidationResult = {
                modelState: result.ModelState,
                isValid: isValid(result.ModelState),
                newToStr: result[Entities.Keys.toStr]
            };
            showErrors(options, validatorResult.modelState);
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

export function getFormValues(prefix: string, prefixRequestKey?: string): FormObject {

    var result;
    if (!prefix) {
        result = cleanFormInputs($("form :input")).serializeObject();
    }
    else {
        var mainControl = prefix.child("divMainControl").get();

        result = cleanFormInputs(mainControl.find(":input")).serializeObject();

        result[prefix.child(Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

        result = $.extend(result, getFormBasics());
    }

    if (prefixRequestKey)
        result[prefixRequestKey] = prefix;

    return result;
}

export function getFormValuesLite(prefix: string, prefixRequestKey?: string): FormObject {

    var result = getFormBasics();

    result[prefix.child(Entities.Keys.runtimeInfo)] = prefix ?
    prefix.child("divMainControl").get().data("runtimeinfo") : 
    prefix.child(Entities.Keys.runtimeInfo).get().val();

    if (prefixRequestKey)
        result[prefixRequestKey] = prefix;

    return result;
}

export function getFormValuesHtml(entityHtml: Entities.EntityHtml, prefixRequestKey?: string): FormObject {

    var mainControl = entityHtml.getChild("divMainControl");

    var result = cleanFormInputs(mainControl.find(":input")).serializeObject();

    result[entityHtml.prefix.child(Entities.Keys.runtimeInfo)] = mainControl.data("runtimeinfo");

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

export function isModelState(result: any): boolean {
    return typeof result == "object" && typeof result.ModelState != "undefined";
}

export function assertModelStateErrors(result: any, prefix: string) {
    if (!isModelState(result))
        return;

    var modelState = result.ModelState;

    showErrors({ prefix: prefix }, modelState);

    SF.Notify.error(lang.signum.error, 2000);

    throw modelState;
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
    $('.' + hasError).removeClass(hasError);
    $('.' + validationSummary).remove();

    var allErrors: string[][] = [];

    var prefix: string;
    for (prefix in modelState) {
        if (modelState.hasOwnProperty(prefix)) {
            var errorsArray = modelState[prefix];
            allErrors.push(errorsArray);
            if (prefix != globalErrorsKey) {
                setHasError($('#' + prefix));
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

export var hasError = "has-error";
export function cleanHasError($element: JQuery) {
    errorElement($element).removeClass(hasError)
}

export function setHasError($element: JQuery) {
    errorElement($element).addClass(hasError)
}

export function errorElement($element: JQuery) {
    var formGroup = $element.closest(".form-group"); 
    if (formGroup.length)
        return formGroup; 

    return $element;
}


//This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
function setPathErrors(valOptions: ValidationOptions, prefix: string, errorsArray: string[]) {

    var partialErrors = errorsArray.map(a=> "<li>" + a + "</li>").join('');

    getPathPrefixes(prefix).forEach(currPrefix=> {

        var summary = valOptions.errorSummaryId && currPrefix == valOptions.prefix? valOptions.errorSummaryId.tryGet() : currPrefix.child(globalValidationSummary).tryGet();

        if (summary.length) {
            var ul = summary.children("ul." + validationSummary);
            if (!ul.length)
                ul = $('<ul class="' + validationSummary + ' alert alert-danger"></ul>').appendTo(summary);

            ul.append(partialErrors);
        }
        if (currPrefix.length < valOptions.prefix.length) {

            var element = $('#' + currPrefix);

            if (element.length > 0 && !element.hasClass("SF-avoid-child-errors"))
                setHasError(element);
        }

    });
}

export function getPathPrefixes(prefix): string[] {
    var path: string[] = [],
        pathSplit = prefix.split("_");

    for (var i = 0, l = pathSplit.length; i < l; i++)
        path[i] = pathSplit.slice(0, i).join("_");

    return path;
}


function isValid(modelState: ModelState) {
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
        throw result;
    });
}
