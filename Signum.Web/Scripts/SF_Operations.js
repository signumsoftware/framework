var OperationManager = function(_options) {
    this.options = $.extend({
        prefix: "",
        operationKey: null,
        isLite: false,
        controllerUrl: null,
        onOk: null,
        multiStep: false,
        navigateOnSuccess: false,
        confirmMsg: null
    }, _options);
};

OperationManager.prototype = {

    entityInfo: function() {
        return EntityInfoFor(this.options.prefix);
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

        var info = this.entityInfo();

        formChildren += qp("isLite", this.options.isLite)
                     + qp("sfRuntimeType", info.runtimeType())
                     + qp("sfId", info.id())
                     + qp("sfOperationFullKey", this.options.operationKey)
                     + qp(sfPrefix, newPrefix)
                     + qp("sfOldPrefix", this.options.prefix)
                     + qp("sfOnOk", singleQuote(this.options.onOk))
                     + qp("sfOnCancel", singleQuote(this.options.onCancel));

        return formChildren;
    },

    entityIsValid: function() {
        log("OperationManager entityIsValid");
        var info = this.entityInfo();
        var isValid = null;
        if (empty(this.options.prefix))
            isValid = new Validator().validate();
        else
            isValid = new PartialValidator({ prefix: this.options.prefix, type: info.runtimeType(), id: info.id() }).validate().isValid;
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
            var info = this.entityInfo();
            new PartialValidator({ prefix: this.options.prefix, type: info.runtimeType(), id: info.id() }).showErrors(modelState);
        }
        return false;
    },

    post: function() {
        log("OperationManager post");
        if (!empty(this.options.confirmMsg) && !confirm(this.options.confirmMsg))
            return;

        NotifyInfo(lang['executingOperation']);

        if (!this.entityIsValid())
            return;

        var info = this.entityInfo();
        $("form").append(hiddenInput('sfRuntimeType', info.runtimeType()) +
            hiddenInput('sfId', info.id()) +
            hiddenInput('isLite', this.options.isLite) +
            hiddenInput('sfOperationFullKey', this.options.operationKey));
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

        if (!this.entityIsValid()) {
            NotifyInfo(lang['error'], 2000);
            return;
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
                        onOk: self.options.onOk
                    }).viewSave(operationResult);
                    return;
                }

                if (empty(self.options.prefix)) //NormalWindow
                    $("#content").html(operationResult.substring(operationResult.indexOf("<form"), operationResult.indexOf("</form>") + 7));
                else { //PopupWindow
                    new ViewNavigator({
                        prefix: newPrefix,
                        containerDiv: newPrefix + "externalPopupDiv",
                        onOk: self.options.onOk
                    }).viewSave(operationResult);
                }

                NotifyInfo(lang['operationExecuted'], 2000);
            },
            error:
                function() { NotifyInfo(lang['error'], 2000); }
        });
    };
};

OperationExecutor.prototype = new OperationManager();

function OperationExecute(executor) {
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

        if (!this.entityIsValid()) {
            NotifyInfo(lang['error'], 2000);
            return;
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

                var navigator = new ViewNavigator({ prefix: self.newPrefix(), type: self.options.returnType }).showCreateSave(operationResult);
                //$(self.pf("divASustituir")).html(operationResult);
                //new popup().show(self.options.prefix + "divASustituir");
                //$('#' + self.options.prefix + sfBtnCancel).click(empty(self.options.onCancel) ? (function() { $('#' + self.options.prefix + "divASustituir").html(""); }) : self.options.onCancel);
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

        if (!this.entityIsValid()) {
            NotifyInfo(lang['error'], 2000);
            return;
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
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfIds=" + ids + qp("sfRuntimeType", typeName) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)),
        async: false,
        dataType: "html",
        success: function(msg) {
            $('#' + prefix + "divASustituir").html(msg);
            new popup().show(self.options.prefix + "divASustituir");
            $('#' + newPrefix + sfBtnOk).click(onOk);
            $('#' + newPrefix + sfBtnCancel).click(empty(onCancel) ? (function() { $('#' + prefix + "divASustituir").html(""); }) : onCancel);
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


