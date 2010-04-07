var Validator = function(_valOptions) {
    this.valOptions = $.extend({
        prefix: "",
        controllerUrl: null,
        prefixToIgnore: null,
        showInlineErrors: true,
        fixedInlineErrorText: "*", //Set to "" for it to be populated from ModelState error messages
        parentDiv: "",
        requestExtraJsonData: null,
        onSuccess: null
    }, _valOptions);

    this.savingControllerUrl = (empty(this.valOptions.controllerUrl)) ? "Signum/TrySave" : this.valOptions.controllerUrl;
    this.validatingControllerUrl = (empty(this.valOptions.controllerUrl)) ? "Signum/Validate" : this.valOptions.controllerUrl;
};

Validator.prototype = {

    pf: function(s) {
        return "#" + this.valOptions.prefix + s;
    },

    constructRequestData: function() {
        log("Validator constructRequestData");
        var formChildren = empty(this.valOptions.parentDiv) ? $("form *") : $("#" + this.valOptions.parentDiv + " *, #" + sfTabId + ", #" + sfReactive);
        formChildren = formChildren.not(".searchControl *");
        var requestData = formChildren.serialize();

        if (!empty(this.valOptions.prefixToIgnore))
            requestData += qp(sfPrefixToIgnore, this.valOptions.prefixToIgnore);

        if (!empty(this.valOptions.requestExtraJsonData)) {
            for (var key in this.valOptions.requestExtraJsonData) {
                requestData += qp(key, this.valOptions.requestExtraJsonData[key]);
            }
        }
        return requestData;
    },

    trySave: function() {
        log("Validator trySave");
        NotifyInfo(lang['saving']);
        var returnValue = false;
        var self = this;
        $.ajax({
            type: "POST",
            url: this.savingControllerUrl,
            async: false,
            data: this.constructRequestData(),
            success: function(msg) {
                if (msg.indexOf("ModelState") > 0) {
                    eval('var result=' + msg);
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
                    if (self.valOptions.onSuccess != null)
                        self.valOptions.onSuccess();
                }
            }
        });
        return returnValue;
    },

    validate: function() {
        log("Validator validate");
        var returnValue = false;
        var self = this;
        $.ajax({
            type: "POST",
            url: this.validatingControllerUrl,
            async: false,
            data: this.constructRequestData(),
            success: function(msg) {
                if (msg.indexOf("ModelState") > 0) {
                    eval('var result=' + msg);
                    var modelState = result["ModelState"];
                    returnValue = self.showErrors(modelState, true);
                }
                else {
                    returnValue = true;
                    if (self.valOptions.onSuccess != null)
                        self.valOptions.onSuccess();
                }
            }
        });
        return returnValue;
    },

    isValid: function(modelState) {
        log("Validator isValid");
        for (var controlID in modelState) {
            var errorsArray = modelState[controlID];
            for (var j = 0; j < errorsArray.length; j++)
                return false; //Stop as soon as I find an error
        }
        return true;
    },

    showErrors: function(modelState, showPathErrors) {
        log("Validator showErrors");
        //Remove previous errors
        $('.' + sfFieldErrorClass).replaceWith("");
        $('.' + sfInputErrorClass).removeClass(sfInputErrorClass);
        $('.' + sfSummaryErrorClass).replaceWith("");

        var allErrors = "";
        var inlineErrorStart = '&nbsp;<span class="' + sfFieldErrorClass + '">';
        var inlineErrorEnd = "</span>";

        for (var controlID in modelState) {
            var errorsArray = modelState[controlID];
            var errorMessage = "";
            var partialErrors = "";
            for (var j = 0; j < errorsArray.length; j++) {
                errorMessage += errorsArray[j];
                partialErrors += "<li>" + errorsArray[j] + "</li>\n";
                allErrors += partialErrors;
            }
            if (controlID != sfGlobalErrorsKey && controlID != "") {
                var control = $('#' + controlID);
                control.addClass(sfInputErrorClass);
                if (this.valOptions.showInlineErrors && control.hasClass(sfInlineErrorVal)) {
                    if (control.next().hasClass("ui-datepicker-trigger")) {
                        if (empty(this.valOptions.fixedInlineErrorText))
                            $('#' + controlID).next().after(inlineErrorStart + errorMessage + inlineErrorEnd);
                        else
                            $('#' + controlID).next().after(inlineErrorStart + this.valOptions.fixedInlineErrorText + inlineErrorEnd);
                    }
                    else {
                        if (empty(this.valOptions.fixedInlineErrorText))
                            $('#' + controlID).after(inlineErrorStart + errorMessage + inlineErrorEnd);
                        else
                            $('#' + controlID).after(inlineErrorStart + this.valOptions.fixedInlineErrorText + inlineErrorEnd);
                    }
                }
            }
            this.setPathErrors(controlID, partialErrors, showPathErrors);
        }

        if (allErrors != "") {
            log("(Errors Validator showErrors): " + allErrors);
            return false;

        }
        return true;
    },

    //This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
    setPathErrors: function(controlID, partialErrors, showPathErrors) {
        var pathPrefixes = (controlID != sfGlobalErrorsKey) ? GetPathPrefixes(controlID) : new Array("");
        for (var entry in pathPrefixes) {
            var currPrefix = pathPrefixes[entry];
            if (currPrefix != undefined) {
                var isEqual = (currPrefix == this.valOptions.prefix);
                var isMoreInner = !isEqual && (currPrefix.indexOf(this.valOptions.prefix) > -1);
                if (showPathErrors || isMoreInner) {
                    $('#' + currPrefix + sfToStr).addClass(sfInputErrorClass);
                    $('#' + currPrefix + sfLink).addClass(sfInputErrorClass);
                }
                if ((isMoreInner || isEqual) && $('#' + currPrefix + sfGlobalValidationSummary).length > 0 && !empty(partialErrors)) {
                    var currentSummary = $('#' + currPrefix + sfGlobalValidationSummary);
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
var PartialValidator = function(_pvalOptions) {
    var self = this;
    Validator.call(this, $.extend({
        parentDiv: _pvalOptions.prefix + "panelPopup",
        type: null,
        id: null
    }, _pvalOptions));

    this.savingControllerUrl = (empty(this.valOptions.controllerUrl)) ? "Signum/TrySavePartial" : this.valOptions.controllerUrl;
    this.validatingControllerUrl = (empty(this.valOptions.controllerUrl)) ? "Signum/ValidatePartial" : this.valOptions.controllerUrl;

    this.constructRequestDataForSaving = function() {
        log("PartialValidator constructRequestDataForSaving");
        var formChildren = $("#" + this.valOptions.parentDiv + " *, #" + sfTabId).add(GetSFInfoParams(this.valOptions.prefix));
        formChildren = formChildren.not(".searchControl *, #" + sfReactive);
        var requestData = formChildren.serialize();

        requestData += qp(sfPrefix, this.valOptions.prefix);

        if (formChildren.filter(this.pf(sfRuntimeInfo)).length == 0)
            requestData += qp(this.valOptions.prefix + sfRuntimeInfo, new RuntimeInfo(this.valOptions.prefix).createValue(this.valOptions.type, '', 'n', ''));

        if (!empty(this.valOptions.prefixToIgnore))
            requestData += qp(sfPrefixToIgnore, this.valOptions.prefixToIgnore);

        if (!empty(this.valOptions.requestExtraJsonData)) {
            for (var key in this.valOptions.requestExtraJsonData) {
                requestData += qp(key, this.valOptions.requestExtraJsonData[key]);
            }
        }
        return requestData;
    };

    this.createValidatorResult = function(ajaxResult) {
        log("PartialValidator createValidatorResult");
        eval('var result=' + ajaxResult);
        var validatorResult = new Object();
        validatorResult.modelState = result["ModelState"];
        validatorResult.isValid = this.isValid(validatorResult.modelState);
        validatorResult.newToStr = result[sfToStr];
        validatorResult.newLink = result[sfLink];
        return validatorResult;
    };

    this.trySave = function() {
        log("PartialValidator trySave");
        //        if (empty(this.valOptions.type))
        //            throw "Type must be specified in PartialValidatorOptions for TrySavePartial";
        NotifyInfo(lang['saving']);
        var validatorResult = null;
        var self = this;
        alert(this.savingControllerUrl);
        $.ajax({
            type: "POST",
            url: this.savingControllerUrl,
            async: false,
            data: this.constructRequestDataForSaving(),
            success: function(result) {
                validatorResult = self.createValidatorResult(result);
                self.showErrors(validatorResult.modelState);
            }
        });
        if (validatorResult != null && validatorResult.isValid) {
            NotifyInfo(lang['saved'], 2000);
            if (this.valOptions.onSuccess != null)
                this.valOptions.onSuccess();
        }
        else
            NotifyInfo(lang['error'], 2000);
        return validatorResult;
    };

    this.constructRequestDataForValidating = function() {
        log("PartialValidator constructRequestDataForValidating");
        var isReactive = ($('#' + sfReactive).length > 0);
        //var formChildren = isReactive ? $("form *") : $("#" + this.valOptions.parentDiv + " *, #" + sfTabId + ", #" + sfReactive);
        var formChildren = $("#" + this.valOptions.parentDiv + " *, #" + sfTabId);
        formChildren = formChildren.not(".searchControl *, #" + sfReactive);
        var requestData = formChildren.serialize();

        //if (!isReactive) {
        if (requestData.indexOf(this.valOptions.prefix + sfRuntimeInfo) < 0) {
            var info = new RuntimeInfo(this.valOptions.prefix);
            var infoField = info.find();
            if (empty(this.valOptions.type)) {
                if (empty(info.runtimeType()))
                    requestData += qp(this.valOptions.prefix + sfRuntimeInfo, info.createValue(StaticInfoFor(this.valOptions.prefix).staticType(), info.id(), 'n', ''));
                else
                    requestData += qp(this.valOptions.prefix + sfRuntimeInfo, infoField.val());
            }
            else {
                if (infoField.length == 0)
                    requestData += qp(this.valOptions.prefix + sfRuntimeInfo, info.createValue(this.valOptions.type, empty(!this.valOptions.id) ? this.valOptions.id : '', 'n', ''));
                else {
                    var infoVal = infoField.val();
                    var index = infoVal.indexOf(";");
                    var index2 = infoVal.indexOf(";", index + 1);
                    var index3 = infoVal.indexOf(";", index2 + 1);
                    var mixedVal = this.valOptions.type + ";" + (empty(!this.valOptions.id) ? this.valOptions.id : '') + infoVal.substring(index2, index3 + 1) + new Date().getTime();

                    requestData += qp(this.valOptions.prefix + sfRuntimeInfo, mixedVal);
                }
            }
        }
        //}

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

    this.validate = function() {
        log("PartialValidator validate");
        var validatorResult = null;
        var self = this;
        $.ajax({
            type: "POST",
            url: this.validatingControllerUrl,
            async: false,
            data: this.constructRequestDataForValidating(),
            success: function(result) {
                validatorResult = self.createValidatorResult(result);
                self.showErrors(validatorResult.modelState);
                if (validatorResult.isValid) {
                    if (self.valOptions.onSuccess != null)
                        self.valOptions.onSuccess();
                }
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