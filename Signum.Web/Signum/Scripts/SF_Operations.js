/// <reference path="SF_Utils.ts"/>
/// <reference path="SF_Globals.ts"/>
/// <reference path="SF_Validator.ts"/>
var SF;
(function (SF) {
    var OperationOptions = (function () {
        function OperationOptions() {
        }
        return OperationOptions;
    })();
    SF.OperationOptions = OperationOptions;

    var OperationManager = (function () {
        function OperationManager(_options) {
            this.options = $.extend({
                sender: null,
                prefix: "",
                parentDiv: "",
                operationKey: null,
                isLite: false,
                controllerUrl: null,
                validationOptions: {},
                onOk: null,
                onCancelled: null,
                contextual: false,
                requestExtraJsonData: null
            }, _options);
        }
        OperationManager.prototype.runtimeInfo = function () {
            return new SF.RuntimeInfo(this.options.prefix);
        };

        OperationManager.prototype.pf = function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        };

        OperationManager.prototype.newPrefix = function () {
            return SF.compose("New", this.options.prefix);
        };

        OperationManager.prototype.requestData = function (newPrefix) {
            var formChildren = "";
            if (SF.isFalse(this.options.isLite)) {
                if (SF.isEmpty(this.options.prefix)) {
                    formChildren = SF.isEmpty(this.options.parentDiv) ? $(this.options.sender).closest("form").find(":input") : $("#" + this.options.parentDiv + " :input");
                } else {
                    formChildren = $(this.pf("panelPopup :input")).add("#" + SF.Keys.tabId).add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");
                }
            } else {
                formChildren = $('#' + SF.Keys.tabId + ", input:hidden[name=" + SF.Keys.antiForgeryToken + "], " + this.pf(SF.Keys.runtimeInfo));
            }
            formChildren = formChildren.not(".sf-search-control *");

            var serializer = new SF.Serializer();
            serializer.add(formChildren.serialize());

            serializer.add({
                isLite: this.options.isLite,
                operationFullKey: this.options.operationKey,
                prefix: newPrefix,
                oldPrefix: this.options.prefix
            });

            if (!SF.isEmpty(this.options.prefix)) {
                var $mainControl = $(".sf-main-control[data-prefix=" + this.options.prefix + "]");

                //Check runtimeInfo present => if it's a popup from a LineControl it will not be
                var myRuntimeInfoKey = SF.compose(this.options.prefix, SF.Keys.runtimeInfo);
                if (formChildren.filter("#" + myRuntimeInfoKey).length == 0) {
                    var value = $mainControl.data("runtimeinfo");
                    serializer.add(myRuntimeInfoKey, value);
                }

                if ($mainControl.closest(".sf-popup-control").children(".sf-button-bar").find(".sf-ok-button").length > 0) {
                    serializer.add("sfOkVisible", true);
                }
            }

            serializer.add(this.options.requestExtraJsonData);

            return serializer.serialize();
        };
        return OperationManager;
    })();
    SF.OperationManager = OperationManager;
})(SF || (SF = {}));

SF.registerModule("Operations", function () {
    SF.OperationManager.prototype = {
        contextualRequestData: function (newPrefix) {
            var serializer = new SF.Serializer();
            serializer.add($("input:hidden[name=" + SF.Keys.antiForgeryToken + "]").serialize());
            serializer.add({
                isLite: this.options.isLite,
                operationFullKey: this.options.operationKey,
                prefix: newPrefix,
                oldPrefix: this.options.prefix,
                liteKeys: $(this.pf("sfSearchControl .sf-td-selection:checked")).closest("tr").map(function () {
                    return $(this).data("entity");
                }).toArray().join(",")
            });
            serializer.add(this.options.requestExtraJsonData);

            return serializer.serialize();
        },
        ajax: function (newPrefix, onSuccess) {
            if (SF.Blocker.isEnabled()) {
                return false;
            } else {
                SF.Blocker.enable();
            }

            if (SF.isEmpty(newPrefix)) {
                newPrefix = this.options.prefix;
            }
            var self = this;
            $.ajax({
                url: this.options.controllerUrl,
                data: this.options.contextual ? this.contextualRequestData(newPrefix) : this.requestData(newPrefix),
                success: function (operationResult) {
                    SF.Blocker.disable();
                    if (self.executedSuccessfully(operationResult)) {
                        if (onSuccess != null) {
                            onSuccess(newPrefix, operationResult, self.options.parentDiv);
                        }
                    } else {
                        SF.Notify.error(lang.signum.error, 2000);
                        return;
                    }
                },
                error: function () {
                    SF.Blocker.disable();
                    SF.Notify.error(lang.signum.error, 2000);
                }
            });

            return false;
        },
        submit: function () {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            var $form = $(this.options.sender).closest("form");
            $form.append(SF.hiddenInput('isLite', this.options.isLite) + SF.hiddenInput('operationFullKey', this.options.operationKey) + SF.hiddenInput("oldPrefix", this.options.prefix));

            if (!SF.isEmpty(this.options.prefix)) {
                //Check runtimeInfo present => if it's a popup from a LineControl it will not be
                var myRuntimeInfoKey = SF.compose(this.options.prefix, SF.Keys.runtimeInfo);
                if ($form.filter("#" + myRuntimeInfoKey).length == 0) {
                    var $mainControl = $(".sf-main-control[data-prefix=" + this.options.prefix + "]");
                    SF.hiddenInput(myRuntimeInfoKey, $mainControl.data("runtimeinfo"));
                }
            }

            SF.submit(this.options.controllerUrl, this.options.requestExtraJsonData, $form);

            return false;
        },
        executedSuccessfully: function (operationResult) {
            if ((typeof (operationResult) !== "object") || (operationResult.result != "ModelState")) {
                return true;
            }
            var modelState = operationResult.ModelState;

            if (SF.isEmpty(this.options.prefix)) {
                new SF.Validator().showErrors(modelState);
            } else {
                new SF.PartialValidator({
                    prefix: this.options.prefix
                }).showErrors(modelState);
            }
            return false;
        },
        validateAndSubmit: function () {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            if (SF.isTrue(this.options.isLite)) {
                this.submit();
            } else {
                var onSuccess = function () {
                    this.submit();
                };
                var self = this;
                var valOptions = $.extend({ prefix: this.options.prefix }, this.options.validationOptions);
                if (!SF.isEmpty(this.options.parentDiv)) {
                    valOptions.parentDiv = this.options.parentDiv;
                }
                if (!SF.EntityIsValid(valOptions, function () {
                    onSuccess.call(self);
                }, this.options.sender)) {
                    return;
                }
            }

            return false;
        }
    };

    SF.OperationExecutor = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: SF.Urls.operationExecute
        }, _options));

        this.validateAndAjax = function (newPrefix, onAjaxSuccess) {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            if (SF.isEmpty(newPrefix)) {
                newPrefix = null;
            }
            onAjaxSuccess = typeof onAjaxSuccess == "undefined" ? SF.opOnSuccessDispatcher : onAjaxSuccess;

            var onSuccess = function () {
                this.ajax(newPrefix, onAjaxSuccess);
            };

            if (SF.isTrue(this.options.isLite)) {
                onSuccess.call(this);
            } else {
                var self = this;
                var valOptions = $.extend({ prefix: this.options.prefix }, this.options.validationOptions);
                if (!SF.isEmpty(this.options.parentDiv)) {
                    valOptions.parentDiv = this.options.parentDiv;
                }
                if (!SF.EntityIsValid(valOptions, function () {
                    onSuccess.call(self);
                }, this.options.sender)) {
                    return;
                }
            }
        };

        this.contextualExecute = function (entityType, id) {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            $('.sf-search-ctxmenu-overlay').remove();

            var multipleOperation = $(this.pf("sfSearchControl .sf-td-selection:checked")).length > 1;
            var defaultController = this.options.controllerUrl == SF.Urls.operationExecute;
            if (multipleOperation) {
                if (defaultController) {
                    this.options.controllerUrl = SF.Urls.operationContextualFromMany;
                }
                this.ajax(this.newPrefix(), SF.opOnSuccessDispatcher);
            } else {
                if (defaultController) {
                    this.options.controllerUrl = SF.Urls.operationContextual;
                }
                this.ajax(null, SF.opMarkCellOnSuccess);
            }
        };
    };

    SF.OperationExecutor.prototype = new SF.OperationManager();

    //ConstructorFrom options = OperationManager options + returnType
    SF.ConstructorFrom = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: SF.Urls.operationConstructFrom,
            returnType: null
        }, _options));

        this.validateAndAjax = function (newPrefix, onAjaxSuccess) {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            if (SF.isEmpty(newPrefix)) {
                newPrefix = null;
            }
            onAjaxSuccess = typeof onAjaxSuccess == "undefined" ? SF.opOnSuccessDispatcher : onAjaxSuccess;

            var onSuccess = function () {
                this.ajax(newPrefix, onAjaxSuccess);
            };

            if (SF.isTrue(this.options.isLite)) {
                onSuccess.call(this);
            } else {
                var self = this;
                var valOptions = $.extend({ prefix: this.options.prefix }, this.options.validationOptions);
                if (!SF.isEmpty(this.options.parentDiv)) {
                    valOptions.parentDiv = this.options.parentDiv;
                }
                if (!SF.EntityIsValid(valOptions, function () {
                    onSuccess.call(self);
                }, this.options.sender)) {
                    return;
                }
            }
        };

        this.contextualConstruct = function (entityType, id) {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            $('.sf-search-ctxmenu-overlay').remove();
            if ($(this.pf("sfSearchControl .sf-td-selection:checked")).length > 1) {
                if (this.options.controllerUrl == SF.Urls.operationConstructFrom) {
                    this.options.controllerUrl = SF.Urls.operationContextualFromMany;
                }
                this.ajax(this.newPrefix(), SF.opOnSuccessDispatcher);
            } else {
                this.ajax(this.newPrefix(), SF.opContextualOnSuccess);
            }
        };
    };

    SF.ConstructorFrom.prototype = new SF.OperationManager();

    SF.DeleteExecutor = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: SF.Urls.operationDelete
        }, _options));

        this.contextualDelete = function (entityType, id) {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            if ($(this.pf("sfSearchControl .sf-td-selection:checked")).length > 1) {
                if (this.options.controllerUrl == SF.Urls.operationDelete) {
                    this.options.controllerUrl = SF.Urls.operationContextualFromMany;
                }
                this.ajax(this.newPrefix(), SF.opOnSuccessDispatcher);
            } else {
                this.ajax(this.options.prefix, function () {
                    SF.Notify.info(lang.signum.executed, 2000);
                });
            }
        };
    };

    SF.DeleteExecutor.prototype = new SF.OperationManager();

    SF.OperationDelete = function (deleteExecutor) {
        deleteExecutor.execute();
    };

    //ConstructorFromMany options = OperationManager options + returnType
    SF.ConstructorFromMany = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: SF.Urls.operationConstructFromMany,
            returnType: null
        }, _options));

        this.requestData = function (newPrefix, items) {
            var serializer = new SF.Serializer().add($('#' + SF.Keys.tabId).serialize()).add($("input:hidden[name=" + SF.Keys.antiForgeryToken + "]").serialize()).add({
                entityType: $(this.pf(SF.Keys.entityTypeNames)).val(),
                operationFullKey: this.options.operationKey,
                prefix: newPrefix,
                oldPrefix: this.options.prefix
            });

            for (var i = 0, l = items.length; i < l; i++) {
                serializer.add("keys", items[i].key);
            }

            serializer.add(this.options.requestExtraJsonData);
            return serializer.serialize();
        };

        this.ajax = function (newPrefix, items, onSuccess) {
            if (SF.Blocker.isEnabled()) {
                return false;
            } else {
                SF.Blocker.enable();
            }

            var self = this;
            $.ajax({
                url: this.options.controllerUrl,
                data: this.requestData(newPrefix, items),
                success: function (operationResult) {
                    SF.Blocker.disable();

                    if (self.executedSuccessfully(operationResult)) {
                        if (onSuccess != null) {
                            onSuccess(newPrefix, operationResult, self.options.parentDiv);
                        }
                    } else {
                        SF.Notify.error(lang.signum.error, 2000);
                        return;
                    }
                },
                error: function () {
                    SF.Blocker.disable();
                    SF.Notify.error(lang.signum.error, 2000);
                }
            });
        };

        this.ajaxSelected = function (newPrefix, onAjaxSuccess) {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            var onSuccess = function (items) {
                this.ajax(newPrefix, items, onAjaxSuccess || SF.opOnSuccessDispatcher);
            };

            var self = this;
            SF.FindNavigator.getFor(this.options.prefix).hasSelectedItems(function (items) {
                onSuccess.call(self, items);
            });
        };

        this.submitSelected = function () {
            if (SF.Blocker.isEnabled()) {
                return false;
            }

            var onSuccess = function (items) {
                for (var i = 0, l = items.length; i < l; i++) {
                    $(this.options.sender).closest("form").append(SF.hiddenInput('keys', items[i].key));
                }
                this.submit();
            };

            var self = this;
            SF.FindNavigator.getFor(this.options.prefix).hasSelectedItems(function (items) {
                onSuccess.call(self, items);
            });
        };
    };

    SF.ConstructorFromMany.prototype = new SF.OperationManager();

    SF.opDisableCtxmenu = function () {
        var clss = "sf-ctxmenu-active";
        $("." + clss).removeClass(clss);
    };

    SF.opOnSuccessDispatcher = function (prefix, operationResult, parentDiv) {
        SF.opDisableCtxmenu();

        if (SF.isEmpty(operationResult)) {
            return null;
        }

        var $result = $(operationResult);
        var newPopupId = SF.compose(prefix, "panelPopup");
        var hasNewPopup = $result.filter("#" + newPopupId).length !== 0;

        if (!hasNewPopup || (hasNewPopup && $("#" + newPopupId + ":visible").length !== 0)) {
            SF.opReloadContent(prefix, operationResult, !SF.isEmpty(parentDiv) ? parentDiv : hasNewPopup ? newPopupId : "");
        } else {
            SF.opOpenPopup(prefix, operationResult);
        }
    };

    SF.opReloadContent = function (prefix, operationResult, parentDiv) {
        SF.opDisableCtxmenu();
        if (SF.isEmpty(prefix)) {
            var $elem = SF.isEmpty(parentDiv) ? $("#divNormalControl") : $("#" + parentDiv);
            $elem.html(operationResult);
            SF.triggerNewContent($elem);
        } else {
            var oldViewNav = new SF.ViewNavigator({ prefix: prefix });
            var tempDivId = oldViewNav.tempDivId();
            var oldViewOptions = $("#" + tempDivId).data("viewOptions");

            SF.closePopup(prefix);

            var viewNavigator = new SF.ViewNavigator($.extend({}, oldViewOptions, {
                prefix: prefix,
                containerDiv: tempDivId
            }));

            if (oldViewOptions.onOk != null) {
                viewNavigator.showViewOk(operationResult);
            } else {
                viewNavigator.viewSave(operationResult);
            }
        }
        SF.Notify.info(lang.signum.executed, 2000);
    };

    SF.opOpenPopup = function (prefix, operationResult) {
        SF.opDisableCtxmenu();
        new SF.ViewNavigator({ prefix: prefix }).showCreateSave(operationResult);
        SF.Notify.info(lang.signum.executed, 2000);
    };

    SF.opOpenPopupNoDefaultOk = function (prefix, operationResult) {
        SF.opDisableCtxmenu();
        new SF.ViewNavigator({ prefix: prefix, onOk: function () {
                return false;
            }, onSave: function () {
                return false;
            } }).showCreateSave(operationResult);
        SF.Notify.info(lang.signum.executed, 2000);
    };

    SF.opNavigate = function (prefix, operationResult) {
        SF.submit(operationResult);
    };

    SF.opMarkCellOnSuccess = function (prefix, operationResult) {
        $(".sf-ctxmenu-active").addClass("sf-entity-ctxmenu-" + (SF.isEmpty(operationResult) ? "success" : "error")).removeClass("sf-ctxmenu-active");

        SF.Notify.info(lang.signum.executed, 2000);
    };

    SF.opContextualOnSuccess = function (prefix, operationResult) {
        SF.opDisableCtxmenu();
        if (SF.isEmpty(operationResult)) {
            return null;
        }

        var $result = $(operationResult);
        var newPopupId = SF.compose(prefix, "panelPopup");
        var hasNewPopup = $result.filter("#" + newPopupId).length !== 0;

        if (hasNewPopup) {
            SF.opOpenPopup(prefix, operationResult);
        } else {
            var $form = $("form");
            $form.html(operationResult);
            SF.triggerNewContent($form);
            SF.Notify.info(lang.signum.executed, 2000);
        }
    };
});
