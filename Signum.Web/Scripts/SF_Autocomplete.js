var currentText = "";
var timerID;

$(document).ready(function() {
    $("form").bind("keypress", function(e) {
        if (e.keyCode == 13) {
            return false;
        }
    });
});

function CreateAutocomplete(ddlName, extendedControlName, entityTypeName, implementations, 
                    entityIdFieldName, controllerUrl, numCharacters, numResults, delay) {
    $('#' + extendedControlName).bind(
            "keyup", function(e) {
                clearTimeout(timerID);
                timerID = setTimeout(
                    "Autocomplete('" + ddlName + "','" + extendedControlName + "','" +
                                  entityTypeName + "','" + implementations + "','" +
                                  entityIdFieldName + "','" +
                                  controllerUrl+"',"+numCharacters+","+numResults+","+
                                  ((e.which) ? e.which : e.keyCode)+")",
                    delay);
            });
    $('#' + extendedControlName).bind(
            "keydown", function(e) {
                clearTimeout(timerID);
                AutoKeyDown(ddlName, extendedControlName, entityIdFieldName, e);
            });
    $("body").bind( "click", function() { $('#' + ddlName).hide(); });
}

function Autocomplete(ddlName, extendedControlName, entityTypeName, implementations, entityIdFieldName,
                      controllerUrl, numCharacters, numResults, key) {
    if (key == 38) { return; } //Arrow up
    if (key == 40) { return; } //Arrow down
    if (key == 13) { return; } //Enter
    
    var input = $('#' + extendedControlName).val();
    if (input != null && input.length < numCharacters) {
        $('#' + ddlName).hide();
        return;
    }

    $.ajax({
        type: "POST",
        url: controllerUrl,
        async: false,
        data: "typeName=" + entityTypeName + "&implementations=" + implementations + "&input=" + input + "&limit=" + numResults,
        success:
            function(result) {
                var txtbox = $('#' + extendedControlName);
                currentText = txtbox.val();
                eval('var possibleResults=' + result);

                var optionStart = "<div id=\"OptOPTVALUEID\" class=\"ddlAuto\" onmouseover=optionOnMouseOver('" + extendedControlName + "',this); >";
                var optionEnd = "</div>";
                var resultList = "";
                for (var id in possibleResults) {
                    resultList += optionStart.replace(/OPTVALUEID/, ddlName + id) + possibleResults[id] + optionEnd;
                }
                var ddl = $('#' + ddlName);
                ddl.html(resultList);

                //var offset = txtbox.offset();
                var offset = txtbox.position();
                ddl[0].style.left = offset.left + "px";
                ddl[0].style.top = offset.top + txtbox.height() + "px";
                ddl.width(txtbox.width());

                ddl[0].style.display = "block";
            },
        error:
            function(XMLHttpRequest, textStatus, errorThrown) {
                ShowError(XMLHttpRequest, textStatus, errorThrown);
            }
    });
}

function AutoKeyDown(ddlName, extendedControlName, entityIdFieldName, evt) {

    var key = (evt.which) ? evt.which : evt.keyCode;
    if (key == 13) { //Enter
        var selectedOption = $('.ddlAutoOn');
        if(selectedOption.length > 0) {
            AutocompleteOnOk(selectedOption.html(), GetIdFromOption(selectedOption[0].id, ddlName), ddlName, extendedControlName, entityIdFieldName);
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
        AutocompleteOnOk(target.innerHTML, GetIdFromOption(target.id, ddlName), ddlName, extendedControlName, entityIdFieldName);
    }
    $('#' + ddlName).hide();
}

function GetIdFromOption(optionId, ddlName)
{
    return optionId.substr(3 + ddlName.length, optionId.length);
}

function AutocompleteOnOk(newValue, newIdAndType, ddlName, extendedControlName, entityIdFieldName) {
    var id = newIdAndType.substr(0, newIdAndType.indexOf("_"));
    $('#' + extendedControlName).val(newValue);
    $('#' + extendedControlName)[0].focus();
    $('#' + entityIdFieldName).val(id);
    $('#' + ddlName).hide();
    //TODO: Borrar campo _sfEntity
    autocompleteOnSelected(extendedControlName, newIdAndType, newValue, true);
}

function ChangeEntityLine(extendedControlName) {
    $('#' + extendedControlName).val(newValue);
    $()
    
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