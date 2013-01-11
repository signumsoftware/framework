"use strict";

SF.registerModule("Files", function () {

    if (typeof $.SF == "undefined" || typeof $.SF.baseLine == "undefined") {
        throw "SF_Lines must be loaded before SF_Files";
    }

    (function ($) {

        $.widget("SF.fileLine", $.SF.baseLine, {

            options: {}, //baseLine options + asyncUpload + uploadUrl + uploadDroppedUrl + downloadUrl

            _create: function () {
                $("#" + this.options.prefix).data("fileLine").initDragDrop($(this.pf("DivNew")));
            },

            initDragDrop: function ($divNew) {
                if (window.File && window.FileList && window.FileReader && new XMLHttpRequest().upload) {
                    var self = this;
                    var $fileDrop = $("<div></div>").addClass("sf-file-drop").html("or drag a file here")
                        .on("dragover", function (e) { self.fileDropHover(e, true); })
                        .on("dragleave", function (e) { self.fileDropHover(e, false); })
                        .appendTo($divNew);
                    $fileDrop[0].addEventListener("drop", function (e) { self.fileDropped(e); }, false);
                }
            },

            fileDropHover: function (e, toggle) {
                e.stopPropagation();
                e.preventDefault();
                $(e.target).toggleClass("sf-file-drop-over", toggle);
            },

            getParentRuntimeInfo: function (parentPrefix) {
                var $runtimeInfoField = new SF.RuntimeInfo(parentPrefix).find();
                if ($runtimeInfoField.length > 0) {
                    return $runtimeInfoField.val();
                }
                else { //popup
                    var $mainControl = $(".sf-main-control[data-prefix=" + parentPrefix + "]");
                    return $mainControl.data("runtimeinfo");
                }
            },

            fileDropped: function (e) {
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

                    var $divNew = $(this.pf("DivNew"));

                    var xhr = new XMLHttpRequest();
                    xhr.open("POST", this.options.uploadDroppedUrl || SF.Urls.uploadDroppedFile, true);
                    xhr.setRequestHeader("X-FileName", fileName);
                    xhr.setRequestHeader("X-" + SF.Keys.runtimeInfo, this.getParentRuntimeInfo($divNew.attr("data-parent-prefix")));
                    xhr.setRequestHeader("X-Prefix", this.options.prefix);
                    xhr.setRequestHeader("X-" + SF.compose(this.options.prefix, SF.Keys.runtimeInfo), this.runtimeInfo().find().val());
                    xhr.setRequestHeader("X-sfFileType", $(this.pf("sfFileType")).val());
                    xhr.setRequestHeader("X-sfTabId", $("#sfTabId").val());

                    var self = this;
                    xhr.onreadystatechange = function () {
                        if (xhr.readyState === 4) {
                            if (xhr.status === 200) {
                                self.createTargetIframe().html(xhr.responseText);
                            }
                            else {
                                SF.log("Error", xhr.statusText);
                            }
                        }
                    };

                    xhr.send(f);
                }
            },

            download: function () {
                var info = this.runtimeInfo();
                if (SF.isEmpty(info.id)) {
                    return;
                }
                var url = this.options.downloadUrl || SF.Urls.downloadFile;
                window.open(url + "?file=" + info.value());
            },

            removeSpecific: function () {
                $(this.pf('DivOld')).hide();
                $(this.pf('DivNew')).show();
            },

            prepareSyncUpload: function () {
                //New file in FileLine but not to be uploaded asyncronously => prepare form for multipart and set runtimeInfo
                $(this.pf(''))[0].setAttribute('value', $(this.pf(''))[0].value);
                var $mform = $('form');
                $mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data');
                this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');
            },

            upload: function () {
                this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');

                var $fileInput = $(this.pf(''));
                $fileInput[0].setAttribute('value', $fileInput[0].value);
                $(this.pf('loading')).show();

                this.createTargetIframe();

                var url = this.options.uploadUrl || SF.Urls.uploadFile;

                var $fileForm = $('<form></form>')
                    .attr('method', 'post').attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data')
                    .attr('target', SF.compose(this.options.prefix, "frame")).attr('action', url)
                    .hide()
                    .appendTo($('body'));

                var $divNew = $(this.pf("DivNew"));
                var $clonedDivNew = $divNew.clone(true);
                $divNew.after($clonedDivNew).appendTo($fileForm);

                var $parentPrefix = $("<input />").attr("type", "hidden")
                    .attr("name", "fileParentRuntimeInfo")
                    .val(this.getParentRuntimeInfo($divNew.attr("data-parent-prefix")))
                    .addClass("sf-file-parent-prefix").appendTo($fileForm);

                var $tabId = $("#" + SF.Keys.tabId).clone().appendTo($fileForm);
                var $antiForgeryToken = $("input[name=" + SF.Keys.antiForgeryToken + "]").clone().appendTo($fileForm);

                $fileForm.submit().remove();
            },

            createTargetIframe: function () {
                var name = SF.compose(this.options.prefix, "frame");
                return $("<iframe id='" + name + "' name='" + name + "' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>")
                    .appendTo($("body"));
            },

            onChanged: function () {
                if (this.options.asyncUpload) {
                    this.upload();
                }
                else {
                    this.prepareSyncUpload();
                }
            },

            updateButtonsDisplay: function (hasEntity) {
            }
        });

        $.widget("SF.fileRepeater", $.SF.entityRepeater, {

            options: {},

            typedCreate: function (_viewOptions) {
                if (!this.canAddItems()) {
                    return;
                }

                var viewOptions = this.viewOptionsForCreating(_viewOptions);
                var template = window[SF.compose(this.options.prefix, "sfTemplate")];
                //Template pre-loaded: It will be created with "_0" itemprefix => replace it with the current one
                template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                this.onItemCreated(template, viewOptions);
                $(".sf-repeater-element > #" + SF.compose(viewOptions.prefix, SF.Keys.runtimeInfo)).remove();
            },

            _getRemoving: function (itemPrefix) {
                return "$('#" + this.options.prefix + "').data('fileRepeater').remove('" + itemPrefix + "');";
            }
        });
    })(jQuery);

});
