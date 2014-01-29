/// <reference path="references.ts"/>

module SF.ViewNavigator {

    export interface ViewOptionsBase {
        controllerUrl?: string;
        partialViewName?: string;
        requestExtraJsonData?: any;
        readOnly?: boolean;
    }

    export function loadPartialView(entityHtml: EntityHtml, viewOptions?: ViewOptionsBase): Promise<EntityHtml> {
        viewOptions = $.extend( {
            controllerUrl: SF.Urls.partialView,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false,
        }, viewOptions);

        return requestHtml(entityHtml, viewOptions);
    }

    export function navigate(runtimeInfo: RuntimeInfoValue, viewOptions?: ViewOptionsBase) {
        viewOptions = $.extend({
            controllerUrl: runtimeInfo.isNew ? SF.Urls.create : SF.Urls.view,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false,
        }, viewOptions);

        $.ajax({
            url: viewOptions.controllerUrl,
            data: requestData(new EntityHtml(null, runtimeInfo), viewOptions),
            async: false,
        });
    }

    export interface NavigatePopupOptions extends ViewOptionsBase {
        onPopupLoaded?: (popupDiv: JQuery) => void;
        onClosed?: () => void;
    }

    export function createTempDiv(entityValue: EntityHtml) : JQuery {
        var tempDivId = SF.compose(entityValue.prefix, "Temp");

        $("body").append(SF.hiddenDiv(tempDivId, ""));

        return $("#" + tempDivId); 
    }

    export function navigatePopup(entityHtml: EntityHtml, viewOptions?: NavigatePopupOptions): void {
        viewOptions = $.extend( {
            controllerUrl: SF.Urls.popupNavigate,
            partialViewName: "",
            requestExtraJsonData: null,
            readOnly: false,
            onPopupLoaded: null,
            onClosed: null,
        }, viewOptions);

        if (entityHtml.html != null)
            openNavigatePopup(entityHtml, viewOptions);

        requestHtml(entityHtml, viewOptions).then(eHTml => {
            openNavigatePopup(eHTml, viewOptions);
        }); 
    }

    function openNavigatePopup(entityHtml: EntityHtml, viewOptions: NavigatePopupOptions) : void {

        var tempDiv = createTempDiv(entityHtml);

        tempDiv.html(entityHtml.html);

        SF.triggerNewContent(tempDiv);

        tempDiv.popup({
            onCancel: function () {

                tempDiv.remove();

                if (viewOptions.onClosed != null)
                    viewOptions.onClosed(); 
            }
        });

        if (viewOptions.onPopupLoaded != null)
            viewOptions.onPopupLoaded(tempDiv);
    }


    export interface ViewPopupOptions extends ViewOptionsBase {
        avoidClone?: boolean;
        avoidValidate?: boolean;
        validationOptions?: SF.Validation.ValidationOptions;
        allowErrors?: AllowErrors;
        onPopupLoaded?: (popupDiv: JQuery) => void;
    }

    export enum AllowErrors {
        Ask,
        Yes,
        No,
    }

    export function viewPopup(entityHtml : EntityHtml, viewOptions?: ViewPopupOptions): Promise<EntityHtml> {

        viewOptions = $.extend({
            controllerUrl: SF.Urls.popupView,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false,
            avoidClone: false,
            avoidValidate: false,
            allowErrors: AllowErrors.Ask,
            onPopupLoaded : null,
        }, viewOptions); 

        if (!viewOptions.avoidValidate)
            viewOptions.validationOptions = $.extend({
                prefix: entityHtml.prefix,
            }, viewOptions.validationOptions); 

        
        if (entityHtml.html != null) {

            if (viewOptions.avoidClone)
                return openPopupView(entityHtml, viewOptions); 

            var clone = new EntityHtml(entityHtml.prefix, entityHtml.runtimeInfo, entityHtml.toStr, entityHtml.link);

            clone.html = cloneWithValues(entityHtml.html);

            return openPopupView(clone, viewOptions);
        }

        return requestHtml(entityHtml, viewOptions).then(eHtml => openPopupView(eHtml, viewOptions));
    }

    function openPopupView(entityHtml: EntityHtml, viewOptions: ViewPopupOptions): Promise<EntityHtml>
    {
        var tempDiv = createTempDiv(entityHtml);

        SF.triggerNewContent(tempDiv);

        return new Promise<EntityHtml>(function (resolve) {
            tempDiv.popup({
                onOk: function ()
                {
                    if (!viewOptions.avoidValidate)
                    {
                        var valResult = checkValidation(viewOptions.validationOptions, viewOptions.allowErrors); 
                        if (valResult == null)
                            return;

                        entityHtml.hasErrors = !valResult.isValid;
                        entityHtml.link = valResult.newLink;
                        entityHtml.toStr = valResult.newToStr;
                    }

                    var $mainControl = tempDiv.find(".sf-main-control[data-prefix=" + entityHtml.prefix + "]");
                    if ($mainControl.length > 0) {
                        entityHtml.runtimeInfo = RuntimeInfoValue.parse($mainControl.data("runtimeinfo"));
                    }

                    tempDiv.popup('destroy');
                    tempDiv.remove();

                    resolve(entityHtml);
                },
                onCancel: function () {
                    tempDiv.remove();

                    resolve(null);
                }
            });

            if (viewOptions.onPopupLoaded != null)
                viewOptions.onPopupLoaded(tempDiv);
        });
    }

    export function reloadContent(prefix: string, options?: ViewOptionsBase) : Promise<EntityHtml> {
        if (!prefix) { //NormalWindow

            options = $.extend({
                controllerUrl: SF.Urls.normalControl,
            }, options);

            var mainControl = $("#divNormalControl");

            return requestHtml(new EntityHtml(prefix, new RuntimeInfoElement(prefix).value()), options).then(eHtml=> {
               
                mainControl.html(eHtml.html);

                SF.triggerNewContent(mainControl);

                return eHtml;
            });
        }
        else { //PopupWindow

            options = $.extend({
                controllerUrl: SF.Urls.popupView,
            }, options);

            var mainControl = $("#{0}_divNormalControl".format(prefix));

            return requestHtml(new EntityHtml(prefix, RuntimeInfoValue.parse(mainControl.data("runtimeInfo"))), options).then(eHtml=> {
                var mainControl = $("#divNormalControl");

                ViewNavigator.reloadPopup(prefix, eHtml.html);

                return eHtml;
            });
        }
    }

    export function reloadPopup(prefix: string, newHtml: any) {

        var tempDivId = SF.compose(prefix, "Temp");

        var tempDiv = $("#" + tempDivId); 

        var popupOptions = tempDiv.popup();

        tempDiv.popup("destroy");

        tempDiv.html(newHtml); 

        SF.triggerNewContent(tempDiv);

        tempDiv.popup(popupOptions); 
    }

    function checkValidation(validatorOptions: Validation.ValidationOptions, allowErrors: AllowErrors): SF.Validation.ValidationResult {

        var result = Validation.validatePartial(validatorOptions);

        if (result.isValid)
            return result;

        Validation.showErrors(validatorOptions, result.modelState, true);

        if (allowErrors == AllowErrors.Yes)
            return result; 

        if (allowErrors == AllowErrors.Ask) {
            if (!confirm(lang.signum.popupErrors))
                return null;

            return result;
        }

        return null;
    }

    function requestHtml(entityHtml: EntityHtml, viewOptions: ViewOptionsBase): Promise<EntityHtml> {
        return new Promise<string>(function (resolve, reject) {
            $.ajax({
                url: viewOptions.controllerUrl,
                data: requestData(entityHtml, viewOptions),
                async: false,
                success: resolve,
                error: reject
            });
        }).then(html=> {
                entityHtml.html = $(html);
            return entityHtml
            });
    }

    function requestData(entityHtml: EntityHtml,  options: ViewOptionsBase) {
        var serializer = new SF.Serializer()
            .add({
                entityType: entityHtml.runtimeInfo.type,
                id: entityHtml.runtimeInfo.id,
                prefix: entityHtml.prefix
            });

        if (options.readOnly == true)
            serializer.add("readOnly", options.readOnly);

        if (!SF.isEmpty(options.partialViewName)) { //Send specific partialview if given
            serializer.add("partialViewName", options.partialViewName);
        }

        serializer.add(options.requestExtraJsonData);
        return serializer.serialize();
    }


    export function typeChooser(staticInfo: StaticInfo): Promise<string>
    {
        var types = staticInfo.types();
        if (types.length == 1) {
            return Promise.resolve(types[0]);
        }

        var typesNiceNames = staticInfo.typeNiceNames();

        var options = types.map((t, i) => ({ type: t, text: typesNiceNames[i] }));

        return chooser(staticInfo.prefix, lang.signum.chooseAType, options)
            .then(t=> t == null ? null : t.type);
    } 

    export function chooser<T>(prefix: string, title: string, options: T[], getStr?: (data: T) => string): Promise<T> {
        var tempDivId = SF.compose(prefix, "Temp");

        if (getStr == null) {
            getStr = (a: any) => a.toString ? a.toString() :
                a.toStr ? a.toStr :
                a.text ? a.text :
                a;
        }

        var div = $('<div id="{0}" class="sf-popup-control" data-prefix="{1}" data-title="{2}"></div>'
            .format(SF.compose(tempDivId, "panelPopup"), tempDivId, title || lang.signum.chooseAValue));

        options.forEach(o=> div.append($('<input type="button" class="sf-chooser-button"/>')
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

}