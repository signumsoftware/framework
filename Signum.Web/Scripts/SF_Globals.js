var sfPrefixToIgnore = "prefixToIgnore";
var sfFieldErrorClass = "field-validation-error";
var sfInputErrorClass = "input-validation-error";
var sfSummaryErrorClass = "validation-summary-errors";
var sfInlineErrorVal = "inlineVal";
var sfGlobalErrorsKey = "sfGlobalErrors";
var sfGlobalValidationSummary = "sfGlobalValidationSummary";

var sfStaticType = "_sfStaticType";
var sfRuntimeType = "_sfRuntimeType";
var sfImplementations = "_sfImplementations";
var sfImplementationsDDL = "_sfImplementationsDDL";
var sfId = "_sfId";
var sfEntity = "_sfEntity";
var sfEntityTemp = "_sfEntityTemp";
var sfToStr = "_sfToStr";
var sfLink = "_sfLink";
var sfIsNew = "_sfIsNew";
var sfIndex = "_sfIndex";
var sfCombo = "_sfCombo";

var sfEntitiesContainer = "_sfEntitiesContainer";
var sfRepeaterElement = "_sfRepeaterElement";

var sfBtnCancel = "sfBtnCancel";
var sfBtnOk = "sfBtnOk";
var sfBtnCancelS = "sfBtnCancelS";
var sfBtnOkS = "sfBtnOkS";

var sfTop = "sfTop";
var sfAllowMultiple = "sfAllowMultiple";
var sfEntityTypeName = "sfEntityTypeName";

function ShowError(XMLHttpRequest, textStatus, errorThrown) {
    var error;
    if (XMLHttpRequest.responseText != null && XMLHttpRequest.responseText != undefined) {
        var startError = XMLHttpRequest.responseText.indexOf("<title>");
        var endError = XMLHttpRequest.responseText.indexOf("</title>");
        if ((startError != -1) && (endError != -1))
            error = XMLHttpRequest.responseText.substring(startError + 7, endError);
        else
            error = XMLHttpRequest.responseText;
    }
    else {
        error = textStatus;
    }
    window.alert("Error: " + error);
}

// establece clase "focused" al div alrededor del campo con foco
function initAroundDivs() {
    $('.valueLine,.rbValueLine').each(function() {
    var elementID = $("#" + this.id);
    var around = elementID.parents('div[class=around]');
    if (around.length > 0) {
        elementID.focus(function() { around.addClass('focused'); });
        elementID.blur(function() { around.removeClass('focused'); });
    }
    });
}

$(function() { initAroundDivs(); });

function singleQuote(myfunction) {
    return myfunction.toString().replace(/"/g, "'");
}