var uiBlocked = false;

$(function () {
    $('.entity-operation').live('click', function () {
        uiBlocked = true;
        var $divblocker = $("<div class='uiBlocker'></div>").width('300%').height('300%');
        $('body').append($divblocker);
    });
});

var OperationManager = function(_options) {
    this.options = $.extend({
        prefix: "",
        operationKey: null,
        isLite: false,
        controllerUrl: null,
        onOk: null,
        onCancelled: null,
        requestExtraJsonData: null
    }, _options);
};

OperationManager.prototype = {

    runtimeInfo: function () {
        return RuntimeInfoFor(this.options.prefix);
    },

    pf: function (s) {
        return "#" + this.options.prefix.compose(s);
    },

    newPrefix: function () {
        return "New".compose(this.options.prefix);
    },

    requestData: function (newPrefix) {
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
        var requestData = [];
        requestData.push(formChildren.serialize());

        var info = this.runtimeInfo();
        var runtimeType = info.runtimeType();

        var myRuntimeInfoKey = this.options.prefix.compose(sfRuntimeInfo);
        if (formChildren.filter("[name=" + myRuntimeInfoKey + "]").length == 0) {
            if (empty(runtimeType))
                requestData.push(
                    qp(myRuntimeInfoKey, info.createValue(StaticInfoFor(this.options.prefix).staticType(), info.id(), info.isNew(), info.ticks()))
                );
            else
                requestData.push(
                    qp(myRuntimeInfoKey, info.find().val())
                );
        }
        requestData.push(qp("isLite", this.options.isLite)
                     + qp("sfOperationFullKey", this.options.operationKey)
                     + qp(sfPrefix, newPrefix)
                     + qp("sfOldPrefix", this.options.prefix)
                     + qp("sfOnOk", singleQuote(this.options.onOk)));

        if (!empty(this.options.requestExtraJsonData)) {
            for (var key in this.options.requestExtraJsonData) {
                if (jQuery.isFunction(this.options.requestExtraJsonData[key]))
                    requestData.push(qp(key, this.options.requestExtraJsonData[key]()));
                else
                    requestData.push(qp(key, this.options.requestExtraJsonData[key]));
            }
        }

        return requestData.join('');
    },

    operationAjax: function (newPrefix, onSuccess) {
        log("OperationManager operationAjax");

        if (uiBlocked)
            return false;

        NotifyInfo(lang['executingOperation']);

        if (empty(newPrefix))
            newPrefix = this.options.prefix;

        var self = this;
        SF.ajax({
            type: "POST",
            url: this.options.controllerUrl,
            data: this.requestData(newPrefix),
            async: true,
            dataType: "html",
            success: function (operationResult) {
                uiBlocked = false;
                $(".uiBlocker").remove();

                if (self.executedSuccessfully(operationResult)) {
                    if (onSuccess != null) {
                        onSuccess(newPrefix, operationResult);
                    }
                }
                else {
                    NotifyInfo(lang['error'], 2000);
                    return;
                }
            },
            error:
                function () {
                    uiBlocked = false;
                    $(".uiBlocker").remove();
                    NotifyInfo(lang['error'], 2000);
                }
        });
    },

    operationSubmit: function () {
        log("OperationManager operationSubmit");

        if (uiBlocked)
            return false;

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

    executedSuccessfully: function (operationResult) {
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

    defaultSubmit: function () {
        log("OperationManager defaultSubmit");

        if (uiBlocked)
            return false;

        if (isTrue(this.options.isLite))
            this.operationSubmit();
        else {
            var onSuccess = function () { this.operationSubmit(); };
            var self = this;
            if (!EntityIsValid({ prefix: this.options.prefix }, function () { onSuccess.call(self) }))
                return;
        }
    }
};

var OperationExecutor = function (_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/OperationExecute"
    }, _options));

    this.defaultExecute = function () {
        log("OperationExecutor defaultExecute");

        if (uiBlocked)
            return false;

        var onSuccess = function () {
            this.operationAjax(null, OpReloadContent);
        };

        var self = this;
        if (isTrue(this.options.isLite))
            onSuccess.call(this);
        else {
            if (!EntityIsValid({ prefix: this.options.prefix }, function () { onSuccess.call(self) }))
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

        if (uiBlocked)
            return false;

        var onSuccess = function () 
        { 
            this.operationAjax(this.newPrefix(), OpOpenPopup);
        }

        var self = this;
        if (isTrue(this.options.isLite))
            onSuccess.call(this);
        else {
            if (!EntityIsValid({ prefix: this.options.prefix }, function() { onSuccess.call(self) }))
                return;
        }
    };
};

ConstructorFrom.prototype = new OperationManager();

var DeleteExecutor = function(_options) {
    OperationManager.call(this, $.extend({
        controllerUrl: "Operation/DeleteExecute"
    }, _options));

    this.defaultDelete = function() {
        log("DeleteExecutor defaultDelete");

        if (uiBlocked)
            return false;

        if (!empty(this.options.confirmMsg) && !confirm(this.options.confirmMsg))
            return;

        var self = this;
        if (isTrue(this.options.isLite)) {
            NotifyInfo(lang['executingOperation']);
            this.operationAjax(this.newPrefix(), function() { NotifyInfo(lang['operationExecuted'], 2000); });
        }
        else {
            throw "Delete operation must be Lite";
        }
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

        var requestData = [];
        requestData.push($('#' + sfTabId).serialize());
        requestData.push(qp("sfRuntimeType", $(this.pf(sfEntityTypeName)).val())
                     + qp("sfOperationFullKey", this.options.operationKey)
                     + qp(sfPrefix, newPrefix)
                     + qp("sfOldPrefix", this.options.prefix)
                     + qp("sfOnOk", singleQuote(this.options.onOk)));

        for (var i = 0, l = items.length; i < l; i++)
            requestData.push(qp("sfIds", items[i].id));

        if (!empty(this.options.requestExtraJsonData)) {
            for (var key in this.options.requestExtraJsonData) {
                if (jQuery.isFunction(this.options.requestExtraJsonData[key]))
                    requestData.push(qp(key, this.options.requestExtraJsonData[key]()));
                else
                    requestData.push(qp(key, this.options.requestExtraJsonData[key]));
            }
        }

        return requestData.join('');
    };

    this.operationAjax = function(newPrefix, items, onSuccess) {
        log("ConstructorFromMany operationAjax");

        NotifyInfo(lang['executingOperation']);

        if (uiBlocked)
            return false;

        var self = this;
        SF.ajax({
            type: "POST",
            url: this.options.controllerUrl,
            data: this.requestData(this.newPrefix(), items),
            async: true,
            dataType: "html",
            success: function(operationResult) {
                uiBlocked = false;
                $(".uiBlocker").remove();

                if (self.executedSuccessfully(operationResult)) {
                    if (onSuccess != null)
                        onSuccess(newPrefix, operationResult);
                }
                else {
                    NotifyInfo(lang['error'], 2000);
                    return;
                }
            },
            error:
                function () {
                    uiBlocked = false;
                    $(".uiBlocker").remove();

                    NotifyInfo(lang['error'], 2000);
                }
        });
    };

    this.defaultConstruct = function() {
        log("ConstructorFromMany defaultConstruct");

        if (uiBlocked)
            return false;

        var onSuccess = function (items) {
            this.operationAjax(this.newPrefix(), items, OpOpenPopup);
        }

        var self = this;
        HasSelectedItems({ prefix: this.options.prefix }, function(items) { onSuccess.call(self, items) });
    };

    this.defaultSubmit = function() {
        log("ConstructorFromMany defaultSubmit");

        if (uiBlocked)
            return false;

        var onSuccess = function (items) {
            for (var i = 0, l = items.length; i < l; i++)
                $("form").append(hiddenInput('sfIds', items[i].id));
            this.operationSubmit();
        };

        var self = this;
        HasSelectedItems({ prefix: this.options.prefix }, function(items) { onSuccess.call(self, items) });
    }
};

ConstructorFromMany.prototype = new OperationManager();

function OperationConstructFromMany(constructorFrom) {
    constructorFromMany.construct();
}

function ReloadEntity(urlController, prefix, parentDiv) {
    var $partialViewName = $('#' + sfPartialViewName);
    var requestData = $("form :input").not(".searchControl :input").serialize() + qp(sfPrefix, prefix);
    if($partialViewName.length == 1)
        requestData += qp(sfPartialViewName, $partialViewName.val());
    SF.ajax({
        type: "POST",
        url: urlController,
        data: requestData,
        async: false,
        dataType: "html",
        success: function (msg) {
            if (!empty(parentDiv))
                $('#' + parentDiv).html(msg);
            else {
                if (empty(prefix))
                    $('#divNormalControl').html(msg);
                else
                    $('#' + prefix.compose("divMainControl")).html(msg);
            }
        }
    });
}

function OpOnSuccessDispatcher(prefix, operationResult) {
    log("OperationExecutor OpDefaultOnSuccess");
    if (empty(operationResult))
        return null;
    if (operationResult.indexOf("jsonResultType") > 0)
        return null; //ModelState errors should have been handled previously and same with redirections

    var $result = $(operationResult);
    var newPopupId = prefix.compose("popupWindow");
    var hasNewPopup = $("#" + newPopupId, $result).length > 0; 
    //Si el resultado es un normalControl, o es un popup y coincide con alguno de los abiertos => ReloadContent
    if (!hasNewPopup ||
            (hasNewPopup &&
            $(".popupWindow:visible").attr('id').filter(function() { return this == newPopupId }).lengh == 0)) {
        OpReloadContent(prefix, operationResult)
    }
    else {
        OpOpenPopup(prefix, operationResult)
    }
}

function OpReloadContent(prefix, operationResult){
    log("OperationExecutor OpReloadContent");
    if (empty(prefix)) //NormalWindow
        $("#divNormalControl").html(operationResult);
    else { //PopupWindow
        new ViewNavigator({
            prefix: prefix,
            containerDiv: prefix.compose("externalPopupDiv")
        }).viewSave(operationResult);
    }
    NotifyInfo(lang['operationExecuted'], 2000); 
}

function OpOpenPopup(prefix, operationResult) {
    log("OperationExecutor OpOpenPopup");
    new ViewNavigator({ prefix: prefix }).showCreateSave(operationResult);
    NotifyInfo(lang['operationExecuted'], 2000); 
}

function OpOpenPopupNoDefaultOk(prefix, operationResult)
{
    log("OperationExecutor OpOpenPopupNoDefaultOk");
    new ViewNavigator({ prefix: prefix, onOk: function() { return false; } }).showCreateSave(operationResult);
    NotifyInfo(lang['operationExecuted'], 2000); 
}

function OpNavigate(prefix, operationResult)
{
    Submit(operationResult); 
}


