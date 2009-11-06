var popup = function(url, _options){
      options = $.extend({
		            divASustituir: "divASustituir",
		            prefix: "",
		            onOk: null,
		            onCancel: null,
		            detailsDiv: "",
		            partialView: "",
					isEmbeded: false
                }, _options);
};
popup.prototype = {
        var t = this;
        init: function() {
        },
        
		newPopup: function(){
			$(t.pf(sfEntity)).after("<input type='hidden' id='" + options.prefix + sfIsNew + "' name='" + options.prefix + sfIsNew + "' value='' />\n");
			if ($(t.pf(sfImplementations)).length == 0)
                $(t.pf(sfRuntimeType)).val($(t.pf(sfStaticType)).val());
			t.open();
		},
		
        open: function() {
            t.setTicksVal();
            t.openCommon();
        },
        
        setTicksVal: function() {
             $(t.pf(sfTicks)).val(new Date().getTime());
        }, 

        openCommon: function() {
			var containedEntity = $(t.pf(sfEntity)).html();
			if (!empty(containedEntity)) { //Already has the containedEntity loaded => show it
				window[options.prefix + sfEntityTemp] = containedEntity;
				show(options.prefix + sfEntity, "modalBackground", "panelPopup");
				$(t.pf(sfBtnOk)).unbind('click').click(options.onOk);
				$(t.pf(sfBtnCancel)).unbind('click').click(options.onCancel);
				return;
			}
			
			var runtimeType = $(t.pf(sfRuntimeType)).val();
			if (empty(runtimeType)) {
				window.alert("Error: RuntimeType could not be solved");
				return;
			}
			
			var idQueryParam = "";
			var idField = $(t.pf(sfId));
			if (idField.length > 0)
				idQueryParam = qp("sfId", idField.val());
			else {  //Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
				var pathInfo = GetPathIdsAndTypes(options.prefix);
				if (pathInfo.length > 0)
					idQueryParam += "&" + pathInfo;
			}
			
			var reactiveParam = ""; 
			if ($('#' + sfReactive).length > 0) { //If reactive => send also tabId and Id & Runtime of the main entity
				reactiveParam = qp(sfReactive, true);
				reactiveParam += qp(sfTabId, $('#' + sfTabId).val());
				reactiveParam += qp(sfRuntimeType, $('#' + sfRuntimeType).val());
				reactiveParam += qp(sfId, $('#' + sfId).val());
			}
			
			var viewQueryParam = "";
			if (!empty(options.partialView))
				viewQueryParam = qp("sfUrl", options.partialView);
			
			t.typed(runtimeType, idQueryParam, reactiveParam, viewQueryParam);
		},
		
		openList: function(select){
            $('#' + select + sfTicks).val(new Date().getTime());   
            if (!empty(detailDiv))
                $("#" + detailDiv).hide();
    
            var selected = $('#' + select + " > option:selected");
            if (selected.length == 0)
                return;
    
            var nameSelected = selected[0].id;
            var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
            t.openCommon();
		},
		
		typed: function(runtimeType, idQueryParam, reactiveParam, viewQueryParam) {
		    $.ajax({
				type: "POST",
				url: options.urlController,
				data: "sfRuntimeType=" + runtimeType + qp("sfOnOk", options.onOk) + qp("sfOnCancel", options.onCancel) + qp(sfPrefix, options.prefix) + idQueryParam + reactiveParam + viewQueryParam,
				async: false,
				dataType: "html",
				success:
						   function(msg) {
							   window[options.prefix + sfEntityTemp] = $("#" + options.prefix + sfEntity).html();
							   if (!empty(options.detailDiv)) 
									$('#' + options.detailDiv).html(msg);
							   else
									$("#" + options.prefix + sfEntity).html(msg);
							    t.show(options.prefix + sfEntity, "modalBackground", "panelPopup");
							   $("#" + options.prefix + sfBtnOk).click(options.onOk);
							   $("#" + options.prefix + sfBtnCancel).click(options.onCancel);
						   },
				error:
						   function(XMLHttpRequest, textStatus, errorThrown) {
								ShowError(XMLHttpRequest, textStatus, errorThrown);
						   }
			});
		},
		
		chooseImplementation: function() {
			options.detailDiv=null;
			t.show(options.prefix + sfImplementations, "modalBackground", "panelPopup");
			$(t.pf(sfBtnOk)).unbind('click').click(options.onOk);
			$(t.pf(sfBtnCancel)).unbind('click').click(options.onCancel);
		},
		
		show: function(globalKey, modalBackgroundKey, panelPopupKey){
			if (!empty(detailDiv))
				$("#" + detailDiv).show();
			else {
				//$("#" + prefix + sfEntity).show();
				$("#" + globalKey).show();
				$(t.pf(modalBackgroundKey)).width(document.documentElement.clientWidth).height(document.documentElement.clientHeight).hide();

				//Read offsetWidth and offsetHeight after display=block or otherwise it's 0
				var popup2 = $(t.pf(panelPopupKey))[0];
				var parentDiv = $(t.pf(sfEntity)).parent();
				var popupWidth = popup2.offsetWidth;
				var bodyWidth = document.body.clientWidth;
				var left = Math.max((bodyWidth - popupWidth) / 2, 10) + "px";
				var popupHeight = popup2.offsetHeight;
				var bodyHeight = document.documentElement.clientHeight;
				var top = Math.max((bodyHeight - popupHeight) / 2, 10) + "px";
			   
				$('#' + globalKey).hide();
				popup2.style.left = left;
				popup2.style.top = top;
				popup2.style.minWidth = popupWidth + "px";
				popup2.style.maxWidth = "95%";
				
				var maxPercentageWidth = 0.95;
				popup2.style.maxWidth = (maxPercentageWidth*100)+"%";
				popup2.style.minWidth = ((popupWidth>(maxPercentageWidth*100)) ? (maxPercentageWidth*100) : popupWidth) + "px"; 

				if ($(t.pf(panelPopupKey) + " :file").length > 0)
					popup2.style.minWidth = "500px";
				
				$('#' + globalKey).show('fast');
				$(t.pf(modalBackgroundKey))[0].style.left=0;
				$(t.pf(modalBackgroundKey)).css('filter','alpha(opacity=40)').fadeIn('slow');
			}
		},
		pf: function(s){
		    console.log("En pf");
		    return ("#"+options.prefix+s);
		}  
	};
	

function OpenPopup(urlController, _divASustituir, prefix, onOk, onCancel, detailDiv, partialView) {
    new popup(urlController, {
        divASustituir:_divASustituir,
        "prefix":prefix,
        "onOk":onOk,
        "onCancel":onCancel,
        "detailDiv":detailDiv
    }).open();
}

function NewPopup(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded, detailDiv, partialView) {
    new popup(urlController, {
        "divASustituir":divASustituir,
        "prefix":prefix,
        "onOk":onOk,
        "onCancel":onCancel,
        "isEmbeded":isEmbeded,
        "detailDiv":detailDiv,
        "partialView":partialView
    }).newPopup();
}

function NewDetail(urlController, divASustituir, prefix, detailDiv, isEmbeded, partialView) {
    new popup(urlController, {
        "divASustituir":divASustituir,
        "prefix":prefix,
        "isEmbeded":isEmbeded,
        "detailDiv":detailDiv,
        "partialView":partialView
    }).newPopup();
    toggleButtonsDisplay(prefix, true);
    $('#' + prefix + sfTicks).val(new Date().getTime());
}

//function OpenDetail(urlController, divASustituir, prefix, onOk, onCancel, detailDiv, reloadOnChangeFunction) {
//    OpenPopup(urlController, divASustituir, prefix, onOk, onCancel, detailDiv);
//    toggleButtonsDisplay(prefix, true);

//    if (!empty(reloadOnChangeFunction)) {
//        $('#' + prefix + sfTicks).val(new Date().getTime());
//        reloadOnChangeFunction();
//    }
//}

function OpenPopupList(urlController, divASustituir, select, onOk, onCancel, detailDiv) {
    new popup(urlController, {
        "onOk":onOk,
        "onCancel":onCancel,
        "divASustituir":divASustituir,
        "prefix":prefixSelected,
        "detailDiv":detailDiv
        }).openList(select);
}

function NewPopupList(urlController, divASustituir, select, onOk, onCancel, runtimeType, isEmbeded, detailDiv) {

    $('#' + select + sfTicks).val(new Date().getTime());
    
    if (!empty(detailDiv))
        $("#" + detailDiv).hide();
    
    NewListOption(select, runtimeType, isEmbeded, detailDiv);

    var selected = $('#' + select + " > option:selected");
    if (selected.length == 0)
        return;
        
    var nameSelected = selected[0].id;
    var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
    $('#' + prefixSelected + sfEntity).after("<input type='hidden' id='" + prefixSelected + sfIsNew + "' name='" + prefixSelected + sfIsNew + "' value='' />\n");
    OpenPopupCommon(urlController, onOk, onCancel, divASustituir, prefixSelected, detailDiv);
}

function OpenPopupCommon(urlController, onOk, onCancel, divASustituir, prefix, detailDiv, partialView) {
    var containedEntity = $('#' + prefix + sfEntity).html();
    if (!empty(containedEntity)) { //Already have the containedEntity loaded => show it
        window[prefix + sfEntityTemp] = containedEntity;
        ShowPopup(prefix, prefix + sfEntity, "modalBackground", "panelPopup", detailDiv);
        $('#' + prefix + sfBtnOk).unbind('click').click(onOk);
        $('#' + prefix + sfBtnCancel).unbind('click').click(onCancel);
        return;
    }

    var runtimeType = $('#' + prefix + sfRuntimeType).val();
    if (empty(runtimeType)) {
        window.alert("Error: RuntimeType could not be solved");
        return;
    }
    
    var idQueryParam = "";
    var idField = $('#' + prefix + sfId);
    if (idField.length > 0)
        idQueryParam = qp("sfId", idField.val());
    else {  //Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
        var pathInfo = GetPathIdsAndTypes(prefix);
        if (pathInfo.length > 0)
            idQueryParam += "&" + pathInfo;
    }

    var reactiveParam = "";
    if ($('#' + sfReactive).length > 0) { //If reactive => send also tabId and Id & Runtime of the main entity
        reactiveParam = qp(sfReactive, true);
        reactiveParam += qp(sfTabId, $('#' + sfTabId).val());
        reactiveParam += qp(sfRuntimeType, $('#' + sfRuntimeType).val());
        reactiveParam += qp(sfId, $('#' + sfId).val());
    }
    
    var viewQueryParam = "";
    if (!empty(partialView))
        viewQueryParam = qp("sfUrl", partialView);

    TypedOpenPopupCommon(urlController, onOk, onCancel, divASustituir, prefix, detailDiv, partialView, runtimeType, idQueryParam, reactiveParam, viewQueryParam);
}

function TypedOpenPopupCommon(urlController, onOk, onCancel, divASustituir, prefix, detailDiv, partialView, runtimeType, idQueryParam, reactiveParam, viewQueryParam) {
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfRuntimeType=" + runtimeType + qp("sfOnOk",onOk) + qp("sfOnCancel",onCancel) + qp(sfPrefix, prefix) + idQueryParam + reactiveParam + viewQueryParam,
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       window[prefix + sfEntityTemp] = containedEntity;
                       if (!empty(detailDiv)) 
                            $('#' + detailDiv).html(msg);
                       else
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
    if (!empty(detailDiv))
        $("#" + detailDiv).show();
    else {
        //$("#" + prefix + sfEntity).show();
        $("#" + globalKey).show();
        $('#' + prefix + modalBackgroundKey).width(document.documentElement.clientWidth).height(document.documentElement.clientHeight).hide();

        //Read offsetWidth and offsetHeight after display=block or otherwise it's 0
        var popup = $('#' + prefix + panelPopupKey)[0];
        var parentDiv = $("#" + prefix + sfEntity).parent();
        var popupWidth = popup.offsetWidth;
        var bodyWidth = document.body.clientWidth;
        var left = Math.max((bodyWidth - popupWidth) / 2, 10) + "px";
        var popupHeight = popup.offsetHeight;
        var bodyHeight = document.documentElement.clientHeight;
        var top = Math.max((bodyHeight - popupHeight) / 2, 10) + "px";
       
        $('#' + globalKey).hide();
        popup.style.left = left;
        popup.style.top = top;
        popup.style.minWidth = popupWidth + "px";
        popup.style.maxWidth = "95%";
        
        var maxPercentageWidth = 0.95;
        popup.style.maxWidth = (maxPercentageWidth*100)+"%";
        popup.style.minWidth = ((popupWidth>(maxPercentageWidth*100)) ? (maxPercentageWidth*100) : popupWidth) + "px"; 

        if ($('#' + prefix + panelPopupKey + " :file").length > 0)
            popup.style.minWidth = "500px";
        
        $('#' + globalKey).show('fast');
        $('#' + prefix + modalBackgroundKey)[0].style.left=0;
        $('#' + prefix + modalBackgroundKey).css('filter','alpha(opacity=40)').fadeIn('slow');
    }
}

function OnPopupOK(urlController, prefix, reloadOnChangeFunction) {
    var correct = ValidatePartial(urlController, prefix, "", true, "*");

    window[prefix + sfEntityTemp] = "";
    $('#' + prefix + sfEntity).hide();

    toggleButtonsDisplay(prefix, true);

    if (!empty(reloadOnChangeFunction)) {
        $('#' + prefix + sfTicks).val(new Date().getTime());
        reloadOnChangeFunction();
    }
}

function OnListPopupOK(urlController, prefix, btnOkId, reloadOnChangeFunction) {
    var itemPrefix = btnOkId.substr(0, btnOkId.indexOf(sfBtnOk));
    var correct = ValidatePartial(urlController, itemPrefix, "", true, "*");

    window[itemPrefix + sfEntityTemp] = "";
    $('#' + itemPrefix + sfEntity).hide();

    toggleButtonsDisplayList(prefix, true);//prefix=itemPrefix.substr(0, itemPrefix.lastIndexOf("_"))
    
    if (!empty(reloadOnChangeFunction)) {
        $('#' + prefix + sfTicks).val(new Date().getTime());
        reloadOnChangeFunction();
    }
}

function ReloadEntity(urlController, prefix, parentDiv) {
    var formChildren = $("form");
    $.ajax({
        type: "POST",
        url: urlController,
        data: formChildren.serialize() + qp(sfPrefix,prefix),
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

function OnImplementationsOk(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded, selectedType) {
    if (empty(selectedType))
        return;
    $('#' + prefix + sfRuntimeType).val(selectedType);
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
        "<input type='hidden' id='" + prefix + newIndex + sfRuntimeType + "' name='" + prefix + newIndex + sfRuntimeType + "' value='" + selectedType + "' />\n" +
        "<script type=\"text/javascript\">var " + prefix + newIndex + sfEntityTemp + " = '';</script>\n");
    var sfEntityDiv = "<div id='" + prefix + newIndex + sfEntity + "' name='" + prefix + newIndex + sfEntity + "' style='display:none'></div>\n";
    if (empty(detailDiv))
        staticType.after(sfEntityDiv);
    else
        $("#" + detailDiv).append(sfEntityDiv);
    if (isEmbeded == "False")
        staticType.after("<input type='hidden' id='" + prefix + newIndex + sfId + "' name='" + prefix + newIndex + sfId + "' value='' />\n");

    var select = $('#' + prefix);
    select.append("\n<option id='" + prefix + newIndex + sfToStr + "' name='" + prefix + newIndex + sfToStr + "' value='' class='valueLine'>&nbsp;</option>");
    $('#' + prefix + " > option").attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
    $('#' + prefix + " > option:last").attr('selected', true);
}

function OnListImplementationsOk(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded, detailDiv, selectedType) {
    if (empty(selectedType))
        return;
    $('#' + prefix + sfImplementations).hide();
    NewPopupList(urlController, divASustituir, prefix, onOk, onCancel, selectedType, isEmbeded, detailDiv);
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
        if (!empty(oldValue)) {
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
    var itemPrefix = btnCancelId.substr(0, btnCancelId.indexOf(sfBtnCancel));
    var prefix = itemPrefix.substr(0, itemPrefix.lastIndexOf("_"));
    var oldValue = window[itemPrefix + sfEntityTemp];
    $('#' + itemPrefix + sfEntity).html(oldValue);

    var id = $('#' + itemPrefix + sfId);
    if (id.length > 0 && id.val() != null && id.val() > 0) {
        toggleButtonsDisplayList(prefix, true);
    }
    else {
        if (!empty(oldValue)) {
            toggleButtonsDisplayList(prefix, true);
        }
        else {
            toggleButtonsDisplayList(prefix, false);
            $('#' + itemPrefix + sfId).remove();
            $('#' + itemPrefix + sfRuntimeType).remove();
            $('#' + itemPrefix + sfToStr).remove();
            $('#' + itemPrefix + sfEntity).remove();
            $('#' + itemPrefix + sfIsNew).remove();
        }
    }

    window[itemPrefix + sfEntityTemp] = "";
    $('#' + itemPrefix + sfEntity).hide();
}

function RemoveListContainedEntity(select) {
    var selected = $('#' + select + " > option:selected");
    if (selected.length == 0)
        return;
    var nameSelected = selected[0].id;
    var prefixSelected = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
     
    $('#' + prefixSelected + sfId).remove();
    $('#' + prefixSelected + sfRuntimeType).remove();
    $('#' + prefixSelected + sfToStr).remove();
    $('#' + prefixSelected + sfEntity).remove();
    $('#' + prefixSelected + sfIndex).remove();
    $('#' + prefixSelected + sfIsNew).remove();
    window[prefixSelected + sfEntityTemp] = "";

    $('#' + select + sfTicks).val(new Date().getTime());
            
    toggleButtonsDisplayList(select, $('#' + select + " > option").length > 0);
}

function RemoveContainedEntity(prefix, reloadOnChangeFunction) {
    $('#' + prefix + sfToStr).val("");
    $('#' + prefix + sfToStr).html("");
    $('#' + prefix + sfToStr).removeClass(sfInputErrorClass);
    $('#' + prefix + sfRuntimeType).val("");
    $('#' + prefix + sfIsNew).remove();
    window[prefix + sfEntityTemp] = "";
    
    $('#' + prefix + sfEntity).html("");
    $('#' + prefix + sfId).val("");
    toggleButtonsDisplay(prefix, false);

    if (!empty(reloadOnChangeFunction)) {
        $('#' + prefix + sfTicks).val(new Date().getTime());
        reloadOnChangeFunction();
    }
}

function RemoveFileLineEntity(prefix, reloadOnChangeFunction) {
    $('#' + prefix + sfToStr).val("");
    $('#' + prefix + sfToStr).removeClass(sfInputErrorClass);
    $('#' + prefix + sfLink).val("");
    $('#' + prefix + sfLink).removeClass(sfInputErrorClass);
    $('#' + prefix + sfRuntimeType).val("");
    $('#' + prefix + sfIsNew).remove();
    window[prefix + sfEntityTemp] = "";

    $('#' + prefix + sfEntity).html("");
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

function RemoveDetailContainedEntity(prefix, detailDiv, reloadOnChangeFunction) {
    $('#' + prefix + sfToStr).val("");
    $('#' + prefix + sfToStr).html("");
    $('#' + prefix + sfToStr).removeClass(sfInputErrorClass);
    $('#' + prefix + sfRuntimeType).val("");
    $('#' + prefix + sfIsNew).remove();
    window[prefix + sfEntityTemp] = "";

    var idField = $('#' + prefix + sfId);
    $('#' + prefix + sfId).val("");
    $('#' + detailDiv).html("");

    toggleButtonsDisplay(prefix, false);

    if (!empty(reloadOnChangeFunction)) {
        $('#' + prefix + sfTicks).val(new Date().getTime());
        reloadOnChangeFunction();
    }
}

var autocompleteOnSelected = function(extendedControlName, newIdAndType, newValue, hasEntity) {
    var prefix = extendedControlName.substr(0, extendedControlName.indexOf(sfToStr));
    var _index = newIdAndType.indexOf("_");
    $('#' + prefix + sfId).val(newIdAndType.substr(0, _index));
    $('#' + prefix + sfRuntimeType).val(newIdAndType.substr(_index+1, newIdAndType.length));
    $('#' + prefix + sfLink).html($('#' + extendedControlName).val());
    $('#' + prefix + sfTicks).val(new Date().getTime());
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
    ValidatePartial(urlController, prefix, "", true, "");

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

function toggleButtonsDisplayList(prefix, hasEntity) {
    var btnRemove = $('#' + prefix + "_btnRemove");
    if (hasEntity == true)
        btnRemove.show();
    else
        btnRemove.hide();
}

function NewRepeaterElement(urlController, prefix, runtimeType, isEmbedded, removeLinkText, maxElements) {
    $('#' + prefix + sfTicks).val(new Date().getTime());
    if (!empty(maxElements)) {
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
        data: "sfRuntimeType=" + runtimeType + qp(sfPrefix, prefix + newIndex),
        async: false,
        dataType: "html",
        success:
                   function(msg) {
                       var newPrefix = prefix + newIndex;
                       $("#" + prefix + sfEntitiesContainer).append("\n" +
                        "<div id='" + newPrefix + sfRepeaterElement + "' name='" + newPrefix + sfRepeaterElement + "' class='repeaterElement'>\n" +
                        "<a id='" + newPrefix + "_btnRemove' title='" + removeLinkText + "' href=\"javascript:RemoveRepeaterEntity('" + newPrefix + sfRepeaterElement + "');\" class='lineButton remove'>" + removeLinkText + "</a>\n" +
                        "<input type='hidden' id='" + newPrefix + sfRuntimeType + "' name='" + newPrefix + sfRuntimeType + "' value='" + runtimeType + "' />\n" +
                        ((isEmbedded == "False") ? ("<input type='hidden' id='" + newPrefix + sfId + "' name='" + newPrefix + sfId + "' value='' />\n") : "") +
                       //"<input type=\"hidden\" id=\"" + newPrefix + sfIndex + "\" name=\"" + newPrefix + sfIndex + "\" value=\"" + (parseInt(lastIndex)+1) + "\" />\n" +
                        "<input type='hidden' id='" + newPrefix + sfIsNew + "' name='" + newPrefix + sfIsNew + "' value='' />\n" +
                        "<script type=\"text/javascript\">var " + newPrefix + sfEntityTemp + " = '';</script>\n" +
                        "<div id='" + newPrefix + sfEntity + "' name='" + newPrefix + sfEntity + "'>\n" +
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

function RemoveRepeaterEntity(idRepeaterElement, prefix, reloadOnChangeFunction) {
    $("#" + idRepeaterElement).remove();
    
    if (!empty(reloadOnChangeFunction)) {
        $('#' + prefix + sfTicks).val(new Date().getTime());
        reloadOnChangeFunction();
    }
}
