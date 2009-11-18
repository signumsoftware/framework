function Find(urlController, queryUrlName, allowMultiple, onOk, onCancel, divASustituir, prefix) {
	$.ajax({
		type: "POST",
		url: urlController,
		data: "sfQueryUrlName=" + queryUrlName + qp("sfAllowMultiple", allowMultiple) + qp(sfPrefix, prefix) + qp("prefixEnd", "S"),
		async: false,
		dataType: "html",
		success: function (msg) {
			$('#' + divASustituir).html(msg);
			ShowPopup(prefix, divASustituir, "modalBackgroundS", "panelPopupS");
			$('#' + prefix + sfBtnOkS).click(onOk);
			$('#' + prefix + sfBtnCancelS).click(onCancel);
		}
	});
}

function SearchCreate(urlController, prefix, onOk, onCancel) {
	var typeName = $('#' + prefix + sfEntityTypeName).val();
	TypedSearchCreate(urlController, prefix, onOk, onCancel, typeName);
}

function TypedSearchCreate(urlController, prefix, onOk, onCancel, typeName) {
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfRuntimeType=" + typeName + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)) + qp(sfPrefix, newPrefix),
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

function RelatedEntityCreate(urlController, prefix, onOk, onCancel, typeName) {
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfRuntimeType=" + typeName + qp("sfIdRelated", $('#'+sfId).val()) + qp("sfRuntimeTypeRelated", $('#'+sfRuntimeType).val()) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)) + qp(sfPrefix, newPrefix),
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

function OnSearchCreateOK(urlController, prefix) {
	var typeName = $('#' + prefix + sfEntityTypeName).val();
	TypedOnSearchCreateOK(urlController, prefix, typeName);
}

function TypedOnSearchCreateOK(urlController, prefix, typeName) {
    var newPrefix = prefix + "New";
    if (TrySavePartial(urlController, newPrefix, "", true, "", typeName, "panelPopup")) {
        OnSearchCreateCancel(prefix);
    }
}
function OnSearchCreateCancel(prefix) {
	$('#' + prefix + "divASustituir").html("");
	var newPrefix = prefix + "New";
	$('#' + newPrefix + sfRuntimeType).remove();
	$('#' + newPrefix + sfId).remove();
	$('#' + newPrefix + sfIsNew).remove();
}

function OnSearchOk(prefix, divASustituir, reloadOnChangeFunction) {
	var entitySelected = $("input:radio[name=" + prefix + "rowSelection]:checked").val();
	if (entitySelected == undefined) return;

	var __index = entitySelected.indexOf("__");
	var __index2 = entitySelected.indexOf("__", __index + 2);

	$('#' + prefix + sfId).val(entitySelected.substring(0, __index));
	$('#' + prefix + sfRuntimeType).val(entitySelected.substring(__index + 2, __index2));
	$('#' + prefix + sfToStr).val(entitySelected.substring(__index2 + 2, entitySelected.length));
	$('#' + prefix + sfLink).html(entitySelected.substring(__index2 + 2, entitySelected.length));
	toggleButtonsDisplay(prefix, true);
	$('#' + divASustituir).hide().html("");

	if (!empty(reloadOnChangeFunction)) {
	    $('#' + prefix + sfTicks).val(new Date().getTime());
	    reloadOnChangeFunction();
	}
}

function OnDetailSearchOk(urlController, prefix, divASustituir, reloadOnChangeFunction, detailDiv, partialView) {
	var entitySelected = $("input:radio[name=" + prefix + "rowSelection]:checked").val();
	if (entitySelected == undefined) return;

	var __index = entitySelected.indexOf("__");
	var __index2 = entitySelected.indexOf("__", __index + 2);

	$('#' + prefix + sfId).val(entitySelected.substring(0, __index));
	$('#' + prefix + sfRuntimeType).val(entitySelected.substring(__index + 2, __index2));
	OpenPopup(urlController, divASustituir, prefix, "", "", detailDiv, partialView)

	toggleButtonsDisplay(prefix, true);
	$('#' + divASustituir).hide().html("");

	if (!empty(reloadOnChangeFunction)) {
	    $('#' + prefix + sfTicks).val(new Date().getTime());
	    reloadOnChangeFunction();
	}
}

function OnListSearchOk(prefix, divASustituir) {
	$("#" + prefix + "tdRowSelection input:checked").each(
	function () {
		var entitySelected = this.value;
		var __index = entitySelected.indexOf("__");
		var __index2 = entitySelected.indexOf("__", __index + 2);
		var id = entitySelected.substring(0, __index);
		var runtimeType = entitySelected.substring(__index + 2, __index2);
		var toStr = entitySelected.substring(__index2 + 2, entitySelected.length);

		NewListOption(prefix, runtimeType, "False");

		var selected = $('#' + prefix + " > option:selected");
		var nameSelected = selected[0].id;
		var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
		$('#' + prefixSelected + sfId).val(id);
		$('#' + prefixSelected + sfToStr).html(toStr);
	});
	$('#' + divASustituir).hide().html("");
}

function GetSelectedElements(prefix) {
	var ids = "";
	var selected = $("input:radio[name=" + prefix + "rowSelection]:checked, #" + prefix + "tdRowSelection input:checked");
	if (selected.length == 0) return ids;

	selected.each(function () {
		var entitySelected = this.value;
		ids += entitySelected.substring(0, entitySelected.indexOf("__")) + ",";
	});
	if (ids.substr(-1) == ",") ids = ids.substring(0, ids.length - 1);
	return ids;
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
		success: function (msg) {
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
		data: "sfOnOk=" + singleQuote(onOk) + qp("sfOnCancel",singleQuote(onCancel)) + qp(sfPrefix, prefix),
		async: false,
		dataType: "html",
		success: function (msg) {
			container.html(msg);
			ShowPopup(prefix, container[0].id, "modalBackground", "panelPopup");
		}
	});
}

function QuickLinkClickServerAjax(urlController,findOptionsRaw,prefix) {
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
	if (isLite == false || isLite == "false" || isLite == "False") {
		if (prefix != "") //PopupWindow
		    formChildren = $('#' + prefix + "panelPopup *, #" + sfReactive + ", #" + sfTabId).serialize();
		else //NormalWindow
		    formChildren = $("form").serialize();
	}
	if (formChildren.length > 0) formChildren = "&" + formChildren;
	var newPrefix = multiStep ? prefix + "New" : prefix;
	$.ajax({
		type: "POST",
		url: urlController,
		data: "isLite=" + isLite + qp("sfRuntimeType", typeName) + qp("sfId", id) + qp("sfOperationFullKey", operationKey) + qp(sfPrefix, newPrefix) + qp("sfOnOk", singleQuote(onOk)) + qp("sfOnCancel", singleQuote(onCancel)) + formChildren,
		async: false,
		dataType: "html",
		success: function (msg) {
		    if (msg.indexOf("ModelState") > 0) {
		        eval('var result=' + msg);
				var modelState = result["ModelState"];
				ShowErrorMessages(prefix, modelState, true, "*");
		    }
		    else{
		        if (multiStep){
		            $('#' + prefix + "divASustituir").html(msg);
                    if (msg.indexOf("<script") == 0)//A script to be run is returned instead of a Popup to open
                        return;
                    ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
                    //$('#' + newPrefix + sfBtnOk).click(onOk);
                    $('#' + newPrefix + sfBtnCancel).click(empty(onCancel) ? (function() { $('#' + prefix + "divASustituir").html(""); }) : onCancel);
		        }
		        else{
		            if (prefix != "") { //PopupWindow
		                $('#' + prefix + "externalPopupDiv").html(msg);
		            }
		            else{
		                $("#content").html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
				    }
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
    if (isLite == false || isLite == "false" || isLite == "False") {
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
	if (isLite == false || isLite == "false" || isLite == "False") {
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
        else
        {
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

//function PostServer(urlController, prefix) {
//	var ids = GetSelectedElements(prefix);
//	if (ids == "") return;

//	document.forms[0].innerHTML = "<input type='hidden' id='sfIds' name='sfIds' value='" + ids + "' />";
//	document.forms[0].action = urlController;
//	document.forms[0].submit();
//}

function PostServer(urlController) {
	document.forms[0].action = urlController;
	document.forms[0].submit();
}

function OnSearchCancel(prefix, divASustituir) {
	$('#' + prefix + sfRuntimeType).val("");
	toggleButtonsDisplay(prefix, false);
	$('#' + divASustituir).hide().html("");
}

function OnListSearchCancel(prefix, divASustituir) {
	$('#' + divASustituir).hide().html("");
}

function QuickFilter(idTD, prefix) {
    var params = "";
    var ahref = $('#' + idTD + ' a');
    if (ahref.length == 0)
        params = qp("isLite", "false") + qp("sfValue", $('#' + idTD).html());
    else
    {
        var route = ahref.attr("href");
        var separator = route.indexOf("/");
        var lastSeparator = route.lastIndexOf("/");
        params = qp("isLite", "true") + qp("typeUrlName", route.substring(separator + 1, lastSeparator)) + qp("sfId", route.substring(lastSeparator + 1, route.length));
    }
    $.ajax({
        type: "POST",
        url: "Signum.aspx/QuickFilter",
        data: "sfQueryUrlName=" + $("#" + prefix + "sfQueryUrlName").val() + qp("sfColIndex", parseInt(idTD.substring(idTD.indexOf("_")+1, idTD.length))-1) + params + qp(sfPrefix, prefix) + qp("index", GetNewFilterRowIndex(prefix)),
        async: false,
        dataType: "html",
        success: function(msg) {
            $("#filters-list .explanation").hide();
            $("#filters-list table").show('fast');
            $("#" + prefix + "tblFilters tbody").append(msg);
        }
    });
}

function GetNewFilterRowIndex(prefix) {
    var lastRow = $("#" + prefix + "tblFilters tbody tr:last");
    var lastRowIndex = -1;
    if (lastRow.length != 0) 
        lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length);
     return parseInt(lastRowIndex) + 1;
}

function AddFilter(urlController, prefix) {

	var selectedColumn = $("#" + prefix + "ddlNewFilters option:selected");
	if (selectedColumn.length == 0) return;

	var tableFilters = $("#" + prefix + "tblFilters");
	if (tableFilters.length == 0) return;

	$("#" + prefix + "filters-list .explanation").hide();
	$("#" + prefix + "filters-list table").show('fast');

	var filterType = selectedColumn.val();
	var optionId = selectedColumn[0].id;
	var filterName = optionId.substring(optionId.indexOf("__") + 2, optionId.length);

	var newRowIndex = GetNewFilterRowIndex(prefix);

	$.ajax({
		type: "POST",
		url: urlController,
		data: "filterType=" + filterType + qp("columnName",filterName) + qp("displayName",selectedColumn.html()) + qp("index",newRowIndex) + qp(sfPrefix,prefix),
		async: false,
		dataType: "html",
		success: function (msg) {
			$("#" + prefix + "tblFilters tbody").append(msg);
		}
	});
}

function DeleteFilter(index, prefix) {
	var tr = $("#" + prefix + "trFilter_" + index)
	if (tr.length == 0) return;

	if ($("#" + prefix + "trFilter_" + index + " select[disabled]").length == 0) tr.remove();
	if ($("#" + prefix + "tblFilters tbody tr").length == 0) {
		$("#" + prefix + "filters-list .explanation").show();
		$("#" + prefix + "filters-list table").hide('fast');
	}
}

function ClearAllFilters(prefix) {
	$("#" + prefix + "tblFilters > tbody > tr").each(function (index) {
		DeleteFilter(this.id.substr(this.id.lastIndexOf("_") + 1, this.id.length), prefix);
	});
}

function Search(urlController, prefix, callBack) {
	var top = $("#" + prefix + sfTop).val();
	var allowMultiple = $("#" + prefix + sfAllowMultiple).val();
	var serializedFilters = SerializeFilters(prefix);
	var async = concurrentSearch[prefix + "btnSearch"];
	if (async) concurrentSearch[prefix + "btnSearch"]=false;
	$.ajax({
	    type: "POST",
	    url: urlController,
	    data: "sfQueryUrlName=" + $("#" + prefix + "sfQueryUrlName").val() + qp("sfTop", top) + qp("sfAllowMultiple", allowMultiple) + qp(sfPrefix, prefix) + serializedFilters,
	    async: async,
	    dataType: "html",
	    success: function(msg) {
	        $("#" + prefix + "divResults").html(msg);
	        if (callBack != undefined) callBack();
	    },
	    error: function(XMLHttpRequest, textStatus, errorThrown) {
	        if (callBack != undefined) callBack();
	    }
	});
}

function SerializeFilters(prefix) {
	var result = "";
	$("#" + prefix + "tblFilters > tbody > tr").each(function (index) {
		result += SerializeFilter(this.id.substr(this.id.lastIndexOf("_") + 1, this.id.length), prefix);
	});
	return result;
}

function SerializeFilter(index, prefix) {
	var tds = $("#" + prefix + "trFilter_" + index + " td");
	var columnName = tds[0].id.substr(tds[0].id.indexOf("__") + 2, tds[0].id.length);
	var selector = $("#" + prefix + "ddlSelector_" + index + " option:selected");
	var value = $("#" + prefix + "value_" + index).val();

	var valBool = $("input:checkbox[id=" + prefix + "value_" + index + "]"); //it's a checkbox
	if (valBool.length > 0) value = valBool[0].checked;

	var id = $("#" + prefix + "value_" + index + sfId); //If it's a Lite, the value is the Id
	if (id.length > 0)
	    value = id.val() + ";" + $("#" + prefix + "value_" + index + sfRuntimeType).val();

	return qp("cn" + index, columnName) + qp("sel" + index, selector.val()) + qp("val" + index, value);
}

function OnSearchImplementationsOk(urlController, queryUrlNameToIgnore, allowMultiple, onOk, onCancel, divASustituir, prefix, selectedType) {
	if (empty(selectedType)) return;
	$('#' + prefix + sfImplementations).hide();
	Find(urlController, selectedType, allowMultiple, onOk, onCancel, divASustituir, prefix);
}

function toggleVisibility(elementId) {
	$('#' + elementId).toggle();
}

var concurrentSearch = new Array();
function SearchOnLoad(btnSearchId) {
    concurrentSearch[btnSearchId] = true;
	$("#" + btnSearchId).click();
}
