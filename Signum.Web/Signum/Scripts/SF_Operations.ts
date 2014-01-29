/// <reference path="references.ts"/>

module SF.Operations
{
    export interface OperationOptions {
        prefix: string;
        operationKey: string;
        controllerUrl?: string;
        requestExtraJsonData?: any;
        isLite: boolean;
    }

    export interface EntityOperationOptions extends OperationOptions {
        sender?: string;

        avoidValidate?: boolean;
        validationOptions?: Validation.ValidationOptions;
        parentDiv?: string;
    }

    function execute(options: EntityOperationOptions): Promise<void> {
        options = $.extend({
            controllerUrl: SF.Urls.operationExecute,
            requestExtraJsonData: null,

            sender: null,
            avoidValidate: false,
            validationOptions: {},
            isLite: false,
            parentDiv: null
        }, options);

        if (!entityIsValidOrLite(options))
            return Promise.reject(new Error("validation error")); 

        return Operations.ajax(options, entityRequestData(options)).then(result=>
            reloadContent(options, result));
    }

    function executeContextual(options: OperationOptions): Promise<void> {
        options = $.extend({
            controllerUrl: SF.Urls.operationContextualExecute,
            requestExtraJsonData: null,
        }, options);

        SF.FindNavigator.removeOverlay()

        return Operations.ajax(options, contextualRequestData(options, true)).then(result=> {
            markCells(options.prefix, result);
        }); 
    }


    function constructFrom(options: EntityOperationOptions, newPrefix?: string): Promise<void>  {
        options = $.extend({
            controllerUrl: SF.Urls.operationConstructFrom,
            requestExtraJsonData: null,

            sender: null,
            avoidValidate: false,
            validationOptions: {},
            isLite: false,
            parentDiv: null
        }, options);

        if (!entityIsValidOrLite(options))
            return Promise.reject(new Error("validation error")); 

        if (!newPrefix)
            newPrefix = getNewPrefix(options);

        return Operations.ajax(options, entityRequestData(options, newPrefix)).then(result=>
            openPopup(newPrefix, result));
    }

    function constructFromContextual(options: OperationOptions, newPrefix?: string): Promise<void> {
        options = $.extend({
            controllerUrl: SF.Urls.operationContextualConstructFrom,
            requestExtraJsonData: null,
        }, options);

        SF.FindNavigator.removeOverlay();

        if (!newPrefix)
            newPrefix = getNewPrefix(options);

        return Operations.ajax(options, contextualRequestData(options, true, newPrefix)).then(result=> {
            openPopup(newPrefix, result); 
            markCells(options.prefix, null);
        });
    }

    function deleteEntity(options: EntityOperationOptions): Promise<void> {
        options = $.extend({
            controllerUrl: SF.Urls.operationDelete,
            requestExtraJsonData: null,

            sender: null,
            avoidValidate: false,
            validationOptions: {},
            isLite: false,
            parentDiv: null
        }, options);

        return Operations.ajax(options, entityRequestData(options)); //ajax prefilter will take redirect
    }

    function deleteContextual(options: OperationOptions): Promise<void> {
        options = $.extend({
            controllerUrl: SF.Urls.operationContextualDelete,
            requestExtraJsonData: null,
        }, options);

        SF.FindNavigator.removeOverlay()

        return Operations.ajax(options, contextualRequestData(options, true)).then(result=> {
            markCells(options.prefix, result);
        });
    }

    function constructFromMany(options: OperationOptions, newPrefix?: string): Promise<void> {
        options = $.extend({
            controllerUrl: SF.Urls.operationConstructFromMany,
            requestExtraJsonData: null,
        }, options);

        SF.FindNavigator.removeOverlay();

        if (!newPrefix)
            newPrefix = getNewPrefix(options);

        return Operations.ajax(options, contextualRequestData(options, false, newPrefix)).then(result=> {
            openPopup(newPrefix, result);
            markCells(options.prefix, null);
        });
    }

    export function openPopup(newPrefix : string, newHtml: string) {
        disableContextMenu();
        var entity = EntityHtml.fromHtml(newPrefix, newHtml);
        ViewNavigator.navigatePopup(entity);
        SF.Notify.info(lang.signum.executed, 2000);
    }

    export function disableContextMenu() {
        $(".sf-ctxmenu-active").removeClass("sf-ctxmenu-active");
    }

    function markCells(prefix: string, operationResult: any) {
        $(".sf-ctxmenu-active")
            .addClass("sf-entity-ctxmenu-" + (SF.isEmpty(operationResult) ? "success" : "error"))
            .removeClass("sf-ctxmenu-active");

        SF.Notify.info(lang.signum.executed, 2000);
    }

    export function reloadContent(options: EntityOperationOptions, newHtml: string) {
        if (!options.prefix) { //NormalWindow
            var $elem = SF.isEmpty(options.parentDiv) ? $("#divNormalControl") : $("#" + options.parentDiv );
            $elem.html(newHtml);
            SF.triggerNewContent($elem);
        }
        else { //PopupWindow
            ViewNavigator.reloadPopup(options.prefix, newHtml);
        }
        SF.Notify.info(lang.signum.executed, 2000);
    }

    export function getNewPrefix(optons: OperationOptions) {
        return SF.compose("New", this.options.prefix);
    }

    export function entityRequestData(options: EntityOperationOptions, newPrefix?: string) {
        var formChildren: JQuery = null;
        if (SF.isFalse(options.isLite)) {
            if (SF.isEmpty(options.prefix)) { //NormalWindow 
                formChildren = SF.isEmpty(options.parentDiv) ? $(options.sender).closest("form").find(":input") : $("#" + options.parentDiv + " :input");
            }
            else { //PopupWindow
                formChildren = $("#{0}_panelPopup :input".format(options.prefix))
                    .add("#" + SF.Keys.tabId)
                    .add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");
            }
        }
        else {
            formChildren = $('#' + SF.Keys.tabId + ", input:hidden[name=" + SF.Keys.antiForgeryToken + "], #" + SF.compose(options.prefix, SF.Keys.runtimeInfo));
        }
        formChildren = formChildren.not(".sf-search-control *");

        var serializer = new SF.Serializer();
        serializer.add(formChildren.serialize());

        serializer.add({
            isLite: options.isLite,
            operationFullKey: options.operationKey,
            prefix: options.prefix,
            newPrefix: newPrefix,
        });

        if (options.prefix) {
            var $mainControl = $(".sf-main-control[data-prefix=" + options.prefix + "]");

            //Check runtimeInfo present => if it's a popup from a LineControl it will not be
            var myRuntimeInfoKey = SF.compose(options.prefix, SF.Keys.runtimeInfo);
            if (formChildren.filter("#" + myRuntimeInfoKey).length == 0) {
                var value = $mainControl.data("runtimeinfo");
                serializer.add(myRuntimeInfoKey, value);
            }

            if ($mainControl.closest(".sf-popup-control").children(".sf-button-bar").find(".sf-ok-button").length > 0) {
                serializer.add("sfOkVisible", true);
            }
        }

        serializer.add(options.requestExtraJsonData);

        return serializer.serialize();
    }

    export function contextualRequestData(options: OperationOptions, justOne : boolean, newPrefix?: string) {

        var items = FindNavigator.getFor(options.prefix).selectedItems();

        if (items.length > 1 && justOne)
            throw new Error("just one entity should have been selected"); 

        var serializer = new SF.Serializer();
        serializer.add($("input:hidden[name=" + SF.Keys.antiForgeryToken + "]").serialize());

        serializer.add({
            isLite: options.isLite,
            operationFullKey: options.operationKey,
            newprefix: newPrefix,
            prefix: options.prefix,
            liteKeys: items.map(i=>i.runtimeInfo.key()).join(","),
        });
        serializer.add(options.requestExtraJsonData);

        return serializer.serialize();
    }


    export function ajax(options: OperationOptions, data: any): Promise<any> {
        return Blocker.wrap(() =>
            new Promise<any>((resolve, reject) => {
                $.ajax({
                    url: options.controllerUrl,
                    data: data,
                    success: (operationResult) => {
                        if (modelStateErrors(operationResult, options)) {
                            SF.Notify.error(lang.signum.error, 2000);

                            reject(operationResult);
                        }
                        else
                            resolve(operationResult);
                    },
                    error: error => {
                        SF.Notify.error(lang.signum.error, 2000);
                        reject(error);
                    }
                });

            }));
    }

    function modelStateErrors(operationResult: any, options: OperationOptions) {
        if ((typeof (operationResult) !== "object") || (operationResult.result != "ModelState"))
            return false;

        var modelState = operationResult.ModelState;

        SF.Validation.showErrors({ prefix: options.prefix }, modelState);

        return true;
    }

    export function entityIsValidOrLite(options: EntityOperationOptions) {
        if (options.isLite || options.avoidValidate)
            return true;

        var valOptions = $.extend({ prefix: options.prefix, parentDiv: options.parentDiv }, options.validationOptions);

        return SF.Validation.entityIsValid(valOptions);
    }

    export function validateAndSubmit(options: EntityOperationOptions) {
        if (entityIsValidOrLite(options))
            submit(options);
    }

     export function submit(options: EntityOperationOptions) {
        var $form = $(options.sender).closest("form");
        $form.append(SF.hiddenInput('isLite', options.isLite) +
            SF.hiddenInput('operationFullKey', options.operationKey) +
            SF.hiddenInput("oldPrefix", options.prefix));

        if (!SF.isEmpty(options.prefix)) {
            //Check runtimeInfo present => if it's a popup from a LineControl it will not be
            var myRuntimeInfoKey = SF.compose(options.prefix, SF.Keys.runtimeInfo);
            if ($form.filter("#" + myRuntimeInfoKey).length == 0) {
                var $mainControl = $(".sf-main-control[data-prefix=" + options.prefix + "]");
                SF.hiddenInput(myRuntimeInfoKey, $mainControl.data("runtimeinfo"));
            }
        }

        SF.submit(options.controllerUrl, options.requestExtraJsonData, $form);

        return false;
    }
}
