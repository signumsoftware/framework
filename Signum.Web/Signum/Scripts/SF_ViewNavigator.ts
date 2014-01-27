/// <reference path="references.ts"/>

module SF.ViewNavigator {

    export interface ViewOptionsBase {
        controllerUrl?: string;
        partialViewName?: string;
        requestExtraJsonData?: any;
        readOnly?: boolean;
    }

    export function loadPartialView(entityHtml: EntityHtml, viewOptions?: ViewOptionsBase): Promise<EntityHtml> {
        viewOptions = $.extend(viewOptions || {}, {
            controllerUrl: SF.Urls.partialView,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false,
        });

        return requestHtml(entityHtml, viewOptions);
    }

    export function navigate(runtimeInfo: RuntimeInfoValue, viewOptions?: ViewOptionsBase) {
        viewOptions = $.extend(viewOptions || {}, {
            controllerUrl: runtimeInfo.isNew ? SF.Urls.create : SF.Urls.view,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false,
        });

        $.ajax({
            url: viewOptions.controllerUrl,
            data: requestData(new EntityHtml(null, runtimeInfo), viewOptions),
            async: false,
        });
    }

    export interface NavigatePopupOptions extends ViewOptionsBase {
        onPopupLoaded: (popupDiv: JQuery) => void;
        onClosed: () => void;
    }

    export function createTempDiv(entityValue: EntityHtml) : JQuery {
        var tempDivId = SF.compose(entityValue.prefix, "Temp");

        $("body").append(SF.hiddenDiv(tempDivId, ""));

        return $("#" + tempDivId); 
    }

    export function navigatePopup(entityHtml: EntityHtml, viewOptions?: NavigatePopupOptions): void {
        viewOptions = $.extend(viewOptions || {}, {
            controllerUrl: SF.Urls.popupNavigate,
            partialViewName: "",
            requestExtraJsonData: null,
            readOnly: false,
            onPopupLoaded: null,
            onClosed: null,
        });

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

        viewOptions = $.extend(viewOptions || {}, {
            controllerUrl: SF.Urls.popupView,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false,
            avoidClone: false,
            avoidValidate: false,
            allowErrors: AllowErrors.Ask,
            onPopupLoaded : null,
        }); 

        if (!viewOptions.avoidValidate)
            viewOptions.validationOptions = $.extend(viewOptions.validationOptions || {}, {
                prefix: entityHtml.prefix,
            }); 

        
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


    export interface ChooserOption {
        id: string;
        text: string;
    }

    export function typeChooser(staticInfo: StaticInfo): Promise<string>
    {
        var types = staticInfo.types();
        if (types.length == 1) {
            return Promise.resolve(types[0]);
        }

        var typesNiceNames = staticInfo.typeNiceNames();

        var options = types.map((t, i) => <ChooserOption>{ id: t, text: typesNiceNames[i] });

        return chooser(staticInfo.prefix, lang.signum.chooseAType, options)
            .then(t=> t == null ? null : t.id);
    } 

    export function chooser(prefix: string, title: string, options: ChooserOption[]): Promise<ChooserOption>
    {
        var tempDivId = SF.compose(prefix, "Temp");

        var div = $('<div id="{0}" class="sf-popup-control" data-prefix="{1}" data-title="{2}"></div>'
            .format(SF.compose(tempDivId, "panelPopup"), tempDivId, title || lang.signum.chooseAValue));

        options.forEach(o=> div.append($('<input type="button" class="sf-chooser-button" value="{1}"/>'
            .format(o.id, o.text)).data("option", o)));

        $("body").append(SF.hiddenDiv(tempDivId, div));

        var tempDiv = $("#" + tempDivId);

        SF.triggerNewContent(tempDiv);

        return new Promise<ChooserOption>((resolve, reject) => {

            tempDiv.on("click", ":button", function () {
                var option = <ChooserOption>$(this).data("option");
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