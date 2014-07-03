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

    function navigate(runtimeInfo, extraJsonArguments, openNewWindow) {
        var url = runtimeInfo.isNew ? SF.Urls.create.replace("MyType", runtimeInfo.type) : SF.Urls.view.replace("MyType", runtimeInfo.type).replace("MyId", runtimeInfo.id);

        if (extraJsonArguments && !$.isEmptyObject(extraJsonArguments)) {
            SF.submitOnly(url, extraJsonArguments, openNewWindow);
        } else {
            if (openNewWindow)
                window.open(url, "_blank");
            else
                window.location.href = url;
        }
    }
    exports.navigate = navigate;

    function navigatePopup(entityHtml, viewOptions) {
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

        return requestHtml(entityHtml, viewOptions).then(function (eHTml) {
            return openNavigatePopup(eHTml, viewOptions);
        });
    }
    exports.navigatePopup = navigatePopup;

    function openNavigatePopup(entityHtml, viewOptions) {
        entityHtml.getChild("panelPopup").data("sf-navigate", true);

        return exports.openEntityHtmlModal(entityHtml, null, viewOptions.onPopupLoaded).then(function () {
            return null;
        });
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
            saveProtected: null,
            showOperations: true,
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
        entityHtml.getChild("panelPopup").data("sf-navigate", false);

        return exports.openEntityHtmlModal(entityHtml, function (isOk) {
            if (!isOk)
                return Promise.resolve(true);

            if (viewOptions.avoidValidate)
                return Promise.resolve(true);

            return checkValidation(viewOptions.validationOptions, viewOptions.allowErrors).then(function (valResult) {
                if (valResult == null)
                    return false;

                entityHtml.hasErrors = !valResult.isValid;
                entityHtml.link = valResult.newLink;
                entityHtml.toStr = valResult.newToStr;

                return true;
            });
        }, viewOptions.onPopupLoaded).then(function (pair) {
            if (!pair.isOk)
                return null;

            return pair.entityHtml;
        });
    }

    function openEntityHtmlModal(entityHtml, canClose, shown) {
        if (!canClose)
            canClose = function () {
                return Promise.resolve(true);
            };

        var panelPopup = entityHtml.getChild("panelPopup");

        var okButtonId = entityHtml.prefix.child("btnOk");

        return exports.openModal(panelPopup, function (button) {
            var main = entityHtml.prefix.child("divMainControl").tryGet(panelPopup);
            if (button.id == okButtonId) {
                if ($(button).hasClass("sf-save-protected") && main.hasClass("sf-changed")) {
                    alert(lang.signum.saveChangesBeforeOrPressCancel);
                    return Promise.resolve(false);
                }

                return canClose(true);
            } else {
                if (main.hasClass("sf-changed") && !confirm(lang.signum.looseCurrentChanges))
                    return Promise.resolve(false);

                return canClose(false);
            }
        }, shown).then(function (pair) {
            var main = entityHtml.prefix.child("divMainControl").tryGet(panelPopup);
            entityHtml.runtimeInfo = Entities.RuntimeInfo.parse(main.data("runtimeinfo"));
            entityHtml.html = pair.modalDiv;

            return { isOk: pair.button.id == okButtonId, entityHtml: entityHtml };
        });
    }
    exports.openEntityHtmlModal = openEntityHtmlModal;

    function openModal(modalDiv, canClose, shown) {
        if (!canClose)
            canClose = function () {
                return Promise.resolve(true);
            };

        $("body").append(modalDiv);

        return new Promise(function (resolve) {
            var button = null;
            modalDiv.on("click", ".sf-close-button", function (event) {
                event.preventDefault();

                button = this;

                canClose(button).then(function (result) {
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
                keyboard: false,
                backdrop: "static"
            });
        });
    }
    exports.openModal = openModal;

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

    function getMainControl(prefix) {
        return $(prefix ? "#{0}_divMainControl".format(prefix) : "#divMainControl");
    }
    exports.getMainControl = getMainControl;

    function hasChanges(prefix) {
        return exports.getMainControl(prefix).hasClass("sf-changed");
    }
    exports.hasChanges = hasChanges;

    function getEmptyEntityHtml(prefix) {
        return new Entities.EntityHtml(prefix, exports.getRuntimeInfoValue(prefix));
    }
    exports.getEmptyEntityHtml = getEmptyEntityHtml;

    function reloadMain(entityHtml) {
        var $elem = $("#divMainPage");
        $elem.html(entityHtml.html);
    }
    exports.reloadMain = reloadMain;

    function closePopup(prefix) {
        var tempDivId = prefix.child("Temp");

        var tempDiv = $("#" + tempDivId);

        tempDiv.modal("hide"); //should remove automatically
    }
    exports.closePopup = closePopup;

    function reloadPopup(entityHtml) {
        var panelPopupId = entityHtml.prefix.child("panelPopup");

        $("#" + panelPopupId).html(entityHtml.html.filter("#" + panelPopupId).children());
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

        return prefix.child("panelPopup").get().data("sf-navigate");
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

        if (options.saveProtected != null)
            obj["saveProtected"] = options.saveProtected;

        if (options.showOperations != true)
            obj["showOperations"] = false;

        if (!SF.isEmpty(options.partialViewName))
            obj["partialViewName"] = options.partialViewName;

        return $.extend(obj, options.requestExtraJsonData);
    }

    function typeChooser(prefix, types) {
        return exports.chooser(prefix, lang.signum.chooseAType, types, function (a) {
            return a.niceName;
        }, function (a) {
            return a.name;
        });
    }
    exports.typeChooser = typeChooser;

    function chooser(prefix, title, options, getStr, getValue) {
        if (options.length == 1) {
            return Promise.resolve(options[0]);
        }

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

        var modalBody = $("<div>");
        options.forEach(function (o) {
            return $('<button type="button" class="sf-chooser-button sf-close-button btn btn-default"/>').data("option", o).attr("data-value", getValue(o)).text(getStr(o)).appendTo(modalBody);
        });

        var modalDiv = exports.createBootstrapModal({ titleText: title, prefix: prefix, body: modalBody, titleClose: true });

        var option;
        return exports.openModal(modalDiv, function (button) {
            option = $(button).data("option");
            return Promise.resolve(true);
        }).then(function (pair) {
            return option;
        });
    }
    exports.chooser = chooser;

    function createBootstrapModal(options) {
        var result = $('<div class="modal fade" tabindex="-1" role="dialog" id="' + options.prefix.child("panelPopup") + '">' + '<div class="modal-dialog modal-sm" >' + '<div class="modal-content">' + (options.title || options.titleText || options.titleClose ? ('<div class="modal-header"></div>') : '') + '<div class="modal-body"></div>' + (options.footer || options.footerOkId || options.footerCancelId ? ('<div class="modal-footer"></div>') : '') + '</div>' + '</div>' + '</div>');

        if (options.titleClose)
            result.find(".modal-header").append('<button type="button" class="close sf-close-button" aria-hidden="true">×</button>');

        if (options.title)
            result.find(".modal-header").append(options.title);

        if (options.titleText)
            result.find(".modal-header").append($('<h4 class="modal-title"></h4>').text(options.titleText));

        if (options.body)
            result.find(".modal-body").append(options.body);

        if (options.footer)
            result.find(".modal-footer").append(options.footer);

        if (options.footerOkId)
            result.find(".modal-footer").append($('<button class="btn btn-primary sf-entity-button sf-close-button sf-ok-button)">)').attr("id", options.footerOkId).text(lang.signum.ok));

        if (options.footerCancelId)
            result.find(".modal-footer").append($('<button class="btn btn-primary sf-entity-button sf-close-button sf-ok-button)">)').attr("id", options.footerCancelId).text(lang.signum.cancel));

        return result;
    }
    exports.createBootstrapModal = createBootstrapModal;

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
        return requestHtml(Entities.EntityHtml.withoutType(options.prefix), {
            controllerUrl: SF.Urls.valueLineBox,
            requestExtraJsonData: options
        }).then(function (eHtml) {
            return exports.openEntityHtmlModal(eHtml);
        }).then(function (pair) {
            if (!pair.isOk)
                return null;

            var html = pair.entityHtml.html;

            var date = html.find(options.prefix.child("Date"));
            var time = html.find(options.prefix.child("Time"));

            if (date.length && time.length)
                return date.val() + " " + time.val();

            var input = pair.entityHtml.html.find(":input:not(:button)");
            if (input.length != 1)
                throw new Error("{0} inputs found in ValueLineBox".format(input.length));

            return input.val();
        });
    }
    exports.valueLineBox = valueLineBox;
});
//# sourceMappingURL=Navigator.js.map
