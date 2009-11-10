var debug=false;
function log(s){
        if (debug)
            if (typeof console != "undefined" && typeof console.debug != "undefined") {
                console.log(s);
            } else {
                alert(s);
            }
}

var popup = function(_url, _options, _visualOptions){
      me = this;
      me.url = _url;
      me.options = $.extend({
		            divASustituir: "divASustituir",
		            prefix: "",
		            onOk: null,
		            onCancel: null,
		            detailsDiv: "",
		            partialView: "",
					isEmbeded: false
                }, _options);
      me.visualOptions = $.extend({
		            fade: false
		            }, _options);                   
};

popup.prototype = {
    
	newPopup: function(){
	    log("En newPopup");
		$(me.pf(sfEntity)).after("<input type='hidden' id='" + me.options.prefix + sfIsNew + "' name='" + me.options.prefix + sfIsNew + "' value='' />\n");
		if ($(me.pf(sfImplementations)).length == 0) 
			$(me.pf(sfRuntimeType)).val($(me.pf(sfStaticType)).val());
		me.open();
	},
	
	newDetail: function(){
	    log("En newDetail");
		me.newPopup();
		toggleButtonsDisplay(prefix, true);
		me.setTicksVal();
	},
	
	open: function(){
	    log("En open");
		me.setTicksVal();
		me.openCommon();
	},
	
	setTicksVal: function(){
	    log("En setTicksVal");
		$(me.pf(sfTicks)).val(new Date().getTime());
	},
	
	openCommon: function(){
	    log("En openCommon");
		var containedEntity = $(me.pf(sfEntity)).html();
		if (!empty(containedEntity)) { //Already has the containedEntity loaded => show it
			window[me.options.prefix + sfEntityTemp] = containedEntity;
			me.show(me.options.prefix + sfEntity, "modalBackground", "panelPopup");
			$(me.pf(sfBtnOk)).unbind('click').click(me.options.onOk);
			$(me.pf(sfBtnCancel)).unbind('click').click(me.options.onCancel);
			return;
		}
		
		var runtimeType = $(me.pf(sfRuntimeType)).val();
		if (empty(runtimeType)) {
			window.alert("Error: RuntimeType could not be solved");
			return;
		}
		
		var idQueryParam = "";
		var idField = $(me.pf(sfId));
		if (idField.length > 0) 
			idQueryParam = qp("sfId", idField.val());
		else { //Embedded Entity => send path of runtimes and ids to be able to construct a typecontext
			var pathInfo = GetPathIdsAndTypes(me.options.prefix);
			if (pathInfo.length > 0) 
				idQueryParam += "&" + pathInfo;
		}
		
		var reactiveParam = "";
		if ($('#' + sfReactive).length > 0) { //If reactive => send also tabId and Id & Runtime of the main entity
			reactiveParam = qp(sfReactive, true) +
			qp(sfTabId, $('#' + sfTabId).val()) +
			qp(sfRuntimeType, $('#' + sfRuntimeType).val()) +
			qp(sfId, $('#' + sfId).val());
		}
		
		var viewQueryParam = "";
		if (!empty(me.options.partialView)) 
			viewQueryParam = qp("sfUrl", me.options.partialView);
		
		me.typed(runtimeType, idQueryParam, reactiveParam, viewQueryParam);
	},
	
	openList: function(select){
		$('#' + select + sfTicks).val(new Date().getTime());
		if (!empty(me.options.detailDiv)) 
			$("#" + me.options.detailDiv).hide();
		
		var selected = $('#' + select + " > option:selected");
		if (selected.length == 0) 
			return;
		
		var nameSelected = selected[0].id;
		me.options.prefix = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
		me.openCommon();
	},
	
	newList: function(select, runtimeType){
		$('#' + select + sfTicks).val(new Date().getTime());
		
		if (!empty(me.options.detailDiv)) 
			$("#" + me.options.detailDiv).hide();
		
		me.newListOption(select, runtimeType);
		
		var selected = $('#' + select + " > option:selected");
		if (selected.length == 0) 
			return;
		
		var nameSelected = selected[0].id;
		me.options.prefix = nameSelected.substr(0, nameSelected.indexOf(sfToStr));
		$(me.pf(sfEntity)).after("<input type='hidden' id='" + me.options.prefix + sfIsNew + "' name='" + me.options.prefix + sfIsNew + "' value='' />\n");
		
		me.openCommon();
	},
	
	typed: function(runtimeType, idQueryParam, reactiveParam, viewQueryParam){
	    log("En typed");
		$.ajax({
			type: "POST",
			url: me.url,
			data: "sfRuntimeType=" + runtimeType + qp("sfOnOk", me.options.onOk) + qp("sfOnCancel", me.options.onCancel) + qp(sfPrefix, me.options.prefix) + idQueryParam + reactiveParam + viewQueryParam,
			async: false,
			dataType: "html",
			success: function(msg){
				window[me.options.prefix + sfEntityTemp] = $("#" + me.options.prefix + sfEntity).html();
				if (!empty(me.options.detailDiv)) 
					$('#' + me.options.detailDiv).html(msg);
				else 
					$(me.pf(sfEntity)).html(msg);
				me.show(me.options.prefix + sfEntity, "modalBackground", "panelPopup");
				$(me.pf(sfBtnOk)).click(me.options.onOk);
				$(me.pf(sfBtnCancel)).click(me.options.onCancel);
			},
			error: function(XMLHttpRequest, textStatus, errorThrown){
				ShowError(XMLHttpRequest, textStatus, errorThrown);
			}
		});
	},
	
	typedDirect: function (runtimeType) {	
	    log("En typedDirect");
	    var oldPrefix = me.options.prefix;
	    var newPrefix = me.options.prefix + "New";
	    $.ajax({
	        type: "POST",
	        url: me.url,
	        data: "sfRuntimeType=" + runtimeType + qp("sfOnOk", singleQuote(me.options.onOk)) + qp("sfOnCancel", singleQuote(me.options.onCancel)) + qp(sfPrefix, newPrefix),
	        async: false,
	        dataType: "html",
	        success: function(msg) {
	            log("Estableciendo el contenido del popup en el div con id " + me.options.prefix + "divASustituir");
	            $('#' + me.options.prefix + "divASustituir").html(msg);
	            if (msg.indexOf("<script") == 0)//A script to be run is returned instead of a Popup to open
	                return;
	            me.options.prefix = newPrefix;
	            me.show(oldPrefix + "divASustituir", "modalBackground", "panelPopup");
	            log("Mostrando");
				$(me.pf(sfBtnOk)).click(me.options.onOk);
				log("Añadiendo evento ok a " + me.pf(sfBtnOk) + " para llamar a " + me.options.onOk);
				$(me.pf(sfBtnCancel)).click(me.options.onCancel);	            
	        },
	        error: function(XMLHttpRequest, textStatus, errorThrown) {
	            ShowError(XMLHttpRequest, textStatus, errorThrown);
	        }
	    });
    },
    
    newListOption: function (prefix,selectedType) {
        me.options.prefix = prefix;
        var lastElement = $('#' + me.options.prefix + " > option:last");
        var lastIndex = -1;
        if (lastElement.length > 0) {
            var nameSelected = lastElement[0].id;
            lastIndex = nameSelected.substring(me.options.prefix.length + 1, nameSelected.indexOf(sfToStr));
        }
        var newIndex = "_" + (parseInt(lastIndex) + 1);
        var staticType = $(me.pf(sfStaticType));
        staticType.after(
            "<input type='hidden' id='" + me.options.prefix + newIndex + sfRuntimeType + "' name='" + me.options.prefix + newIndex + sfRuntimeType + "' value='" + selectedType + "' />\n" +
            "<script type=\"text/javascript\">var " + me.options.prefix + newIndex + sfEntityTemp + " = '';</script>\n");
        var sfEntityDiv = "<div id='" + me.options.prefix + newIndex + sfEntity + "' name='" + me.options.prefix + newIndex + sfEntity + "' style='display:none'></div>\n";
        if (empty(me.options.detailDiv))
            staticType.after(sfEntityDiv);
        else
            $("#" + me.options.detailDiv).append(sfEntityDiv);
        if (me.options.isEmbeded == "False")
            staticType.after("<input type='hidden' id='" + me.options.prefix + newIndex + sfId + "' name='" + me.options.prefix + newIndex + sfId + "' value='' />\n");

        var select = $('#' + me.options.prefix);
        select.append("\n<option id='" + me.options.prefix + newIndex + sfToStr + "' name='" + me.options.prefix + newIndex + sfToStr + "' value='' class='valueLine'>&nbsp;</option>");
        $('#' + me.options.prefix + " > option").attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
        $('#' + me.options.prefix + " > option:last").attr('selected', true);
    },
	
	chooseImplementation: function(){
		me.options.detailDiv = null;
		me.show(me.options.prefix + sfImplementations, "modalBackground", "panelPopup");
		$(me.pf(sfBtnOk)).unbind('click').click(me.options.onOk);
		$(me.pf(sfBtnCancel)).unbind('click').click(me.options.onCancel);
	},
	
	show: function(globalKey, modalBackgroundKey, panelPopupKey){
	    log("En show");
		if (!empty(me.options.detailDiv)) 
			$("#" + me.options.detailDiv).show();
		else {
			//$("#" + prefix + sfEntity).show();
			$("#" + globalKey).show();
			$(me.pf(modalBackgroundKey)).width(document.documentElement.clientWidth).height(document.documentElement.clientHeight).hide();
			
			//Read offsetWidth and offsetHeight after display=block or otherwise it's 0
			log(me.pf(panelPopupKey));
			log($(me.pf(panelPopupKey)).length);
			var popup2 = $(me.pf(panelPopupKey))[0];
			var parentDiv = $(me.pf(sfEntity)).parent();
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
			popup2.style.maxWidth = (maxPercentageWidth * 100) + "%";
			popup2.style.minWidth = ((popupWidth > (maxPercentageWidth * 100)) ? (maxPercentageWidth * 100) : popupWidth) + "px";
			
			if ($(me.pf(panelPopupKey) + " :file").length > 0) 
				popup2.style.minWidth = "500px";
			
			$('#' + globalKey).show('fast');
			var background = $(me.pf(modalBackgroundKey));
			background[0].style.left = 0;
			background.css('filter', 'alpha(opacity=40)')
			if (me.visualOptions.fade) 
				background.fadeIn('slow');
			else 
				background.show();
		}
	},
	
	onPopupOk: function(reloadOnChangeFunction){
	    log("En onPopupOk");
		var correct = ValidatePartial(me.url, me.options.prefix, "", true, "*");
		me.endPopupOk(me.options.prefix, reloadOnChangeFunction);
	},
	
	onListPopupOk: function(btnOkId, reloadOnChangeFunction){
		var itemPrefix = btnOkId.substr(0, btnOkId.indexOf(sfBtnOk));
		me.endPopupOk(itemPrefix, reloadOnChangeFunction);
	},
	
	endPopupOk: function(itemPrefix, reloadOnChangeFunction){
		var correct = ValidatePartial(me.url, itemPrefix, "", true, "*");
		window[itemPrefix + sfEntityTemp] = "";
		$(me.pf(sfEntity)).hide();
		toggleButtonsDisplay(me.options.prefix, true);
		log("Valor de reloadOnChangeFunction " + reloadOnChangeFunction);
		if (!empty(reloadOnChangeFunction)) {
			me.setTicksVal();
			reloadOnChangeFunction();
		}
	},
	
	onImplementationsOk: function(selectedType){
		if (empty(selectedType)) 
			return;
		$(me.pf(sfRuntimeType)).val(selectedType);
		$(me.pf(sfImplementations)).hide();
		me.newPopup();
	},
	
	onListImplementationsOk: function(selectedType){
		if (empty(selectedType)) 
			return;
		$(me.pf(sfImplementations)).hide();
		me.newList(me.options.prefix, selectedType);
	},
	
	onImplementationsCancel: function(){
		$(me.pf(sfImplementations)).hide();
	},
	
	onPopupCancel: function(){
		var oldValue = window[me.options.prefix + sfEntityTemp];
		$(me.pf(sfEntity)).html(oldValue);
		
		var id = $(me.pf(sfId));
		if (id.length > 0 && id.val() != null && id.val() > 0) 
			toggleButtonsDisplay(me.options.prefix, true);
		else {
			if (!empty(oldValue)) {
				toggleButtonsDisplay(me.options.prefix, true);
			}
			else {
				toggleButtonsDisplay(me.options.prefix, false);
				$(me.pf(sfRuntimeType)).val("");
				$(me.pf(sfIsNew)).remove();
			}
		}
		window[me.options.prefix + sfEntityTemp] = "";
		$(me.pf(sfEntity)).hide();
	},
	
	onListPopupCancel: function(btnCancelId){
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
	},
	
	onPopupComboOk: function(){
		ValidatePartial(me.url, me.options.prefix, "", true, "");
		
		//Clean panelPopup
		window[me.options.prefix + sfEntityTemp] = "";
		$(me.pf(sfEntity)).hide();
		
		var runtimeType = $(me.pf(sfRuntimeType));
		if (runtimeType.val() == "") 
			runtimeType.val($(me.pf(sfStaticType)).val());
		
		toggleButtonsDisplay(me.options.prefix, true);
	},
	
	onPopupComboCancel: function(){
		window[me.options.prefix + sfEntityTemp] = "";
		$(me.pf(sfEntity)).hide();
	},
	
	pf: function(s){ return "#"+me.options.prefix+s;}
}

function OpenPopup(urlController, divASustituir, prefix, onOk, onCancel, detailDiv, partialView) {
    new popup(urlController, {
        divASustituir: divASustituir,
        prefix: prefix,
        onOk: onOk,
        onCancel: onCancel,
        detailDiv: detailDiv
    }).open();
}

function NewPopup(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded, detailDiv, partialView) {
    new popup(urlController, {
        divASustituir: divASustituir,
        prefix: prefix,
        onOk: onOk,
        onCancel: onCancel,
        isEmbeded: isEmbeded,
        detailDiv: detailDiv,
        partialView: partialView
    }).newPopup();
}

function NewDetail(urlController, divASustituir, prefix, detailDiv, isEmbeded, partialView) {
    new popup(urlController, {
        divASustituir: divASustituir,
        prefix: prefix,
        isEmbeded: isEmbeded,
        detailDiv: detailDiv,
        partialView: partialView
    }).newDetail();
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
        onOk: onOk,
        onCancel: onCancel,
        divASustituir: divASustituir,
        detailDiv: detailDiv
        }).openList(select);
}

function NewPopupList(urlController, divASustituir, select, onOk, onCancel, runtimeType, isEmbeded, detailDiv) { 
    new popup(urlController, {
        onOk: onOk,
        onCancel: onCancel,
        divASustituir: divASustituir,
        detailDiv: detailDiv,
        isEmbeded: isEmbeded
        }).newList(select, runtimeType);              
}

function OpenPopupCommon(urlController, onOk, onCancel, divASustituir, prefix, detailDiv, partialView) {
    new popup(urlController, {
        onOk: onOk,
        onCancel: onCancel,
        divASustituir: divASustituir,
        prefix: prefix,
        detailDiv: detailDiv,
        partialView: partialView
        }).openCommon();  
}

function TypedOpenPopupCommon(urlController, onOk, onCancel, divASustituir, prefix, detailDiv, partialView, runtimeType, idQueryParam, reactiveParam, viewQueryParam) {
    new popup(urlController, {
        onOk: onOk,
        onCancel: onCancel,
        divASustituir: divASustituir,
        prefix: prefix,
        detailDiv: detailDiv,
        partialView: partialView
        }).typed(runtimeType, idQueryParam, reactiveParam, viewQueryParam);
}

function ChooseImplementation(divASustituir, prefix, onOk, onCancel) {    
    ShowPopup(prefix, prefix + sfImplementations, "modalBackground", "panelPopup", null);
    $('#' + prefix + sfBtnOk).unbind('click').click(onOk);
    $('#' + prefix + sfBtnCancel).unbind('click').click(onCancel);
}

function ShowPopup(prefix, globalKey, modalBackgroundKey, panelPopupKey, detailDiv) {
    new popup(null, {
        prefix: prefix,
        detailDiv: detailDiv
        }).show(globalKey, modalBackgroundKey, panelPopupKey);
}

function OnPopupOK(urlController, prefix, reloadOnChangeFunction) {
    new popup(urlController, {
        prefix: prefix
        }).onPopupOk(reloadOnChangeFunction);
}

function OnListPopupOK(urlController, prefix, btnOkId, reloadOnChangeFunction) {
    new popup(urlController, {
        prefix: prefix
        }).onListPopupOk(btnOkId,reloadOnChangeFunction);
}

function ReloadEntity(urlController, prefix, parentDiv) {
    $.ajax({
        type: "POST",
        url: urlController,
        data: $("form").serialize() + qp(sfPrefix,prefix),
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
    new popup(urlController,
        {
        onOk: onOk,
        onCancel: onCancel,
        isEmbeded: isEmbeded,
        divASustituir: divASustituir,
        prefix: prefix,
        }).onImplementationsOk(selectedType);
}
/*
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
}*/

function OnListImplementationsOk(urlController, divASustituir, prefix, onOk, onCancel, isEmbeded, detailDiv, selectedType) {
    new popup(urlController,
        {
        onOk: onOk,
        onCancel: onCancel,
        isEmbeded: isEmbeded,
        divASustituir: divASustituir,
        detailDiv: detailDiv,
        prefix: prefix
        }).onListImplementationsOk(selectedType);       
}

function OnImplementationsCancel(prefix) {
    new popup(null, {prefix: prefix}).onImplementationsCancel();
}

function OnPopupCancel(prefix) {
    new popup(null, {prefix: prefix}).onPopupCancel();
}

function OnListPopupCancel(btnCancelId) {
    new popup(null).OnListPopupCancel(btnCancelId);
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
    $('#' + prefix + sfToStr).val("").html("").removeClass(sfInputErrorClass);
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
    $('#' + prefix + sfToStr).val("").removeClass(sfInputErrorClass);
    $('#' + prefix + sfLink).val("").removeClass(sfInputErrorClass);
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
    $('#' + prefix + sfToStr).val("").html("").removeClass(sfInputErrorClass);
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

function AutocompleteOnSelected(extendedControlName, newIdAndType, newValue, hasEntity) {
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
	new pop(urlController, {prefix: prefix}).onPopupComboOk();
}

function OnPopupComboCancel(prefix) {
	new pop(null, {prefix: prefix}).onPopupComboCancel();
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
