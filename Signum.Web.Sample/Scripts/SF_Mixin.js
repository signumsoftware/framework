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
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function RemoveFileLineEntity(prefix, reloadOnChangeFunction) {
    $('#' + prefix + sfToStr).val("").removeClass(sfInputErrorClass);
    $('#' + prefix + sfLink).val("").removeClass(sfInputErrorClass);
    $('#' + prefix + sfRuntimeType).val("");
    $('#' + prefix + sfIsNew).remove();
    window[prefix + sfEntityTemp] = "";

    /*$('#' + prefix + sfEntity).html(""); */
    $('#' + prefix + sfId).val("");

    $('#div' + prefix + 'Old').hide();
    $('#div' + prefix + 'New').show();

    if (!empty(reloadOnChangeFunction)) {
        $('#' + prefix + sfTicks).val(new Date().getTime());
        reloadOnChangeFunction();
    }
}

function DownloadFile(urlController, prefix) {
    var id = $('#' + prefix + sfId).val();
    if (empty(id) || id < 0)
        return;

    window.open(urlController + "?filePathID=" + id);
}

function UploadFile(urlController, prefix) {
    $('#' + prefix)[0].setAttribute('value', $('#' + prefix)[0].value);
    $('#' + prefix + 'loading').show();
    var mform = $('form');
    var cEncType = mform.attr('enctype');
    var cEncoding = mform.attr('encoding');
    var cTarget = mform.attr('target');
    var cAction = mform.attr('action');
    mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data').attr('target', 'frame' + prefix).attr('action', urlController).submit();
    mform.attr('enctype', cEncType).attr('encoding', cEncoding).attr('target', cTarget).attr('action', cAction);
}

function AutocompleteOnSelected(extendedControlName, newIdAndType, newValue, hasEntity) {
    var prefix = extendedControlName.substr(0, extendedControlName.indexOf(sfToStr));
    var _index = newIdAndType.indexOf("_");
    $('#' + prefix + sfId).val(newIdAndType.substr(0, _index));
    $('#' + prefix + sfRuntimeType).val(newIdAndType.substr(_index + 1, newIdAndType.length));
    $('#' + prefix + sfLink).html($('#' + extendedControlName).val());
    $('#' + prefix + sfTicks).val(new Date().getTime());
    toggleButtonsDisplay(prefix, hasEntity);
}

function NewRepeaterFile(urlController, prefix, fileType, removeLinkText, maxElements) {
    debug = true;

    $('#' + prefix + sfTicks).val(new Date().getTime());
    if (!empty(maxElements)) {
        var elements = $("#" + prefix + sfItemsContainer + " > div[name$=" + sfRepeaterItem + "]").length;
        if (elements >= parseInt(maxElements))
            return;
    }
    var lastElement = $("#" + prefix + sfItemsContainer + " > div[name$=" + sfRepeaterItem + "]:last");
    var lastIndex = -1;
    if (lastElement.length > 0) {
        var nameSelected = lastElement[0].id;
        lastIndex = nameSelected.substring(prefix.length + 1, nameSelected.indexOf(sfRepeaterItem));
    }
    var newIndex = "_" + (parseInt(lastIndex) + 1);

    log($("#" + prefix + sfItemsContainer).length);
    log(lastIndex + " | " + newIndex + " | " + lastElement.length);

    var runtimeType = "FilePathDN";
    $.ajax({
        type: "POST",
        url: urlController,
        data: { prefix: prefix + newIndex, fileType: fileType },
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       var newPrefix = prefix + newIndex;
                       $("#" + prefix + sfItemsContainer).append("\n" +
                        "<div id='" + newPrefix + sfRepeaterItem + "' name='" + newPrefix + sfRepeaterItem + "' class='repeaterElement'>\n" +
                        "<a id='" + newPrefix + "_btnRemove' title='" + removeLinkText + "' href=\"javascript:RemoveRepeaterEntity('" + newPrefix + sfRepeaterItem + "');\" class='lineButton remove'>" + removeLinkText + "</a>\n" +
                       // "<input type='hidden' id='" + newPrefix + sfRuntimeType + "' name='" + newPrefix + sfRuntimeType + "' value='" + runtimeType + "' />\n" +
                       // "<input type='hidden' id='" + newPrefix + sfId + "' name='" + newPrefix + sfId + "' value='' />\n" +
                       //"<input type=\"hidden\" id=\"" + newPrefix + sfIndex + "\" name=\"" + newPrefix + sfIndex + "\" value=\"" + (parseInt(lastIndex)+1) + "\" />\n" +
                       // "<input type='hidden' id='" + newPrefix + sfIsNew + "' name='" + newPrefix + sfIsNew + "' value='' />\n" +
                        "<script type=\"text/javascript\">var " + newPrefix + sfEntityTemp + " = '';</script>\n" +
                        "<div id='" + newPrefix + sfEntity + "' name='" + newPrefix + sfEntity + "'>\n" +
                        msg + "\n" +
                        "</div>\n" + //sfEntity
                        "</div>\n" //sfRepeaterItem                        
                        );
                   }
    });
}

function RemoveRepeaterEntity(idRepeaterElement, prefix, reloadOnChangeFunction) {
    $("#" + idRepeaterElement).remove();

    if (!empty(reloadOnChangeFunction)) {
        $('#' + prefix + sfTicks).val(new Date().getTime());
        reloadOnChangeFunction();
    }
}

function RelatedEntityCreate(urlController, prefix, onOk, onCancel, typeName) {
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfRuntimeType=" + typeName + qp(sfIdRelated, $('#' + sfId).val()) + qp(sfRuntimeTypeRelated, $('#' + sfRuntimeType).val()) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)) + qp(sfPrefix, newPrefix),
        async: false,
        dataType: "html",
        success: function(msg) {
            $('#' + prefix + "divASustituir").html(msg);
            if (msg.indexOf("<script") == 0)//A script to be run is returned instead of a Popup to open
                return;
            ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
            $('#' + newPrefix + sfBtnOk).click(onOk).after("\n" +
			 "<input type='hidden' id='" + newPrefix + sfRuntimeType + "' name='" + newPrefix + sfRuntimeType + "' value='" + typeName + "' />\n" +
			 "<input type='hidden' id='" + newPrefix + sfId + "' name='" + newPrefix + sfId + "' value='' />\n" +
			 "<input type='hidden' id='" + newPrefix + sfIsNew + "' name='" + newPrefix + sfIsNew + "' value='' />\n");
            $('#' + newPrefix + sfBtnCancel).click(onCancel);
        }
    });
}

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

function CloseChooser(urlController, onOk, onCancel, prefix) {
    var container = $('#' + prefix + "externalPopupDiv").parent();
    $('#' + prefix + sfBtnCancel).click();
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfOnOk=" + singleQuote(onOk) + qp("sfOnCancel", singleQuote(onCancel)) + qp(sfPrefix, prefix),
        async: false,
        dataType: "html",
        success: function(msg) {
            container.html(msg);
            ShowPopup(prefix, container[0].id, "modalBackground", "panelPopup");
        }
    });
}

function QuickLinkClickServerAjax(urlController, findOptionsRaw, prefix) {
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: findOptionsRaw + qp(sfPrefix, newPrefix) + qp("prefixEnd", "S"),
        async: false,
        dataType: "html",
        success: function(msg) {
            if (msg.indexOf("ModelState") > 0) {
                eval('var result=' + msg);
                var modelState = result["ModelState"];
                ShowErrorMessages(newPrefix, modelState, true, "*");
            }
            else {
                $('#' + prefix + "divASustituir").html(msg);
                ShowPopup(newPrefix, prefix + "divASustituir", "modalBackgroundS", "panelPopupS");
                $('#' + newPrefix + sfBtnCancelS).click(function() { $('#' + prefix + "divASustituir").html("") });
            }
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

            //			if (prefix != "") { //PopupWindow
            //				if (msg.indexOf("ModelState") > 0) {
            //					eval('var result=' + msg);
            //					var modelState = result["ModelState"];
            //					ShowErrorMessages(prefix, modelState, true, "*");
            //				}
            //				else {
            //					$('#' + prefix + "externalPopupDiv").html(msg);
            //				}
            //			}
            //			else { //NormalWindow
            //				if (msg.indexOf("ModelState") > 0) {
            //					eval('var result=' + msg);
            //					var modelState = result["ModelState"];
            //					ShowErrorMessages(prefix, modelState, true, "*");
            //				}
            //				else {
            //					//var newForm = new RegExp("<form[\w\W]*</form>");
            //					//$('form').html(newForm.exec(msg));
            //					$("#content").html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
            //					NotifyInfo(lang['operationExecuted'], 2000);
            //					return;
            //				}
            //			}
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

