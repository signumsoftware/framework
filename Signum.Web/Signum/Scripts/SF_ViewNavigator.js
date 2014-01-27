/// <reference path="references.ts"/>
var SF;
(function (SF) {
    (function (ViewNavigator) {
        function loadPartialView(entityHtml, viewOptions) {
            viewOptions = $.extend({
                controllerUrl: SF.Urls.partialView,
                partialViewName: null,
                requestExtraJsonData: null,
                readOnly: false
            }, viewOptions);

            return requestHtml(entityHtml, viewOptions);
        }
        ViewNavigator.loadPartialView = loadPartialView;

        function navigate(runtimeInfo, viewOptions) {
            viewOptions = $.extend({
                controllerUrl: runtimeInfo.isNew ? SF.Urls.create : SF.Urls.view,
                partialViewName: null,
                requestExtraJsonData: null,
                readOnly: false
            }, viewOptions);

            $.ajax({
                url: viewOptions.controllerUrl,
                data: requestData(new SF.EntityHtml(null, runtimeInfo), viewOptions),
                async: false
            });
        }
        ViewNavigator.navigate = navigate;

        function createTempDiv(entityValue) {
            var tempDivId = SF.compose(entityValue.prefix, "Temp");

            $("body").append(SF.hiddenDiv(tempDivId, ""));

            return $("#" + tempDivId);
        }
        ViewNavigator.createTempDiv = createTempDiv;

        function navigatePopup(entityHtml, viewOptions) {
            viewOptions = $.extend({
                controllerUrl: SF.Urls.popupNavigate,
                partialViewName: "",
                requestExtraJsonData: null,
                readOnly: false,
                onPopupLoaded: null,
                onClosed: null
            }, viewOptions);

            if (entityHtml.html != null)
                openNavigatePopup(entityHtml, viewOptions);

            requestHtml(entityHtml, viewOptions).then(function (eHTml) {
                openNavigatePopup(eHTml, viewOptions);
            });
        }
        ViewNavigator.navigatePopup = navigatePopup;

        function openNavigatePopup(entityHtml, viewOptions) {
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

        (function (AllowErrors) {
            AllowErrors[AllowErrors["Ask"] = 0] = "Ask";
            AllowErrors[AllowErrors["Yes"] = 1] = "Yes";
            AllowErrors[AllowErrors["No"] = 2] = "No";
        })(ViewNavigator.AllowErrors || (ViewNavigator.AllowErrors = {}));
        var AllowErrors = ViewNavigator.AllowErrors;

        function viewPopup(entityHtml, viewOptions) {
            viewOptions = $.extend({
                controllerUrl: SF.Urls.popupView,
                partialViewName: null,
                requestExtraJsonData: null,
                readOnly: false,
                avoidClone: false,
                avoidValidate: false,
                allowErrors: 0 /* Ask */,
                onPopupLoaded: null
            }, viewOptions);

            if (!viewOptions.avoidValidate)
                viewOptions.validationOptions = $.extend({
                    prefix: entityHtml.prefix
                }, viewOptions.validationOptions);

            if (entityHtml.html != null) {
                if (viewOptions.avoidClone)
                    return openPopupView(entityHtml, viewOptions);

                var clone = new SF.EntityHtml(entityHtml.prefix, entityHtml.runtimeInfo, entityHtml.toStr, entityHtml.link);

                clone.html = SF.cloneWithValues(entityHtml.html);

                return openPopupView(clone, viewOptions);
            }

            return requestHtml(entityHtml, viewOptions).then(function (eHtml) {
                return openPopupView(eHtml, viewOptions);
            });
        }
        ViewNavigator.viewPopup = viewPopup;

        function openPopupView(entityHtml, viewOptions) {
            var tempDiv = createTempDiv(entityHtml);

            SF.triggerNewContent(tempDiv);

            return new Promise(function (resolve) {
                tempDiv.popup({
                    onOk: function () {
                        if (!viewOptions.avoidValidate) {
                            var valResult = checkValidation(viewOptions.validationOptions, viewOptions.allowErrors);
                            if (valResult == null)
                                return;

                            entityHtml.hasErrors = !valResult.isValid;
                            entityHtml.link = valResult.newLink;
                            entityHtml.toStr = valResult.newToStr;
                        }

                        var $mainControl = tempDiv.find(".sf-main-control[data-prefix=" + entityHtml.prefix + "]");
                        if ($mainControl.length > 0) {
                            entityHtml.runtimeInfo = SF.RuntimeInfoValue.parse($mainControl.data("runtimeinfo"));
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

        function reloadPopup(prefix, newHtml) {
            var tempDivId = SF.compose(prefix, "Temp");

            var tempDiv = $("#" + tempDivId);

            var popupOptions = tempDiv.popup();

            tempDiv.popup("destroy");

            tempDiv.html(newHtml);

            SF.triggerNewContent(tempDiv);

            tempDiv.popup(popupOptions);
        }
        ViewNavigator.reloadPopup = reloadPopup;

        function checkValidation(validatorOptions, allowErrors) {
            var result = SF.Validation.validatePartial(validatorOptions);

            if (result.isValid)
                return result;

            SF.Validation.showErrors(validatorOptions, result.modelState, true);

            if (allowErrors == 1 /* Yes */)
                return result;

            if (allowErrors == 0 /* Ask */) {
                if (!confirm(lang.signum.popupErrors))
                    return null;

                return result;
            }

            return null;
        }

        function requestHtml(entityHtml, viewOptions) {
            return new Promise(function (resolve, reject) {
                $.ajax({
                    url: viewOptions.controllerUrl,
                    data: requestData(entityHtml, viewOptions),
                    async: false,
                    success: resolve,
                    error: reject
                });
            }).then(function (html) {
                entityHtml.html = $(html);
                return entityHtml;
            });
        }

        function requestData(entityHtml, options) {
            var serializer = new SF.Serializer().add({
                entityType: entityHtml.runtimeInfo.type,
                id: entityHtml.runtimeInfo.id,
                prefix: entityHtml.prefix
            });

            if (options.readOnly == true)
                serializer.add("readOnly", options.readOnly);

            if (!SF.isEmpty(options.partialViewName)) {
                serializer.add("partialViewName", options.partialViewName);
            }

            serializer.add(options.requestExtraJsonData);
            return serializer.serialize();
        }

        function typeChooser(staticInfo) {
            var types = staticInfo.types();
            if (types.length == 1) {
                return Promise.resolve(types[0]);
            }

            var typesNiceNames = staticInfo.typeNiceNames();

            var options = types.map(function (t, i) {
                return { id: t, text: typesNiceNames[i] };
            });

            return chooser(staticInfo.prefix, lang.signum.chooseAType, options).then(function (t) {
                return t == null ? null : t.id;
            });
        }
        ViewNavigator.typeChooser = typeChooser;

        function chooser(prefix, title, options) {
            var tempDivId = SF.compose(prefix, "Temp");

            var div = $('<div id="{0}" class="sf-popup-control" data-prefix="{1}" data-title="{2}"></div>'.format(SF.compose(tempDivId, "panelPopup"), tempDivId, title || lang.signum.chooseAValue));

            options.forEach(function (o) {
                return div.append($('<input type="button" class="sf-chooser-button" value="{1}"/>'.format(o.id, o.text)).data("option", o));
            });

            $("body").append(SF.hiddenDiv(tempDivId, div));

            var tempDiv = $("#" + tempDivId);

            SF.triggerNewContent(tempDiv);

            return new Promise(function (resolve, reject) {
                tempDiv.on("click", ":button", function () {
                    var option = $(this).data("option");
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
        ViewNavigator.chooser = chooser;
    })(SF.ViewNavigator || (SF.ViewNavigator = {}));
    var ViewNavigator = SF.ViewNavigator;
})(SF || (SF = {}));
//# sourceMappingURL=SF_ViewNavigator.js.map
