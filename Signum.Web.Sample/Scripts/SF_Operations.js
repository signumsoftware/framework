var OperationManager = function(_options) {
    this.options = $.extend({
        prefix:"",
        operationKey:null,
        isLite:false,
        type:null,
        id:null,
        controllerUrl:null,
        onOk:null,
        onCancel:null,
        multiStep:false
    }, _options);
};

OperationManager.prototype = {

    entityInfo: function() {
        return EntityInfoFor(this.options.prefix);
    },

    pf: function(s) {
        return "#" + this.options.prefix + s;
    }
};
  
var Executor = function(_options) {
    OperationManager.call(this, _options);

    this.execute = function() {
    NotifyInfo(lang['executingOperation']);
    var formChildren = "";
    if (this.options.isLite) throw "OperationManager isLite cannot be true for execute operation. Use executeLite instead"
    
    if (this.options.prefix != "") //PopupWindow
        formChildren = $(this.pf("panelPopup *") + ", #" + sfReactive + ", #" + sfTabId).serialize();
    else //NormalWindow
        formChildren = $("form").serialize();
    
    var newPrefix = (!multiStep) ? prefix : (prefix + "New");
    $.ajax({
        type: "POST",
        url: urlController,
        data: formChildren + "isLite=false" + qp("sfRuntimeType", this.options.type) + qp("sfId", this.options.id) + qp("sfOperationFullKey", this.options.operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(this.options.onOk)) + qp("sfOnCancel", singleQuote(this.options.onCancel)),
        async: false,
        dataType: "html",
        success: function(msg) {
            if (msg.indexOf("ModelState") > 0) {
                eval('var result=' + msg);
                var modelState = result["ModelState"];
                ShowErrorMessages(prefix, modelState, true, "*");
            }
            else {
                if (isFalse(multiStep)) {
                    if (prefix != "") { //PopupWindow
                        $('#' + prefix + "externalPopupDiv").html(msg);
                    }
                    else {
                        $("#content").html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                    }
                }
                else {
                    $('#' + prefix + "divASustituir").html(msg);
                    if (msg.indexOf("<script") == 0)//A script to be run is returned instead of a Popup to open
                        return;
                    ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
                    //$('#' + newPrefix + sfBtnOk).click(onOk);
                    $('#' + newPrefix + sfBtnCancel).click(empty(onCancel) ? (function() { $('#' + prefix + "divASustituir").html(""); }) : onCancel);
                }
            }
        }
    });
    NotifyInfo(lang['operationExecuted'], 2000);
    };
};

Executor.prototype = new OperationManager();

function CallServer(urlController, prefix) {
    var ids = GetSelectedElements(prefix);
    if (ids == "") return;
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfIds=" + ids,
        async: false,
        dataType: "html",
        success: function(msg) {
            window.alert(msg);
        }
    });
}

function OperationExecute(urlController, typeName, id, operationKey, isLite, prefix, onOk, onCancel, multiStep) {
    NotifyInfo(lang['executingOperation']);
    var formChildren = "";
    if (isFalse(isLite)) {
        if (prefix != "") //PopupWindow
            formChildren = $('#' + prefix + "panelPopup *, #" + sfReactive + ", #" + sfTabId).serialize();
        else //NormalWindow
            formChildren = $("form").serialize();
    }
    if (formChildren.length > 0) formChildren = "&" + formChildren;
    var newPrefix = (isFalse(multiStep)) ? prefix : (prefix + "New");
    $.ajax({
        type: "POST",
        url: urlController,
        data: "isLite=" + isLite + qp("sfRuntimeType", typeName) + qp("sfId", id) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)) + formChildren,
        async: false,
        dataType: "html",
        success: function(msg) {
            if (msg.indexOf("ModelState") > 0) {
                eval('var result=' + msg);
                var modelState = result["ModelState"];
                ShowErrorMessages(prefix, modelState, true, "*");
            }
            else {
                if (isFalse(multiStep)) {
                    if (prefix != "") { //PopupWindow
                        $('#' + prefix + "externalPopupDiv").html(msg);
                    }
                    else {
                        $("#content").html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                    }
                }
                else {
                    $('#' + prefix + "divASustituir").html(msg);
                    if (msg.indexOf("<script") == 0)//A script to be run is returned instead of a Popup to open
                        return;
                    ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
                    //$('#' + newPrefix + sfBtnOk).click(onOk);
                    $('#' + newPrefix + sfBtnCancel).click(empty(onCancel) ? (function() { $('#' + prefix + "divASustituir").html(""); }) : onCancel);
                }
            }
            NotifyInfo(lang['operationExecuted'], 2000);
        }
    });
}

function DeleteExecute(urlController, typeName, id, operationKey, isLite, prefix, onOk, onCancel, confirmMsg) {
    if (!confirm(confirmMsg))
        return;
    NotifyInfo(lang['executingOperation']);
    var formChildren = "";
    if (isFalse(isLite)) {
        if (prefix != "") //PopupWindow
            formChildren = $('#' + prefix + "panelPopup *, #" + sfReactive + ", #" + sfTabId).serialize();
        else //NormalWindow
            formChildren = $("form").serialize();
    }
    if (formChildren.length > 0) formChildren = "&" + formChildren;
    $.ajax({
        type: "POST",
        url: urlController,
        data: "isLite=" + isLite + qp("sfRuntimeType", typeName) + qp("sfId", id) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, prefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)) + formChildren,
        async: false,
        dataType: "html",
        success: function(msg) {
            NotifyInfo(lang['operationExecuted'], 2000);
        }
    });
}

function ConstructFromExecute(urlController, typeName, id, operationKey, isLite, prefix, onOk, onCancel, multiStep) {
    NotifyInfo(lang['executingOperation']);
    var formChildren = "";
    if (isFalse(isLite)) {
        if (prefix != "") //PopupWindow
            formChildren = $('#' + prefix + "panelPopup *, #" + sfReactive + ", #" + sfTabId).serialize();
        else //NormalWindow
            formChildren = $("form").serialize();
    }
    if (formChildren.length > 0) formChildren = "&" + formChildren;
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: "isLite=" + isLite + qp("sfRuntimeType", typeName) + qp("sfId", id) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)) + formChildren,
        async: false,
        dataType: "html",
        success: function(msg) {
            if (msg.indexOf("ModelState") > 0) {
                eval('var result=' + msg);
                var modelState = result["ModelState"];
                ShowErrorMessages(prefix, modelState, true, "*");
            }
            else {
                $('#' + prefix + "divASustituir").html(msg);
                if (msg.indexOf("<script") == 0)//A script to be run is returned instead of a Popup to open
                    return;
                ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
                //$('#' + newPrefix + sfBtnOk).click(onOk);
                $('#' + newPrefix + sfBtnCancel).click(empty(onCancel) ? (function() { $('#' + prefix + "divASustituir").html(""); }) : onCancel);
            }
            NotifyInfo(lang['operationExecuted'], 2000);
        }
    });
}

function ConstructFromExecutePost(urlController, typeName, id, operationKey, isLite) {
    NotifyInfo(lang['executingOperation']);
    if (Validate({ prefixToIgnore: "my" })) {
        $("form").append("<input type='hidden' id='sfRuntimeType' name='sfRuntimeType' value='" + typeName + "'/>")
            .append("<input type='hidden' id='sfId' name='sfId' value='" + id + "'/>")
            .append("<input type='hidden' id='isLite' name='isLite' value='" + isLite + "'/>")
            .append("<input type='hidden' id='sfOperationFullKey' name='sfOperationFullKey' value='" + operationKey + "'/>");
        document.forms[0].action = urlController;
        document.forms[0].submit();
    }
}

function ConstructFromManyExecute(urlController, typeName, operationKey, prefix, onOk, onCancel) {
    var ids = GetSelectedElements(prefix);
    if (ids == "") return;
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfIds=" + ids + qp("sfRuntimeType", typeName) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)),
        async: false,
        dataType: "html",
        success: function(msg) {
            $('#' + prefix + "divASustituir").html(msg);
            ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
            $('#' + newPrefix + sfBtnOk).click(onOk);
            $('#' + newPrefix + sfBtnCancel).click(empty(onCancel) ? (function() { $('#' + prefix + "divASustituir").html(""); }) : onCancel);
        }
    });
}

