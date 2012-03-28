"use strict";

SF.registerModule("Files", function () {

    if (SF.EBaseLine == undefined) {
        throw "SF_Lines must be loaded before SF_Files";
    }

    /**
    * @constructor
    */
    //FLineOptions = EBaseOptions + asyncUpload + controllerUrl
    SF.FLine = function (_flineOptions) {
        SF.log("FLine");
        SF.EBaseLine.call(this, _flineOptions);

        this.init = function () {
            $("#" + SF.compose(this.options.prefix, "DivNew") + " .sf-file-drop")[0].addEventListener("drop", function (e) {
                e.stopPropagation();
                e.preventDefault();

                // fetch FileList object  
                var files = e.target.files || e.dataTransfer.files;
                // process all File objects  
                for (var i = 0, f; f = files[i]; i++) {
                    window.alert(f.name);
                }

            }, false);
        };

        this.download = function () {
            SF.log("FLine download");
            var id = this.runtimeInfo().id();
            if (SF.isEmpty(id)) {
                return;
            }
            window.open(this.options.controllerUrl + "?filePathID=" + id);
        };

        this.removeSpecific = function () {
            SF.log("FLine removeSpecific");
            $(this.pf('DivOld')).hide();
            $(this.pf('DivNew')).show();
        };

        this.prepareSyncUpload = function () {
            SF.log("FLine prepareSyncUpload");
            //New file in FileLine but not to be uploaded asyncronously => prepare form for multipart and set runtimeInfo
            $(this.pf(''))[0].setAttribute('value', $(this.pf(''))[0].value);
            var mform = $('form');
            mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data');
            this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');
        };

        this.upload = function () {
            SF.log("FLine upload");
            this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');
            $(this.pf(''))[0].setAttribute('value', $(this.pf(''))[0].value);
            $(this.pf('') + 'loading').show();
            var mform = $('form');
            var cEncType = mform.attr('enctype');
            var cEncoding = mform.attr('encoding');
            var cTarget = mform.attr('target');
            var cAction = mform.attr('action');
            mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data').attr('target', 'frame' + this.options.prefix).attr('action', this.options.controllerUrl).submit();
            mform.attr('enctype', cEncType || "").attr('encoding', cEncoding || "").attr('target', cTarget || "").attr('action', cAction || "");
        };

        this.onChanged = function () {
            if (this.options.asyncUpload) {
                this.upload();
            }
            else {
                this.prepareSyncUpload();
            }
        };

        this.updateButtonsDisplay = function (hasEntity) { };
    };

    SF.FLine.prototype = new SF.EBaseLine();

    SF.FRep = function (_frepOptions) {
        SF.log("FRep");
        SF.ERep.call(this, _frepOptions);

        this.typedCreate = function (_viewOptions) {
            SF.log("FRep create");
            if (!this.canAddItems()) {
                return;
            }

            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = window[SF.compose(this.options.prefix, "sfTemplate")];
            //Template pre-loaded: It will be created with "_0" itemprefix => replace it with the current one
            template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
            this.onItemCreated(template, viewOptions);
            $(".sf-repeater-element > #" + SF.compose(viewOptions.prefix, SF.Keys.runtimeInfo)).remove();
        };
    };

    SF.FRep.prototype = new SF.ERep();
});
