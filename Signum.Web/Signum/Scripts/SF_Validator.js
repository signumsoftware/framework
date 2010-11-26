if (!Validator && typeof Validator == "undefined") {

    var Validator = function (_valOptions) {
        this.valOptions = $.extend({
            prefix: "",
            controllerUrl: null,
            prefixToIgnore: null,
            showInlineErrors: true,
            fixedInlineErrorText: "*", //Set to "" for it to be populated from ModelState error messages
            parentDiv: "",
            requestExtraJsonData: null,
            ajaxError: null,
            errorSummaryId : null
        }, _valOptions);

        this.savingControllerUrl = this.valOptions.controllerUrl || "Signum/TrySave";
        this.validatingControllerUrl = this.valOptions.controllerUrl || "Signum/Validate";
    };

    Validator.prototype = {

        pf: function (s) {
            return "#" + this.valOptions.prefix.compose(s);
        },

        constructRequestData: function () {
            log("Validator constructRequestData");
            var formChildren = empty(this.valOptions.parentDiv) ?
            $("form :input") : $("#" + this.valOptions.parentDiv + " :input")
                .add("#" + sfTabId)
                .add("#" + sfReactive);

            var searchControlInputs = $(".searchControl :input");
            formChildren = formChildren.not(searchControlInputs);

            var requestData = formChildren.serialize();
            requestData += qp(sfPrefix, this.valOptions.prefix);

            if (!empty(this.valOptions.prefixToIgnore))
                requestData += qp(sfPrefixToIgnore, this.valOptions.prefixToIgnore);

            if (!empty(this.valOptions.requestExtraJsonData)) {
                for (var key in this.valOptions.requestExtraJsonData) {
                    requestData += qp(key, this.valOptions.requestExtraJsonData[key]);
                }
            }
            return requestData;
        },

        trySave: function () {
            log("Validator trySave");
            NotifyInfo(lang['saving']);
            var returnValue = false;
            var self = this;
            SF.ajax({
                type: "POST",
                url: this.savingControllerUrl,
                async: false,
                data: this.constructRequestData(),
                success: function (msg) {
                    if (msg.indexOf("ModelState") > 0) {
                        var result = $.parseJSON(msg);  //eval('var result=' + msg);
                        var modelState = result["ModelState"];
                        returnValue = self.showErrors(modelState, true);
                        NotifyInfo(lang['error'], 2000);
                    }
                    else {
                        if (empty(self.valOptions.parentDiv))
                            $("#content").html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                        else
                            $("#" + self.valOptions.parentDiv).html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                        returnValue = true;
                        NotifyInfo(lang['saved'], 2000);
                    }
                }
            });
            return returnValue;
        },

        validate: function () {
            log("Validator validate");
            var returnValue = false;
            var self = this;
            SF.ajax({
                type: "POST",
                url: this.validatingControllerUrl,
                async: false,
                data: this.constructRequestData(),
                success: function (msg) {
                    if (msg.indexOf("ModelState") > 0) {
                        var result = $.parseJSON(msg);  //eval('var result=' + msg);
                        var modelState = result["ModelState"];
                        returnValue = self.showErrors(modelState, true);
                    }
                    else {
                        returnValue = true;
                    }
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    if (self.ajaxError != null) self.ajaxError(xhr, ajaxOptions, thrownError);
                }
            });
            return returnValue;
        },

        isValid: function (modelState) {
            log("Validator isValid");
            for (var controlID in modelState) {
                if (modelState[controlID].length) return false; //Stop as soon as I find an error
            }
            return true;
        },

        showErrors: function (modelState, showPathErrors) {
            log("Validator showErrors");
            //Remove previous errors
            $('.' + sfFieldErrorClass).replaceWith("");
            $('.' + sfInputErrorClass).removeClass(sfInputErrorClass);
            $('.' + sfSummaryErrorClass).replaceWith("");

            var allErrors = [];
            var inlineErrorStart = '&nbsp;<span class="' + sfFieldErrorClass + '">';
            var inlineErrorEnd = "</span>";

            for (var controlID in modelState) {
                var errorsArray = modelState[controlID];
                var errorMessage = [];
                var partialErrors = [];
                for (var j = 0; j < errorsArray.length; j++) {
                    errorMessage.push(errorsArray[j]);
                    partialErrors.push("<li>" + errorsArray[j] + "</li>");
                    allErrors.push(partialErrors);
                }
                if (controlID != sfGlobalErrorsKey && controlID != "") {
                    var $control = $('#' + controlID);
                    $control.addClass(sfInputErrorClass);
                    if (this.valOptions.showInlineErrors && $control.hasClass(sfInlineErrorVal)) {
                        if ($control.next().hasClass("ui-datepicker-trigger")) {
                            if (empty(this.valOptions.fixedInlineErrorText))
                                $control.next().after(inlineErrorStart + errorMessage.join('') + inlineErrorEnd);
                            else
                                $control.next().after(inlineErrorStart + this.valOptions.fixedInlineErrorText + inlineErrorEnd);
                        }
                        else {
                            if (empty(this.valOptions.fixedInlineErrorText))
                                $control.after(inlineErrorStart + errorMessage.join('') + inlineErrorEnd);
                            else
                                $control.after(inlineErrorStart + this.valOptions.fixedInlineErrorText + inlineErrorEnd);
                        }
                    }
                }
                this.setPathErrors(controlID, partialErrors.join(''), showPathErrors);
            }

            if (allErrors.length) {
                log("(Errors Validator showErrors): " + allErrors.join(''));
                return false;

            }
            return true;
        },

        //This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
        setPathErrors: function (controlID, partialErrors, showPathErrors) {
            var pathPrefixes = (controlID != sfGlobalErrorsKey) ? GetPathPrefixes(controlID) : new Array("");
            for (var entry in pathPrefixes) {
                var currPrefix = pathPrefixes[entry];
                if (currPrefix != undefined) {
                    var isEqual = (currPrefix == this.valOptions.prefix);
                    var isMoreInner = !isEqual && (currPrefix.indexOf(this.valOptions.prefix) > -1);
                    if (showPathErrors || isMoreInner) {
                        $('#' + currPrefix.compose(sfToStr)).addClass(sfInputErrorClass);
                        $('#' + currPrefix.compose(sfLink)).addClass(sfInputErrorClass);
                    }
                    if ((isMoreInner || isEqual) && $('#' + currPrefix.compose(sfGlobalValidationSummary)).length > 0 && !empty(partialErrors)) {
                        var currentSummary = !empty(this.valOptions.errorSummaryId) ?
                                             $('#' + this.valOptions.errorSummaryId) :
                                             empty(this.valOptions.parentDiv) ? 
                                                $('#' + currPrefix.compose(sfGlobalValidationSummary)) :
                                                $('#' + this.valOptions.parentDiv + " #" + currPrefix.compose(sfGlobalValidationSummary));
                        var summaryUL = currentSummary.children('.' + sfSummaryErrorClass);
                        if (summaryUL.length == 0)
                            currentSummary.append('<ul class="' + sfSummaryErrorClass + '">\n' + partialErrors + '</ul>');
                        else
                            summaryUL.append(partialErrors);
                    }
                }
            }
        }
    };

    function TrySave(_valOptions) {
        var validator = new Validator(_valOptions);
        return validator.trySave();
    };

    function Validate(_valOptions) {
        var validator = new Validator(_valOptions);
        return validator.validate();
    };

    //PartialValidatorOptions = ValidatorOptions + type + id + onFinish
    var PartialValidator = function (_pvalOptions) {
        var self = this;
        Validator.call(this, $.extend({
            parentDiv: _pvalOptions.prefix.compose("panelPopup"),
            type: null,
            id: null
        }, _pvalOptions));

        this.savingControllerUrl = this.valOptions.controllerUrl || "Signum/TrySavePartial";
        this.validatingControllerUrl = this.valOptions.controllerUrl || "Signum/ValidatePartial";

        this.constructRequestDataForSaving = function () {
            log("PartialValidator constructRequestDataForSaving");
            var formChildren = $("#" + this.valOptions.parentDiv + " *, #" + sfTabId).add(GetSFInfoParams(this.valOptions.prefix));
            formChildren = formChildren.not(".searchControl *, #" + sfReactive);
            var requestData = [];
            requestData.push(formChildren.serialize());
            if (!empty(this.valOptions.prefix))
                requestData.push(qp(sfPrefix, this.valOptions.prefix));

            if (formChildren.filter(this.pf(sfRuntimeInfo)).length == 0)
                requestData.push(
                qp(this.valOptions.prefix.compose(sfRuntimeInfo),
                new RuntimeInfo(this.valOptions.prefix).createValue(this.valOptions.type, '', 'n', '')));

            if (!empty(this.valOptions.prefixToIgnore))
                requestData.push(qp(sfPrefixToIgnore, this.valOptions.prefixToIgnore));

            if (!empty(this.valOptions.requestExtraJsonData)) {
                for (var key in this.valOptions.requestExtraJsonData) {
                    requestData.push(qp(key, this.valOptions.requestExtraJsonData[key]));
                }
            }
            return requestData.join('');
        };

        this.createValidatorResult = function (r) {
            var validatorResult = {
                "modelState": r["ModelState"],
                "isValid": this.isValid(r["ModelState"]),
                "newToStr": r[sfToStr],
                "newLink": r[sfLink]
            };
            return validatorResult;
        };

        this.trySave = function () {
            log("PartialValidator trySave");
            //        if (empty(this.valOptions.type))
            //            throw "Type must be specified in PartialValidatorOptions for TrySavePartial";
            NotifyInfo(lang['saving']);
            var validatorResult = null;
            var self = this;
            SF.ajax({
                type: "POST",
                url: this.savingControllerUrl,
                async: false,
                data: this.constructRequestDataForSaving(),
                dataType: "JSON",
                success: function (result) {
                    validatorResult = self.createValidatorResult(result);
                    self.showErrors(validatorResult.modelState);
                }
            });
            if (validatorResult != null && validatorResult.isValid) {
                NotifyInfo(lang['saved'], 2000);
            }
            else
                NotifyInfo(lang['error'], 2000);
            return validatorResult;
        };

        this.constructRequestDataForValidating = function () {
            log("PartialValidator constructRequestDataForValidating");
            //var isReactive = $('#' + sfReactive).length > 0;
            //var formChildren = isReactive ? $("form *") : $("#" + this.valOptions.parentDiv + " *, #" + sfTabId + ", #" + sfReactive);
            var formChildren = empty(this.valOptions.parentDiv) ?
                               $("form :input, #" + sfTabId) :
                               $("#" + this.valOptions.parentDiv + " :input, #" + sfTabId);
            formChildren = formChildren.not(".searchControl :input, #" + sfReactive);

            var requestData = formChildren.serialize();

            var myRuntimeInfoKey = this.valOptions.prefix.compose(sfRuntimeInfo);
            if (formChildren.filter("[name=" + myRuntimeInfoKey + "]").length == 0) {
                var info = new RuntimeInfo(this.valOptions.prefix);
                var infoField = info.find();
                if (empty(this.valOptions.type)) {
                    if (empty(info.runtimeType()))
                        requestData +=
                        qp(myRuntimeInfoKey,
                        info.createValue(StaticInfoFor(this.valOptions.prefix).staticType(), info.id(), 'n', ''));
                    else
                        requestData += qp(myRuntimeInfoKey, infoField.val());
                }
                else {
                    if (infoField.length == 0)
                        requestData += qp(myRuntimeInfoKey, info.createValue(this.valOptions.type, empty(!this.valOptions.id) ? this.valOptions.id : '', 'n', ''));
                    else {
                        var infoVal = infoField.val();
                        var index = infoVal.indexOf(";"); 			//TODO: Split ;
                        var index2 = infoVal.indexOf(";", index + 1);
                        var index3 = infoVal.indexOf(";", index2 + 1);
                        var currTicks = (($('#' + sfReactive).length > 0) ? new Date().getTime() : "");
                        var mixedVal = this.valOptions.type + ";" + (!empty(this.valOptions.id) ? this.valOptions.id : '') + infoVal.substring(index2, index3 + 1) + currTicks;

                        requestData += qp(myRuntimeInfoKey, mixedVal);
                    }
                }
            }

            requestData += qp(sfPrefix, this.valOptions.prefix);

            if (!empty(this.valOptions.prefixToIgnore))
                requestData += qp(sfPrefixToIgnore, this.valOptions.prefixToIgnore);

            if (!empty(this.valOptions.requestExtraJsonData)) {
                for (var key in this.valOptions.requestExtraJsonData) {
                    requestData += qp(key, this.valOptions.requestExtraJsonData[key]);
                }
            }

            return requestData;
        };

        this.validate = function () {
            log("PartialValidator validate");
            var validatorResult = null;
            var self = this;
            SF.ajax({
                type: "POST",
                url: this.validatingControllerUrl,
                async: false,
                data: this.constructRequestDataForValidating(),
                dataType: "json",
                success: function (result) {
                    validatorResult = self.createValidatorResult(result);
                    self.showErrors(validatorResult.modelState);
                }
            });
            return validatorResult;
        };
    };

    PartialValidator.prototype = new Validator();

    function TrySavePartial(_partialValOptions) {
        var validator = new PartialValidator(_partialValOptions);
        return validator.trySave();
    };

    function ValidatePartial(_partialValOptions) {
        var validator = new PartialValidator(_partialValOptions);
        return validator.validate();
    };

    function EntityIsValid(validationOptions, onSuccess) {
        log("Validator EntityIsValid");

        NotifyInfo(lang['validating']);

        var isValid = null;
        if (empty(validationOptions.prefix))
            isValid = new Validator(validationOptions).validate();
        else {
            var info = RuntimeInfoFor(validationOptions.prefix);
            isValid = new PartialValidator($.extend(validationOptions, { type: info.runtimeType(), id: info.id() })).validate().isValid;
        }

        if (isValid) {
            NotifyInfo('', 1);
            if (onSuccess != null)
                onSuccess();
        }
        else {
            NotifyInfo(lang['error'], 2000);
            window.alert(lang['popupErrorsStop']);
        }
    };
}