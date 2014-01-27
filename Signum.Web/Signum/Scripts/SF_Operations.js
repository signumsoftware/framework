/// <reference path="references.ts"/>
var SF;
(function (SF) {
    (function (Operations) {
        function execute(options) {
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

            return ajax(options, entityRequestData(options)).then(function (result) {
                return reloadContent(options, result);
            });
        }

        function executeContextual(options) {
            options = $.extend({
                controllerUrl: SF.Urls.operationContextualExecute,
                requestExtraJsonData: null
            }, options);

            SF.FindNavigator.removeOverlay();

            return ajax(options, contextualRequestData(options, true)).then(function (result) {
                markCells(options.prefix, result);
            });
        }

        function constructFrom(options, newPrefix) {
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

            return ajax(options, entityRequestData(options, newPrefix)).then(function (result) {
                return openPopup(newPrefix, result);
            });
        }

        function constructFromContextual(options, newPrefix) {
            options = $.extend({
                controllerUrl: SF.Urls.operationContextualConstructFrom,
                requestExtraJsonData: null
            }, options);

            SF.FindNavigator.removeOverlay();

            if (!newPrefix)
                newPrefix = getNewPrefix(options);

            return ajax(options, contextualRequestData(options, true, newPrefix)).then(function (result) {
                openPopup(newPrefix, result);
                markCells(options.prefix, null);
            });
        }

        function deleteEntity(options) {
            options = $.extend({
                controllerUrl: SF.Urls.operationDelete,
                requestExtraJsonData: null,
                sender: null,
                avoidValidate: false,
                validationOptions: {},
                isLite: false,
                parentDiv: null
            }, options);

            return ajax(options, entityRequestData(options));
        }

        function deleteContextual(options) {
            options = $.extend({
                controllerUrl: SF.Urls.operationContextualDelete,
                requestExtraJsonData: null
            }, options);

            SF.FindNavigator.removeOverlay();

            return ajax(options, contextualRequestData(options, true)).then(function (result) {
                markCells(options.prefix, result);
            });
        }

        function constructFromMany(options, newPrefix) {
            options = $.extend({
                controllerUrl: SF.Urls.operationConstructFromMany,
                requestExtraJsonData: null
            }, options);

            SF.FindNavigator.removeOverlay();

            if (!newPrefix)
                newPrefix = getNewPrefix(options);

            return ajax(options, contextualRequestData(options, false, newPrefix)).then(function (result) {
                openPopup(newPrefix, result);
                markCells(options.prefix, null);
            });
        }

        function openPopup(newPrefix, newHtml) {
            disableContextMenu();
            var entity = SF.EntityHtml.fromHtml(newPrefix, newHtml);
            SF.ViewNavigator.navigatePopup(entity);
            SF.Notify.info(lang.signum.executed, 2000);
        }
        Operations.openPopup = openPopup;

        function disableContextMenu() {
            $(".sf-ctxmenu-active").removeClass("sf-ctxmenu-active");
        }
        Operations.disableContextMenu = disableContextMenu;

        function markCells(prefix, operationResult) {
            $(".sf-ctxmenu-active").addClass("sf-entity-ctxmenu-" + (SF.isEmpty(operationResult) ? "success" : "error")).removeClass("sf-ctxmenu-active");

            SF.Notify.info(lang.signum.executed, 2000);
        }

        function reloadContent(options, newHtml) {
            if (!options.prefix) {
                var $elem = SF.isEmpty(options.parentDiv) ? $("#divNormalControl") : $("#" + options.parentDiv);
                $elem.html(newHtml);
                SF.triggerNewContent($elem);
            } else {
                SF.ViewNavigator.reloadPopup(options.prefix, newHtml);
            }
            SF.Notify.info(lang.signum.executed, 2000);
        }
        Operations.reloadContent = reloadContent;

        function getNewPrefix(optons) {
            return SF.compose("New", this.options.prefix);
        }
        Operations.getNewPrefix = getNewPrefix;

        function entityRequestData(options, newPrefix) {
            var formChildren = null;
            if (SF.isFalse(options.isLite)) {
                if (SF.isEmpty(options.prefix)) {
                    formChildren = SF.isEmpty(options.parentDiv) ? $(options.sender).closest("form").find(":input") : $("#" + options.parentDiv + " :input");
                } else {
                    formChildren = $("#{0}_panelPopup :input".format(options.prefix)).add("#" + SF.Keys.tabId).add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");
                }
            } else {
                formChildren = $('#' + SF.Keys.tabId + ", input:hidden[name=" + SF.Keys.antiForgeryToken + "], #" + SF.compose(options.prefix, SF.Keys.runtimeInfo));
            }
            formChildren = formChildren.not(".sf-search-control *");

            var serializer = new SF.Serializer();
            serializer.add(formChildren.serialize());

            serializer.add({
                isLite: options.isLite,
                operationFullKey: options.operationKey,
                prefix: options.prefix,
                newPrefix: newPrefix
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
        Operations.entityRequestData = entityRequestData;

        function contextualRequestData(options, justOne, newPrefix) {
            var items = SF.FindNavigator.getFor(options.prefix).selectedItems();

            if (items.length > 1 && justOne)
                throw new Error("just one entity should have been selected");

            var serializer = new SF.Serializer();
            serializer.add($("input:hidden[name=" + SF.Keys.antiForgeryToken + "]").serialize());

            serializer.add({
                isLite: options.isLite,
                operationFullKey: options.operationKey,
                newprefix: newPrefix,
                prefix: options.prefix,
                liteKeys: items.map(function (i) {
                    return i.runtimeInfo.key();
                }).join(",")
            });
            serializer.add(options.requestExtraJsonData);

            return serializer.serialize();
        }
        Operations.contextualRequestData = contextualRequestData;

        function ajax(options, data) {
            return SF.Blocker.wrap(function () {
                return new Promise(function (resolve, reject) {
                    $.ajax({
                        url: options.controllerUrl,
                        data: data,
                        success: function (operationResult) {
                            if (modelStateErrors(operationResult, options)) {
                                SF.Notify.error(lang.signum.error, 2000);

                                reject(operationResult);
                            } else
                                resolve(operationResult);
                        },
                        error: function (error) {
                            SF.Notify.error(lang.signum.error, 2000);
                            reject(error);
                        }
                    });
                });
            });
        }
        Operations.ajax = ajax;

        function modelStateErrors(operationResult, options) {
            if ((typeof (operationResult) !== "object") || (operationResult.result != "ModelState"))
                return false;

            var modelState = operationResult.ModelState;

            SF.Validation.showErrors({ prefix: options.prefix }, modelState);

            return true;
        }

        function entityIsValidOrLite(options) {
            if (options.isLite || options.avoidValidate)
                return true;

            var valOptions = $.extend({ prefix: options.prefix, parentDiv: options.parentDiv }, options.validationOptions);

            return SF.Validation.entityIsValid(valOptions);
        }
        Operations.entityIsValidOrLite = entityIsValidOrLite;

        function validateAndSubmit(options) {
            if (entityIsValidOrLite(options))
                submit(options);
        }
        Operations.validateAndSubmit = validateAndSubmit;

        function submit(options) {
            var $form = $(options.sender).closest("form");
            $form.append(SF.hiddenInput('isLite', options.isLite) + SF.hiddenInput('operationFullKey', options.operationKey) + SF.hiddenInput("oldPrefix", options.prefix));

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
        Operations.submit = submit;
    })(SF.Operations || (SF.Operations = {}));
    var Operations = SF.Operations;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Operations.js.map
