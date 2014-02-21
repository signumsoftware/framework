/// <reference path="globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Validator"], function(require, exports, Entities, Validator) {
    function requestPartialView(entityHtml, viewOptions) {
        viewOptions = $.extend({
            controllerUrl: SF.Urls.partialView,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false
        }, viewOptions);

        return requestHtml(entityHtml, viewOptions);
    }
    exports.requestPartialView = requestPartialView;

    function navigate(runtimeInfo, openNewWindow) {
        var url = runtimeInfo.isNew ? "{0}/{1}".format(SF.Urls.create, runtimeInfo.type) : "{0}/{1}/{2}".format(SF.Urls.view, runtimeInfo.type, !SF.isEmpty(runtimeInfo.id) ? runtimeInfo.id : "");

        if (openNewWindow)
            window.open(url, "_blank");
        else
            window.location.href = url;
    }
    exports.navigate = navigate;

    function createTempDiv(entityHtml) {
        var tempDivId = SF.compose(entityHtml.prefix, "Temp");

        $("body").append(SF.hiddenDiv(tempDivId, ""));

        var tempDiv = $("#" + tempDivId);

        tempDiv.html(entityHtml.html);

        SF.triggerNewContent(tempDiv);

        return tempDivId;
    }
    exports.createTempDiv = createTempDiv;

    function navigatePopup(entityHtml, viewOptions) {
        viewOptions = $.extend({
            controllerUrl: SF.Urls.popupNavigate,
            partialViewName: "",
            requestExtraJsonData: null,
            readOnly: false,
            onPopupLoaded: null
        }, viewOptions);

        if (entityHtml.isLoaded())
            return openNavigatePopup(entityHtml, viewOptions);

        return requestHtml(entityHtml, viewOptions).then(function (eHTml) {
            return openNavigatePopup(eHTml, viewOptions);
        });
    }
    exports.navigatePopup = navigatePopup;

    function openNavigatePopup(entityHtml, viewOptions) {
        var tempDivId = exports.createTempDiv(entityHtml);

        var tempDiv = $("#" + tempDivId);

        var result = new Promise(function (resolve) {
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

    (function (AllowErrors) {
        AllowErrors[AllowErrors["Ask"] = 0] = "Ask";
        AllowErrors[AllowErrors["Yes"] = 1] = "Yes";
        AllowErrors[AllowErrors["No"] = 2] = "No";
    })(exports.AllowErrors || (exports.AllowErrors = {}));
    var AllowErrors = exports.AllowErrors;

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

        return requestHtml(entityHtml, viewOptions).then(function (eHtml) {
            return openPopupView(eHtml, viewOptions);
        });
    }
    exports.viewPopup = viewPopup;

    function openPopupView(entityHtml, viewOptions) {
        var tempDivId = exports.createTempDiv(entityHtml);

        var tempDiv = $("#" + tempDivId);

        return new Promise(function (resolve) {
            tempDiv.popup({
                onOk: function () {
                    var continuePromise = viewOptions.avoidValidate ? Promise.resolve(true) : checkValidation(viewOptions.validationOptions, viewOptions.allowErrors).then(function (valResult) {
                        if (valResult == null)
                            return false;

                        entityHtml.hasErrors = !valResult.isValid;
                        entityHtml.link = valResult.newLink;
                        entityHtml.toStr = valResult.newToStr;

                        return true;
                    });

                    continuePromise.then(function (result) {
                        if (result) {
                            var newTempDiv = $("#" + tempDivId);
                            var $mainControl = newTempDiv.find(".sf-main-control[data-prefix=" + entityHtml.prefix + "]");
                            if ($mainControl.length > 0) {
                                entityHtml.runtimeInfo = Entities.RuntimeInfo.parse($mainControl.data("runtimeinfo"));
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

    function basicPopupView(entityHtml, onOk) {
        var tempDivId = exports.createTempDiv(entityHtml);

        var tempDiv = $("#" + tempDivId);

        return new Promise(function (resolve) {
            tempDiv.popup({
                onOk: function () {
                    onOk($("#" + tempDivId)).then(function (result) {
                        if (result) {
                            var newTempDiv = $("#" + tempDivId);
                            newTempDiv.popup('destroy');
                            resolve(null);
                        }
                    });
                },
                onCancel: function () {
                    var newTempDiv = $("#" + tempDivId);
                    newTempDiv.popup('destroy');
                    resolve(null);
                }
            });
        });
    }
    exports.basicPopupView = basicPopupView;

    function requestAndReload(prefix, options) {
        options = $.extend({
            controllerUrl: !prefix ? SF.Urls.normalControl : exports.isNavigatePopup(prefix) ? SF.Urls.popupNavigate : SF.Urls.popupView
        }, options);

        return requestHtml(exports.getEmptyEntityHtml(prefix), options).then(function (eHtml) {
            exports.reload(eHtml);

            eHtml.html = null;

            return eHtml;
        });
    }
    exports.requestAndReload = requestAndReload;

    function getRuntimeInfoValue(prefix) {
        if (!prefix)
            return Entities.RuntimeInfo.getFromPrefix(prefix);

        var mainControl = $("#{0}_divMainControl".format(prefix));

        return Entities.RuntimeInfo.parse(mainControl.data("runtimeinfo"));
    }
    exports.getRuntimeInfoValue = getRuntimeInfoValue;

    function getEmptyEntityHtml(prefix) {
        return new Entities.EntityHtml(prefix, exports.getRuntimeInfoValue(prefix));
    }
    exports.getEmptyEntityHtml = getEmptyEntityHtml;

    function reloadMain(entityHtml) {
        var $elem = $("#divNormalControl");
        $elem.html(entityHtml.html);
        SF.triggerNewContent($elem);
    }
    exports.reloadMain = reloadMain;

    function closePopup(prefix) {
        var tempDivId = SF.compose(prefix, "Temp");

        var tempDiv = $("#" + tempDivId);

        var popupOptions = tempDiv.popup();

        tempDiv.popup("destroy");

        tempDiv.remove();
    }
    exports.closePopup = closePopup;

    function reloadPopup(entityHtml) {
        var tempDivId = SF.compose(entityHtml.prefix, "Temp");

        var tempDiv = $("#" + tempDivId);

        var popupOptions = tempDiv.popup();

        tempDiv.popup("destroy");

        tempDiv.html(entityHtml.html);

        SF.triggerNewContent(tempDiv);

        tempDiv.popup(popupOptions);
    }
    exports.reloadPopup = reloadPopup;

    function reload(entityHtml) {
        if (!entityHtml.prefix)
            exports.reloadMain(entityHtml);
        else
            exports.reloadPopup(entityHtml);
    }
    exports.reload = reload;

    function isNavigatePopup(prefix) {
        if (SF.isEmpty(prefix))
            return false;

        var tempDivId = SF.compose(prefix, "Temp");

        var tempDiv = $("#" + tempDivId);

        var popupOptions = tempDiv.popup();

        return popupOptions.onOk == null;
    }
    exports.isNavigatePopup = isNavigatePopup;

    function checkValidation(validatorOptions, allowErrors) {
        return Validator.validate(validatorOptions).then(function (result) {
            if (result.isValid)
                return result;

            Validator.showErrors(validatorOptions, result.modelState);

            if (allowErrors == 1 /* Yes */)
                return result;

            if (allowErrors == 0 /* Ask */) {
                if (!confirm(lang.signum.popupErrors))
                    return null;

                return result;
            }

            return null;
        });
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
        }).then(function (htmlText) {
            entityHtml.loadHtml(htmlText);
            return entityHtml;
        });
    }

    function requestData(entityHtml, options) {
        var obj = {
            entityType: entityHtml.runtimeInfo.type,
            id: entityHtml.runtimeInfo.id,
            prefix: entityHtml.prefix
        };

        if (options.readOnly == true)
            obj["readOnly"] = options.readOnly;

        if (!SF.isEmpty(options.partialViewName))
            obj["partialViewName"] = options.partialViewName;

        return $.extend(obj, options.requestExtraJsonData);
    }

    function typeChooser(prefix, types) {
        return exports.chooser(prefix, lang.signum.chooseAType, types).then(function (t) {
            return t == null ? null : t.type;
        });
    }
    exports.typeChooser = typeChooser;

    function chooser(prefix, title, options, getStr, getValue) {
        if (options.length == 1) {
            return Promise.resolve(options[0]);
        }

        var tempDivId = SF.compose(prefix, "Temp");

        if (getStr == null) {
            getStr = function (a) {
                return a.toStr ? a.toStr : a.text ? a.text : a.toString();
            };
        }

        if (getValue == null) {
            getValue = function (a) {
                return a.type ? a.type : a.value ? a.value : a.toString();
            };
        }

        var div = $('<div id="{0}" class="sf-popup-control" data-prefix="{1}" data-title="{2}"></div>'.format(SF.compose(tempDivId, "panelPopup"), tempDivId, title || lang.signum.chooseAValue));

        options.forEach(function (o) {
            return div.append($('<button type="button" class="sf-chooser-button"/>').data("option", o).attr("data-value", getValue(o)).text(getStr(o)));
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
    exports.chooser = chooser;

    (function (ValueLineType) {
        ValueLineType[ValueLineType["Boolean"] = 0] = "Boolean";
        ValueLineType[ValueLineType["RadioButtons"] = 1] = "RadioButtons";
        ValueLineType[ValueLineType["Combo"] = 2] = "Combo";
        ValueLineType[ValueLineType["DateTime"] = 3] = "DateTime";
        ValueLineType[ValueLineType["TextBox"] = 4] = "TextBox";
        ValueLineType[ValueLineType["TextArea"] = 5] = "TextArea";
        ValueLineType[ValueLineType["Number"] = 6] = "Number";
    })(exports.ValueLineType || (exports.ValueLineType = {}));
    var ValueLineType = exports.ValueLineType;

    function valueLineBox(options) {
        return new Promise(function (resolve) {
            requestHtml(Entities.EntityHtml.withoutType(options.prefix), {
                controllerUrl: SF.Urls.valueLineBox,
                requestExtraJsonData: options
            }).then(function (eHtml) {
                var result = null;
                exports.basicPopupView(eHtml, function (div) {
                    var input = div.find(":input:not(:button)");
                    if (input.length != 1)
                        throw new Error("{0} inputs found in ValueLineBox".format(input.length));

                    result = input.val();
                    return Promise.resolve(true);
                }).then(function () {
                    return resolve(result);
                });
            });
        });
    }
    exports.valueLineBox = valueLineBox;

    once("widgetToggler", function () {
        return $(document).on("click", ".sf-widget-toggler", function (evt) {
            SF.Dropdowns.toggle(evt, this, 1);
            return false;
        });
    });
});
//# sourceMappingURL=Navigator.js.map
