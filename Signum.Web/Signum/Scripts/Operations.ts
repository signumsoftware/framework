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
    isContextual?: boolean;
    avoidReturnRedirect?: boolean;
    avoidReturnView?: boolean;
    confirmMessage?: string;
}

export interface EntityOperationOptions extends OperationOptions {
    avoidValidate?: boolean;
    validationOptions?: Validator.ValidationOptions;
    isNavigatePopup?: boolean
}

export function executeDefault(options: EntityOperationOptions): Promise<void> {
    options = $.extend({
        avoidValidate: false,
        validationOptions: {},
        isLite: false,
    }, options);

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

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
        isNavigatePopup : Navigator.isNavigatePopup(options.prefix)
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: entityRequestData(options) })
        .then(result=> {
            Validator.assertModelStateErrors(result, options.prefix);
            assertMessageBox(result);
            return Entities.EntityHtml.fromHtml(options.prefix, result)
        });
}

export function executeDefaultContextual(options: OperationOptions): Promise<void> {

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    return executeAjaxContextual(options).then(result=> { if (result) markCells(options.prefix); });
}

export function executeAjaxContextual(options: OperationOptions, runtimeInfo?: Entities.RuntimeInfo): Promise<boolean> {
    options = $.extend({
        controllerUrl: SF.Urls.operationExecute,
        avoidReturnView: true,
        avoidReturnRedirect: true,
        isLite: true,
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: contextualRequestData(options, null, runtimeInfo) })
        .then(SF.isEmpty);
}

export function executeDefaultContextualMultiple(options: OperationOptions): Promise<void> {

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    return SF.ajaxPost({ url: options.controllerUrl || SF.Urls.operationExecuteMultiple, data: conextualMultipleRequestData(options) })
        .then(result=> { markCells(options.prefix); });
}

export function constructFromDefault(options: EntityOperationOptions, openNewWindowOrEvent : any): Promise<void> {
    options = $.extend({
        avoidValidate: false,
        validationOptions: {},
        isLite: true,
    }, options);

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    return entityIsValidOrLite(options)
        .then(() => {
            if (Navigator.isOpenNewWindow(openNewWindowOrEvent))
                constructFromSubmit(options);
            else
                return constructFromAjax(options, getNewPrefix(options)).then(eHtml=> openPopup(eHtml));
        });
}

export function constructFromAjax(options: EntityOperationOptions, newPrefix: string) : Promise<Entities.EntityHtml>  {
    options = $.extend({
        controllerUrl: SF.Urls.operationConstructFrom,
        isLite: true,
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: entityRequestData(options, newPrefix) })
        .then(result=> {
            assertMessageBox(result);
            return Entities.EntityHtml.fromHtml(newPrefix, result);
        });
}

export function assertMessageBox(ajaxResult) {

    var mb = getMessageBoxOptions(ajaxResult);

    if (mb) {
        Navigator.openMessageBox(mb);
        throw mb;
    }
}

export function getMessageBoxOptions(ajaxResult): Navigator.MessageBoxOptions {
    if (SF.isEmpty(ajaxResult))
        return null;

    if (typeof ajaxResult !== "object")
        return null;

    if (ajaxResult.result == null)
        return null;

    if (ajaxResult.result == 'messageBox')
        return <Navigator.MessageBoxOptions>ajaxResult;

    return null;
}


export function constructFromSubmit(options: EntityOperationOptions) : void {
    options = $.extend({
        controllerUrl: SF.Urls.operationConstructFrom,
        isLite: true,
    }, options);

    SF.submitOnly(options.controllerUrl, entityRequestData(options, ""), true);
}


export function constructFromDefaultContextual(options: OperationOptions, openNewWindowOrEvent: any): Promise<void> {
    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    if (Navigator.isOpenNewWindow(openNewWindowOrEvent)) {
        markCells(options.prefix);
        constructFromSubmitContextual(options);
    } else {
        return constructFromAjaxContextual(options, getNewPrefix(options)).then(eHtml=> {
            markCells(options.prefix);
            return openPopup(eHtml);
        });
    }
}

export function constructFromAjaxContextual(options: OperationOptions, newPrefix: string, runtimeInfo?: Entities.RuntimeInfo): Promise<Entities.EntityHtml> {
    options = $.extend({
        controllerUrl: SF.Urls.operationConstructFrom,
        isLite: true,
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: contextualRequestData(options, newPrefix, runtimeInfo) })
        .then(result=> {
            assertMessageBox(result);
            return Entities.EntityHtml.fromHtml(newPrefix, result);
        });
}

export function constructFromSubmitContextual(options: OperationOptions, runtimeInfo?: Entities.RuntimeInfo): void {
    options = $.extend({
        controllerUrl: SF.Urls.operationConstructFrom,
        isLite: true,
    }, options);

    SF.submitOnly(options.controllerUrl, contextualRequestData(options, "", runtimeInfo), true);
}

export function constructFromDefaultContextualMultiple(options: OperationOptions): Promise<void> {

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    return SF.ajaxPost({ url: options.controllerUrl || SF.Urls.operationConstructFromMultiple, data: conextualMultipleRequestData(options) })
        .then(result=> { markCells(options.prefix); });
}

export function deleteDefault(options: EntityOperationOptions) : Promise <void> {
    options = $.extend({
        avoidValidate: true,
        isLite: true,
    }, options);

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

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
        isLite: true,
    }, options);

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    return deleteAjaxContextual(options).then(result=> {
        markCells(options.prefix);
    });
}

export function deleteAjaxContextual(options: OperationOptions, runtimeInfo?: Entities.RuntimeInfo): Promise<any> {
    options = $.extend({
        controllerUrl: SF.Urls.operationDelete,
        avoidReturnRedirect: true,
        isLite: true
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: contextualRequestData(options, null, runtimeInfo) });
}

export function deleteDefaultContextualMultiple(options: OperationOptions): Promise<void> {

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    return SF.ajaxPost({ url: options.controllerUrl || SF.Urls.operationDeleteMultiple, data: conextualMultipleRequestData(options) })
        .then(result=> { markCells(options.prefix); });
}

export function constructFromManyDefault(options: OperationOptions, openNewWindowOrEvent: any): Promise<void> {

    if (!confirmIfNecessary(options))
        return Promise.reject("confirmation");

    if (Navigator.isOpenNewWindow(openNewWindowOrEvent)) {
        markCells(options.prefix);
        constructFromManySubmit(options);
    } else {
        return constructFromManyAjax(options, getNewPrefix(options)).then(eHtml=> {
            markCells(options.prefix);
            if (!eHtml)
                return null;

            return openPopup(eHtml);
        });
    }
}

export function constructFromManyAjax(options: OperationOptions, newPrefix: string): Promise<Entities.EntityHtml> {
    options = $.extend({
        isLite: true,
        controllerUrl: SF.Urls.operationConstructFromMany,
    }, options);

    return SF.ajaxPost({ url: options.controllerUrl, data: conextualMultipleRequestData(options, newPrefix) })
        .then(result=> {
            assertMessageBox(result);
            return Entities.EntityHtml.fromHtml(newPrefix, result);
        });
}

export function constructFromManySubmit(options: OperationOptions): void {
    options = $.extend({
        isLite: true,
        controllerUrl: SF.Urls.operationConstructFromMany,
    }, options);

    SF.submitOnly(options.controllerUrl, conextualMultipleRequestData(options, ""), true);
}

export function confirmIfNecessary(options: OperationOptions): boolean {
    return !options.confirmMessage || confirm(options.confirmMessage);
}

export function openPopup(entityHtml : Entities.EntityHtml) : Promise<void> {
    notifyExecuted();
    return Navigator.navigatePopup(entityHtml);
}

export function markCells(prefix: string) {
    Finder.getFor(prefix).then(sc=>sc.markSelectedAsSuccess());
    notifyExecuted();
}

export function notifyExecuted() {
    SF.Notify.info(lang.signum.executed, 2000);
}

export function getNewPrefix(options: OperationOptions) {
    return options.prefix.child("New");
}

export function entityRequestData(options: EntityOperationOptions, newPrefix?: string): FormData {

    var result = baseRequestData(options, newPrefix); 

    var formValues: FormObject = options.isLite ?
        Validator.getFormValuesLite(options.prefix) :
        Validator.getFormValues(options.prefix);

    formValues[Entities.Keys.viewMode] = options.isNavigatePopup ? "Navigate" : "View";

    return $.extend(result, formValues);
}

export function conextualMultipleRequestData(options: OperationOptions, newPrefix?: string, liteKey? : string[]) : FormData {

    var result = baseRequestData(options, newPrefix); 

    if (!liteKey) {
        var items = Finder.SearchControl.getSelectedItems(options.prefix);
        liteKey = items.map(i => i.runtimeInfo.key());
    
    }

    result["liteKeys"] = liteKey.join(",");
    return result; 
}

export function contextualRequestData(options: OperationOptions, newPrefix?: string, runtimeInfo? : Entities.RuntimeInfo): FormData {

    var result = baseRequestData(options, newPrefix); 

    if (!runtimeInfo) {
        var items = Finder.SearchControl.getSelectedItems(options.prefix);

        if (items.length > 1)
            throw new Error("just one entity should have been selected");

        runtimeInfo = items[0].runtimeInfo;
    }

    result[options.prefix.child(Entities.Keys.runtimeInfo)] = runtimeInfo.toString();

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

    var mainControl = options.prefix ? $("#{0}_divMainControl".format(options.prefix)) : $("#divMainControl")

    var $form = mainControl.closest("form");
    $form.append(SF.hiddenInput('isLite', options.isLite) +
        SF.hiddenInput('operationFullKey', options.operationKey) +
        SF.hiddenInput("prefix", options.prefix));

    if (!SF.isEmpty(options.prefix)) {
        //Check runtimeInfo present => if it's a popup from a LineControl it will not be
        var myRuntimeInfoKey = options.prefix.child(Entities.Keys.runtimeInfo);
        if (myRuntimeInfoKey.tryGet().length == 0) {
            SF.hiddenInput(myRuntimeInfoKey, mainControl.data("runtimeinfo"));
        }
    }

    SF.submit(options.controllerUrl, options.requestExtraJsonData, $form);

    return false;
}

