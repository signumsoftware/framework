function ValidateAndPostServer(urlValidateController, urlPostController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText) {
	if (Validate(urlValidateController, prefixToIgnore, showInlineError, fixedInlineErrorText)) {
		document.forms[0].action = urlPostController;
		document.forms[0].submit();
	}
}

function ValidatePartialAndPostServer(urlValidateController, urlPostController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, idQueryParam, typeQueryParam) {
    var panelPopupKey = prefix + "panelPopup";
    if (TypedValidatePartial(urlValidateController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, 'panelPopup', idQueryParam, typeQueryParam)) {
        $('form').append("<input type='hidden' id='sfPrefix' name='sfPrefix' value='" + prefix + "' />")
                 .attr('action',urlPostController).submit();
    }
}

function ValidatePartialAndCallJavascript(urlValidateController, urlJavascriptController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, idQueryParam, typeQueryParam) {
    var panelPopupKey = prefix + "panelPopup";
    if (TypedValidatePartial(urlValidateController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, 'panelPopup', idQueryParam, typeQueryParam)) {
        $.ajax({
            type: "POST",
            url: urlJavascriptController,
            data: $('#' + panelPopupKey + " *").serialize() + "&sfPrefix=" + prefix + idQueryParam + typeQueryParam,
            async: false,
            dataType: "html",
            success: function(msg) {
                $('#' + panelPopupKey).append("<script type=\"text/javascript\">" + msg + "</script>");
            }
        });
	}
}

function NotifyError(s,t) {
    NotifyInfo(s,t);
}
function NotifyInfo(s,t){
    $("#loading-area-text").html(s);
    //$("#loading-area-text").css({left: parseInt(document.documentElement.clientWidth - $("#loading-area").outerWidth() / 2) + "px"});
    $("#loading-area").css({marginLeft: - parseInt($("#loading-area").outerWidth() / 2) + "px"});      
    $("#loading-area").show();
    if (t!=undefined){   
        //$("#loading-area").animate({backgroundColor: "#BFFF9F"}, 350);
        var timer = setTimeout(function(){     
            $("#loading-area").fadeOut("slow");
            clearTimeout(timer);
            timer = null;
        }, t);
    }     
}

//fixedInlineErrorText = "" for it to be populated from ModelState error messages
function TrySave(urlController, prefixToIgnore, showInlineError, fixedInlineErrorText, parentDiv) {
    NotifyInfo(lang['saving']);
    var returnValue = false;
	var formChildren = empty(parentDiv) ? $("form") : $("#" + parentDiv + " *, #" + sfTabId);
	$.ajax({
		type: "POST",
		url: urlController,
		async: false,
		data: formChildren.serialize() + qp(sfPrefixToIgnore, prefixToIgnore),
		success: function (msg) {
			if (msg.indexOf("ModelState") > 0) {
				eval('var result=' + msg);
				var modelState = result["ModelState"];
				returnValue = ShowErrorMessages("", modelState, showInlineError, fixedInlineErrorText);
			}
			else {
				$("#" + (parentDiv != undefined ? parentDiv : "content")).html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
				returnValue = true;
			}
			NotifyInfo(lang['saved'], 2000);
		}
	});
	return returnValue;
}

//fixedInlineErrorText = "" for it to be populated from ModelState error messages
function Validate(urlController, prefixToIgnore, showInlineError, fixedInlineErrorText, parentDiv) {
    var returnValue = false;
    var formChildren = empty(parentDiv) ? $("form") : $("#" + parentDiv + " *, #" + sfTabId);
	$.ajax({
		type: "POST",
		url: urlController,
		async: false,
		data: formChildren.serialize() + qp(sfPrefixToIgnore,prefixToIgnore),
		success: function (msg) {
			if (msg.indexOf("ModelState") > 0) {
				eval('var result=' + msg);
				var modelState = result["ModelState"];
				returnValue = ShowErrorMessages("", modelState, showInlineError, fixedInlineErrorText);
			}
			else {
				returnValue = true;
			}
		}
	});
	return returnValue;
}

//fixedInlineErrorText = "" for it to be populated from ModelState error messages
function TrySavePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, typeName, panelPopupKey) {
    var returnValue = false;
    if (empty(panelPopupKey)) panelPopupKey = "panelPopup";
    var formChildren = $('#' + prefix + panelPopupKey + " *, #" + sfTabId).add(SFparams(prefix));
    var typeNameParam = "";
    if ($('#' + prefix + sfRuntimeType).length == 0) //From SearchCreate I don't have sfRuntimeType of the subentity => use typeName param
        typeNameParam = qp(prefix + sfRuntimeType, typeName);
	$.ajax({
		type: "POST",
		url: urlController,
		async: false,
		data: formChildren.serialize() + qp(sfPrefix,prefix) + qp(sfPrefixToIgnore,prefixToIgnore) + typeNameParam,
		success: function (result) {
			eval('var result=' + result);
			var toStr = result[sfToStr];
			var link = $("#" + prefix + sfLink);
			if (link.length > 0) link.html(toStr); //EntityLine
			else {
				var tost = $("#" + prefix + sfToStr);
				if (tost.length > 0) tost.html(toStr); //EntityList
				else {
					var combo = $("#" + prefix + sfCombo);
					if (combo.length > 0) $('#' + prefix + sfCombo + " option:selected").html(toStr);
				}
			}
			var modelState = result["ModelState"];
			returnValue = ShowErrorMessages(prefix, modelState, showInlineError, fixedInlineErrorText);
			}
	});
	return returnValue;
}

//fixedInlineErrorText = "" for it to be populated from ModelState error messages
//function ValidatePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText) {
//	return TypedValidatePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, typeName, "");
//}

////fixedInlineErrorText = "" for it to be populated from ModelState error messages
//function ValidatePartialList(urlController, prefix, itemPrefix, prefixToIgnore, showInlineError, fixedInlineErrorText) {
//	var typeName = $('#' + prefix + sfStaticType).val();
//	var runtimeType = $('#' + itemPrefix + sfRuntimeType).val(); //typeName is an interface
//	if (runtimeType != null && runtimeType != "") {
//		typeName = runtimeType;
//	}
//	return TypedValidatePartial(urlController, itemPrefix, prefixToIgnore, showInlineError, fixedInlineErrorText, typeName, "");
//}

function GetPath(prefix) {
    var path = prefix.split("_");
    var formChildren = $("#" + sfId + ", #" + sfRuntimeType);
    for (var i = 0; i < path.length; i++) { 
        var currPrefix = concat(path, i);
        formChildren = formChildren.add("#" + currPrefix + sfId + ", #" + currPrefix + sfRuntimeType + ", #" + currPrefix + sfStaticType + ", #" + currPrefix + sfIsNew + ", #" + currPrefix + sfIndex);
    }
    return formChildren;
}

function GetPathIdsAndTypes(prefix) {
    var path = prefix.split("_");
    var formChildren = $("#" + sfId + ", #" + sfRuntimeType);
    for (var i = 0; i < path.length; i++) {
        var currPrefix = concat(path, i);
        formChildren = formChildren.add("#" + currPrefix + sfId + ", #" + currPrefix + sfRuntimeType + ", #" + currPrefix + sfStaticType);
    }
    return formChildren.serialize();
}

function concat(array, toIndex)
{
    var path = "";
    for (var i = 0; i <= toIndex; i++) {
        if (array[i] != "")
            path += "_" + array[i];
    }
    return path;
}

function SFparams(prefix) {
    return $("#" + prefix + sfId + ", #" + prefix + sfRuntimeType + ", #" + prefix + sfStaticType + ", #" + prefix + sfIsNew + ", #" + prefix + sfIndex + ", #" + prefix + sfTicks);
}

//fixedInlineErrorText = "" for it to be populated from ModelState error messages
function ValidatePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, panelPopupKey) {
    var returnValue = false;
    if (empty(panelPopupKey)) panelPopupKey = "panelPopup";
    
    var idQueryParam = ""; var typeNameParam = "";
    if ($('#' + sfReactive).length == 0) {
        formChildren = $('#' + prefix + panelPopupKey + " *, #" + sfTabId);
        if (formChildren.filter('#' + prefix + sfId).length == 0 && $('#' + prefix + sfId).length > 0)
            idQueryParam = qp(prefix + sfId, $('#' + prefix + sfId).val());
        if (formChildren.filter('#' + prefix + sfRuntimeType).length == 0 && $('#' + prefix + sfRuntimeType).length > 0)
            typeNameParam = qp(prefix + sfRuntimeType, $('#' + prefix + sfRuntimeType).val());    
    }
    return TypedValidatePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, panelPopupKey, idQueryParam, typeNameParam);
}

function TypedValidatePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, panelPopupKey, idQueryParam, typeNameParam) {
    var returnValue = false;
    var formChildren;
    if ($('#' + sfReactive).length > 0) {
        formChildren = $("form");
    }
    else {
        formChildren = $('#' + prefix + panelPopupKey + " *, #" + sfTabId);
    }
    $.ajax({
        type: "POST",
        url: urlController,
        async: false,
        data: formChildren.serialize() + qp(sfPrefix, prefix) + qp(sfPrefixToIgnore, prefixToIgnore) + idQueryParam + typeNameParam, 
        success: function(result) {
            eval('var result=' + result);
            var toStr = result[sfToStr];
            var link = $("#" + prefix + sfLink);
            if (link.length > 0) link.html(toStr); //EntityLine
            else {
                var tost = $("#" + prefix + sfToStr);
                if (tost.length > 0) tost.html(toStr); //EntityList
                else {
                    var combo = $("#" + prefix + sfCombo);
                    if (combo.length > 0) $('#' + prefix + sfCombo + " option:selected").html(toStr);
                }
            }
            var modelState = result["ModelState"];
            returnValue = ShowErrorMessages(prefix, modelState, showInlineError, fixedInlineErrorText);
        }
    });
    return returnValue;
}

function ShowErrorMessages(prefix, modelState, showInlineError, fixedInlineErrorText) {
	//Remove previous errors
	$('.' + sfFieldErrorClass).replaceWith("");
	$('.' + sfInputErrorClass).removeClass(sfInputErrorClass);
	$('.' + sfSummaryErrorClass).replaceWith("");

	var allErrors = "";
	var inlineErrorStart = '&nbsp;<span class="' + sfFieldErrorClass + '">';
	var inlineErrorEnd = "</span>";

	for (var controlID in modelState) {
		var errorsArray = modelState[controlID];
		var errorMessage = "";
		for (var j = 0; j < errorsArray.length; j++) {
			errorMessage += errorsArray[j];
			allErrors += "<li>" + errorsArray[j] + "</li>\n";
		}
		if (controlID != sfGlobalErrorsKey && controlID != "") {
			var control = $('#' + controlID);
			control.addClass(sfInputErrorClass);
			if (showInlineError && control.hasClass(sfInlineErrorVal)) {
				if (control.next().hasClass("ui-datepicker-trigger")) {
					if (fixedInlineErrorText == "") $('#' + controlID).next().after(inlineErrorStart + errorMessage + inlineErrorEnd);
					else $('#' + controlID).next().after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
				}
				else { if (fixedInlineErrorText == "") $('#' + controlID).after(inlineErrorStart + errorMessage + inlineErrorEnd);
					else $('#' + controlID).after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
				}
			}
		}
	}

	if (allErrors != "") {
		$('#' + prefix + sfToStr).addClass(sfInputErrorClass);
		$('#' + prefix + sfLink).addClass(sfInputErrorClass);
		if ($('#' + prefix + sfGlobalValidationSummary).length > 0) {
			$('#' + prefix + sfGlobalValidationSummary).html('<br /><ul class="' + sfSummaryErrorClass + '">\n' + allErrors + '</ul><br />\n');
		}
		return false;
	}
	return true;
}
