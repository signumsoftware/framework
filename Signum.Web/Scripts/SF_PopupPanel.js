function OpenPopup(urlController, divASustituir, prefix, onOk, onCancel) {
    var typeName = $('#' + prefix + sfStaticType).val();
    var hasImplementations = $('#' + prefix + sfImplementations).length > 0; 
    TypedOpenPopup(urlController, onOk, onCancel, divASustituir, prefix, typeName, hasImplementations, "");
}

function NewPopup(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded) {
    $('#' + prefix + sfEntity).after("<input type=\"hidden\" id=\"" + prefix + sfIsNew + "\" name=\"" + prefix + sfIsNew + "\" value=\"\" />\n");
    if ($('#' + prefix + sfImplementations).length == 0)
        $('#' + prefix + sfRuntimeType).val($('#' + prefix + sfStaticType).val());
    OpenPopup(urlController, divASustituir, prefix, onOk, onCancel);
}

function OpenPopupList(urlController, divASustituir, select, onOk, onCancel, detailDiv) {
    if (detailDiv != null && detailDiv != "") {
        $("#" + detailDiv + " > div").hide();
    }
    var selected = $('#' + select + " > option:selected");
    
    var typeName = $('#' + select + sfStaticType).val();
    if (selected.length == 0) { //Create New
        return;
//        NewListOption(select, typeName);
//        selected = $('#' + select + " > option:selected"); //Needs refresh
    }
        
    var nameSelected = selected[0].id;
    var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
    var hasImplementations = $('#' + select + sfImplementations).length > 0;
    TypedOpenPopup(urlController, onOk, onCancel, divASustituir, prefixSelected, typeName, hasImplementations, detailDiv);
}

function NewPopupList(urlController, divASustituir, select, onOk, onCancel, runtimeType, isEmbeded, detailDiv) {
    if (detailDiv != null && detailDiv != "") {
        $("#" + detailDiv + " > div").hide();
    }
    NewListOption(select, runtimeType, isEmbeded, detailDiv);
    
    var selected = $('#' + select + " > option:selected");
    var nameSelected = selected[0].id;
    var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
    var hasImplementations = $('#' + select + sfImplementations).length > 0;
    $('#' + prefixSelected + sfEntity).after("<input type=\"hidden\" id=\"" + prefixSelected + sfIsNew + "\" name=\"" + prefixSelected + sfIsNew + "\" value=\"\" />\n");
    TypedOpenPopup(urlController, onOk, onCancel, divASustituir, prefixSelected, runtimeType, hasImplementations, detailDiv);
}

function TypedOpenPopup(urlController, onOk, onCancel, divASustituir, prefix, typeName, hasImplementations, detailDiv) {
    var containedEntity = $('#' + prefix + sfEntity).html();
    if (containedEntity != null && containedEntity != "") { //Already have the containedEntity loaded => show it
        window[prefix + sfEntityTemp] = containedEntity;
        ShowPopup(prefix, prefix + sfEntity, "modalBackground", "panelPopup", detailDiv);
        $('#' + prefix + sfBtnOk).unbind('click').click(onOk);
        $('#' + prefix + sfBtnCancel).unbind('click').click(onCancel);
        return;
    }

    //Don't have the containedEntity loaded => ask the server for it
    if (hasImplementations) {//It's an interface => The runtime type is the one to open
        var runtimeType = $('#' + prefix + sfRuntimeType).val();
        if (runtimeType != null && runtimeType != "") {
            typeName = runtimeType;
        }
        else {
            window.alert("Error: The different possible implementations of the interface are not loaded");
            return;
        }
    }
    var idQueryParam = "";
    var idField = $('#' + prefix + sfId);
    if (idField.length != 0) {
        idQueryParam = "&sfId=" + idField.val();
    }
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfStaticType=" + typeName + "&sfOnOk=" + onOk + "&sfOnCancel=" + onCancel + "&prefix=" + prefix + idQueryParam,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       window[prefix + sfEntityTemp] = containedEntity;
                       $('#' + prefix + sfEntity).html(msg);
                       ShowPopup(prefix, prefix + sfEntity, "modalBackground", "panelPopup", detailDiv);
                       $('#' + prefix + sfBtnOk).click(onOk);
                       $('#' + prefix + sfBtnCancel).click(onCancel);
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                        ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function ChooseImplementation(divASustituir, prefix, onOk, onCancel) {
    ShowPopup(prefix, prefix + sfImplementations, "modalBackground", "panelPopup", null);
    $('#' + prefix + sfBtnOk).unbind('click').click(onOk);
    $('#' + prefix + sfBtnCancel).unbind('click').click(onCancel);
}

function ShowPopup(prefix, globalKey, modalBackgroundKey, panelPopupKey, detailDiv) {
    if (detailDiv != null && detailDiv != "") {
        $("#" + prefix + "_sfEntity").show();
    }
    else {
        //$("#" + prefix + "_sfEntity").show();
        $("#" + globalKey).show();
        
        $('#' + prefix + modalBackgroundKey).width(document.documentElement.clientWidth);
        $('#' + prefix + modalBackgroundKey).height(document.documentElement.clientHeight);
        $('#' + prefix + modalBackgroundKey).hide();
        
        //Read offsetWidth and offsetHeight after display=block or otherwise it's 0
        var popup = $('#' + prefix + panelPopupKey)[0];
        var parentDiv = $("#" + prefix + "_sfEntity").parent();
        var left;
        var top;
        var popupWidth = 500;
        /*if (parentDiv.length > 0 && parentDiv[0].id.indexOf(panelPopupKey) > -1) {
            left = "25px";
            top = "25px";
            
        }
        else {*/
            popupWidth = popup.offsetWidth;
            var bodyWidth = document.body.clientWidth;
            left = ((bodyWidth - popupWidth) / 2) + "px";
            var popupHeight = popup.offsetHeight;
            var bodyHeight = document.documentElement.clientHeight;
            top = ((bodyHeight - popupHeight) / 2) + "px";
       // }
        //$('#' + prefix + globalKey).hide();
        $('#' + globalKey).hide();
        popup.style.left = left;
        popup.style.top = top;
        popup.style.width = popupWidth + "px";
        //$('#' + prefix + globalKey).show('fast');
        $('#' + globalKey).show('fast');
        $('#' + prefix + modalBackgroundKey)[0].style.left=0;
        $('#' + prefix + modalBackgroundKey).css('filter','alpha(opacity=40)').fadeIn('slow');
    }
}

function OnPopupOK(urlController, prefix, urlReloadController, parentPrefix) {
    var correcto = TrySavePartial(urlController, prefix, "", true, "*");

    //Clean panelPopup
    window[prefix + sfEntityTemp] = "";
    $('#' + prefix + sfEntity).hide();

    var runtimeType = $('#' + prefix + sfRuntimeType);
    if (runtimeType.val() == "")
        runtimeType.val($('#' + prefix + sfStaticType).val());

    toggleButtonsDisplay(prefix, true);

    if (correcto) {
        if (urlReloadController != null && urlReloadController != undefined && urlReloadController != "")
            ReloadEntity(urlReloadController, parentPrefix);
    }
}

function ReloadEntity(urlController, prefix) {
    var formChildren;
    if ($('#' + prefix + "panelPopup").length != 0)
        formChildren = $('#' + prefix + "panelPopup *, #" + prefix + sfId + ", #" + prefix + sfRuntimeType + ", #" + prefix + sfStaticType + ", #" + prefix + sfIsNew);
    else
        formChildren = $("form");
    $.ajax({
        type: "POST",
        url: urlController,
        data: formChildren.serialize() + "&prefix=" + prefix,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       $('#' + prefix + "divMainControl").html(msg);
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function OnListPopupOK(urlController, prefix, btnOkId) {
    var itemPrefix = btnOkId.substr(0, btnOkId.indexOf(sfBtnOk));
    TrySavePartialList(urlController, prefix, itemPrefix, "", true, "*");

    //Clean panelPopup
    window[itemPrefix + sfEntityTemp] = "";
    $('#' + itemPrefix + sfEntity).hide();

    var runtimeType = $('#' + itemPrefix + sfRuntimeType);
    if (runtimeType.val() == "")
        runtimeType.val($('#' + itemPrefix + sfStaticType).val());
}

function OnImplementationsOk(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded) {
    var selectedType = $('#' + prefix + sfImplementationsDDL + " > option:selected");
    if (selectedType.length == 0 || selectedType.val() == "")
        return;
    $('#' + prefix + sfRuntimeType).val(selectedType.val());
    $('#' + prefix + sfImplementations).hide();
    NewPopup(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded);
}

function NewListOption(prefix, selectedType, isEmbeded, detailDiv) {
    var lastElement = $('#' + prefix + " > option:last");
    var lastIndex = -1;
    if (lastElement.length > 0) {
        var nameSelected = lastElement[0].id;
        lastIndex = nameSelected.substring(prefix.length + 1, nameSelected.indexOf(sfToStr));
    }
    var newIndex = "_" + (parseInt(lastIndex) + 1);
    var staticType = $("#" + prefix + sfStaticType);
    staticType.after(
        "<input type=\"hidden\" id=\"" + prefix + newIndex + sfRuntimeType + "\" name=\"" + prefix + newIndex + sfRuntimeType + "\" value=\"" + selectedType + "\" />\n" +
        "<script type=\"text/javascript\">var " + prefix + newIndex + sfEntityTemp + " = \"\";</script>\n");
    var sfEntityDiv = "<div id=\"" + prefix + newIndex + sfEntity + "\" name=\"" + prefix + newIndex + sfEntity + "\" style=\"display:none\"></div>\n";
    if (detailDiv == null || detailDiv == "")
        staticType.after(sfEntityDiv);
    else
        $("#" + detailDiv).html($("#" + detailDiv).html() + sfEntityDiv);
    if (isEmbeded == "False")
        staticType.after("<input type=\"hidden\" id=\"" + prefix + newIndex + sfId + "\" name=\"" + prefix + newIndex + sfId + "\" value=\"\" />\n");

    var select = $('#' + prefix);
    select.html(select.html() + "\n<option id='" + prefix + newIndex + sfToStr + "' name='" + prefix + newIndex + sfToStr + "' value='' class='valueLine'>&nbsp;</option>");
    $('#' + prefix + " > option").attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
    $('#' + prefix + " > option:last").attr('selected', true);
}

function OnListImplementationsOk(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded, detailDiv) {
    var selectedType = $('#' + prefix + sfImplementationsDDL + " > option:selected");
    if (selectedType.length == 0 || selectedType.val() == "")
        return;

    $('#' + prefix + sfImplementations).hide();

    NewPopupList(urlController, divASustituir, prefix, onOk, onCancel, selectedType.val(), isEmbeded, detailDiv);
}

function OnImplementationsCancel(prefix) {
    $('#' + prefix + sfImplementations).hide();
}

function OnPopupCancel(prefix) {
    var oldValue = window[prefix + sfEntityTemp];
    $('#' + prefix + sfEntity).html(oldValue);

    var id = $('#' + prefix + sfId);
    if (id.length > 0 && id.val() != null && id.val() > 0)
        toggleButtonsDisplay(prefix, true);
    else {
        if (oldValue != undefined && oldValue != null && oldValue != "") {
            toggleButtonsDisplay(prefix, true);
        }
        else {
            toggleButtonsDisplay(prefix, false);
            $('#' + prefix + sfRuntimeType).val("");
            $('#' + prefix + sfIsNew).remove();
        }
    }

    window[prefix + sfEntityTemp] = "";
    $('#' + prefix + sfEntity).hide();
}

function OnListPopupCancel(btnCancelId) {
    var prefix = btnCancelId.substr(0, btnCancelId.indexOf(sfBtnCancel));
    var oldValue = window[prefix + sfEntityTemp];
    $('#' + prefix + sfEntity).html(oldValue);

    var id = $('#' + prefix + sfId);
    if (id.length > 0 && id.val() != null && id.val() > 0)
    { }
    else {
        if (oldValue != undefined && oldValue != null && oldValue != "") {
            { }
        }
        else {
            $('#' + prefix + sfId).remove();
            $('#' + prefix + sfRuntimeType).remove();
            $('#' + prefix + sfToStr).remove();
            $('#' + prefix + sfEntity).remove();
            $('#' + prefix + sfIsNew).remove();
        }
    }

    window[prefix + sfEntityTemp] = "";
    $('#' + prefix + sfEntity).hide();
}

function RemoveListContainedEntity(select) {
    var selected = $('#' + select + " > option:selected");
    if (selected.length == 0)
        return;
    var nameSelected = selected[0].id;
    var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
    var idField = $('#' + prefixSelected + sfId);
    var typeName = $('#' + select + sfStaticType).val();
    
    $('#' + prefixSelected + sfId).remove();
    $('#' + prefixSelected + sfRuntimeType).remove();
    $('#' + prefixSelected + sfToStr).remove();
    $('#' + prefixSelected + sfEntity).remove();
    $('#' + prefixSelected + sfIndex).remove();
    $('#' + prefixSelected + sfIsNew).remove();
    window[prefixSelected + sfEntityTemp] = "";
}

function RemoveContainedEntity(prefix, urlReloadController, parentPrefix) {
    $('#' + prefix + sfToStr).val("");
    $('#' + prefix + sfToStr).html("");
    $('#' + prefix + sfToStr).removeClass(sfInputErrorClass);
    $('#' + prefix + sfRuntimeType).val("");
    $('#' + prefix + sfIsNew).remove();
    window[prefix + sfEntityTemp] = "";
    
    var idField = $('#' + prefix + sfId);
    $('#' + prefix + sfEntity).html("");
    $('#' + prefix + sfId).val("");
    toggleButtonsDisplay(prefix, false);

    if (urlReloadController != null && urlReloadController != undefined && urlReloadController != "")
        ReloadEntity(urlReloadController, parentPrefix);
}

var autocompleteOnSelected = function(extendedControlName, newIdAndType, newValue, hasEntity) {
    var prefix = extendedControlName.substr(0, extendedControlName.indexOf(sfToStr));
    var _index = newIdAndType.indexOf("_");
    $('#' + prefix + sfId).val(newIdAndType.substr(0, _index));
    $('#' + prefix + sfRuntimeType).val(newIdAndType.substr(_index+1, newIdAndType.length));
    $('#' + prefix + sfLink).html($('#' + extendedControlName).val());
    toggleButtonsDisplay(prefix, hasEntity);
}

function EntityComboOnChange(prefix) {
    var selected = $("#" + prefix + sfCombo + " > option:selected");
    if (selected.length == 0)
        return;
    $("#" + prefix + sfId).val(selected.val());
    if (selected.val() != "") {
        $("#" + prefix + sfRuntimeType).val($("#" + prefix + sfStaticType).val());
        toggleButtonsDisplay(prefix, true);
    }
    else {
        $("#" + prefix + sfRuntimeType).val("");
        toggleButtonsDisplay(prefix, false);
    }
    window[prefix + sfEntityTemp] = "";
    $('#' + prefix + sfEntity).html("");
}

function OnPopupComboOk(urlController, prefix) {
    TrySavePartial(urlController, prefix, "", true, "");

    //Clean panelPopup
    window[prefix + sfEntityTemp] = "";
    $('#' + prefix + sfEntity).hide();

    var runtimeType = $('#' + prefix + sfRuntimeType);
    if (runtimeType.val() == "")
        runtimeType.val($('#' + prefix + sfStaticType).val());

    toggleButtonsDisplay(prefix, true);
}

function OnPopupComboCancel(prefix) {
    window[prefix + sfEntityTemp] = "";
    $('#' + prefix + sfEntity).hide();
}

function toggleButtonsDisplay(prefix, hasEntity) {
    var btnCreate = $('#' + prefix + "_btnCreate");
    var btnRemove = $('#' + prefix + "_btnRemove");
    var btnFind = $('#' + prefix + "_btnFind");
    var btnView = $('#' + prefix + "_btnView");
    var link = $('#' + prefix + sfLink);
    var txt = $('#' + prefix + sfToStr);
    
    if (hasEntity == true) {
        link.show();
        if (link.html() == "")
            link.html("&nbsp;");
        txt.hide();
        btnCreate.hide();
        btnFind.hide();
        btnRemove.show();
        btnView.show();
    }
    else {
        link.hide();
        txt.show();
        btnCreate.show();
        btnFind.show();
        btnRemove.hide();
        btnView.hide();
    }
}

function NewRepeaterElement(urlController, prefix, runtimeType, isEmbedded, removeLinkText, maxElements) {
    if (maxElements != null && maxElements != "") {
        var elements = $("#" + prefix + sfEntitiesContainer + " > div[name$=" + sfRepeaterElement + "]").length;
        if (elements >= parseInt(maxElements))
            return;
    }
    var lastElement = $("#" + prefix + sfEntitiesContainer + " > div[name$=" + sfRepeaterElement + "]:last");
    var lastIndex = -1;
    if (lastElement.length > 0) {
        var nameSelected = lastElement[0].id;
        lastIndex = nameSelected.substring(prefix.length + 1, nameSelected.indexOf(sfRepeaterElement));
    }
    var newIndex = "_" + (parseInt(lastIndex) + 1);

    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfStaticType=" + runtimeType + "&prefix=" + prefix + newIndex,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                        var container = $("#" + prefix + sfEntitiesContainer);
                        container.append("\n" +
                        "<div id=\"" + prefix + newIndex + sfRepeaterElement +"\" name=\"" + prefix + newIndex + sfRepeaterElement +"\" class=\"repeaterElement\">\n" +
                        "<a id=\"" + prefix + newIndex + "_btnRemove\" title=\"" + removeLinkText + "\" href=\"javascript:RemoveRepeaterEntity('" + prefix + newIndex + sfRepeaterElement + "');\" class=\"lineButton\">" + removeLinkText + "</a>\n" +
                        "<input type=\"hidden\" id=\"" + prefix + newIndex + sfRuntimeType + "\" name=\"" + prefix + newIndex + sfRuntimeType + "\" value=\"" + runtimeType + "\" />\n" +
                        ((isEmbedded == "False") ? ("<input type=\"hidden\" id=\"" + prefix + newIndex + sfId + "\" name=\"" + prefix + newIndex + sfId + "\" value=\"\" />\n") : "") +
                        //"<input type=\"hidden\" id=\"" + prefix + newIndex + sfIndex + "\" name=\"" + prefix + newIndex + sfIndex + "\" value=\"" + (parseInt(lastIndex)+1) + "\" />\n" +
                        "<input type=\"hidden\" id=\"" + prefix + newIndex + sfIsNew + "\" name=\"" + prefix + newIndex + sfIsNew + "\" value=\"\" />\n" +
                        "<script type=\"text/javascript\">var " + prefix + newIndex + sfEntityTemp + " = \"\";</script>\n" +
                        "<div id=\"" + prefix + newIndex + sfEntity + "\" name=\"" + prefix + newIndex + sfEntity + "\">\n" +
                        msg + "\n" +
                        "</div>\n" + //sfEntity
                        "</div>\n" //sfRepeaterElement                        
                        );
                   },
        error:
                   function(XMLHttpRequest, textStatus, errorThrown) {
                       ShowError(XMLHttpRequest, textStatus, errorThrown);
                   }
    });
}

function RemoveRepeaterEntity(idRepeaterElement) {
    $("#" + idRepeaterElement).remove();
}
