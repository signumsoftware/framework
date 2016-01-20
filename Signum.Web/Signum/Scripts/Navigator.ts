/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")

export interface ViewOptionsBase {
    controllerUrl?: string;
    partialViewName?: string;
    requestExtraJsonData?: any;
    readOnly?: boolean;
    showOperations?: boolean;
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

export function navigate(runtimeInfo: Entities.RuntimeInfo, extraJsonArguments?: any, openNewWindowOrEvent?: any) {
    var url = runtimeInfo.isNew ?
        SF.Urls.create.replace("MyType", runtimeInfo.type) :
        SF.Urls.view.replace("MyType", runtimeInfo.type).replace("MyId", runtimeInfo.id);

    var openNewWindow = isOpenNewWindow(openNewWindowOrEvent);

    if (extraJsonArguments && !$.isEmptyObject(extraJsonArguments)) {
        SF.submitOnly(url, extraJsonArguments, openNewWindow);
    } else {
        if (openNewWindow)
            window.open(url, "_blank");
        else
            window.location.href = url;
    }
}

export function attachMavigatePopupClick(container: JQuery, prefix: string) {

    container.on("click", "[data-entity-link]", event=> {
        var liteKey = $(event.currentTarget).attr("data-entity-link");

        if (isOpenNewWindow(event))
            return;

        event.stopPropagation();
        event.preventDefault();

        navigatePopup(new Entities.EntityHtml(prefix.child("nav"), Entities.RuntimeInfo.fromKey(liteKey)));
    });
}

export function isOpenNewWindow(openNewWindowOrEvent: any): boolean {
    if (openNewWindowOrEvent == null)
        return false;

    if (typeof openNewWindowOrEvent === "boolean")
        return <boolean>openNewWindowOrEvent;

    var event = <MouseEvent>openNewWindowOrEvent;
    if (event.which === undefined)
        throw new Error("openNewWindowOrEvent shold be a boolean or an Event");

    return event.which == 2 || event.ctrlKey;
}

export interface NavigatePopupOptions extends ViewOptionsBase {
    onPopupLoaded?: (popupDiv: JQuery) => void;
}

export function navigatePopup(entityHtml: Entities.EntityHtml, viewOptions?: NavigatePopupOptions): Promise<void> {
    viewOptions = $.extend({
        controllerUrl: SF.Urls.popupNavigate,
        partialViewName: "",
        requestExtraJsonData: null,
        readOnly: false,
        onPopupLoaded: null,
        showOperations: true
    }, viewOptions);

    if (entityHtml.isLoaded())
        return openNavigatePopup(entityHtml, viewOptions);

    return requestHtml(entityHtml, viewOptions).then(eHTml => {
        return openNavigatePopup(eHTml, viewOptions);
    });
}

function openNavigatePopup(entityHtml: Entities.EntityHtml, viewOptions?: NavigatePopupOptions): Promise<void> {

    entityHtml.getChild("panelPopup").data("sf-navigate", true);

    return openEntityHtmlModal(entityHtml, null, viewOptions.onPopupLoaded).then(() => null);
}

export interface ViewPopupOptions extends ViewOptionsBase {
    avoidClone?: boolean;
    avoidValidate?: boolean;
    validationOptions?: Validator.ValidationOptions;
    allowErrors?: AllowErrors;
    onPopupLoaded?: (popupDiv: JQuery) => void;
    saveProtected?: boolean;
    canClose?: (isOk: boolean) => Promise<boolean>;
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
        saveProtected: null,
        showOperations: true,
        avoidClone: false,
        avoidValidate: false,
        allowErrors: AllowErrors.Ask,
        onPopupLoaded: null,
    }, viewOptions);

    if (!viewOptions.canClose) {

        if (viewOptions.avoidValidate)
            viewOptions.canClose = isOk => Promise.resolve(true);
        else {
            viewOptions.validationOptions = $.extend({
                prefix: entityHtml.prefix,
                showPathErrors: true
            }, viewOptions.validationOptions);

            viewOptions.canClose = isOk => {
                if (!isOk)
                    return Promise.resolve(true);

                if (viewOptions.avoidValidate)
                    return Promise.resolve(true);

                return checkValidation(viewOptions.validationOptions, viewOptions.allowErrors).then(valResult=> {
                    if (valResult == null)
                        return false;

                    entityHtml.hasErrors = !valResult.isValid;
                    entityHtml.toStr = valResult.newToStr;

                    return true;
                });
            };
        }
    }

    if (entityHtml.isLoaded()) {

        if (viewOptions.avoidClone)
            return openPopupView(entityHtml, viewOptions);

        var clone = new Entities.EntityHtml(entityHtml.prefix, entityHtml.runtimeInfo, entityHtml.toStr);

        clone.html = entityHtml.html.clone(true);

        return openPopupView(clone, viewOptions);
    }

    return requestHtml(entityHtml, viewOptions).then(eHtml => openPopupView(eHtml, viewOptions));
}

function openPopupView(entityHtml: Entities.EntityHtml, viewOptions: ViewPopupOptions): Promise<Entities.EntityHtml> {

    entityHtml.getChild("panelPopup").data("sf-navigate", false);

    return openEntityHtmlModal(entityHtml, viewOptions.canClose, viewOptions.onPopupLoaded).then(pair=> {
        if (!pair.isOk)
            return null;

        return pair.entityHtml;
    });
}

export function openEntityHtmlModal(entityHtml: Entities.EntityHtml,
    canClose?: (isOk: boolean) => Promise<boolean>,
    shown?: (modalDiv: JQuery) => void
    ): Promise<{ isOk: boolean; entityHtml: Entities.EntityHtml }> {

    if (!canClose)
        canClose = () => Promise.resolve(true);

    var panelPopup = entityHtml.getChild("panelPopup");

    var okButtonId = entityHtml.prefix.child("btnOk");

    return openModal(panelPopup, button => {
        var main = entityHtml.prefix.child("divMainControl").tryGet(panelPopup);
        if (button.id == okButtonId) {
            if ($(button).hasClass("sf-save-protected") && main.hasClass("sf-changed")) {
                alert(lang.signum.saveChangesBeforeOrPressCancel);
                return Promise.resolve(false);
            }

            return canClose(true);

        } else {
            if (main.hasClass("sf-changed") && !confirm(lang.signum.loseCurrentChanges))
                return Promise.resolve(false);

            return canClose(false);
        }
    }, shown).then(pair => {

        var main = entityHtml.prefix.child("divMainControl").tryGet(panelPopup);
        entityHtml.runtimeInfo = Entities.RuntimeInfo.parse(main.data("runtimeinfo"));
        entityHtml.html = pair.modalDiv;

        return { isOk: pair.button && pair.button.id == okButtonId, entityHtml: entityHtml };
    });

}

export function openModal(modalDiv: JQuery,
    canClose?: (button: HTMLElement) => Promise<boolean>,
    shown?: (modalDiv: JQuery) => void
    ): Promise<{ button: HTMLElement; modalDiv: JQuery }> {

    if (!canClose)
        canClose = () => Promise.resolve(true);

    $("body").append(modalDiv);

    return new Promise<{ button: HTMLElement; modalDiv: JQuery }>(function (resolve) {

        var button: HTMLElement = null;
        modalDiv.on("click", ".sf-close-button", function (event) {
            event.preventDefault();

            button = this;

            canClose(button).then(result=> {
                if (result) {
                    modalDiv.modal("hide");
                }
            });
        });

        modalDiv.on("hidden.bs.modal", function (event) {
            modalDiv.remove();

            resolve({ button: button, modalDiv: modalDiv });
        });

        if (shown)
            modalDiv.on("shown.bs.modal", function (event) {
                shown(modalDiv);
            });

        modalDiv.modal({
            keyboard: true,
            backdrop: "static",
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

export function getRuntimeInfoValue(prefix: string): Entities.RuntimeInfo {
    if (!prefix)
        return Entities.RuntimeInfo.getFromPrefix(prefix);

    var mainControl = $("#{0}_divMainControl".format(prefix));

    return Entities.RuntimeInfo.parse(mainControl.data("runtimeinfo"));
}

export function getMainControl(prefix: string): JQuery {
    return $(prefix ? "#{0}_divMainControl".format(prefix) : "#divMainControl");
}

export function hasChanges(prefix: string): boolean {
    return getMainControl(prefix).hasClass("sf-changed");
}

export function getEmptyEntityHtml(prefix: string): Entities.EntityHtml {
    return new Entities.EntityHtml(prefix, getRuntimeInfoValue(prefix));
}

export function reloadMain(entityHtml: Entities.EntityHtml) {
    var $elem = $("#divMainPage");
    $elem.html(entityHtml.html);
}

export function closePopup(prefix: string): void {

    prefix.child("panelPopup").get().modal("hide"); //should remove automatically
}

export function reloadPopup(entityHtml: Entities.EntityHtml) {

    var panelPopupId = entityHtml.prefix.child("panelPopup");

    $("#" + panelPopupId).html(entityHtml.html.filter("#" + panelPopupId).children());
}

export function reload(entityHtml: Entities.EntityHtml): void {
    if (!entityHtml.prefix)
        reloadMain(entityHtml);
    else
        reloadPopup(entityHtml);
}

export function isNavigatePopup(prefix: string): boolean {

    if (SF.isEmpty(prefix))
        return false;

    return prefix.child("panelPopup").get().data("sf-navigate")
}

function checkValidation(validatorOptions: Validator.ValidationOptions, allowErrors: AllowErrors): Promise<Validator.ValidationResult> {

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

    if ((<ViewPopupOptions>options).saveProtected != null)
        obj["saveProtected"] = (<ViewPopupOptions>options).saveProtected;

    if (options.showOperations != null)
        obj["showOperations"] = options.showOperations;

    if (!SF.isEmpty(options.partialViewName)) //Send specific partialview if given
        obj["partialViewName"] = options.partialViewName;

    return $.extend(obj, options.requestExtraJsonData);
}

export interface ConstructChooserOption {
    value: string;
    toStr: string;
    operationConstructor: (form: FormObject) => Promise<FormObject>
}

export function chooseConstructor(extraJsonData: FormObject, prefix: string, title: string, options: ConstructChooserOption[]): Promise<FormObject> {

    return chooser(prefix, title, options).then(co=> {
        if (!co)
            return null;

        extraJsonData = $.extend(extraJsonData, { operationFullKey: co.value });

        if (co.operationConstructor)
            return <any>co.operationConstructor(extraJsonData);

        return extraJsonData;
    });
}

export function typeChooser(prefix: string, types: Entities.TypeInfo[]): Promise<Entities.TypeInfo> {
    return chooser(prefix, lang.signum.chooseAType, types, a=> a.niceName, a=> a.name);
}

export function chooser<T>(prefix: string, title: string, options: T[], getStr?: (data: T) => string, getValue?: (data: T) => string): Promise<T> {

    if (options.length == 1) {
        return Promise.resolve(options[0]);
    }

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

    var modalBody = $("<div>")
    options.forEach(o=> $('<button type="button" class="sf-chooser-button sf-close-button btn btn-default"/>')
        .data("option", o).attr("data-value", getValue(o)).text(getStr(o)).appendTo(modalBody));

    var modalDiv = createBootstrapModal({ titleText: title, prefix: prefix, body: modalBody, titleClose: true });

    var option: T;
    return openModal(modalDiv,
        button => { option = <T>$(button).data("option"); return Promise.resolve(true); })
        .then(pair=> option);
}

export interface MessageBoxOptions {
    prefix: string;

    title: string;
    message: string;
}

export function openMessageBox(options: MessageBoxOptions) {
    var modalBody = $("<div>").text(options.message);

    var modalDiv = createBootstrapModal({
        titleText: options.title,
        prefix: options.prefix,
        body: modalBody,
        titleClose: true,
        footerOkId: options.prefix.child("ok")
    });

    openModal(modalDiv);
}

export interface BootstrapModalOptions {
    prefix: string;

    title?: JQuery;
    titleText?: string;
    titleClose?: boolean;

    body?: JQuery;

    footer?: JQuery;
    footerOkId?: string;
    footerCancelId?: string;
}

export function createBootstrapModal(options: BootstrapModalOptions): JQuery {
    var result = $('<div class="modal fade" tabindex="-1" role="dialog" id="' + options.prefix.child("panelPopup") + '">'
        + '<div class="modal-dialog modal-sm" >'
        + '<div class="modal-content">'

        + (options.title || options.titleText || options.titleClose ? ('<div class="modal-header"></div>') : '')
        + '<div class="modal-body"></div>'
        + (options.footer || options.footerOkId || options.footerCancelId ? ('<div class="modal-footer"></div>') : '')

        + '</div>'
        + '</div>'
        + '</div>');

    if (options.titleClose)
        result.find(".modal-header").append('<button type="button" class="close sf-close-button" aria-hidden="true">×</button>');

    if (options.title)
        result.find(".modal-header").append(options.title);

    if (options.titleText)
        result.find(".modal-header").append($('<h4 class="modal-title"></h4>').text(options.titleText));

    if (options.body)
        result.find(".modal-body").append(options.body);

    if (options.footer)
        result.find(".modal-footer").append(options.footer)

    if (options.footerOkId)
        result.find(".modal-footer").append($('<button class="btn btn-primary sf-entity-button sf-close-button sf-ok-button)">)')
            .attr("id", options.footerOkId)
            .text(lang.signum.ok));

    if (options.footerCancelId)
        result.find(".modal-footer").append($('<button class="btn btn-primary sf-entity-button sf-close-button sf-ok-button)">)')
            .attr("id", options.footerCancelId)
            .text(lang.signum.cancel));

    return result;
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
    prefix: string;
    type: ValueLineType;
    title?: string;
    labelText?: string;
    message?: string;
    value?: any;
}

export function valueLineBox(options: ValueLineBoxOptions): Promise<string> {
    return requestHtml(Entities.EntityHtml.withoutType(options.prefix), {
        controllerUrl: SF.Urls.valueLineBox,
        requestExtraJsonData: options,
    })
        .then(eHtml=> openEntityHtmlModal(eHtml))
        .then(pair => {
        if (!pair.isOk)
            return null;

        var html = pair.entityHtml.html;

        var date = html.find("#" + options.prefix.child("value").child("Date"));
        var time = html.find("#" + options.prefix.child("value").child("Time"));

        if (date.length && time.length)
            return date.val() + " " + time.val();

        var input = pair.entityHtml.html.find(":input:not(:button)");
        if (input.length != 1)
            throw new Error("{0} inputs found in ValueLineBox".format(input.length));

        return input.val();
    });
}


