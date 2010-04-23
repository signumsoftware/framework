var OperationManager = function(_options) {
    this.options = $.extend({
        prefix: "",
        operationKey: null,
        isLite: false,
        controllerUrl: null,
        onOk: null,
        onCancelled: null,
        requestExtraJsonData: null,
    }, _options);
};

OperationManager.prototype = {

    runtimeInfo: function() {
        return RuntimeInfoFor(this.options.prefix);
    },

    pf: function(s) {
        return "#" + this.options.prefix.compose(s);
    },

    newPrefix: function() {
        return "New".compose(this.options.prefix);
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

        var myRuntimeInfoKey = this.options.prefix.compose(sfRuntimeInfo);
        if (formChildren.filter("[name=" + myRuntimeInfoKey + "]").length  == 0) {
            if (empty(runtimeType))
                requestData += qp(myRuntimeInfoKey, info.createValue(StaticInfoFor(this.options.prefix).staticType(), info.id(), info.isNew(), info.ticks()));
            else
                requestData += qp(myRuntimeInfoKey, info.find().val());
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

    operationAjax: function(newPrefix, onSuccess)
    {
        log("OperationManager callServer");
    
        NotifyInfo(lang['executingOperation']);
    
        if (empty(newPrefix))
            newPrefix = this.options.prefix;
    
        var self = this;
        $.ajax({
            type: "POST",
            url: this.options.controllerUrl,
            data: this.requestData(newPrefix),
            async: false,
            dataType: "html",
            success: function(operationResult) {
                if (self.executedSuccessfully(operationResult)) {
                    if (onSuccess != null)
                        onSuccess(newPrefix, operationResult);
                }
                else{
                    NotifyInfo(lang['error'], 2000);
                    return;
                }
             },
             error:
                function() { NotifyInfo(lang['error'], 2000); }
        });
    },
    
    operationSubmit: function()
    {
        log("OperationManager operationSubmit");
    
        var info = this.runtimeInfo();
        if (info.find().length > 0)
        {
            $("form").append(hiddenInput("sfRuntimeType", info.runtimeType()) +
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
         
        Submit(this.options.controllerUrl);
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

    defaultSubmit: function() {
        log("OperationManager defaultSubmit");
        
        if (isTrue(this.options.isLite))
            operationSubmit();
        else {
            var onSuccess = function() { this.operationSubmit(); };
            if (!EntityIsValid({prefix:this.options.prefix}, onSuccess.call(this)))
                return;
        }
    }
};

var OperationExecutor = function(_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/OperationExecute"
    }, _options));

    this.defaultExecute = function() {
        log("OperationExecutor defaultExecute");
        
        var onSuccess = function() 
        { 
            this.operationAjax(null, ReloadContent); 
            NotifyInfo(lang['operationExecuted'], 2000); 
        };
        
        if (isTrue(this.options.isLite))
            onSuccess();
        else {
            if (!EntityIsValid({prefix:this.options.prefix}, onSuccess.call(this)))
                return;
        }
    };
};

OperationExecutor.prototype = new OperationManager();

//ConstructorFrom options = OperationManager options + returnType
var ConstructorFrom = function(_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/ConstructFromExecute",
        returnType: null
    }, _options));

    this.defaultConstruct = function() {
        log("ConstructorFrom construct");

        var onSuccess = function() 
        { 
            this.operationAjax(this.newPrefix(), OpenPopup); 
            NotifyInfo(lang['operationExecuted'], 2000); 
        }
        
        if (isTrue(this.options.isLite))
            onSuccess();
        else {
            if (!EntityIsValid({prefix:this.options.prefix}, onSuccess.call(this)))
                return;
        }
    };
};

ConstructorFrom.prototype = new OperationManager();

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
                    Submit(operationResult);
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
                    Submit(operationResult);
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
//    var newPrefix = "New".compose(prefix);
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
               $('#' + prefix.compose("divMainControl")).html(msg);
           
           if (!isFalse(reloadButtonBar))
           {
               var info = RuntimeInfoFor(prefix);
               $.post('Signum/GetButtonBar', { sfRuntimeType: info.runtimeType(), sfId: info.id(), prefix: prefix, sfTabId: $("#" + sfTabId).val() } ,function(data){ $('#' + prefix.compose("divButtonBar")).html(data) });
           }
        }
    });
}

function OpReloadContent(prefix, operationResult){
    log("OperationExecutor defaultOnSuccess");
    if (operationResult.indexOf("<form") >= 0) //NormalWindow: It might have prefix but the operation returns a full page reload
        $("#content").html(operationResult.substring(operationResult.indexOf("<form"), operationResult.indexOf("</form>") + 7));
    else { //PopupWindow
        new ViewNavigator({
            prefix: prefix,
            containerDiv: prefix.compose("externalPopupDiv")
        }).viewSave(operationResult);
    }
}

function OpOpenPopup(prefix, operationResult)
{
    new ViewNavigator({ prefix: prefix }).showCreateSave(operationResult);
}

function OpNavigate(prefix, operationResult)
{
    Submit(operationResult);
}


