"use strict";

SF.registerModule("Validator", function () {

    SF.Validator = function (_valOptions) {
        this.valOptions = $.extend({
            prefix: "",
            controllerUrl: null,
            showInlineErrors: true,
            fixedInlineErrorText: "*", //Set to "" for it to be populated from ModelState error messages
            parentDiv: "",
            requestExtraJsonData: null,
            ajaxError: null,
            errorSummaryId: null
        }, _valOptions);
    };

    SF.Validator.inputErrorClass = "input-validation-error";

    SF.Validator.prototype = {

        fieldErrorClass: "sf-field-validation-error",
        inputErrorClass: SF.Validator.inputErrorClass,
        summaryErrorClass: "validation-summary-errors",
        inlineErrorVal: "inlineVal",
        globalErrorsKey: "sfGlobalErrors",
        globalValidationSummary: "sfGlobalValidationSummary",

        pf: function (s) {
            return "#" + SF.compose(this.valOptions.prefix, s);
        },

        constructRequestData: function () {
            SF.log("Validator constructRequestData");
            var formChildren = SF.isEmpty(this.valOptions.parentDiv) ?
            $("form :input") : $("#" + this.valOptions.parentDiv + " :input")
                .add("#" + SF.Keys.tabId)
                .add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");

            var searchControlInputs = $(".sf-search-control :input");
            formChildren = formChildren.not(searchControlInputs);

            var serializer = new SF.Serializer();
            serializer.add(formChildren.serialize())
                .add("prefix", this.valOptions.prefix)
                .add(this.valOptions.requestExtraJsonData);

            return serializer.serialize();
        },

        trySave: function () {
            SF.log("Validator trySave");
            var returnValue = false;
            var self = this;
            $.ajax({
                url: this.valOptions.controllerUrl || SF.Urls.trySave,
                async: false,
                data: this.constructRequestData(),
                success: function (msg) {
                    if (typeof msg === "object") {
                        if (msg.result != "ModelState") {
                            throw "Validator trySave: Incorrect result type " + msg.result;
                        }
                        var modelState = msg.ModelState;
                        returnValue = self.showErrors(modelState, true);
                        SF.Notify.error(lang.signum.error, 2000);
                    }
                    else {
                        if (SF.isEmpty(self.valOptions.parentDiv)) {
                            $("#content").html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                            SF.triggerNewContent($("#content"));
                        }
                        else {
                            $("#" + self.valOptions.parentDiv).html(msg.substring(msg.indexOf("<form"), msg.indexOf("</form>") + 7));
                            SF.triggerNewContent($("#" + self.valOptions.parentDiv));
                        }
                        returnValue = true;
                        SF.Notify.info(lang.signum.saved, 2000);
                    }
                }
            });
            return returnValue;
        },

        validate: function () {
            SF.log("Validator validate");
            var returnValue = false;
            var self = this;
            $.ajax({
                url: this.valOptions.controllerUrl || SF.Urls.validate,
                async: false,
                data: this.constructRequestData(),
                success: function (msg) {
                    if (typeof msg === "object") {
                        if (msg.result != "ModelState") {
                            throw "Validator validate: Incorrect result type " + msg.result;
                        }
                        var modelState = msg.ModelState;
                        returnValue = self.showErrors(modelState, true);
                    }
                    else {
                        returnValue = true;
                    }
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    if (self.ajaxError != null) {
                        self.ajaxError(xhr, ajaxOptions, thrownError);
                    }
                }
            });
            return returnValue;
        },

        isValid: function (modelState) {
            SF.log("Validator isValid");
            var controlID;
            for (controlID in modelState) {
                if (modelState.hasOwnProperty(controlID) && modelState[controlID].length) {
                    return false; //Stop as soon as I find an error
                }
            }
            return true;
        },

        showErrors: function (modelState, showPathErrors) {
            SF.log("Validator showErrors");
            //Remove previous errors
            $('.' + this.fieldErrorClass).remove()
            $('.' + this.inputErrorClass).removeClass(this.inputErrorClass);
            $('.' + this.summaryErrorClass).remove();

            var allErrors = [];
            var inlineErrorStart = '<span class="' + this.fieldErrorClass + '">';
            var inlineErrorEnd = "</span>";

            var controlID;
            for (controlID in modelState) {
                if (modelState.hasOwnProperty(controlID)) {
                    var errorsArray = modelState[controlID],
                        errorMessage = [],
                        partialErrors = [],
                        j;

                    for (j = 0; j < errorsArray.length; j++) {
                        errorMessage.push(errorsArray[j]);
                        partialErrors.push("<li>" + errorsArray[j] + "</li>");
                        allErrors.push(partialErrors);
                    }
                    var fixedInlineErrorText = this.valOptions.fixedInlineErrorText;

                    if (controlID != this.globalErrorsKey && controlID != "") {
                        var $control = $('#' + controlID);
                        $control.addClass(this.inputErrorClass);
                        $('#' + SF.compose(controlID, SF.Keys.toStr) + ',#' + SF.compose(controlID, SF.Keys.link)).addClass(this.inputErrorClass);
                        if (this.valOptions.showInlineErrors && $control.hasClass(this.inlineErrorVal)) {
                            if ($control.next().hasClass("ui-datepicker-trigger")) {
                                if (SF.isEmpty(fixedInlineErrorText)) {
                                    $control.next().after(inlineErrorStart + errorMessage.join('') + inlineErrorEnd);
                                } else {
                                    $control.next().after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                                }
                            }
                            else {
                                if (SF.isEmpty(fixedInlineErrorText)) {
                                    $control.after(inlineErrorStart + errorMessage.join('') + inlineErrorEnd);
                                }
                                else {
                                    $control.after(inlineErrorStart + fixedInlineErrorText + inlineErrorEnd);
                                }
                            }
                        }
                    }
                    this.setPathErrors(controlID, partialErrors.join(''), showPathErrors);
                }
            }

            if (allErrors.length) {
                SF.log("(Errors Validator showErrors): " + allErrors.join(''));
                return false;

            }
            return true;
        },

        //This will mark all the path with the error class, and it will also set summary error entries for the controls more inner than the current one
        setPathErrors: function (controlID, partialErrors, showPathErrors) {
            var pathPrefixes = (controlID != this.globalErrorsKey) ? SF.getPathPrefixes(controlID) : new Array("");
            for (var i = 0, l = pathPrefixes.length; i < l; i++) {
                var currPrefix = pathPrefixes[i];
                if (currPrefix != undefined) {
                    var isEqual = (currPrefix === this.valOptions.prefix);
                    var isMoreInner = !isEqual && (currPrefix.indexOf(this.valOptions.prefix) > -1);
                    if (showPathErrors || isMoreInner) {
                        $('#' + SF.compose(currPrefix, SF.Keys.toStr)).addClass(this.inputErrorClass);
                        $('#' + SF.compose(currPrefix, SF.Keys.link)).addClass(this.inputErrorClass);
                    }
                    if ((isMoreInner || isEqual) && $('#' + SF.compose(currPrefix, this.globalValidationSummary)).length > 0 && !SF.isEmpty(partialErrors)) {
                        var currentSummary = !SF.isEmpty(this.valOptions.errorSummaryId) ?
                                             $('#' + this.valOptions.errorSummaryId) :
                                             SF.isEmpty(this.valOptions.parentDiv) ?
                                                $('#' + SF.compose(currPrefix, this.globalValidationSummary)) :
                                                $('#' + this.valOptions.parentDiv + " #" + SF.compose(currPrefix, this.globalValidationSummary));
                        var summaryUL = currentSummary.children('.' + this.summaryErrorClass);
                        if (summaryUL.length === 0) {
                            currentSummary.append('<ul class="' + this.summaryErrorClass + '">\n' + partialErrors + '</ul>');
                        }
                        else {
                            summaryUL.append(partialErrors);
                        }
                    }
                }
            }
        }
    };

    //PartialValidatorOptions = ValidatorOptions + type + id + onFinish
    SF.PartialValidator = function (_pvalOptions) {
        var self = this;
        SF.Validator.call(this, $.extend({
            parentDiv: SF.compose(_pvalOptions.prefix, "panelPopup"),
            type: null,
            id: null
        }, _pvalOptions));

        this.checkOrAddRuntimeInfo = function ($formChildren, serializer) {
            //Check runtimeInfo present => if it's a popup from a LineControl it will not be
            var myRuntimeInfoKey = SF.compose(this.valOptions.prefix, SF.Keys.runtimeInfo);
            if ($formChildren.filter("#" + myRuntimeInfoKey).length == 0) {
                var $mainControl = $(".sf-main-control[data-prefix=" + this.valOptions.prefix + "]");
                serializer.add(myRuntimeInfoKey, $mainControl.data("runtimeinfo"));
            }
        };

        this.constructRequestDataForSaving = function () {
            SF.log("PartialValidator constructRequestDataForSaving");
            var prefix = this.valOptions.prefix;
            var formChildren = $("#" + this.valOptions.parentDiv + " :input")
                .add("#" + SF.Keys.tabId)
                .add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]")
                .add(SF.getInfoParams(prefix));
            formChildren = formChildren.not(".sf-search-control *");

            var serializer = new SF.Serializer();
            serializer.add(formChildren.serialize());

            if (!SF.isEmpty(prefix)) {
                serializer.add("prefix", prefix);
            }

            this.checkOrAddRuntimeInfo(formChildren, serializer);

            serializer.add(this.valOptions.requestExtraJsonData);

            return serializer.serialize();
        };

        this.createValidatorResult = function (r) {
            var validatorResult = {
                "modelState": r["ModelState"],
                "isValid": this.isValid(r["ModelState"]),
                "newToStr": r[SF.Keys.toStr],
                "newLink": r[SF.Keys.link]
            };
            return validatorResult;
        };

        this.trySave = function () {
            SF.log("PartialValidator trySave");
            var validatorResult = null;
            var self = this;
            $.ajax({
                url: this.valOptions.controllerUrl || SF.Urls.trySavePartial,
                async: false,
                data: this.constructRequestDataForSaving(),
                success: function (result) {
                    validatorResult = self.createValidatorResult(result);
                    self.showErrors(validatorResult.modelState);
                }
            });
            if (validatorResult != null && validatorResult.isValid) {
                SF.Notify.info(lang.signum.saved, 2000);
            }
            else
                SF.Notify.error(lang.signum.error, 2000);
            return validatorResult;
        };

        this.constructRequestDataForValidating = function () {
            SF.log("PartialValidator constructRequestDataForValidating");

            //Send main form (or parent popup) to be able to construct a typecontext if EmbeddedEntity
            var staticInfo = SF.StaticInfo(this.valOptions.prefix);
            var isEmbedded = false;
            if (staticInfo.find().length == 0 && !SF.isEmpty(this.valOptions.prefix)) { //If List => use parent
                var lastPrefix = this.valOptions.prefix.substr(0, this.valOptions.prefix.lastIndexOf(SF.Keys.separator));
                staticInfo = SF.StaticInfo(lastPrefix);
            }
            if (staticInfo.find().length > 0 && staticInfo.isEmbedded()) {
                isEmbedded = true;
            }

            var formChildren = null;
            var parentPrefix;
            if (isEmbedded) {
                var myRuntimeInfoKey = SF.compose(this.valOptions.prefix, SF.Keys.runtimeInfo);
                var pathPrefixes = SF.getPathPrefixes(this.valOptions.prefix);
                for (var i = pathPrefixes.length - 1; 0 < i; i--) {
                    var parentPopup = $(".sf-main-control[data-prefix='" + pathPrefixes[i] + "']");
                    if (parentPopup.length > 0) {
                        parentPrefix = pathPrefixes[i];
                        formChildren = parentPopup.find(":input").add($("#" + SF.Keys.tabId + ", input:hidden[name=" + SF.Keys.antiForgeryToken + "]"));
                    }
                }
                if (typeof (parentPrefix) == "undefined") {
                    formChildren = $("form :input:not('#" + myRuntimeInfoKey + "')")
                        .add("#" + SF.Keys.tabId)
                        .add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");
                }
                if (!SF.isEmpty(this.valOptions.parentDiv) && $("form").find("#" + this.valOptions.parentDiv).length == 0) {
                    formChildren = formChildren.add($("#" + this.valOptions.parentDiv + " :input:not('#" + myRuntimeInfoKey + "')"));
                }
            }
            else if (!SF.isEmpty(this.valOptions.parentDiv)) {
                if (formChildren == null) {
                    formChildren = $("#" + this.valOptions.parentDiv + " :input")
                        .add("#" + SF.Keys.tabId)
                        .add("input:hidden[name=" + SF.Keys.antiForgeryToken + "]");
                }
                else {
                    formChildren = formChildren.add($("#" + this.valOptions.parentDiv + " :input"));
                }
            }
            formChildren = formChildren.not(".sf-search-control :input");

            var serializer = new SF.Serializer().add(formChildren.serialize());

            this.checkOrAddRuntimeInfo(formChildren, serializer);

            serializer.add("prefix", this.valOptions.prefix);
            serializer.add(this.valOptions.requestExtraJsonData);

            if (typeof (parentPrefix) != "undefined") {
                serializer.add("parentPrefix", parentPrefix);

                if (formChildren.filter("#" + SF.compose(parentPrefix, "sfRuntimeInfo")).length == 0) {
                    var $parentMainControl = $(".sf-main-control[data-prefix=" + parentPrefix + "]");
                    serializer.add(SF.compose(parentPrefix, "sfRuntimeInfo"), $parentMainControl.data("runtimeinfo"));
                }
            }

            return serializer.serialize();
        };

        this.validate = function () {
            SF.log("PartialValidator validate");
            var validatorResult = null;
            var self = this;
            $.ajax({
                url: this.valOptions.controllerUrl || SF.Urls.validatePartial,
                async: false,
                data: this.constructRequestDataForValidating(),
                success: function (result) {
                    validatorResult = self.createValidatorResult(result);
                    self.showErrors(validatorResult.modelState);
                }
            });
            return validatorResult;
        };
    };

    SF.PartialValidator.prototype = new SF.Validator();

    SF.EntityIsValid = function (validationOptions, onSuccess, sender) {
        SF.log("Validator EntityIsValid");

        var isValid;

        if (SF.isEmpty(validationOptions.prefix)) {
            isValid = new SF.Validator(validationOptions).validate();
        }
        else {
            isValid = new SF.PartialValidator(validationOptions).validate().isValid;
        }

        if (isValid) {
            if (onSuccess != null) {
                if (typeof sender != "undefined") {
                    onSuccess.call(sender);
                }
                else {
                    onSuccess();
                }
            }
        }
        else {
            SF.Notify.error(lang.signum.error, 2000);
            alert(lang.signum.popupErrorsStop);
        }
    };
});
