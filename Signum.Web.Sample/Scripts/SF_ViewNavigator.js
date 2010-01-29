var ViewNavigator = function(_viewOptions) {
    this.viewOptions = $.extend({
        containerDiv: null,
        onOk: null,
        onCancelled: null,
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

    createSave: function() {
        log("ViewNavigator createSave");
        if (!empty(this.viewOptions.containerDiv))
            throw "ContainerDiv cannot be specified to Navigator on createSave mode";
        if (empty(this.viewOptions.type))
            throw "Type must be specified to Navigator on createSave mode";
        var self = this;
        this.callServer(function(controlHtml) { self.showCreateSave(controlHtml); });
    },

    navigate: function() {
        log("ViewNavigator navigate");
        if (!empty(this.viewOptions.containerDiv))
            throw "ContainerDiv cannot be specified to Navigator on Navigate mode";
        if (empty(this.viewOptions.type))
            throw "Type must be specified to Navigator on Navigate mode";
        var self = this;
        this.callServer(function(url) { PostServer(url); });
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
        $('#' + this.viewOptions.prefix + sfBtnOk).unbind('click').click(function() { self.onViewOk(); });
        $('#' + this.viewOptions.prefix + sfBtnCancel).unbind('click').click(function() { self.onViewCancel(); });
    },

    showCreateOk: function(newHtml) {
        log("ViewNavigator showCreateOk");
        if (!empty(newHtml))
            $('#divASustituir').after(hiddenDiv(this.tempDivId(), newHtml));

        new popup().show(this.tempDivId());
        var self = this;
        $('#' + this.viewOptions.prefix + sfBtnOk).unbind('click').click(function() { self.onCreateOk(); });
        $('#' + this.viewOptions.prefix + sfBtnCancel).unbind('click').click(function() { self.onCreateCancel(); });
    },

    showCreateSave: function(newHtml) {
        log("ViewNavigator showCreateSave");
        if (!empty(newHtml))
            $('#divASustituir').after(hiddenDiv(this.tempDivId(), newHtml));

        new popup().show(this.tempDivId());
        var self = this;
        $('#' + this.viewOptions.prefix + sfBtnOk).unbind('click').click(function() { self.onCreateSave(); });
        $('#' + this.viewOptions.prefix + sfBtnCancel).unbind('click').click(function() { self.onCreateCancel(); });
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
        if (doDefault != false)
            $('#' + this.tempDivId()).remove();
    },

    onCreateSave: function() {
        log("ViewNavigator onCreateSave");
        var doDefault = (this.viewOptions.onOk != null) ? this.viewOptions.onOk(this.tempDivId()) : true;
        if (doDefault != false) {
            var validator = new PartialValidator({ prefix: this.viewOptions.prefix, type: this.viewOptions.type }).trySave();
            if (!validator.isValid) {
                window.alert(lang['popupErrorsStop']);
                return;
            }
            $('#' + this.tempDivId()).remove();
        }
    },

    onCreateCancel: function() {
        log("ViewNavigator onCreateCancel");
        $('#' + this.tempDivId()).remove();
        if (this.viewOptions.onCancelled != null)
            this.viewOptions.onCancelled();
    }
}

function typeSelector(_prefix, onTypeClicked) {
    log("typeSelector");
    //Construct popup
    var tempDivId = _prefix + "Temp";
    $.ajax({
        type: "POST",
        url: "Signum/GetChooser",
        data: "prefix=" + tempDivId + "&sfImplementations=" + $('#' + _prefix + sfImplementations).val(),
        async: false,
        dataType: "html",
        success: function(chooserHTML) {
            $('#divASustituir').after(hiddenDiv(tempDivId, chooserHTML));
            //Set continuation for each type button
            $('#' + tempDivId + " :button").each(function() {
                $('#' + this.id).unbind('click').click(function() {
                    var type = this.id;
                    $('#' + tempDivId).remove();
                    onTypeClicked(type);
                });
            });
            new popup({ prefix: _prefix }).show(tempDivId);
            $('#' + tempDivId + sfBtnCancel).unbind('click').click(function() { $('#' + tempDivId).remove(); });
        }
    });

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