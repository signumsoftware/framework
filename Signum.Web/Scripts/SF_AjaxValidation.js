function TrySaveAndPostServer(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText) {
    if (TrySave('Signum/Validate', prefixToIgnore, showInlineError, fixedInlineErrorText)) {
        document.forms[0].action = urlController;
        document.forms[0].submit();
    }
}

//fixedInlineErrorText = "" for it to be populated from ModelState error messages
function TrySave(urlController, prefixToIgnore, showInlineError, fixedInlineErrorText) {
    var returnValue = false;
    $.ajax({
        type: "POST",
        url: urlController,
        async: false,
        data: $("form").serialize() + "&" + sfPrefixToIgnore + "=" + prefixToIgnore,
        success:
            function(result) {
                eval('var modelState=' + result);

                //Remove previous errors
                $('.' + sfFieldErrorClass).replaceWith("");
                $('.' + sfInputErrorClass).removeClass(sfInputErrorClass);
                $('.' + sfSummaryErrorClass).replaceWith("");

                var allErrors = "";
                var inlineErrorStart = "&nbsp;<span class=\"" + sfFieldErrorClass + "\">";
                var inlineErrorEnd = "</span>"
                for (var controlID in modelState) {
                    var errorsArray = modelState[controlID];
                    var errorMessage = "";
                    for (var j = 0; j < errorsArray.length; j++) {
                        errorMessage += errorsArray[j];
                        allErrors += "<li>" + errorsArray[j] + "</li>\n";
                    }
                    if (controlID != sfGlobalErrorsKey && controlID != "") {
                        var control = $('#' + controlID);

                        if (control.length == 0) {   //radioButtons
                            control = $("input:radio[name='" + controlID + "']");
                        }

                        control.addClass(sfInputErrorClass);
                        if (showInlineError && control.hasClass(sfInlineErrorVal)) {
                            if (control.next().hasClass("ui-datepicker-trigger")) {
                                if (fixedInlineErrorText == "")
                                    $('#' + controlID).next().after(inlineErrorStart + errorMessage + inlineErrorEnd);
                                else
                                    $('#' + controlID).next().after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                            }
                            else {
                                if (fixedInlineErrorText == "")
                                    $('#' + controlID).after(inlineErrorStart + errorMessage + inlineErrorEnd);
                                else
                                    $('#' + controlID).after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                            }
                        }
                    }
                }

                if (allErrors != "") {
                    if (document.getElementById(sfGlobalValidationSummary) != null) {
                        document.getElementById(sfGlobalValidationSummary).innerHTML = "<ul class=\"" + sfSummaryErrorClass + "\">\n" + allErrors + "</ul>\n";
                    }
                    return;
                }
                returnValue = true;
                return;
            },
        error:
            function(XMLHttpRequest, textStatus, errorThrown) {
                ShowError(XMLHttpRequest, textStatus, errorThrown);
            }
    });
        return returnValue;
    }

    //fixedInlineErrorText = "" for it to be populated from ModelState error messages
    function TrySavePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText) {
        var typeName = $('#' + prefix + sfStaticType).val();
        var runtimeType = $('#' + prefix + sfRuntimeType).val(); //typeName is an interface
        if (runtimeType != null && runtimeType != "") {
            typeName = runtimeType;
        }
        return TypedTrySavePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, typeName, "", false);
    }

    //fixedInlineErrorText = "" for it to be populated from ModelState error messages
    function TrySavePartialList(urlController, prefix, itemPrefix, prefixToIgnore, showInlineError, fixedInlineErrorText) {
        var typeName = $('#' + prefix + sfStaticType).val();
        var runtimeType = $('#' + itemPrefix + sfRuntimeType).val(); //typeName is an interface
        if (runtimeType != null && runtimeType != "") {
            typeName = runtimeType;
        }
        return TypedTrySavePartial(urlController, itemPrefix, prefixToIgnore, showInlineError, fixedInlineErrorText, typeName, "", false);
    }

    //fixedInlineErrorText = "" for it to be populated from ModelState error messages
    function TypedTrySavePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, staticType, panelPopupKey, save) {
        if (panelPopupKey == "" || panelPopupKey == undefined)
            panelPopupKey = "panelPopup"
        var formChildren = $('#' + prefix + panelPopupKey + " *, #" + prefix + sfId + ", #" + prefix + sfRuntimeType + ", #" + prefix + sfStaticType + ", #" + prefix + sfIsNew);
        var idField = document.getElementById(prefix + sfId);
        var idQueryParam = "";
        if (idField != null && idField != undefined) {
            idQueryParam = "&sfId=" + idField.value;
        }

        var returnValue = false;
        $.ajax({
            type: "POST",
            url: urlController,
            async: false,
            data: formChildren.serialize() + "&prefix=" + prefix + "&" + sfPrefixToIgnore + "=" + prefixToIgnore + "&sfStaticType=" + staticType + "&save=" + save + idQueryParam,
            success:
            function(result) {
                eval('var result=' + result);
                var toStr = result[sfToStr];
                var link = $("#" + prefix + sfLink);
                if (link.length > 0)
                    link.html(toStr); //EntityLine
                else {
                    var tost = $("#" + prefix + sfToStr);
                    if (tost.length > 0)
                        tost.html(toStr); //EntityList
                    else {
                        var combo = $("#" + prefix + sfCombo);
                        if (combo.length > 0)
                            $('#' + prefix + sfCombo + " option:selected").html(toStr);
                    }
                }
                var modelState = result["ModelState"];
                returnValue = ShowErrorMessages(prefix, modelState, showInlineError, fixedInlineErrorText);
                return;
            },
            error:
            function(XMLHttpRequest, textStatus, errorThrown) {
                ShowError(XMLHttpRequest, textStatus, errorThrown);
            }
        });
        return returnValue;
    }

    //fixedInlineErrorText = "" for it to be populated from ModelState error messages
    function ValidatePartial(urlController, prefix, prefixToIgnore, showInlineError, fixedInlineErrorText, staticType, panelPopupKey) {
        if (panelPopupKey == "" || panelPopupKey == undefined)
            panelPopupKey = "panelPopup"
        var formChildren = $('#' + prefix + panelPopupKey + " *, #" + prefix + sfId + ", #" + prefix + sfRuntimeType + ", #" + prefix + sfStaticType + ", #" + prefix + sfIsNew);
        var idField = document.getElementById(prefix + sfId);
        var idQueryParam = "";
        if (idField != null && idField != undefined) {
            idQueryParam = "&sfId=" + idField.value;
        }

        var returnValue = false;
        $.ajax({
            type: "POST",
            url: urlController,
            async: false,
            data: formChildren.serialize() + "&prefix=" + prefix + "&" + sfPrefixToIgnore + "=" + prefixToIgnore + "&sfStaticType=" + staticType + idQueryParam,
            success:
            function(result) {
                eval('var result=' + result);
                var toStr = result[sfToStr];
                var link = $("#" + prefix + sfLink);
                if (link.length > 0)
                    link.html(toStr); //EntityLine
                else {
                    var tost = $("#" + prefix + sfToStr);
                    if (tost.length > 0)
                        tost.html(toStr); //EntityList
                    else {
                        var combo = $("#" + prefix + sfCombo);
                        if (combo.length > 0)
                            $('#' + prefix + sfCombo + " option:selected").html(toStr);
                    }
                }
                var modelState = result["ModelState"];
                returnValue = ShowErrorMessages(prefix, modelState, showInlineError, fixedInlineErrorText);
                return;
            },
            error:
            function(XMLHttpRequest, textStatus, errorThrown) {
                ShowError(XMLHttpRequest, textStatus, errorThrown);
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
        var inlineErrorStart = "&nbsp;<span class=\"" + sfFieldErrorClass + "\">";
        var inlineErrorEnd = "</span>"

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
                        if (fixedInlineErrorText == "")
                            $('#' + controlID).next().after(inlineErrorStart + errorMessage + inlineErrorEnd);
                        else
                            $('#' + controlID).next().after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                    }
                    else {
                        if (fixedInlineErrorText == "")
                            $('#' + controlID).after(inlineErrorStart + errorMessage + inlineErrorEnd);
                        else
                            $('#' + controlID).after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                    }
                }
            }
        }

        if (allErrors != "") {
            $('#' + prefix + sfToStr).addClass(sfInputErrorClass);
            $('#' + prefix + sfLink).addClass(sfInputErrorClass);
            if (document.getElementById(prefix + sfGlobalValidationSummary) != null) {
                document.getElementById(prefix + sfGlobalValidationSummary).innerHTML = "<br /><ul class=\"" + sfSummaryErrorClass + "\">\n" + allErrors + "</ul><br />\n";
            }
            return false;
        }
        return true;
    }

    