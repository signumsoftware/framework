/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")


export interface ViewOptionsBase {
    controllerUrl?: string;
    partialViewName?: string;
    requestExtraJsonData?: any;
    readOnly?: boolean;
}

export function requestPartialView(entityHtml: Entities.EntityHtml, viewOptions?: ViewOptionsBase): Promise<Entities.EntityHtml> {
    viewOptions = $.extend({
        controllerUrl: SF.Urls.partialView,
        partialViewName: null,
        requestExtraJsonData: null,
        readOnly: false,
    }, viewOptions);

    return requestHtml(entityHtml, viewOptions);
}

export function navigate(runtimeInfo: Entities.RuntimeInfo, openNewWindow?: boolean) {
    var url = runtimeInfo.isNew ?
        "{0}/{1}".format(SF.Urls.create, runtimeInfo.type) :
        "{0}/{1}/{2}".format(SF.Urls.view, runtimeInfo.type, !SF.isEmpty(runtimeInfo.id) ? runtimeInfo.id : "");

    if (openNewWindow)
        window.open(url, "_blank");
    else
        window.location.href = url;
}

export interface NavigatePopupOptions extends ViewOptionsBase {
    onPopupLoaded?: (popupDiv: JQuery) => void;
}

export function createTempDiv(entityHtml: Entities.EntityHtml): string {
    var tempDivId = SF.compose(entityHtml.prefix, "Temp");

    $("body").append(SF.hiddenDiv(tempDivId, ""));

    var tempDiv = $("#" + tempDivId);

    tempDiv.html(entityHtml.html);

    return tempDivId; 
}

export function navigatePopup(entityHtml: Entities.EntityHtml, viewOptions?: NavigatePopupOptions): Promise<void> {
    viewOptions = $.extend({
        controllerUrl: SF.Urls.popupNavigate,
        partialViewName: "",
        requestExtraJsonData: null,
        readOnly: false,
        onPopupLoaded: null,
    }, viewOptions);

    if (entityHtml.isLoaded())
        return openNavigatePopup(entityHtml, viewOptions);

    return requestHtml(entityHtml, viewOptions).then(eHTml => {
        return openNavigatePopup(eHTml, viewOptions);
    });
}

function openNavigatePopup(entityHtml: Entities.EntityHtml, viewOptions: NavigatePopupOptions): Promise<void> {

    var tempDivId = createTempDiv(entityHtml);

    var tempDiv = $("#" + tempDivId);

    var result = new Promise<void>(resolve => {
        tempDiv.popup({
            onCancel: function () {

                $("#" + tempDivId).remove(); // could be reloaded

                resolve(null);
            }
        });
    });

    if (viewOptions.onPopupLoaded != null)
        viewOptions.onPopupLoaded(tempDiv);

    return result;
}


export interface ViewPopupOptions extends ViewOptionsBase {
    avoidClone?: boolean;
    avoidValidate?: boolean;
    validationOptions?: Validator.ValidationOptions;
    allowErrors?: AllowErrors;
    onPopupLoaded?: (popupDiv: JQuery) => void;
}

export enum AllowErrors {
    Ask,
    Yes,
    No,
}

export function viewPopup(entityHtml: Entities.EntityHtml, viewOptions?: ViewPopupOptions): Promise<Entities.EntityHtml> {

    viewOptions = $.extend({
        controllerUrl: SF.Urls.popupView,
        partialViewName: null,
        requestExtraJsonData: null,
        readOnly: false,
        avoidClone: false,
        avoidValidate: false,
        allowErrors: AllowErrors.Ask,
        onPopupLoaded: null,
    }, viewOptions);

    if (!viewOptions.avoidValidate)
        viewOptions.validationOptions = $.extend({
            prefix: entityHtml.prefix,
            showPathErrors: true
        }, viewOptions.validationOptions);


    if (entityHtml.isLoaded()) {

        if (viewOptions.avoidClone)
            return openPopupView(entityHtml, viewOptions);

        var clone = new Entities.EntityHtml(entityHtml.prefix, entityHtml.runtimeInfo, entityHtml.toStr, entityHtml.link);

        clone.html = SF.cloneWithValues(entityHtml.html);

        return openPopupView(clone, viewOptions);
    }

    return requestHtml(entityHtml, viewOptions).then(eHtml => openPopupView(eHtml, viewOptions));
}

function openPopupView(entityHtml: Entities.EntityHtml, viewOptions: ViewPopupOptions): Promise<Entities.EntityHtml> {

    var tempDivId = createTempDiv(entityHtml);

    var tempDiv = $("#" + tempDivId);

    return new Promise<Entities.EntityHtml>(function (resolve) {
        tempDiv.popup({
            onOk: function () {

                var continuePromise: Promise<boolean> =
                    viewOptions.avoidValidate ? Promise.resolve<boolean>(true) :
                    checkValidation(viewOptions.validationOptions, viewOptions.allowErrors).then(valResult=> {
                        if (valResult == null)
                            return false;

                        entityHtml.hasErrors = !valResult.isValid;
                        entityHtml.link = valResult.newLink;
                        entityHtml.toStr = valResult.newToStr;

                        return true;
                    });

                continuePromise.then(result=> {
                    if (result) {
                        var newTempDiv = $("#" + tempDivId);
                        var $mainControl = newTempDiv.find(".sf-main-control[data-prefix=" + entityHtml.prefix + "]");
                        if ($mainControl.length > 0) {
                            entityHtml.runtimeInfo = Entities.RuntimeInfo.parse($mainControl.data("runtimeinfo"));
                        }

                        newTempDiv.popup('restoreTitle');
                        newTempDiv.popup('destroy');
                        entityHtml.html = newTempDiv.children();
                        newTempDiv.remove();

                        resolve(entityHtml);
                    }
                });
            },
            onCancel: function () {
                $("#" + tempDivId).remove();

                resolve(null);
            }
        });

        if (viewOptions.onPopupLoaded != null)
            viewOptions.onPopupLoaded(tempDiv);
    });
}


export function basicPopupView(entityHtml: Entities.EntityHtml, onOk: (div: JQuery) => Promise<boolean>): Promise<void> {

    var tempDivId = createTempDiv(entityHtml);

    var tempDiv = $("#" + tempDivId);

    return new Promise<void>(function (resolve) {
        tempDiv.popup({
            onOk: function () {
                onOk($("#" + tempDivId)).then(result => {
                    if (result) {
                        var newTempDiv = $("#" + tempDivId);
                          $("#" + tempDivId).remove();
                        resolve(null);
                    }
                });
            },
            onCancel: function () {
                var newTempDiv = $("#" + tempDivId);
                $("#" + tempDivId).remove();
                resolve(null);
            }
        });
    });
}


export function requestAndReload(prefix: string, options?: ViewOptionsBase): Promise<Entities.EntityHtml> {

    options = $.extend({
        controllerUrl: !prefix ? SF.Urls.normalControl :
        isNavigatePopup(prefix) ? SF.Urls.popupNavigate : SF.Urls.popupView,
    }, options);

    return requestHtml(getEmptyEntityHtml(prefix), options).then(eHtml=> {

        reload(eHtml);

        eHtml.html = null;

        return eHtml;
    });
}

export function getRuntimeInfoValue(prefix: string) : Entities.RuntimeInfo {
    if (!prefix)
        return Entities.RuntimeInfo.getFromPrefix(prefix);

    var mainControl = $("#{0}_divMainControl".format(prefix)); 

    return Entities.RuntimeInfo.parse(mainControl.data("runtimeinfo"));
}

export function getEmptyEntityHtml(prefix: string): Entities.EntityHtml {
    return new Entities.EntityHtml(prefix, getRuntimeInfoValue(prefix));
}

export function reloadMain(entityHtml: Entities.EntityHtml) {
    var $elem = $("#divNormalControl");
    $elem.html(entityHtml.html);
}

export function closePopup(prefix: string): void {

    var tempDivId = SF.compose(prefix, "Temp");

    var tempDiv = $("#" + tempDivId);

    var popupOptions = tempDiv.popup();

    tempDiv.popup("destroy");

    tempDiv.remove();
}



export function reloadPopup(entityHtml : Entities.EntityHtml) {

    var tempDivId = SF.compose(entityHtml.prefix, "Temp");

    var tempDiv = $("#" + tempDivId);

    var popupOptions = tempDiv.popup();

    tempDiv.popup("destroy");

    tempDiv.html(entityHtml.html);

    tempDiv.popup(popupOptions);
}

export function reload(entityHtml: Entities.EntityHtml): void {
    if (!entityHtml.prefix)
        reloadMain(entityHtml);
    else
        reloadPopup(entityHtml);
}

export function isNavigatePopup(prefix: string) : boolean {

    if (SF.isEmpty(prefix))
        return false;

    var tempDivId = SF.compose(prefix, "Temp");

    var tempDiv = $("#" + tempDivId);

    var popupOptions = tempDiv.popup();

    return popupOptions.onOk == null
}


function checkValidation(validatorOptions: Validator.ValidationOptions, allowErrors: AllowErrors):  Promise<Validator.ValidationResult> {

    return Validator.validate(validatorOptions).then(result=> {

        if (result.isValid)
            return result;

        Validator.showErrors(validatorOptions, result.modelState);

        if (allowErrors == AllowErrors.Yes)
            return result;

        if (allowErrors == AllowErrors.Ask) {
            if (!confirm(lang.signum.popupErrors))
                return null;

            return result;
        }

        return null;
    });
}

function requestHtml(entityHtml: Entities.EntityHtml, viewOptions: ViewOptionsBase): Promise<Entities.EntityHtml> {
    return new Promise<string>(function (resolve, reject) {
        $.ajax({
            url: viewOptions.controllerUrl,
            data: requestData(entityHtml, viewOptions),
            async: false,
            success: resolve,
            error: reject,
        });
    }).then(htmlText=> {
            entityHtml.loadHtml(htmlText);
            return entityHtml
            });
}


function requestData(entityHtml: Entities.EntityHtml, options: ViewOptionsBase): FormObject {
    var obj: FormObject = {
        entityType: entityHtml.runtimeInfo.type,
        id: entityHtml.runtimeInfo.id,
        prefix: entityHtml.prefix
    };

    if (options.readOnly == true)
        obj["readOnly"] = options.readOnly;

    if (!SF.isEmpty(options.partialViewName)) //Send specific partialview if given
        obj["partialViewName"] = options.partialViewName;

    return $.extend(obj, options.requestExtraJsonData);
}


export function typeChooser(prefix: string, types: ChooserOption[]): Promise<string> {
    return chooser(prefix, lang.signum.chooseAType, types)
        .then(t=> t == null ? null : t.value);
}

export function chooser<T>(prefix: string, title: string, options: T[], getStr?: (data: T) => string, getValue?: (data: T) => string): Promise<T> {

    if (options.length == 1) {
        return Promise.resolve(options[0]);
    }

    var tempDivId = SF.compose(prefix, "Temp");


    if (getStr == null) {
        getStr = (a: any) =>
            a.toStr ? a.toStr :
            a.text ? a.text :
            a.toString();
    }

    if (getValue == null) {
        getValue = (a: any) =>
            a.type ? a.type :
            a.value ? a.value :
            a.toString();
    }

    var div = $('<div id="{0}" class="sf-popup-control" data-prefix="{1}" data-title="{2}"></div>'
        .format(SF.compose(tempDivId, "panelPopup"), tempDivId, title || lang.signum.chooseAValue));

    options.forEach(o=> div.append($('<button type="button" class="sf-chooser-button"/>')
        .data("option", o).attr("data-value", getValue(o)).text(getStr(o))));

    $("body").append(SF.hiddenDiv(tempDivId, div));

    var tempDiv = $("#" + tempDivId);

    return new Promise<T>((resolve, reject) => {

        tempDiv.on("click", ":button", function () {
            var option = <T>$(this).data("option");
            tempDiv.remove();
            resolve(option);
        });

        tempDiv.popup({
            onCancel: function () {
                tempDiv.remove();
                resolve(null);
            }
        });
    });
}

export interface ChooserOption {
    value: string;
    toStr: string;
}

export enum ValueLineType {
    Boolean,
    RadioButtons,
    Combo,
    DateTime,
    TextBox,
    TextArea,
    Number
}

export interface ValueLineBoxOptions {
    type: ValueLineType;
    title: string;
    labelText: string;
    message: string;
    prefix: string;
    value: any;
}

export function valueLineBox(options: ValueLineBoxOptions) : Promise<string>
{
    return new Promise<string>(resolve => {
        requestHtml(Entities.EntityHtml.withoutType(options.prefix), {
            controllerUrl: SF.Urls.valueLineBox,
            requestExtraJsonData: options,
        }).then(eHtml=> {
                var result = null;
                basicPopupView(eHtml, div => {
                    var input = div.find(":input:not(:button)");
                    if (input.length != 1)
                        throw new Error("{0} inputs found in ValueLineBox".format(input.length)); 

                    result = input.val();
                    return Promise.resolve(true);
                }).then(() => resolve(result));
            }); 
    }); 
}


once("widgetToggler", () =>
    $(document).on("click", ".sf-widget-toggler", function (evt) {
        SF.Dropdowns.toggle(evt, this, 1);
        return false;
    }));

