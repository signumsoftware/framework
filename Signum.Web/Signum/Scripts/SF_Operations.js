"use strict";

SF.registerModule("Operations", function () {

    var uiBlocked = false;

    function blockUI() {
        uiBlocked = true;
        $("<div/>", {
            "class": "uiBlocker",
            "width": "300%",
            "height": "300%"
        }).appendTo($("body"));
    }

    SF.OperationManager = function (_options) {
        this.options = $.extend({
            prefix: "",
            parentDiv: "",
            operationKey: null,
            isLite: false,
            controllerUrl: null,
            validationControllerUrl: null,
            onOk: null,
            onCancelled: null,
            contextual: false,
            requestExtraJsonData: null
        }, _options);
    };

    SF.OperationManager.prototype = {

        runtimeInfo: function () {
            return new SF.RuntimeInfo(this.options.prefix);
        },

        pf: function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        },

        newPrefix: function () {
            return SF.compose("New", this.options.prefix);
        },

        requestData: function (newPrefix) {
            SF.log("OperationManager requestData");
            var formChildren = "";
            if (SF.isFalse(this.options.isLite)) {
                if (SF.isEmpty(this.options.prefix)) //NormalWindow 
                    formChildren = SF.isEmpty(this.options.parentDiv) ? $("form :input") : $("#" + this.options.parentDiv + " :input");
                else //PopupWindow
                    formChildren = $(this.pf("panelPopup *") + ", #" + SF.Keys.reactive + ", #" + SF.Keys.tabId);
            }
            else {
                formChildren = $('#' + SF.Keys.tabId);
            }
            formChildren = formChildren.not(".searchControl *");

            var info = this.runtimeInfo();
            var runtimeType = info.runtimeType();

            var serializer = new SF.Serializer();
            serializer.add(formChildren.serialize());

            var myRuntimeInfoKey = SF.compose(this.options.prefix, SF.Keys.runtimeInfo);
            if (formChildren.filter("[name=" + myRuntimeInfoKey + "]").length == 0) {
                var value = SF.isEmpty(runtimeType)
                ? info.createValue(SF.StaticInfo(this.options.prefix).singleType(), info.id(), info.isNew(), info.ticks())
                : info.find().val();

                serializer.add(myRuntimeInfoKey, value);
            }

            serializer.add({ isLite: this.options.isLite,
                operationFullKey: this.options.operationKey,
                prefix: newPrefix,
                oldPrefix: this.options.prefix
            });
            serializer.add(this.options.requestExtraJsonData);

            return serializer.serialize();
        },

        contextualRequestData: function (newPrefix) {
            SF.log("OperationManager contextualRequestData");

            var serializer = new SF.Serializer()
            .add({
                isLite: this.options.isLite,
                operationFullKey: this.options.operationKey,
                prefix: newPrefix,
                oldPrefix: this.options.prefix
            })
            .add(this.options.requestExtraJsonData);

            return serializer.serialize();
        },

        operationAjax: function (newPrefix, onSuccess) {
            SF.log("OperationManager operationAjax");

            if (uiBlocked) {
                return false;
            }
            else {
                blockUI();
            }

            SF.Notify.info(lang.signum.executingOperation);

            if (SF.isEmpty(newPrefix))
                newPrefix = this.options.prefix;

            var self = this;
            SF.ajax({
                type: "POST",
                url: this.options.controllerUrl,
                data: this.options.contextual ? this.contextualRequestData(newPrefix) : this.requestData(newPrefix),
                async: true,
                dataType: "html",
                success: function (operationResult) {
                    uiBlocked = false;
                    $(".uiBlocker").remove();

                    if (self.executedSuccessfully(operationResult)) {
                        var $operationResult = jQuery(operationResult);
                        $("body").trigger("sf-new-content", [$operationResult]);
                        if (onSuccess != null) {
                            onSuccess(newPrefix, $operationResult, self.options.parentDiv);
                        }
                    }
                    else {
                        SF.Notify.error(lang.signum.error, 2000);
                        return;
                    }
                },
                error:
                function () {
                    uiBlocked = false;
                    $(".uiBlocker").remove();
                    SF.Notify.error(lang.signum.error, 2000);
                }
            });
        },

        operationSubmit: function () {
            SF.log("OperationManager operationSubmit");

            if (uiBlocked) {
                return false;
            }

            $("form").append(SF.hiddenInput('isLite', this.options.isLite) +
            SF.hiddenInput('operationFullKey', this.options.operationKey) +
            SF.hiddenInput("oldPrefix", this.options.prefix));

            SF.submit(this.options.controllerUrl, this.options.requestExtraJsonData);
        },

        executedSuccessfully: function (operationResult) {
            SF.log("OperationManager executedSuccessfully");

            if (operationResult.indexOf("ModelState") === -1) {
                return true;
            }

            var result = $.parseJSON(operationResult),
            modelState = result.ModelState;

            if (SF.isEmpty(this.options.prefix)) {
                new Validator().showErrors(modelState);
            }
            else {
                var info = this.runtimeInfo();
                new SF.PartialValidator({
                    prefix: this.options.prefix,
                    type: info.runtimeType(),
                    id: info.id()
                }).showErrors(modelState);
            }
            return false;
        },

        defaultSubmit: function () {
            SF.log("OperationManager defaultSubmit");

            if (uiBlocked) {
                return false;
            }

            if (SF.isTrue(this.options.isLite)) {
                this.operationSubmit();
            }
            else {
                var onSuccess = function () { this.operationSubmit(); };
                var self = this;
                var valOptions = {
                    prefix: this.options.prefix,
                    controllerUrl: this.options.validationControllerUrl
                };
                if (!SF.isEmpty(this.options.parentDiv)) { // So as not to override parentDiv to be set in PartialValidator constructor
                    valOptions.parentDiv = this.options.parentDiv;
                }
                if (!SF.EntityIsValid(valOptions, function () { onSuccess.call(self) })) {
                    return;
                }
            }
        }
    };

    SF.OperationExecutor = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: null
        }, _options));

        this.defaultExecute = function () {
            SF.log("OperationExecutor defaultExecute");

            if (uiBlocked) {
                return false;
            }

            var onSuccess = function () {
                this.operationAjax(null, SF.opReloadContent);
            };

            if (SF.isTrue(this.options.isLite)) {
                onSuccess.call(this);
            }
            else {
                var self = this;
                var valOptions = {
                    prefix: this.options.prefix,
                    controllerUrl: this.options.validationControllerUrl
                };
                if (!SF.isEmpty(this.options.parentDiv)) { // So as not to override parentDiv to be set in PartialValidator constructor
                    valOptions.parentDiv = this.options.parentDiv;
                }
                if (!SF.EntityIsValid(valOptions, function () { onSuccess.call(self) })) {
                    return; 
                }
            }
        };

        this.contextualExecute = function (runtimeType, id) {
            SF.log("OperationExecutor contextualExecute");

            if (uiBlocked) {
                return false;
            }

            this.operationAjax(null, SF.opMarkCellOnSuccess);
        };
    };

    SF.OperationExecutor.prototype = new SF.OperationManager();

    //ConstructorFrom options = OperationManager options + returnType
    SF.ConstructorFrom = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: null,
            returnType: null
        }, _options));

        this.defaultConstruct = function () {
            SF.log("ConstructorFrom construct");

            if (uiBlocked) {
                return false;
            }

            var onSuccess = function () {
                this.operationAjax(this.newPrefix(), SF.opOpenPopup);
            }

            if (SF.isTrue(this.options.isLite)) {
                onSuccess.call(this);
            }
            else {
                var self = this;
                var valOptions = {
                    prefix: this.options.prefix,
                    controllerUrl: this.options.validationControllerUrl
                };
                if (!SF.isEmpty(this.options.parentDiv)) { // So as not to override parentDiv to be set in PartialValidator constructor
                    valOptions.parentDiv = this.options.parentDiv;
                }
                if (!SF.EntityIsValid(valOptions, function () { onSuccess.call(self) })) {
                    return;
                }
            }
        };

        this.contextualConstruct = function (runtimeType, id) {
            SF.log("ConstructorFrom contextualConstruct");

            if (uiBlocked) {
                return false;
            }

            this.operationAjax(this.newPrefix(), SF.opContextualOnSuccess);
        };
    };

    SF.ConstructorFrom.prototype = new SF.OperationManager();

    SF.DeleteExecutor = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: null
        }, _options));

        this.defaultDelete = function () {
            SF.log("DeleteExecutor defaultDelete");

            if (uiBlocked) {
                return false;
            }

            if (SF.isTrue(this.options.isLite)) {
                SF.Notify.info(lang.signum.executingOperation);
                this.operationAjax(this.options.prefix, function () {
                    SF.Notify.info(lang.signum.operationExecuted, 2000);
                });
            }
            else {
                throw "Delete operation must be Lite";
            }
        };

        this.contextualDelete = function (runtimeType, id) {
            SF.log("DeleteExecutor contextualDelete");

            if (uiBlocked) {
                return false;
            }

            SF.Notify.info(lang.signum.executingOperation);
            this.operationAjax(this.options.prefix, function () { SF.Notify.info(lang.signum.operationExecuted, 2000); });
        };
    };

    SF.DeleteExecutor.prototype = new SF.OperationManager();

    SF.OperationDelete = function (deleteExecutor) {
        deleteExecutor.execute();
    }

    //ConstructorFromMany options = OperationManager options + returnType
    SF.ConstructorFromMany = function (_options) {
        SF.OperationManager.call(this, $.extend({
            controllerUrl: null,
            returnType: null
        }, _options));

        this.requestData = function (newPrefix, items) {
            SF.log("ConstructorFromMany requestData");

            var serializer = new SF.Serializer()
                                .add($('#' + SF.Keys.tabId).serialize())
                                .add({ runtimeType: $(this.pf(SF.Keys.entityTypeName)).val(),
                                    operationFullKey: this.options.operationKey,
                                    prefix: newPrefix,
                                    oldPrefix: this.options.prefix
                                });

            for (var i = 0, l = items.length; i < l; i++) {
                serializer.add("ids", items[i].id);
            }

            serializer.add(this.options.requestExtraJsonData);

            return serializer.serialize();
        };

        this.operationAjax = function (newPrefix, items, onSuccess) {
            SF.log("ConstructorFromMany operationAjax");

            SF.Notify.info(lang.signum.executingOperation);

            if (uiBlocked) {
                return false;
            }

            var self = this;
            SF.ajax({
                type: "POST",
                url: this.options.controllerUrl,
                data: this.requestData(this.newPrefix(), items),
                async: true,
                dataType: "html",
                success: function (operationResult) {
                    uiBlocked = false;
                    $(".uiBlocker").remove();

                    if (self.executedSuccessfully(operationResult)) {
                        var $operationResult = jQuery(operationResult);
                        $("body").trigger("sf-new-content", [$operationResult]);
                        if (onSuccess != null) {
                            onSuccess(newPrefix, $operationResult, self.options.parentDiv);
                        }
                    }
                    else {
                        SF.Notify.error(lang.signum.error, 2000);
                        return;
                    }
                },
                error:
                function () {
                    uiBlocked = false;
                    $(".uiBlocker").remove();
                    SF.Notify.error(lang.signum.error, 2000);
                }
            });
        };

        this.defaultConstruct = function () {
            SF.log("ConstructorFromMany defaultConstruct");

            if (uiBlocked) {
                return false;
            }

            var onSuccess = function (items) {
                this.operationAjax(this.newPrefix(), items, SF.opOpenPopup);
            }

            var self = this;
            new SF.FindNavigator({ prefix: this.options.prefix }).hasSelectedItems(function (items) { onSuccess.call(self, items) });
        };

        this.defaultSubmit = function () {
            SF.log("ConstructorFromMany defaultSubmit");

            if (uiBlocked) {
                return false;
            }

            var onSuccess = function (items) {
                for (var i = 0, l = items.length; i < l; i++) {
                    $("form").append(SF.hiddenInput('ids', items[i].id));
                }
                this.operationSubmit();
            };

            var self = this;
            new SF.FindNavigator({ prefix: this.options.prefix }).hasSelectedItems(function (items) { onSuccess.call(self, items) });
        }
    };

    SF.ConstructorFromMany.prototype = new SF.OperationManager();

    SF.reloadEntity = function (urlController, prefix, parentDiv) {
        var $partialViewName = $('#sfPartialViewName');
        var requestData = $("form :input").not(".searchControl :input").serialize() + "&prefix=" + prefix;
        if ($partialViewName.length === 1) {
            requestData += "&partialViewName=" + $partialViewName.val();
        }
        SF.ajax({
            type: "POST",
            url: urlController,
            data: requestData,
            async: false,
            dataType: "html",
            success: function (msg) {

                var $msg = jQuery(msg);
                $("body").trigger("sf-new-content", [$msg]);

                if (!SF.isEmpty(parentDiv)) {
                    $('#' + parentDiv + ' input[onblur]').attr('onblur', ''); // To avoid Chrome to fire onblur when replacing parentdiv content
                    $('#' + parentDiv).html('').append($msg);
                }
                else {
                    if (SF.isEmpty(prefix)) {
                        $('#divNormalControl').html('').append($msg);
                    }
                    else {
                        $('#' + SF.compose(prefix, "divMainControl") + ' input[onblur]').attr('onblur', ''); // To avoid Chrome to fire onblur when replacing popup content
                        $('#' + SF.compose(prefix, "divMainControl")).html('').append($msg);
                    }
                }
            }
        });
    }

    SF.opOnSuccessDispatcher = function (prefix, $operationResult, parentDiv) {
        SF.log("OperationExecutor OpDefaultOnSuccess");
        $(".contextmenu-active").removeClass("contextmenu-active");
        if ($operationResult === null) {
            return null;
        }

        var newPopupId = SF.compose(prefix, "panelPopup");
        var hasNewPopup = $("#" + newPopupId, $operationResult).length !== 0;
        //If result is a NormalControl, or an already opened popup => ReloadContent
        if (!hasNewPopup ||
            (hasNewPopup &&
            $(".popupWindow:visible").filter(function () { return this.id == newPopupId }).length !== 0)) {
            SF.opReloadContent(prefix, $operationResult, parentDiv)
        }
        else {
            SF.opOpenPopup(prefix, $operationResult)
        }
    }

    SF.opReloadContent = function (prefix, $operationResult, parentDiv) {
        SF.log("OperationExecutor OpReloadContent");
        $(".contextmenu-active").removeClass("contextmenu-active");
        if (SF.isEmpty(prefix)) { //NormalWindow

            var $elem = SF.isEmpty(parentDiv) ? $("#divNormalControl") : $("#" + parentDiv);
            $elem.html('').append($operationResult);
        }
        else { //PopupWindow
            new SF.ViewNavigator({
                prefix: prefix,
                containerDiv: SF.compose(prefix, "externalPopupDiv")
            }).viewSave($operationResult);
        }
        SF.Notify.info(lang.signum.operationExecuted, 2000);
    }

    SF.opOpenPopup = function (prefix, $operationResult) {
        SF.log("OperationExecutor OpOpenPopup");
        $(".contextmenu-active").removeClass("contextmenu-active");
        new SF.ViewNavigator({ prefix: prefix }).showCreateSave($operationResult);
        SF.Notify.info(lang.signum.operationExecuted, 2000);
    }

    SF.opOpenPopupNoDefaultOk = function (prefix, $operationResult) {
        SF.log("OperationExecutor OpOpenPopupNoDefaultOk");
        $(".contextmenu-active").removeClass("contextmenu-active");
        new SF.ViewNavigator({ prefix: prefix, onOk: function () { return false; } }).showCreateSave($operationResult);
        SF.Notify.info(lang.signum.operationExecuted, 2000);
    }

    SF.opNavigate = function (prefix, $operationResult) {
        SF.submit($operationResult.html());
    }

    SF.opMarkCellOnSuccess = function (prefix, $operationResult) {
        SF.log("OperationExecutor OpMarkCellOnSuccess");
        $(".contextmenu-active")
        .addClass($operationResult === null ? 'entityCtxMenuSuccess' : 'entityCtxMenuError')
        .removeClass("contextmenu-active");

        SF.Notify.info(lang.signum.operationExecuted, 2000);
    }

    SF.opContextualOnSuccess = function (prefix, $operationResult) {
        SF.log("OperationExecutor OpContextualOnSuccess");
        $(".contextmenu-active").removeClass("contextmenu-active");
        if ($operationResult === null) {
            return null;
        }

        var newPopupId = SF.compose(prefix, "panelPopup");
        var hasNewPopup = $("#" + newPopupId, $operationResult).length !== 0;
        //If result is a NormalControl => Load it
        if (hasNewPopup) {
            SF.opOpenPopup(prefix, $operationResult)
        }
        else {
            $("form").html('').append($operationResult);
            SF.Notify.info(lang.signum.operationExecuted, 2000);
        }
    }
});