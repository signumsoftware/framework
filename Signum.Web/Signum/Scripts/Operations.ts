/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")


export interface OperationOptions {
    prefix: string;
    operationKey: string;
    controllerUrl?: string;
    requestExtraJsonData?: any;
    isLite?: boolean;
    avoidReturnRedirect?: boolean;
    avoidReturnView?: boolean;
}

export interface EntityOperationOptions extends OperationOptions {
    avoidValidate?: boolean;
    validationOptions?: Validator.ValidationOptions;
}

export function executeDefault(options: EntityOperationOptions): Promise<void> {
    options = $.extend({
        avoidValidate: false,
        validationOptions: {},
        isLite: false,
    }, options);

    return entityIsValidOrLite(options).then(() =>
        executeAjax(options).then(eHtml=> {
            Navigator.reload(eHtml);
            notifyExecuted();
        }));
}

export function executeAjax(options: EntityOperationOptions): Promise<Entities.EntityHtml> {
    options = $.extend({
        controllerUrl: SF.Urls.operationExecute,
        isLite: false,
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: entityRequestData(options) })
        .then(result=> {
            assertModelStateErrors(result, options);
            return Entities.EntityHtml.fromHtml(options.prefix, result)
        });
}

export function executeDefaultContextual(options: OperationOptions): Promise<void> {
    Finder.removeOverlay();

    return executeAjaxContextual(options).then(result=> markCells(options.prefix, result));
}

export function executeAjaxContextual(options: OperationOptions, runtimeInfo?: Entities.RuntimeInfoValue): Promise<boolean> {
    options = $.extend({
        controllerUrl: SF.Urls.operationExecute,
        avoidReturnView: true,
        isLite: true,
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: contextualRequestData(options, null, runtimeInfo) })
        .then(SF.isEmpty);
}


export function constructFromDefault(options: EntityOperationOptions): Promise<void> {
    options = $.extend({
        avoidValidate: false,
        validationOptions: {},
        isLite: true,
    }, options);

    return entityIsValidOrLite(options)
        .then(() => constructFromAjax(options))
        .then(eHtml=> openPopup(eHtml));
}

export function constructFromAjax(options: EntityOperationOptions, newPrefix?: string) : Promise<Entities.EntityHtml>  {
    options = $.extend({
        controllerUrl: SF.Urls.operationConstructFrom,
    }, options);

    if (!newPrefix)
        newPrefix = getNewPrefix(options);

    return SF.ajaxPost({ url: options.controllerUrl, data: entityRequestData(options, newPrefix) })
        .then(html=> Entities.EntityHtml.fromHtml(newPrefix, html));
}

export function constructFromDefaultContextual(options: OperationOptions, newPrefix?: string): Promise<void> {
  
    Finder.removeOverlay();

    return constructFromAjaxContextual(options).then(eHtml=> {
        openPopup(eHtml);
        markCells(options.prefix, null);
    });
}

export function constructFromAjaxContextual(options: OperationOptions, newPrefix?: string, runtimeInfo?: Entities.RuntimeInfoValue): Promise<Entities.EntityHtml> {
    options = $.extend({
        controllerUrl: SF.Urls.operationConstructFrom,
    }, options);

    if (!newPrefix)
        newPrefix = getNewPrefix(options);

    return SF.ajaxPost({ url: options.controllerUrl, data: contextualRequestData(options, newPrefix, runtimeInfo) })
        .then(html=> Entities.EntityHtml.fromHtml(newPrefix, html));
}

export function deleteDefault(options: EntityOperationOptions) : Promise <void> {
    options = $.extend({
        avoidValidate: true,
        isLite: true,
    }, options);

    return entityIsValidOrLite(options).then(() => deleteAjax(options)).then(() => {
        //ajax prefilter will take redirect
        if (options.prefix) {
            Navigator.closePopup(options.prefix);
        }
    });
}

export function deleteAjax(options: EntityOperationOptions): Promise<any> {
    options = $.extend({
        controllerUrl: SF.Urls.operationDelete,
        avoidReturnRedirect: !!options.prefix,
        isLite: true,
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: entityRequestData(options) })
}

export function deleteDefaultContextual(options: OperationOptions): Promise<any> {
    options = $.extend({
        
    }, options);

    Finder.removeOverlay();

    return deleteAjaxContextual(options).then(result=> {
        markCells(options.prefix, result);
    });
}

export function deleteAjaxContextual(options: EntityOperationOptions, runtimeInfo?: Entities.RuntimeInfoValue): Promise<any> {
    options = $.extend({
        controllerUrl: SF.Urls.operationDelete,
        avoidReturnRedirect: true,
        isLite: true
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: contextualRequestData(options, null, runtimeInfo) });
}

export function constructFromManyDefault(options: OperationOptions, newPrefix?: string): Promise<void> {

    options = $.extend({
        controllerUrl: SF.Urls.operationConstructFromMany,
    }, options);

    Finder.removeOverlay();

    return constructFromManyAjax(options).then(eHtml=> {
        openPopup(eHtml);
        markCells(options.prefix, null);
    });
}

export function constructFromManyAjax(options: OperationOptions, newPrefix?: string): Promise<Entities.EntityHtml> {
    options = $.extend({
        isLite: true,
        controllerUrl: SF.Urls.operationConstructFromMany,
    }, options);

    if (!newPrefix)
        newPrefix = getNewPrefix(options);

    return SF.ajaxPost({ url: options.controllerUrl, data: constructFromManyRequestData(options, newPrefix) })
        .then(html=> Entities.EntityHtml.fromHtml(newPrefix, html));
}

export function reload(entityHtml: Entities.EntityHtml) {
    disableContextMenu();
    Navigator.reload(entityHtml);
}

export function openPopup(entityHtml : Entities.EntityHtml) {
    disableContextMenu();
    Navigator.navigatePopup(entityHtml);
    notifyExecuted();
}

export function disableContextMenu() {
    $(".sf-ctxmenu-active").removeClass("sf-ctxmenu-active");
}

export function markCells(prefix: string, success: boolean) {
    $(".sf-ctxmenu-active")
        .addClass("sf-entity-ctxmenu-" + (success ? "success" : "error"))
        .removeClass("sf-ctxmenu-active");
    notifyExecuted();
}

export function notifyExecuted() {

    SF.Notify.info(lang.signum.executed, 2000);
}

export function getNewPrefix(options: OperationOptions) {
    return SF.compose(options.prefix, "New");
}

export function entityRequestData(options: EntityOperationOptions, newPrefix?: string): FormData {

    var result = baseRequestData(options, newPrefix); 

    var formValues: FormObject = options.isLite ?
        Validator.getFormValuesLite(options.prefix) :
        Validator.getFormValues(options.prefix);

    return $.extend(result, formValues);
}

export function constructFromManyRequestData(options: OperationOptions, newPrefix?: string, liteKey? : string[]) : FormData {

    var result = baseRequestData(options, newPrefix); 

    if (!liteKey) {
        var items = Finder.getFor(options.prefix).selectedItems();
        liteKey = items.map(i=> i.runtimeInfo.key());
    }

    result["liteKeys"] = liteKey.join(",");

    return result; 
}

export function contextualRequestData(options: OperationOptions, newPrefix?: string, runtimeInfo? : Entities.RuntimeInfoValue): FormData {

    var result = baseRequestData(options, newPrefix); 

    if (!runtimeInfo) {
        var items = Finder.getFor(options.prefix).selectedItems();

        if (items.length > 1)
            throw new Error("just one entity should have been selected");

        runtimeInfo = items[0].runtimeInfo;
    }

    result[SF.compose(options.prefix, Entities.Keys.runtimeInfo)] = runtimeInfo.toString();

    return result;
}

export function baseRequestData(options: OperationOptions, newPrefix?: string) {

    var formValues = Validator.getFormBasics();

    formValues = $.extend({
        isLite: options.isLite,
        operationFullKey: options.operationKey,
        newprefix: newPrefix,
        prefix: options.prefix,
    }, formValues);

    if (options.avoidReturnRedirect)
        formValues["sfAvoidReturnRedirect"] = true;

    if (options.avoidReturnView)
        formValues["sfAvoidReturnView"] = true;

    return $.extend(formValues, options.requestExtraJsonData);
}


function assertModelStateErrors(operationResult: any, options: OperationOptions) {
    if ((typeof (operationResult) !== "object") || (operationResult.result != "ModelState"))
        return false;

    var modelState = operationResult.ModelState;

    Validator.showErrors({ prefix: options.prefix }, modelState);

    SF.Notify.error(lang.signum.error, 2000);

    throw modelState;
}

export function entityIsValidOrLite(options: EntityOperationOptions) : Promise<void> {
    if (options.isLite || options.avoidValidate)
        return Promise.resolve<void>(null);

    var valOptions = $.extend({ prefix: options.prefix }, options.validationOptions);

    return Validator.entityIsValid(valOptions);
}

export function validateAndSubmit(options: EntityOperationOptions) {
    if (entityIsValidOrLite(options))
        submit(options);
}

export function submit(options: EntityOperationOptions) {

    var mainControl = options.prefix ? $("#{0}_divMainControl".format(options.prefix)) : $("#divNormalControl")

    var $form = mainControl.closest("form");
    $form.append(SF.hiddenInput('isLite', options.isLite) +
        SF.hiddenInput('operationFullKey', options.operationKey) +
        SF.hiddenInput("prefix", options.prefix));

    if (!SF.isEmpty(options.prefix)) {
        //Check runtimeInfo present => if it's a popup from a LineControl it will not be
        var myRuntimeInfoKey = SF.compose(options.prefix, Entities.Keys.runtimeInfo);
        if ($form.filter("#" + myRuntimeInfoKey).length == 0) {
            SF.hiddenInput(myRuntimeInfoKey, mainControl.data("runtimeinfo"));
        }
    }

    SF.submit(options.controllerUrl, options.requestExtraJsonData, $form);

    return false;
}

