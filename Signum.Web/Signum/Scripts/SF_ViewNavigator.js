"use strict";

SF.registerModule("ViewNavigator", function () {

    SF.ViewNavigator = function (_viewOptions) {
        this.viewOptions = $.extend({
            containerDiv: null,
            onOk: null,
            onOkClosed: null,
            onCancelled: null,
            onLoaded: null,
            controllerUrl: null,
            type: null,
            id: null,
            prefix: "",
            partialViewName: null,
            requestExtraJsonData: null
        }, _viewOptions);

        this.backup = ""; //jquery object with the cloned original elements
    };

    SF.ViewNavigator.prototype = {

        tempDivId: function () {
            return SF.compose(this.viewOptions.prefix, "Temp");
        },

        viewOk: function () {
            SF.log("ViewNavigator viewOk");
            if (SF.isEmpty(this.viewOptions.containerDiv))
                throw "No containerDiv was specified to Navigator on viewOk mode";
            if (this.isLoaded())
                return this.showViewOk(null);
            var self = this;
            this.callServer(function (controlHtml) { self.showViewOk(controlHtml); });
        },

        createOk: function () {
            SF.log("ViewNavigator createOk");
            if (!SF.isEmpty(this.viewOptions.containerDiv))
                throw "ContainerDiv cannot be specified to Navigator on createOk mode";
            var self = this;
            this.callServer(function (controlHtml) { self.showCreateOk(controlHtml); });
        },

        viewEmbedded: function () {
            SF.log("ViewNavigator viewEmbedded");
            if (SF.isEmpty(this.viewOptions.containerDiv))
                throw "No containerDiv was specified to Navigator on viewEmbedded mode";
            var self = this;
            this.callServer(function (controlHtml) { $('#' + self.viewOptions.containerDiv).html(controlHtml); });
        },

        createEmbedded: function (onHtmlReceived) {
            SF.log("ViewNavigator createEmbedded");
            if (!SF.isEmpty(this.viewOptions.containerDiv))
                throw "ContainerDiv cannot be specified to Navigator on createEmbedded mode";
            this.callServer(function (controlHtml) { onHtmlReceived(controlHtml) });
        },

        viewSave: function (html) {
            SF.log("ViewNavigator viewSave");
            if (SF.isEmpty(this.viewOptions.containerDiv))
                throw "No ContainerDiv was specified to Navigator on viewSave mode";
            if ($('#' + this.viewOptions.containerDiv).length == 0)
                $("body").append(SF.hiddenDiv(this.viewOptions.containerDiv, ""));
            if (!SF.isEmpty(html)) {
                $('#' + this.viewOptions.containerDiv).html(html);
            }
            if (this.isLoaded())
                return this.showViewSave();
            else {
                if (SF.isEmpty(this.viewOptions.type) && new SF.RuntimeInfo(this.viewOptions.prefix).find().length == 0)
                    throw "Type must be specified to Navigator on viewSave mode";
                var self = this;
                this.callServer(function (controlHtml) { self.showViewSave(controlHtml); });
            }
        },

        createSave: function (saveUrl) {
            SF.log("ViewNavigator createSave");
            if (!SF.isEmpty(this.viewOptions.containerDiv))
                throw "ContainerDiv cannot be specified to Navigator on createSave mode";
            if (SF.isEmpty(this.viewOptions.type))
                throw "Type must be specified to Navigator on createSave mode";
            var self = this;
            this.viewOptions.prefix = SF.compose("New", this.viewOptions.prefix);
            this.callServer(function (controlHtml) { self.showCreateSave(controlHtml, saveUrl); });
        },

        navigate: function () {
            SF.log("ViewNavigator navigate");
            if (!SF.isEmpty(this.viewOptions.containerDiv))
                throw "ContainerDiv cannot be specified to Navigator on Navigate mode";
            if (SF.isEmpty(this.viewOptions.type))
                throw "Type must be specified to Navigator on Navigate mode";
            var self = this;
            this.callServer(function (url) { /*$.ajaxPrefilter will handle the redirect*/ }); 
        },

        isLoaded: function () {
            SF.log("ViewNavigator isLoaded");
            return !SF.isEmpty($('#' + this.viewOptions.containerDiv).html());
        },

        showViewOk: function (newHtml) {
            SF.log("ViewNavigator showViewOk");

            if (SF.isEmpty(newHtml))
                newHtml = $('#' + this.viewOptions.containerDiv).html(); //preloaded

            //Backup current Html (for cancel scenarios)
            this.backup = SF.cloneContents(this.viewOptions.containerDiv);
            $('#' + this.viewOptions.containerDiv).html(''); //avoid id-collision

            $("body").append(SF.hiddenDiv(this.tempDivId(), newHtml));
            SF.triggerNewContent($("#" + this.tempDivId()));

            var self = this;
            $("#" + this.tempDivId()).popup({
                onOk: function () { self.onViewOk() },
                onCancel: function () { self.onViewCancel() }
            });
        },

        showViewSave: function (newHtml) {
            SF.log("ViewNavigator showViewSave");
            if (!SF.isEmpty(newHtml)) {
                $('#' + this.viewOptions.containerDiv).html(newHtml);
            }

            SF.triggerNewContent($("#" + this.viewOptions.containerDiv));

            var self = this;
            $("#" + this.viewOptions.containerDiv).popup({
                onOk: function () { self.onCreateSave() },
                onCancel: function () { self.onCreateCancel() }
            });
        },

        showCreateOk: function (newHtml) {
            SF.log("ViewNavigator showCreateOk");
            var tempDivId = this.tempDivId();

            if (!SF.isEmpty(newHtml)) {
                $("body").append(SF.hiddenDiv(tempDivId, newHtml));
            }

            SF.triggerNewContent($("#" + tempDivId));

            var self = this;
            $("#" + tempDivId).popup({
                onOk: function () { self.onCreateOk() },
                onCancel: function () { self.onCreateCancel() }
            });

            if (this.viewOptions.onLoaded != null)
                this.viewOptions.onLoaded(this.tempDivId());
        },

        showCreateSave: function (newHtml, saveUrl) {
            SF.log("ViewNavigator showCreateSave");
            var tempDivId = this.tempDivId();

            if (!SF.isEmpty(newHtml)) {
                $("body").append(SF.hiddenDiv(tempDivId, newHtml));
            }

            SF.triggerNewContent($("#" + tempDivId));

            var self = this;
            $("#" + tempDivId).popup({
                onOk: function () { self.onCreateSave(saveUrl) },
                onCancel: function () { self.onCreateCancel() }
            });

            if (this.viewOptions.onLoaded != null)
                this.viewOptions.onLoaded(this.tempDivId());
        },

        constructRequestData: function () {
            SF.log("ViewNavigator constructRequestData");

            var options = this.viewOptions,
                serializer = new SF.Serializer()
                                .add({
                                    runtimeType: options.type,
                                    id: options.id,
                                    prefix: options.prefix
                                });

            if (!SF.isEmpty(options.partialViewName)) //Send specific partialview if given
                serializer.add("url", options.partialViewName);

            serializer.add(options.requestExtraJsonData);
            return serializer.serialize();
        },

        callServer: function (onSuccess) {
            SF.log("ViewNavigator callServer");
            $.ajax({
                url: this.viewOptions.controllerUrl,
                data: this.constructRequestData(),
                async: false,
                success: function (newHtml) {
                    onSuccess(newHtml);
                }
            });
        },

        onViewOk: function () {
            SF.log("ViewNavigator onViewOk");
            var doDefault = (this.viewOptions.onOk != null) ? this.viewOptions.onOk() : true;
            if (doDefault != false) {
                $('#' + this.tempDivId()).popup('destroy');
                this.backup = SF.cloneContents(this.tempDivId());
                $('#' + this.tempDivId()).remove();
                $('#' + this.viewOptions.containerDiv).html(this.backup);

                if (this.viewOptions.onOkClosed != null)
                    this.viewOptions.onOkClosed();
            }
        },

        onViewCancel: function () {
            SF.log("ViewNavigator onViewCancel");
            $('#' + this.tempDivId()).remove();
            var $popupPanel = $('#' + this.viewOptions.containerDiv);
            $popupPanel.html(this.backup);
            this.backup = "";

            if (this.viewOptions.onCancelled != null)
                this.viewOptions.onCancelled();
        },

        onCreateOk: function () {
            SF.log("ViewNavigator onCreateOk");
            var doDefault = (this.viewOptions.onOk != null) ? this.viewOptions.onOk(SF.cloneContents(this.tempDivId())) : true;
            if (doDefault != false) {
                $('#' + this.tempDivId()).remove();
                if (this.viewOptions.onOkClosed != null)
                    this.viewOptions.onOkClosed();
            }
        },

        onCreateSave: function (saveUrl) {
            SF.log("ViewNavigator onCreateSave");
            var doDefault = (this.viewOptions.onOk != null) ? this.viewOptions.onOk(this.tempDivId()) : true;
            if (doDefault != false) {
                var validatorResult = new SF.PartialValidator({ prefix: this.viewOptions.prefix, type: this.viewOptions.type, controllerUrl: saveUrl }).trySave();
                if (!validatorResult.isValid) {
                    window.alert(lang.signum.popupErrorsStop);
                    return;
                }
                if (SF.isEmpty(this.viewOptions.containerDiv))
                    $('#' + this.tempDivId()).remove();
                else
                    $('#' + this.viewOptions.containerDiv).remove();
                if (this.viewOptions.onOkClosed != null)
                    this.viewOptions.onOkClosed();
            }
        },

        onCreateCancel: function () {
            SF.log("ViewNavigator onCreateCancel");
            if (SF.isEmpty(this.viewOptions.containerDiv))
                $('#' + this.tempDivId()).remove();
            else
                $('#' + this.viewOptions.containerDiv).remove();
            if (this.viewOptions.onCancelled != null)
                this.viewOptions.onCancelled();
        }
    }

    SF.closePopup = function (prefix) {
        $('#' + SF.compose(prefix, "panelPopup")).closest(".ui-dialog-content,.ui-dialog").remove();
    }

    /* chooserOptions: controllerUrl & types*/
    SF.openTypeChooser = function (prefix, onTypeChosen, chooserOptions) {
        SF.log("openTypeChooser");
        var tempDivId = SF.compose(prefix, "Temp");
        $.ajax({
            url: chooserOptions.controllerUrl,
            data: { prefix: tempDivId, types: (SF.isEmpty(chooserOptions.types) ? SF.StaticInfo(prefix).types() : chooserOptions.types) },
            async: false,
            success: function (chooserHTML) {
                $("body").append(SF.hiddenDiv(tempDivId, chooserHTML));
                SF.triggerNewContent($("#" + tempDivId));
                //Set continuation for each type button
                $('#' + tempDivId + " :button").each(function () {
                    $('#' + this.id).unbind('click').click(function () {
                        var option = this.id;
                        $('#' + tempDivId).remove();
                        onTypeChosen(option);
                    });
                });

                $("#" + tempDivId).popup({ onCancel: function () {
                    $('#' + tempDivId).remove();
                    if (onCancelled != null)
                        onCancelled();
                }
                });
            }
        });
    }

    /* chooserOptions */
    /* ids: List of ids */
    /* title: Window title */
    SF.openChooser = function (_prefix, onOptionClicked, jsonOptionsListFormat, onCancelled, chooserOptions) {
        SF.log("openChooser");
        //Construct popup
        var tempDivId = SF.compose(_prefix, "Temp");
        var requestData = "prefix=" + tempDivId;
        if (SF.isEmpty(jsonOptionsListFormat)) {
            throw "chooser options must be provider. Use openTypeChooser for automatic type chooser";
        }
        else {
            for (var i = 0; i < jsonOptionsListFormat.length; i++) {
                requestData += "&buttons=" + jsonOptionsListFormat[i];  //This will Bind to the List<string> "buttons"
                if (chooserOptions && chooserOptions.ids != null) requestData += "&ids=" + chooserOptions.ids[i];  //This will Bind to the List<string> "ids"            
            }
        }
        if (chooserOptions && chooserOptions.title) requestData += "&title=" + chooserOptions.title;

        $.ajax({
            url: chooserOptions.controllerUrl,
            data: requestData,
            async: false,
            success: function (chooserHTML) {
                $("body").append(SF.hiddenDiv(tempDivId, chooserHTML));
                SF.triggerNewContent($("#" + tempDivId));
                //Set continuation for each type button
                $('#' + tempDivId + " :button").each(function () {
                    $('#' + this.id).unbind('click').click(function () {
                        var option = $(this).attr("data-id");
                        $('#' + tempDivId).remove();
                        onOptionClicked(option);
                    });
                });

                $("#" + tempDivId).popup({ onCancel: function () {
                    $('#' + tempDivId).remove();
                    if (onCancelled != null)
                        onCancelled();
                }
                });
            }
        });
    }

    SF.relatedEntityCreate = function (viewOptions) {
        var info = new SF.RuntimeInfo('');
        var extraJson = {
            sfIdRelated: info.id(),
            sfRuntimeTypeRelated: info.runtimeType()
        };

        var navigator = new SF.ViewNavigator($.extend(viewOptions, { requestExtraJsonData: extraJson }));
        navigator.createSave();
    }
});
