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
        confirmMsg: null
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
                formChildren = $("form").serialize();
            else //PopupWindow
                formChildren = $(this.pf("panelPopup *") + ", #" + sfReactive + ", #" + sfTabId).serialize();
        }
        else {
            formChildren = qp(sfTabId, $('#' + sfTabId).val());
        }

        var info = this.runtimeInfo();

        formChildren += qp("isLite", this.options.isLite)
                     + qp("sfRuntimeType", info.runtimeType())
                     + qp("sfId", info.id())
                     + qp("sfOperationFullKey", this.options.operationKey)
                     + qp(sfPrefix, newPrefix)
                     + qp("sfOldPrefix", this.options.prefix)
                     + qp("sfOnOk", singleQuote(this.options.onOk));

        return formChildren;
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

        if (!this.options.avoidValidation) {
            if (!this.entityIsValid())
                return;
        }

        var info = this.runtimeInfo();
        $("form").append(hiddenInput('sfRuntimeType', info.runtimeType()) +
            hiddenInput('sfId', info.id()) +
            hiddenInput('isLite', this.options.isLite) +
            hiddenInput('sfOperationFullKey', this.options.operationKey) +
            hiddenInput(sfPrefix, this.options.prefix));
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

        if (!this.options.avoidValidation) {
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
                    return;
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

                if (empty(self.options.prefix)) //NormalWindow
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

        if (!this.options.avoidValidation) {
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

        if (!this.options.avoidValidation) {
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

function ConstructFromManyExecute(urlController, typeName, operationKey, prefix, onOk, onCancel) {
    var ids = GetSelectedElements(prefix);
    if (ids == "") return;
    var newPrefix = prefix + "_New";
    var self = this;
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfIds=" + ids + qp("sfRuntimeType", typeName) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)),
        async: false,
        dataType: "html",
        success: function(msg) {
            $('#' + prefix + "divASustituir").html(msg);
            new popup().show(prefix + "divASustituir");
            $('#' + newPrefix + sfBtnOk).click(onOk);
            $('#' + newPrefix + sfBtnCancel).click(function() { $('#' + prefix + "divASustituir").html(""); if (onCancel != null) onCancel(); });
        }
    });
}

function ReloadEntity(urlController, prefix, parentDiv) {
    $.ajax({
        type: "POST",
        url: urlController,
        data: $("form").serialize() + qp(sfPrefix, prefix),
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       if (!empty(parentDiv))
                           $('#' + parentDiv).html(msg);
                       else
                           $('#' + prefix + "divMainControl").html(msg);
                   }
    });
}


