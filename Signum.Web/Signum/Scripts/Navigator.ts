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

export function navigate(runtimeInfo: Entities.RuntimeInfoValue, viewOptions?: ViewOptionsBase) {
    viewOptions = $.extend({
        controllerUrl: runtimeInfo.isNew ? SF.Urls.create : SF.Urls.view,
        partialViewName: null,
        requestExtraJsonData: null,
        readOnly: false,
    }, viewOptions);

    $.ajax({
        url: viewOptions.controllerUrl,
        data: requestData(new Entities.EntityHtml("", runtimeInfo), viewOptions),
        async: false,
    });
}

export interface NavigatePopupOptions extends ViewOptionsBase {
    onPopupLoaded?: (popupDiv: JQuery) => void;
}

export function createTempDiv(entityHtml: Entities.EntityHtml): string {
    var tempDivId = SF.compose(entityHtml.prefix, "Temp");

    $("body").append(SF.hiddenDiv(tempDivId, ""));

    var tempDiv = $("#" + tempDivId);

    tempDiv.html(entityHtml.html);

    SF.triggerNewContent(tempDiv);

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
                    var newTempDiv = $("#" + tempDivId);

                    if (result) {
                        var $mainControl = newTempDiv.find(".sf-main-control[data-prefix=" + entityHtml.prefix + "]");
                        if ($mainControl.length > 0) {
                            entityHtml.runtimeInfo = Entities.RuntimeInfoValue.parse($mainControl.data("runtimeinfo"));
                        }

                        newTempDiv.popup('restoreTitle');
                        newTempDiv.popup('destroy');
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

export function getRuntimeInfoValue(prefix: string) : Entities.RuntimeInfoValue {
    if (!prefix)
        return new Entities.RuntimeInfoElement(prefix).value();

    var mainControl = $("#{0}_divMainControl".format(prefix)); 

    return Entities.RuntimeInfoValue.parse(mainControl.data("runtimeinfo"));
}

export function getEmptyEntityHtml(prefix: string): Entities.EntityHtml {
    return new Entities.EntityHtml(prefix, getRuntimeInfoValue(prefix));
}

export function reloadMain(entityHtml: Entities.EntityHtml) {
    var $elem = $("#divNormalControl");
    $elem.html(entityHtml.html);
    SF.triggerNewContent($elem);
}

export function closePopup(prefix: string): void {

    var tempDivId = SF.compose(prefix, "Temp");

    var tempDiv = $("#" + tempDivId);

    var popupOptions = tempDiv.popup();

    tempDiv.popup("destroy");

    tempDiv.remove();
}

export function isNavigatePopup(prefix: string)
{
    var tempDivId = SF.compose(prefix, "Temp");

    var tempDiv = $("#" + tempDivId);

    var popupOptions = tempDiv.popup();

    return popupOptions.onOk == null
}

export function reloadPopup(entityHtml : Entities.EntityHtml) {

    var tempDivId = SF.compose(entityHtml.prefix, "Temp");

    var tempDiv = $("#" + tempDivId);

    var popupOptions = tempDiv.popup();

    tempDiv.popup("destroy");

    tempDiv.html(entityHtml.html);

    SF.triggerNewContent(tempDiv);

    tempDiv.popup(popupOptions);
}

export function reload(entityHtml: Entities.EntityHtml): void {
    if (!entityHtml.prefix)
        reloadMain(entityHtml);
    else
        reloadPopup(entityHtml);
}

export function viewMode(prefix: string): string {
   return $(".sf-main-control[data-prefix=" + prefix + "]")
        .closest(".sf-popup-control").children(".sf-button-bar").find(".sf-ok-button").length > 0 ? "View" : "Navigate"
}


function checkValidation(validatorOptions: Validator.ValidationOptions, allowErrors: AllowErrors):  Promise<Validator.ValidationResult> {

    return Validator.validate(validatorOptions).then(result=> {

        if (result.isValid)
            return result;

        Validator.showErrors(validatorOptions, result.modelState, true);

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
            error: reject
        });
    }).then(htmlText=> {
            entityHtml.loadHtml(htmlText);
            return entityHtml
            });
}

export function serialize(prefix): string {
    var id = SF.compose(prefix, "panelPopup");
    var $formChildren = $("#" + id + " :input");
    var data = $formChildren.serialize();

    var myRuntimeInfoKey = SF.compose(prefix, Entities.Keys.runtimeInfo);
    if ($formChildren.filter("#" + myRuntimeInfoKey).length == 0) {
        var $mainControl = $(".sf-main-control[data-prefix=" + prefix + "]");
        data += "&" + myRuntimeInfoKey + "=" + $mainControl.data("runtimeinfo");
    }
    return data;
}

export function serializeJson(prefix): any {

    var id = SF.compose(prefix, "panelPopup");
    var arr = $("#" + id + " :input").serializeArray();
    var data = {};
    for (var index = 0; index < arr.length; index++) {
        if (data[arr[index].name] != null) {
            data[arr[index].name] += "," + arr[index].value;
        }
        else {
            data[arr[index].name] = arr[index].value;
        }
    }

    var myRuntimeInfoKey = SF.compose(prefix, Entities.Keys.runtimeInfo);
    if (typeof data[myRuntimeInfoKey] == "undefined") {
        var $mainControl = $(".sf-main-control[data-prefix=" + prefix + "]");
        data[myRuntimeInfoKey] = $mainControl.data("runtimeinfo");
    }
    return data;
};

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


export function typeChooser(staticInfo: Entities.StaticInfo): Promise<string> {
    var types = staticInfo.types();
    if (types.length == 1) {
        return Promise.resolve(types[0]);
    }

    var typesNiceNames = staticInfo.typeNiceNames();

    var options = types.map((t, i) => ({ type: t, text: typesNiceNames[i] }));

    return chooser(staticInfo.prefix, lang.signum.chooseAType, options)
        .then(t=> t == null ? null : t.type);
}

export function chooser<T>(parentPrefix: string, title: string, options: T[], getStr?: (data: T) => string, getValue?: (data: T) => string): Promise<T> {

    var tempDivId = SF.compose(parentPrefix, "Temp");

    if (getStr == null) {
        getStr = (a: any) =>
            a.toStr ? a.toStr :
            a.text ? a.text :
            a.toString();
    }

    if (getValue == null) {
        getValue = (a: any) =>
            a.toStr ? a.type :
            a.text ? a.value :
            a.toString();
    }

    var div = $('<div id="{0}" class="sf-popup-control" data-prefix="{1}" data-title="{2}"></div>'
        .format(SF.compose(tempDivId, "panelPopup"), tempDivId, title || lang.signum.chooseAValue));

    options.forEach(o=> div.append($('<button type="button" class="sf-chooser-button"/>')
        .data("option", o).text(getStr(o))));

    $("body").append(SF.hiddenDiv(tempDivId, div));

    var tempDiv = $("#" + tempDivId);

    SF.triggerNewContent(tempDiv);

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


once("widgetToggler", () =>
    $(document).on("click", ".sf-widget-toggler", function (evt) {
        SF.Dropdowns.toggle(evt, this, 1);
        return false;
    }));

