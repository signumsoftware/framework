var ViewNavigator = function(_viewOptions) {
    this.viewOptions = $.extend({
        containerDiv: null,
        onOk: null,
        onOkClosed: null,
        onCancelled: null,
        onLoaded: null,
        controllerUrl: "Signum/PopupView",
        type: null,
        id: null,
        prefix: "",
        partialViewName: null,
        requestExtraJsonData: null
    }, _viewOptions);
    
    this.backup = ""; //jquery object with the cloned original elements
};

ViewNavigator.prototype = {

    tempDivId: function() {
        return this.viewOptions.prefix + "Temp";
    },

    viewOk: function() {
        log("ViewNavigator viewOk");
        if (empty(this.viewOptions.containerDiv))
            throw "No containerDiv was specified to Navigator on viewOk mode";
        if (this.isLoaded())
            return this.showViewOk();
        var self = this;
        this.callServer(function(controlHtml) { self.showViewOk(controlHtml); });
    },

    createOk: function() {
        log("ViewNavigator createOk");
        if (!empty(this.viewOptions.containerDiv))
            throw "ContainerDiv cannot be specified to Navigator on createOk mode";
        var self = this;
        this.callServer(function(controlHtml) { self.showCreateOk(controlHtml); });
    },

    viewEmbedded: function() {
        log("ViewNavigator viewEmbedded");
        if (empty(this.viewOptions.containerDiv))
            throw "No containerDiv was specified to Navigator on viewEmbedded mode";
        var self = this;
        this.callServer(function(controlHtml) { $('#' + self.viewOptions.containerDiv).html(controlHtml); });
    },

    createEmbedded: function(onHtmlReceived) {
        log("ViewNavigator createEmbedded");
        if (!empty(this.viewOptions.containerDiv))
            throw "ContainerDiv cannot be specified to Navigator on createEmbedded mode";
        this.callServer(function(controlHtml) { onHtmlReceived(controlHtml) });
    },

    viewSave: function(html) {
        log("ViewNavigator viewSave");
        if (empty(this.viewOptions.containerDiv))
            throw "No ContainerDiv was specified to Navigator on viewSave mode";
        if ($('#' + this.viewOptions.containerDiv).length == 0)
            $('#divASustituir').after(hiddenDiv(this.viewOptions.containerDiv, ''));
        if (html != null)
            $('#' + this.viewOptions.containerDiv).html(html);
        if (this.isLoaded())
            return this.showViewSave();
        else {
            if (empty(this.viewOptions.type) && RuntimeInfoFor(this.viewOptions.prefix).find().length == 0)
                throw "Type must be specified to Navigator on viewSave mode";
            var self = this;
            this.callServer(function(controlHtml) { self.showViewSave(controlHtml); });
        }
    },

    createSave: function() {
        log("ViewNavigator createSave");
        if (!empty(this.viewOptions.containerDiv))
            throw "ContainerDiv cannot be specified to Navigator on createSave mode";
        if (empty(this.viewOptions.type))
            throw "Type must be specified to Navigator on createSave mode";
        var self = this;
        this.viewOptions.prefix += "_New";
        this.callServer(function(controlHtml) { self.showCreateSave(controlHtml); });
    },

    navigate: function() {
        log("ViewNavigator navigate");
        if (!empty(this.viewOptions.containerDiv))
            throw "ContainerDiv cannot be specified to Navigator on Navigate mode";
        if (empty(this.viewOptions.type))
            throw "Type must be specified to Navigator on Navigate mode";
        var self = this;
        this.callServer(function(url) { PostServer(url, self.viewOptions.requestExtraJsonData); });
    },

    isLoaded: function() {
        log("ViewNavigator isLoaded");
        return !empty($('#' + this.viewOptions.containerDiv).html());
    },

    showViewOk: function(newHtml) {
        log("ViewNavigator showViewOk");
        //Backup current Html (for cancel scenarios)
        this.backup = cloneContents(this.viewOptions.containerDiv);
        //Insert new Html in the appropriate place
        if (!empty(newHtml))
            $('#' + this.viewOptions.containerDiv).html(newHtml);

        new popup().show(this.viewOptions.containerDiv);
        var self = this;
        $('#' + this.viewOptions.prefix.compose(sfBtnOk)).unbind('click').click(function() { self.onViewOk(); });
        $('#' + this.viewOptions.prefix.compose(sfBtnCancel)).unbind('click').click(function() { self.onViewCancel(); });
    },

    showViewSave: function(newHtml) {
        log("ViewNavigator showViewSave");
        if (!empty(newHtml))
            $('#' + this.viewOptions.containerDiv).html(newHtml);

        new popup().show(this.viewOptions.containerDiv);
        var self = this;
        $('#' + this.viewOptions.prefix.compose(sfBtnOk)).unbind('click').click(function() { self.onCreateSave(); });
        $('#' + this.viewOptions.prefix.compose(sfBtnCancel)).unbind('click').click(function() { self.onCreateCancel(); });
    },

    showCreateOk: function(newHtml) {
        log("ViewNavigator showCreateOk");
        if (!empty(newHtml))
            $('#divASustituir').after(hiddenDiv(this.tempDivId(), newHtml));

        new popup().show(this.tempDivId());
        var self = this;
        $('#' + this.viewOptions.prefix.compose(sfBtnOk)).unbind('click').click(function() { self.onCreateOk(); });
        $('#' + this.viewOptions.prefix.compose(sfBtnCancel)).unbind('click').click(function() { self.onCreateCancel(); });
    },

    showCreateSave: function(newHtml) {
        log("ViewNavigator showCreateSave");
        if (!empty(newHtml))
            $('#divASustituir').after(hiddenDiv(this.tempDivId(), newHtml));

        new popup().show(this.tempDivId());
        var self = this;
        
        $('#' + this.viewOptions.prefix.compose(sfBtnOk)).unbind('click').click(function() { self.onCreateSave(); });
        $('#' + this.viewOptions.prefix.compose(sfBtnCancel)).unbind('click').click(function() { self.onCreateCancel(); });

		if (this.viewOptions.onLoaded != null)
            this.viewOptions.onLoaded(this.tempDivId());
    },

    constructRequestData: function() {
        log("ViewNavigator constructRequestData");

        var requestData = "sfRuntimeType=" + this.viewOptions.type +
                qp("sfId", this.viewOptions.id) +
                qp(sfPrefix, this.viewOptions.prefix);

        if (!empty(this.viewOptions.partialViewName)) //Send specific partialview if given
            requestData += qp("sfUrl", this.viewOptions.partialViewName);

        if (!empty(this.viewOptions.requestExtraJsonData)) {
            for (var key in this.viewOptions.requestExtraJsonData) {
                requestData += qp(key, this.viewOptions.requestExtraJsonData[key]);
            }
        }

        return requestData;
    },

    callServer: function(onSuccess) {
        log("ViewNavigator callServer");
        $.ajax({
            type: "POST",
            url: this.viewOptions.controllerUrl,
            data: this.constructRequestData(),
            async: false,
            dataType: "html",
            success: onSuccess
        });
    },

    onViewOk: function() {
        log("ViewNavigator onViewOk");
        var doDefault = (this.viewOptions.onOk != null) ? this.viewOptions.onOk() : true;
        if (doDefault != false) {
            this.backup = null;
            $('#' + this.viewOptions.containerDiv).hide();
            if (this.viewOptions.onOkClosed != null)
                this.viewOptions.onOkClosed();
        }
    },

    onViewCancel: function() {
        log("ViewNavigator onViewCancel");
        $('#' + this.viewOptions.containerDiv).hide().html('').append(this.backup);
        this.backup = "";
        if (this.viewOptions.onCancelled != null)
            this.viewOptions.onCancelled();
    },

    onCreateOk: function() {
        log("ViewNavigator onCreateOk");
        var doDefault = (this.viewOptions.onOk != null) ? this.viewOptions.onOk(cloneContents(this.tempDivId())) : true;
        if (doDefault != false) {
            $('#' + this.tempDivId()).remove();
            if (this.viewOptions.onOkClosed != null)
                this.viewOptions.onOkClosed();
        }
    },

    onCreateSave: function() {
        log("ViewNavigator onCreateSave");
        var doDefault = (this.viewOptions.onOk != null) ? this.viewOptions.onOk(this.tempDivId()) : true;
        if (doDefault != false) {
            var validatorResult = new PartialValidator({ prefix: this.viewOptions.prefix, type: this.viewOptions.type }).trySave();
            if (!validatorResult.isValid) {
                window.alert(lang['popupErrorsStop']);
                return;
            }
            if (empty(this.viewOptions.containerDiv))
                $('#' + this.tempDivId()).remove();
            else
                $('#' + this.viewOptions.containerDiv).remove();
            if (this.viewOptions.onOkClosed != null)
                this.viewOptions.onOkClosed();
        }
    },

    onCreateCancel: function() {
        log("ViewNavigator onCreateCancel");
        if (empty(this.viewOptions.containerDiv))
            $('#' + this.tempDivId()).remove();
        else
            $('#' + this.viewOptions.containerDiv).remove();
        if (this.viewOptions.onCancelled != null)
            this.viewOptions.onCancelled();
    }
}

function openChooser(_prefix, onOptionClicked, jsonOptionsListFormat) {
    log("openChooser");
    //Construct popup
    var tempDivId = _prefix + "Temp";
    var requestData = "prefix=" + tempDivId;
    var url = "Signum/GetChooser";
    if (empty(jsonOptionsListFormat))
    {
        requestData += "&sfImplementations=" + $('#' + _prefix.compose(sfImplementations)).val();
        url = "Signum/GetTypeChooser";
        }
    else {
        for (var i = 0; i < jsonOptionsListFormat.length; i++)
            requestData += "&buttons=" + jsonOptionsListFormat[i];  //This will Bind to the List<string> "buttons"
    }
    $.ajax({
        type: "POST",
        url: url,
        data: requestData,
        async: false,
        dataType: "html",
        success: function(chooserHTML) {
            $('#divASustituir').after(hiddenDiv(tempDivId, chooserHTML));
            //Set continuation for each type button
            $('#' + tempDivId + " :button").each(function() {
                $('#' + this.id).unbind('click').click(function() {
                    var option = this.id;
                    $('#' + tempDivId).remove();
                    onOptionClicked(option);
                });
            });
            new popup({ prefix: _prefix }).show(tempDivId);
            $('#' + tempDivId.compose(sfBtnCancel)).unbind('click').click(function() { $('#' + tempDivId).remove(); });
        }
    });
}

function RelatedEntityCreate(viewOptions) {
    var extraJsonData = new Object();
    var info = RuntimeInfoFor('');
    extraJsonData.sfIdRelated = info.id();
    extraJsonData.sfRuntimeTypeRelated = info.runtimeType();
    
    var navigator = new ViewNavigator($.extend(viewOptions, {requestExtraJsonData:extraJsonData}));
    navigator.createSave();
}

function cloneContents(sourceContainerId) {
    var clone = $('#' + sourceContainerId).children().clone(true);
    var selects = $('#' + sourceContainerId).find("select");
    $(selects).each(function(i) {
        var select = this;
        $(clone).find("select").eq(i).val($(select).val());
    });
    return clone;
}

function hiddenInput(id, value)
{
    return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
}
