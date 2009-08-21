function Find(urlController, queryUrlName, allowMultiple, onOk, onCancel, divASustituir, prefix) {
    var a = 0;
    $.ajax({
        type: "POST",
        url: urlController,
        data: "queryFriendlyName=" + queryUrlName + "&allowMultiple=" + allowMultiple + "&prefix=" + prefix + "&prefixEnd=S",
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       $('#' + divASustituir).html(msg);
                       ShowPopup(prefix, divASustituir, "modalBackgroundS", "panelPopupS");
                       $('#' + prefix + sfBtnOkS).click(onOk);
                       $('#' + prefix + sfBtnCancelS).click(onCancel);
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                        ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function SearchCreate(urlController, prefix, onOk, onCancel) {
    var typeName = $('#' + prefix + sfEntityTypeName).val();
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfStaticType=" + typeName + "&sfOnOk=" + singleQuote(onOk) + "&sfOnCancel=" + singleQuote(onCancel) + "&prefix=" + newPrefix,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
        $('#' + prefix + "divASustituir").html(msg);
        ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
        $('#' + newPrefix + sfBtnOk).click(onOk);
        $('#' + newPrefix + sfBtnCancel).click(onCancel);
        $('#' + newPrefix + sfBtnOk).after(
                            "<input type=\"hidden\" id=\"" + newPrefix + sfRuntimeType + "\" name=\"" + newPrefix + sfRuntimeType + "\" value=\"" + typeName + "\" />\n" +
                            "<input type=\"hidden\" id=\"" + newPrefix + sfId + "\" name=\"" + newPrefix + sfId + "\" value=\"\" />\n" +
                            "<input type=\"hidden\" id=\"" + newPrefix + sfIsNew + "\" name=\"" + newPrefix + sfIsNew + "\" value=\"\" />\n");
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function OnSearchCreateOK(urlController, prefix) {
    var typeName = $('#' + prefix + sfEntityTypeName).val();
    var newPrefix = prefix + "New";
    if (TypedTrySavePartial(urlController, newPrefix, "", true, "", typeName, "panelPopup", true)) {
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
    var entitySelected = $("input:radio[name="+prefix+"rowSelection]:checked").val();
    if (entitySelected == undefined)
        return;

    var __index = entitySelected.indexOf("__");
    var __index2 = entitySelected.indexOf("__", __index+2);

    $('#' + prefix + sfId).val(entitySelected.substring(0, __index));
    $('#' + prefix + sfRuntimeType).val(entitySelected.substring(__index + 2, __index2));
    $('#' + prefix + sfToStr).val(entitySelected.substring(__index2+2, entitySelected.length));
    $('#' + prefix + sfLink).html(entitySelected.substring(__index2+2, entitySelected.length));
    toggleButtonsDisplay(prefix, true);
    //$('#' + prefix + sfEntity).hide().html("");
    $('#' + divASustituir).hide().html("");

    if (reloadOnChangeFunction != null && reloadOnChangeFunction != undefined && reloadOnChangeFunction != "")
        reloadOnChangeFunction();
}

function OnListSearchOk(prefix, divASustituir) {
    $("#"+prefix+"tdRowSelection input:checked").each(
        function() {
            var entitySelected = this.value;
            var __index = entitySelected.indexOf("__");
            var __index2 = entitySelected.indexOf("__", __index+2);
            var id = entitySelected.substring(0, __index);
            var runtimeType = entitySelected.substring(__index + 2, __index2);
            var toStr = entitySelected.substring(__index2 + 2, entitySelected.length);

            NewListOption(prefix, runtimeType, "False");

            var selected = $('#' + prefix + " > option:selected");
            var nameSelected = selected[0].id;
            var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
            $('#' + prefixSelected + sfId).val(id);
            $('#' + prefixSelected + sfToStr).html(toStr);
        }
    );
        $('#' + divASustituir).hide().html("");
}

function GetSelectedElements(prefix) {
    var ids = "";
    var selected = $("input:radio[name=" + prefix + "rowSelection]:checked, #" + prefix + "tdRowSelection input:checked");
    if (selected.length == 0)
        return ids;

    selected.each(
        function() {
            var entitySelected = this.value;
            ids += entitySelected.substring(0, entitySelected.indexOf("__")) + ",";
        });
    if (ids.substr(-1) == ",")
        ids = ids.substring(0, ids.length - 1);
    return ids;
}

function CallServer(urlController, prefix) {
    var ids = GetSelectedElements(prefix);
    if (ids == "")
        return;
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfIds=" + ids,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       window.alert(msg);
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}



function CloseChooser(urlController, onOk, onCancel, prefix) {
    var container = $('#' + prefix + "externalPopupDiv").parent();
    $('#' + prefix + sfBtnCancel).click();
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfOnOk=" + singleQuote(onOk) + "&sfOnCancel=" + singleQuote(onCancel) + "&prefix=" + prefix,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                        container.html(msg);
                        ShowPopup(prefix, container[0].id, "modalBackground", "panelPopup");
                       //$('#' + prefix + "divASustituir").html(msg);
                       //ShowPopup(prefix, prefix + "divASustituir", "modalBackground", "panelPopup");
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function OperationExecute(urlController, typeName, id, operationKey, isLazy, prefix, onOk, onCancel) {
        var formChildren = "";
        if (isLazy == false || isLazy == "false" || isLazy == "False")
            formChildren = $('#' + prefix + "panelPopup *").serialize();
        $.ajax({
            type: "POST",
            url: urlController,
            data: "isLazy=" + isLazy + "&sfTypeName=" + typeName + "&sfId=" + id + "&sfOperationFullKey=" + operationKey + "&prefix=" + prefix + "&sfOnOk=" + singleQuote(onOk) + "&sfOnCancel=" + singleQuote(onCancel) + formChildren,
            async: false,
            dataType: "html",
            success:
                       function(msg) {
                           if (prefix != "") { //PopupWindow
                               if (msg.indexOf("ModelState") > 0) {
                                   eval('var result=' + msg);
                                   var modelState = result["ModelState"];
                                   ShowErrorMessages(prefix, modelState, true, "*");
                               }
                               else {
                                   $('#' + prefix + "externalPopupDiv").html(msg);
                               }
                           }
                           else { //NormalWindow
                               if (msg.indexOf("ModelState") > 0) {
                                   eval('var result=' + msg);
                                   var modelState = result["ModelState"];
                                   ShowErrorMessages(prefix, modelState, true, "*");
                               }
                               else {
                                   //var newForm = new RegExp("<form[\w\W]*</form>");
                                   //$('form').html(newForm.exec(msg));
                                   $('form').html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                               }
                           }
                       },
            error:
                       function(XMLHttpRequest, textStatus, errorThrown) {
                           ShowError(XMLHttpRequest, textStatus, errorThrown);
                       }
        });
}

function ConstructFromManyExecute(urlController, typeName, queryName, operationKey, prefix, onOk, onCancel) {
    var ids = GetSelectedElements(prefix);
    if (ids == "")
        return;
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfIds=" + ids + "&sfTypeName=" + typeName + "&sfQueryName=" + queryName + "&sfOperationFullKey=" + operationKey + "&prefix=" + prefix + "&sfOnOk=" + singleQuote(onOk) + "&sfOnCancel=" + singleQuote(onCancel),
        async: false,
        dataType: "html",
        success:
                       function(msg) {
                            $('#' + prefix + "divASustituir").html(msg);
                            ShowPopup(newPrefix, prefix + "divASustituir", "modalBackground", "panelPopup");
                            $('#' + newPrefix + sfBtnOk).click(onOk);
                            $('#' + newPrefix + sfBtnCancel).click(onCancel);
                       },
        error:
                       function(XMLHttpRequest, textStatus, errorThrown) {
                           ShowError(XMLHttpRequest, textStatus, errorThrown);
                       }
    });

}

function PostServer(urlController, prefix) {
    var ids = GetSelectedElements(prefix);
    if (ids == "")
        return;

    document.forms[0].innerHTML = "<input type='hidden' id='sfIds' name='sfIds' value='" + ids + "' />";
    document.forms[0].action = urlController;
    document.forms[0].submit();
}

function PostServer(urlController) {
    document.forms[0].action = urlController;
    document.forms[0].submit();
}

function OnSearchCancel(prefix, divASustituir) {
    $('#' + prefix + sfRuntimeType).val("");
    toggleButtonsDisplay(prefix, false);
    //$('#' + prefix + sfEntity).hide().html("");
    $('#' + divASustituir).hide().html("");
}

function OnListSearchCancel(prefix, divASustituir) {
    $('#' + divASustituir).hide().html("");
}

function AddFilter(urlController, prefix) {

    var selectedColumn = $("#"+prefix+"ddlNewFilters option:selected");
    if (selectedColumn.length == 0) return;
    
    var tableFilters = $("#"+prefix+"tblFilters");
    if (tableFilters.length == 0) return;

    $("#filters-list .explanation").hide();
    $("#filters-list table").show('fast');

    var filterType = selectedColumn.val();
    var optionId = selectedColumn[0].id;
    var filterName = optionId.substring(optionId.indexOf("__") + 2, optionId.length);

    var lastRow = $("#"+prefix+"tblFilters tbody tr:last");
    var lastRowIndex = -1;
    if (lastRow.length != 0)
        lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_")+1, lastRow[0].id.length);
    var newRowIndex = parseInt(lastRowIndex) + 1;
    
    $.ajax({
        type: "POST",
        url: urlController,
        data: "filterType=" + filterType + "&columnName=" + filterName + "&displayName=" + selectedColumn.html() + "&index=" + newRowIndex + "&entityTypeName=" + $("#" + prefix + sfEntityTypeName).val() + "&prefix=" + prefix,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       var filterRows = $("#"+prefix+"tblFilters tbody");
                       filterRows.append(msg);
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                        ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function DeleteFilter(index, prefix) { 
    var tr = $("#"+prefix+"trFilter_" + index)
    if (tr.length == 0) return;
    
    if ($("#"+prefix+"trFilter_" + index + " select[disabled]").length == 0)
        tr.remove();
    if  ($("#"+prefix+"tblFilters tbody tr").length == 0){
        $("#filters-list .explanation").show();
        $("#filters-list table").hide('fast');
    }
}

function ClearAllFilters(prefix) {
    $("#"+prefix+"tblFilters > tbody > tr").each(function(index) {
        DeleteFilter(this.id.substr(this.id.lastIndexOf("_")+1,this.id.length), prefix); 
        });
    }

    function Search(urlController, prefix, callBack) {
        var top = $("#" + prefix + sfTop).val();
        var allowMultiple = $("#" + prefix + sfAllowMultiple).val();
        var serializedFilters = SerializeFilters(prefix);
        $.ajax({
            type: "POST",
            url: urlController,
            data: "sfQueryNameToStr=" + $("#" + prefix + "sfQueryName").val() + "&sfTop=" + top + "&sfAllowMultiple=" + allowMultiple + "&sfPrefix=" + prefix + serializedFilters,
            async: false,
            dataType: "html",
            success:
                   function(msg) {
                       $("#" + prefix + "divResults").html(msg);
                       if (callBack != undefined) callBack();
                   },
            error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       ShowError(XMLHttpRequest, textStatus, errorThrown);
                       if (callBack != undefined) callBack();
                   }
        });
}

function SerializeFilters(prefix){    
    var result = "";
    $("#"+prefix+"tblFilters > tbody > tr").each(function(index) {
        result = result + SerializeFilter(this.id.substr(this.id.lastIndexOf("_")+1,this.id.length),prefix);
    });
    return result;
}

function SerializeFilter(index, prefix){
    var tds = $("#"+prefix+"trFilter_" + index + " td");
    var columnName = tds[0].id.substr(tds[0].id.indexOf("__") + 2, tds[0].id.length);
    var selector = $("#"+prefix+"ddlSelector_" + index + " option:selected");
    var value = $("#" + prefix + "value_" + index).val();

    var valBool = $("input:checkbox[id=" + prefix + "value_" + index + "]"); //it's a checkbox
    if (valBool.length > 0)
        value = valBool[0].checked;
        
    var id = $("#" + prefix + "value_" + index + sfId); //If it's a Lazy, the value is the Id
    if (id.length > 0)
        value = id.val();
        
    var typeName = $("#" + prefix + "type_" + index);
    return "&name" + index + "=" + columnName + "&sel" + index + "=" + selector.val() + "&val" + index + "=" + value; 
}

function OnSearchImplementationsOk(urlController, queryUrlNameToIgnore, allowMultiple, onOk, onCancel, divASustituir, prefix, selectedType) { 
    //var selectedType = $('#' + prefix + sfImplementationsDDL + " > option:selected");
    //if (selectedType.length == 0 || selectedType.val() == "")
    if (selectedType==null || selectedType==undefined || selectedType=="")
        return;

    $('#' + prefix + sfImplementations).hide();

    Find(urlController, selectedType, allowMultiple, onOk, onCancel, divASustituir, prefix);
}

function toggleVisibility(elementId) {
    $('#' + elementId).toggle();
}

function SearchOnLoad(btnSearchId) {
    $("#" + btnSearchId).click();
}
