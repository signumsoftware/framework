var currentText = "";
var timerID;

$(function(){$('#form input[type=text]').keypress(function(e){return e.which!=13})})

function CreateAutocomplete(ddlName, extendedControlName, entityTypeName, implementations, entityIdFieldName, controllerUrl, numCharacters, numResults, delay, AutoKeyDowned, extraJsonData) {
	$('#' + extendedControlName).bind("keyup", function (e) {
		clearTimeout(timerID);
		timerID = setTimeout("Autocomplete('" + ddlName + "','" + extendedControlName + "','" + entityTypeName + "','" + implementations + "','" + entityIdFieldName + "','" + controllerUrl + "'," + numCharacters + "," + numResults + "," + ((e.which) ? e.which : e.keyCode) + "," + extraJsonData + ")", delay);
	});
	$('#' + extendedControlName).bind("keydown", function (e) {
		clearTimeout(timerID);
		AutoKeyDown(ddlName, extendedControlName, entityIdFieldName, e, AutoKeyDowned);
	});
	$("body").bind("click", function () {
		$('#' + ddlName).hide();
	});
}

function Autocomplete(
    ddlName,
    extendedControlName,
    entityTypeName,
    implementations,
    entityIdFieldName,
    controllerUrl,
    numCharacters,
    numResults,
    key,
    extraJsonData) {
	if (key == 38) {
		return;
	} //Arrow up
	if (key == 40) {
		return;
	} //Arrow down
	if (key == 13) {
		return;
	} //Enter
	var input = $('#' + extendedControlName).val();
	if (input != null && input.length < numCharacters) {
		$('#' + ddlName).hide();
		return;
	}

    var params = {typeName: entityTypeName,
		        implementations: implementations,
		        input: input,
		        limit: numResults};
    
    params = $.extend(params, eval(extraJsonData));		        
	
	$.ajax({
		type: "POST",
		url: controllerUrl,
		async: false,
		dataType: "json",
		data: params,
		success: function (possibleResults) {
			var $txtbox = $('#' + extendedControlName);
			currentText = $txtbox.val();
			var optionStart = "<div id=\"OptOPTVALUEID\" class=\"ddlAuto\" onmouseover=optionOnMouseOver('" + extendedControlName + "',this); >";
			var optionEnd = "</div>";
			var resultList = "";
			for (var id in possibleResults) {
				resultList += optionStart.replace(/OPTVALUEID/, ddlName + id) + possibleResults[id] + optionEnd;
			}
			var $ddl = $('#' + ddlName);
			$ddl.html(resultList);

			var offset = $txtbox.position();
			$ddl.css({
			    left: offset.left,
			    top: offset.top + $txtbox.height(),
			    width: $txtbox.width(),
			    display: "block"
			});
		}
	});
}

function AutoKeyDown(ddlName, extendedControlName, entityIdFieldName, evt, AutoKeyDowned) {

	var key = (evt.which) ? evt.which : evt.keyCode;
	if (key == 13) { //Enter
		var selectedOption = $('.ddlAutoOn');
		if (selectedOption.length > 0) {
			AutocompleteOnOk(selectedOption.html(), GetIdFromOption(selectedOption[0].id, ddlName), ddlName, extendedControlName, entityIdFieldName, AutoKeyDowned);
		}
		return;
	}
	if (key == 38) { //Arrow up
		if (currentText != "") { //autocomplete dropdown is shown
			MoveUp(ddlName, extendedControlName);
			return;
		}
	}
	if (key == 40) { //Arrow down
		if (currentText != "") { //autocomplete dropdown is shown
			MoveDown(ddlName, extendedControlName);
			return;
		}
	}
}

function optionOnMouseOver(extendedControlName, option) {
	AutocompleteSelectIndex(extendedControlName, option)
}

function AutocompleteOnClick(ddlName, extendedControlName, entityIdFieldName, evt) {
	var target = evt.srcElement || evt.target;
	if (target != null) {
		AutocompleteOnOk(target.innerHTML, GetIdFromOption(target.id, ddlName), ddlName, extendedControlName, entityIdFieldName, "");
	}
	$('#' + ddlName).hide();
}

function GetIdFromOption(optionId, ddlName) {
	return optionId.substr(3 + ddlName.length, optionId.length);
}

function AutocompleteOnOk(newValue, newIdAndType, ddlName, extendedControlName, entityIdFieldName, AutoKeyDowned) {
	var id = newIdAndType.substr(0, newIdAndType.indexOf("_"));
	$('#' + extendedControlName).val(newValue);
	$('#' + extendedControlName)[0].focus();
	if (!(entityIdFieldName == "" || entityIdFieldName == undefined || entityIdFieldName == null))
        $('#' + entityIdFieldName).val(id);
	$('#' + ddlName).hide();
	//TODO: Borrar campo _sfEntity
	AutocompleteOnSelected(extendedControlName, newIdAndType, newValue, true);
	if (AutoKeyDowned != null && AutoKeyDowned != undefined && AutoKeyDowned != "") AutoKeyDowned();
}

function MoveDown(ddlName, extendedControlName) {
	var current = $('.ddlAutoOn');
	if (current.length == 0) { //Not yet in the DDL, select the first one
		AutocompleteSelectIndex(extendedControlName, $('#' + ddlName).children()[0]);
		return;
	}
	AutocompleteSelectIndex(extendedControlName, current.next()[0]);
}

function MoveUp(ddlName, extendedControlName) {
	var current = $('.ddlAutoOn');
	if (current.length == 0) { //Not yet in the DDL, select the last one
		var ddl = $('#' + ddlName);
		AutocompleteSelectIndex(extendedControlName, ddl.children()[ddl.children().length - 1]);
		return;
	}
	AutocompleteSelectIndex(extendedControlName, current.prev()[0]);
}

function AutocompleteSelectIndex(extendedControlName, option) {
	$('.ddlAutoOn').removeClass("ddlAutoOn");
	var txtbox = $('#' + extendedControlName);
	if (option == null || option == undefined) {
		txtbox.val(currentText);
		txtbox[0].focus();
		return;
	}
	$('#' + option.id).addClass("ddlAutoOn");
	txtbox[0].focus();
}