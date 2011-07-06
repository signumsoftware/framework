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
        }
    };

    /**
    * @constructor
    */
    SF.FLine.prototype = new SF.EBaseLine();
});
