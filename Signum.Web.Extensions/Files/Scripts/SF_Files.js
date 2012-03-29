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

        this.initDragDrop = function ($divNew) {
            if (window.File && window.FileList && window.FileReader && new XMLHttpRequest().upload) {
                var self = this;
                var $fileDrop = $("<div></div>").addClass("sf-file-drop").html("or drag a file here")
                .on("dragover", function (e) { self.fileDropHover(e, true); })
                .on("dragleave", function (e) { self.fileDropHover(e, false); })
                .appendTo($divNew);
                $fileDrop[0].addEventListener("drop", function (e) { self.fileDropped(e); }, false);
            }
        };

        this.fileDropHover = function (e, toggle) {
            e.stopPropagation();
            e.preventDefault();
            $(e.target).toggleClass("sf-file-drop-over", toggle);
        };

        this.fileDropped = function (e) {
            var files = e.target.files || e.dataTransfer.files;
            e.stopPropagation();
            e.preventDefault();
            
            if (files.length == 0) {
                this.fileDropHover(e, false);
                return;
            }

            for (var i = 0, f; f = files[i]; i++) {
                $(this.pf('loading')).show();
                this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');

                var fileName = f.name;

                var xhr = new XMLHttpRequest();
                xhr.open("POST", $(this.pf("DivNew")).attr("data-drop-url"), true);
                xhr.setRequestHeader("X-FileName", fileName);
                xhr.setRequestHeader("X-" + SF.Keys.runtimeInfo, new SF.RuntimeInfo().find().val());
                xhr.setRequestHeader("X-Prefix", this.options.prefix);
                xhr.setRequestHeader("X-" + SF.compose(this.options.prefix, SF.Keys.runtimeInfo), this.runtimeInfo().find().val());
                xhr.setRequestHeader("X-FileType", $(this.pf("FileType")).val());
                xhr.setRequestHeader("X-sfTabId", $("#sfTabId").val());

                var self = this;
                xhr.onreadystatechange = function () {
                    if (xhr.readyState === 4) {
                        if (xhr.status === 200) {
                            SF.log(xhr.responseText);
                            $(self.pf("DivNew iframe")).html(xhr.responseText);
                        }
                        else {
                            SF.log("Error", xhr.statusText);
                        }
                    }
                };

                xhr.send(f);
            }
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

    $(".sf-file-line-new").each(function () {
        var $this = $(this);
        if ($this.find("sf-file-drop").length == 0) {
            var id = $this.attr("id");
            var prefix = id.substring(0, id.indexOf("DivNew"));
            new SF.FLine({ prefix: prefix }).initDragDrop($this);
        }
    });

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
            new SF.FLine({ prefix: viewOptions.prefix }).initDragDrop($("#" + SF.compose(viewOptions.prefix, "DivNew")));
        };
    };

    SF.FRep.prototype = new SF.ERep();
});
