/// <reference path="globals.ts"/>
define(["require", "exports", "Entities", "Validator"], function(require, exports, Entities, Validator) {
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

    function navigate(runtimeInfo, viewOptions) {
        viewOptions = $.extend({
            controllerUrl: runtimeInfo.isNew ? SF.Urls.create : SF.Urls.view,
            partialViewName: null,
            requestExtraJsonData: null,
            readOnly: false
        }, viewOptions);

        $.ajax({
            url: viewOptions.controllerUrl,
            data: requestData(new Entities.EntityHtml(null, runtimeInfo), viewOptions),
            async: false
        });
    }
    exports.navigate = navigate;

    function createTempDiv(entityValue) {
        var tempDivId = SF.compose(entityValue.prefix, "Temp");

        $("body").append(SF.hiddenDiv(tempDivId, ""));

        return $("#" + tempDivId);
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

        if (entityHtml.html != null)
            return openNavigatePopup(entityHtml, viewOptions);

        requestHtml(entityHtml, viewOptions).then(function (eHTml) {
            return openNavigatePopup(eHTml, viewOptions);
        });
    }
    exports.navigatePopup = navigatePopup;

    function openNavigatePopup(entityHtml, viewOptions) {
        var tempDiv = exports.createTempDiv(entityHtml);

        tempDiv.html(entityHtml.html);

        SF.triggerNewContent(tempDiv);

        var result = new Promise(function (resolve) {
            tempDiv.popup({
                onCancel: function () {
                    tempDiv.remove();

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
                prefix: entityHtml.prefix
            }, viewOptions.validationOptions);

        if (entityHtml.html != null) {
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
        var tempDiv = exports.createTempDiv(entityHtml);

        SF.triggerNewContent(tempDiv);

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
                            var $mainControl = tempDiv.find(".sf-main-control[data-prefix=" + entityHtml.prefix + "]");
                            if ($mainControl.length > 0) {
                                entityHtml.runtimeInfo = Entities.RuntimeInfoValue.parse($mainControl.data("runtimeinfo"));
                            }

                            tempDiv.popup('destroy');
                            tempDiv.remove();

                            resolve(entityHtml);
                        }
                    });
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

    function requestAndReload(prefix, options) {
        options = $.extend({
            controllerUrl: prefix ? SF.Urls.popupView : SF.Urls.normalControl
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
            return new Entities.RuntimeInfoElement(prefix).value();

        var mainControl = $("#{0}_divNormalControl".format(prefix));

        return Entities.RuntimeInfoValue.parse(mainControl.data("runtimeInfo"));
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

    function viewMode(prefix) {
        return $(".sf-main-control[data-prefix=" + prefix + "]").closest(".sf-popup-control").children(".sf-button-bar").find(".sf-ok-button").length > 0 ? "View" : "Navigate";
    }
    exports.viewMode = viewMode;

    function checkValidation(validatorOptions, allowErrors) {
        return Validator.validate(validatorOptions).then(function (result) {
            if (result.isValid)
                return result;

            Validator.showErrors(validatorOptions, result.modelState, true);

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
        }).then(function (html) {
            entityHtml.html = $(html);
            return entityHtml;
        });
    }

    function serialize(prefix) {
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
    exports.serialize = serialize;

    function serializeJson(prefix) {
        var id = SF.compose(prefix, "panelPopup");
        var arr = $("#" + id + " :input").serializeArray();
        var data = {};
        for (var index = 0; index < arr.length; index++) {
            if (data[arr[index].name] != null) {
                data[arr[index].name] += "," + arr[index].value;
            } else {
                data[arr[index].name] = arr[index].value;
            }
        }

        var myRuntimeInfoKey = SF.compose(prefix, Entities.Keys.runtimeInfo);
        if (typeof data[myRuntimeInfoKey] == "undefined") {
            var $mainControl = $(".sf-main-control[data-prefix=" + prefix + "]");
            data[myRuntimeInfoKey] = $mainControl.data("runtimeinfo");
        }
        return data;
    }
    exports.serializeJson = serializeJson;
    ;

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

    function typeChooser(staticInfo) {
        var types = staticInfo.types();
        if (types.length == 1) {
            return Promise.resolve(types[0]);
        }

        var typesNiceNames = staticInfo.typeNiceNames();

        var options = types.map(function (t, i) {
            return ({ type: t, text: typesNiceNames[i] });
        });

        return exports.chooser(staticInfo.prefix, lang.signum.chooseAType, options).then(function (t) {
            return t == null ? null : t.type;
        });
    }
    exports.typeChooser = typeChooser;

    function chooser(prefix, title, options, getStr) {
        var tempDivId = SF.compose(prefix, "Temp");

        if (getStr == null) {
            getStr = function (a) {
                return a.toString ? a.toString() : a.toStr ? a.toStr : a.text ? a.text : a;
            };
        }

        var div = $('<div id="{0}" class="sf-popup-control" data-prefix="{1}" data-title="{2}"></div>'.format(SF.compose(tempDivId, "panelPopup"), tempDivId, title || lang.signum.chooseAValue));

        options.forEach(function (o) {
            return div.append($('<input type="button" class="sf-chooser-button"/>').data("option", o).text(getStr(o)));
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
});
//# sourceMappingURL=Navigator.js.map
