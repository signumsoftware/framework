var OperationManager = function(_options) {
    this.options = $.extend({
        prefix: "",
        operationKey: null,
        isLite: false,
        controllerUrl: null,
        validationControllerUrl: null,
        avoidValidation: false,
        onOk: null,
        onCancelled: null,
        onOperationSuccess: null,
        multiStep: false,
        navigateOnSuccess: false,
        closePopupOnSuccess: false,
        confirmMsg: null,
        requestExtraJsonData: null,
    }, _options);
};

OperationManager.prototype = {

    runtimeInfo: function() {
        return RuntimeInfoFor(this.options.prefix);
    },

    pf: function(s) {
        return "#" + this.options.prefix + s;
    },

    newPrefix: function() {
        return this.options.prefix + "_New";
    },

    requestData: function(newPrefix) {
        log("OperationManager requestData");
        var formChildren = "";
        if (isFalse(this.options.isLite)) {
            if (empty(this.options.prefix)) //NormalWindow 
                formChildren = $("form *");
            else //PopupWindow
                formChildren = $(this.pf("panelPopup *") + ", #" + sfReactive + ", #" + sfTabId);
        }
        else {
            formChildren = $('#' + sfTabId);
        }
        formChildren = formChildren.not(".searchControl *");
        var requestData = formChildren.serialize();
        
        var info = this.runtimeInfo();
        var runtimeType = info.runtimeType();

        if (requestData.indexOf(this.options.prefix + sfRuntimeInfo) < 0)
        {
            if (empty(runtimeType))
                requestData += qp(this.options.prefix + sfRuntimeInfo, info.createValue(StaticInfoFor(this.options.prefix).staticType(), info.id(), info.isNew(), info.ticks()));
            else
                requestData += qp(this.options.prefix + sfRuntimeInfo, info.find().val());
        }
        requestData += qp("isLite", this.options.isLite)
                     + qp("sfRuntimeType", empty(runtimeType) ? StaticInfoFor(this.options.prefix).staticType() : runtimeType)
                     + qp("sfId", info.id())
                     + qp("sfOperationFullKey", this.options.operationKey)
                     + qp(sfPrefix, newPrefix)
                     + qp("sfOldPrefix", this.options.prefix)
                     + qp("sfOnOk", singleQuote(this.options.onOk));

        if (!empty(this.options.requestExtraJsonData)) {
            for (var key in this.options.requestExtraJsonData) {
                if (jQuery.isFunction(this.options.requestExtraJsonData[key]))
                    requestData += qp(key, this.options.requestExtraJsonData[key]());
                else
                    requestData += qp(key, this.options.requestExtraJsonData[key]);
            }
        }
        
        return requestData;
    },

    entityIsValid: function() {
        log("OperationManager entityIsValid");
        var info = this.runtimeInfo();
        var isValid = null;
        if (empty(this.options.prefix))
            isValid = new Validator({ controllerUrl: this.options.validationControllerUrl }).validate();
        else
            isValid = new PartialValidator({ controllerUrl: this.options.validationControllerUrl, prefix: this.options.prefix, type: info.runtimeType(), id: info.id() }).validate().isValid;
        if (!isValid) {
            window.alert(lang['popupErrorsStop']);
            return false;
        }
        return true;
    },

    executedSuccessfully: function(operationResult) {
        log("OperationManager executedSuccessfully");
        if (operationResult.indexOf("ModelState") < 0)
            return true;

        eval('var result=' + operationResult);
        var modelState = result["ModelState"];
        if (empty(this.options.prefix))
            new Validator().showErrors(modelState);
        else {
            var info = this.runtimeInfo();
            new PartialValidator({ prefix: this.options.prefix, type: info.runtimeType(), id: info.id() }).showErrors(modelState);
        }
        return false;
    },

    post: function() {
        log("OperationManager post");
        if (!empty(this.options.confirmMsg) && !confirm(this.options.confirmMsg))
            return;

        NotifyInfo(lang['executingOperation']);

        if (!this.options.avoidValidation && isFalse(this.options.isLite)) {
            if (!this.entityIsValid())
                return;
        }

        var info = this.runtimeInfo();
        if (info.find().length > 0)
        {
            $("form").append(hiddenInput('sfRuntimeType', info.runtimeType()) +
                hiddenInput('sfId', info.id()));
        }
        $("form").append(hiddenInput('isLite', this.options.isLite) +
            hiddenInput('sfOperationFullKey', this.options.operationKey) +
            hiddenInput("sfOldPrefix", this.options.prefix));
            
        if (!empty(this.options.requestExtraJsonData)) {
            for (var key in this.options.requestExtraJsonData) {
                if (jQuery.isFunction(this.options.requestExtraJsonData[key]))
                    $("form").append(hiddenInput(key, this.options.requestExtraJsonData[key]()));
                else
                    $("form").append(hiddenInput(key, this.options.requestExtraJsonData[key]));
            }
        }
            
        document.forms[0].action = this.options.controllerUrl;
        document.forms[0].submit();
    }
};

var OperationExecutor = function(_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/OperationExecute"
    }, _options));

    this.execute = function() {
        log("OperationExecutor execute");
        if (!empty(this.options.confirmMsg) && !confirm(this.options.confirmMsg))
            return;

        NotifyInfo(lang['executingOperation']);

        if (!this.options.avoidValidation && isFalse(this.options.isLite)) {
            if (!this.entityIsValid()) {
                NotifyInfo(lang['error'], 2000);
                return;
            }
        }

        var newPrefix = (isFalse(this.options.multiStep)) ? this.options.prefix : this.newPrefix();
        var self = this;
        $.ajax({
            type: "POST",
            url: this.options.controllerUrl,
            data: this.requestData(newPrefix),
            async: false,
            dataType: "html",
            success: function(operationResult) {
                if (!self.executedSuccessfully(operationResult)) {
                    NotifyInfo(lang['error'], 2000);
                    return;
                }

                if (self.options.navigateOnSuccess) {
                    PostServer(operationResult);
                    return false;
                }

                if (self.options.multiStep) {
                    new ViewNavigator({
                        prefix: newPrefix,
                        containerDiv: newPrefix + "externalPopupDiv",
                        onOk: self.options.onOk,
                        onCancelled: self.options.onCancelled
                    }).viewSave(operationResult);
                    return;
                }

                //if (empty(self.options.prefix)) //NormalWindow
                if (operationResult.indexOf("<form") >= 0) //NormalWindow: It might have prefix but the operation returns a full page reload
                    $("#content").html(operationResult.substring(operationResult.indexOf("<form"), operationResult.indexOf("</form>") + 7));
                else { //PopupWindow
                    if (self.options.closePopupOnSuccess) {
                        $('#' + newPrefix + "externalPopupDiv").remove();
                    }
                    else {
                        new ViewNavigator({
                            prefix: newPrefix,
                            containerDiv: newPrefix + "externalPopupDiv",
                            onOk: self.options.onOk,
                            onCancelled: self.options.onCancelled
                        }).viewSave(operationResult);
                    }
                }

                if (!empty(self.options.onOperationSuccess))
                    self.options.onOperationSuccess();

                NotifyInfo(lang['operationExecuted'], 2000);
            },
            error:
                function() { NotifyInfo(lang['error'], 2000); }
        });
    };
};

OperationExecutor.prototype = new OperationManager();

function OperationExecute(executor, prefix) {
    if (!empty(prefix))
        executor.options = $.extend(executor.options, { prefix: prefix });
    executor.execute();
}

function OperationExecutePost(executor) {
    executor.post();
}

//ConstructorFrom options = OperationManager options + returnType
var ConstructorFrom = function(_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/ConstructFromExecute",
        returnType: null
    }, _options));

    this.construct = function() {
        log("ConstructorFrom construct");

        if (!empty(this.options.confirmMsg) && !confirm(this.options.confirmMsg))
            return;

        NotifyInfo(lang['executingOperation']);

        if (!this.options.avoidValidation && isFalse(this.options.isLite)) {
            if (!this.entityIsValid()) {
                NotifyInfo(lang['error'], 2000);
                return;
            }
        }

        var self = this;
        $.ajax({
            type: "POST",
            url: this.options.controllerUrl,
            data: this.requestData(this.newPrefix()),
            async: false,
            dataType: "html",
            success: function(operationResult) {
                if (!self.executedSuccessfully(operationResult)) {
                    NotifyInfo(lang['error'], 2000);
                    return;
                }

                if (self.options.navigateOnSuccess) {
                    PostServer(operationResult);
                    return;
                }

                var navigator = new ViewNavigator({
                    prefix: self.newPrefix(),
                    type: self.options.returnType,
                    onOk: self.options.onOk,
                    onCancelled: self.options.onCancelled
                }).showCreateSave(operationResult);
                //$(self.pf("divASustituir")).html(operationResult);
                //new popup().show(self.options.prefix + "divASustituir");
                //$('#' + self.options.prefix + sfBtnCancel).click(empty(self.options.onCancel) ? (function() { $('#' + self.options.prefix + "divASustituir").html(""); }) : self.options.onCancel);

                if (!empty(self.options.onOperationSuccess))
                    self.options.onOperationSuccess();

                NotifyInfo(lang['operationExecuted'], 2000);
            },
            error:
                function() { NotifyInfo(lang['error'], 2000); }
        });
    };
};

ConstructorFrom.prototype = new OperationManager();

function OperationConstructFrom(constructorFrom) {
    constructorFrom.construct();
}

function OperationConstructFromPost(constructorFrom) {
    constructorFrom.post();
}

var DeleteExecutor = function(_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/DeleteExecute"
    }, _options));

    this.execute = function() {
        log("DeleteOperation delete");

        if (!empty(this.options.confirmMsg) && !confirm(this.options.confirmMsg))
            return;

        NotifyInfo(lang['executingOperation']);

        if (!this.options.avoidValidation && isFalse(this.options.isLite)) {
            if (!this.entityIsValid()) {
                NotifyInfo(lang['error'], 2000);
                return;
            }
        }

        var self = this;
        $.ajax({
            type: "POST",
            url: this.options.controllerUrl,
            data: this.requestData(this.newPrefix()),
            async: false,
            dataType: "html",
            success: function(operationResult) {
                if (!self.executedSuccessfully(operationResult)) {
                    NotifyInfo(lang['error'], 2000);
                    return;
                }

                if (self.options.navigateOnSuccess) {
                    PostServer(operationResult);
                    return;
                }

                if (!empty(self.options.onOperationSuccess))
                    self.options.onOperationSuccess();

                NotifyInfo(lang['operationExecuted'], 2000);
            },
            error:
                function() { NotifyInfo(lang['error'], 2000); }
        });
    };
};

DeleteExecutor.prototype = new OperationManager();

function OperationDelete(deleteExecutor) {
    deleteExecutor.execute();
}

//ConstructorFromMany options = OperationManager options + returnType
var ConstructorFromMany = function(_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/ConstructFromManyExecute",
        returnType: null
    }, _options));

    this.requestData = function(newPrefix, items) {
        log("ConstructorFromMany requestData");
        var requestData = $('#' + sfTabId).serialize();
        requestData += qp("sfRuntimeType", $(this.pf(sfEntityTypeName)).val())
                     + qp("sfOperationFullKey", this.options.operationKey)
                     + qp(sfPrefix, newPrefix)
                     + qp("sfOldPrefix", this.options.prefix)
                     + qp("sfOnOk", singleQuote(this.options.onOk));

        for(var i = 0; i<items.length; i++)
            requestData += qp("sfIds", items[i].id);

        if (!empty(this.options.requestExtraJsonData)) {
            for (var key in this.options.requestExtraJsonData) {
                if (jQuery.isFunction(this.options.requestExtraJsonData[key]))
                    requestData += qp(key, this.options.requestExtraJsonData[key]());
                else
                    requestData += qp(key, this.options.requestExtraJsonData[key]);
            }
        }
        
        return requestData;
    },

    this.construct = function() {
        log("ConstructorFromMany construct");

        if (!empty(this.options.confirmMsg) && !confirm(this.options.confirmMsg))
            return;

        NotifyInfo(lang['executingOperation']);

        var items = SelectedItems({prefix:this.options.prefix});
        if (items.length == 0) 
        {
            NotifyInfo(lang['noElementsSelected']);
            return;
        }
    
        var self = this;
        $.ajax({
            type: "POST",
            url: this.options.controllerUrl,
            data: this.requestData(this.newPrefix(), items),
            async: false,
            dataType: "html",
            success: function(operationResult) {
                if (!self.executedSuccessfully(operationResult)) {
                    NotifyInfo(lang['error'], 2000);
                    return;
                }

                if (self.options.navigateOnSuccess) {
                    PostServer(operationResult);
                    return;
                }

                var navigator = new ViewNavigator({
                    prefix: self.newPrefix(),
                    type: self.options.returnType,
                    onOk: self.options.onOk,
                    onCancelled: self.options.onCancelled
                }).showCreateSave(operationResult);
                
                if (!empty(self.options.onOperationSuccess))
                    self.options.onOperationSuccess();

                NotifyInfo(lang['operationExecuted'], 2000);
            },
            error:
                function() { NotifyInfo(lang['error'], 2000); }
        });
    };
};

ConstructorFromMany.prototype = new OperationManager();

function OperationConstructFromMany(constructorFrom) {
    constructorFromMany.construct();
}

//function ConstructFromManyExecute(urlController, typeName, operationKey, prefix, onOk, onCancel) {
//    var ids = GetSelectedElements(prefix);
//    if (ids == "") return;
//    var newPrefix = prefix + "_New";
//    var self = this;
//    $.ajax({
//        type: "POST",
//        url: urlController,
//        data: "sfIds=" + ids + qp("sfRuntimeType", typeName) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)),
//        async: false,
//        dataType: "html",
//        success: function(msg) {
//            $('#' + prefix + "divASustituir").html(msg);
//            new popup().show(prefix + "divASustituir");
//            $('#' + newPrefix + sfBtnOk).click(onOk);
//            $('#' + newPrefix + sfBtnCancel).click(function() { $('#' + prefix + "divASustituir").html(""); if (onCancel != null) onCancel(); });
//        }
//    });
//}

function ReloadEntity(urlController, prefix, parentDiv, reloadButtonBar) {
    $.ajax({
        type: "POST",
        url: urlController,
        data: $("form *").not(".searchControl *").serialize() + qp(sfPrefix, prefix),
        async: false,
        dataType: "html",
        success: function(msg) {
           if (!empty(parentDiv))
               $('#' + parentDiv).html(msg);
           else
               $('#' + prefix + "divMainControl").html(msg);
           
           if (!isFalse(reloadButtonBar))
           {
               var info = RuntimeInfoFor(prefix);
               $.post('Signum/GetButtonBar', { sfRuntimeType: info.runtimeType(), sfId: info.id(), prefix: prefix, sfTabId: $("#" + sfTabId).val() } ,function(data){ $('#' + prefix + "divButtonBar").html(data) });
           }
        }
    });
}


