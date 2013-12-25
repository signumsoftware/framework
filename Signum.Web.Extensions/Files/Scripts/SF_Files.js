/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var SF;
(function (SF) {
    (function (Files) {
        var FileLine = (function (_super) {
            __extends(FileLine, _super);
            function FileLine(element, _options) {
                _super.call(this, element, _options);
            }
            FileLine.prototype._create = function () {
                $("#" + this.options.prefix).data("SF-fileLine").initDragDrop($(this.pf("DivNew")));
            };

            FileLine.prototype.initDragDrop = function ($divNew) {
                if (window.File && window.FileList && window.FileReader && new XMLHttpRequest().upload) {
                    var self = this;
                    var $fileDrop = $("<div></div>").addClass("sf-file-drop").html("or drag a file here").on("dragover", function (e) {
                        self.fileDropHover(e, true);
                    }).on("dragleave", function (e) {
                        self.fileDropHover(e, false);
                    }).appendTo($divNew);
                    $fileDrop[0].addEventListener("drop", function (e) {
                        self.fileDropped(e);
                    }, false);
                }
            };

            FileLine.prototype.fileDropHover = function (e, toggle) {
                e.stopPropagation();
                e.preventDefault();
                $(e.target).toggleClass("sf-file-drop-over", toggle);
            };

            FileLine.prototype.getParentRuntimeInfo = function (parentPrefix) {
                var $runtimeInfoField = new SF.RuntimeInfo(parentPrefix).find();
                if ($runtimeInfoField.length > 0) {
                    return $runtimeInfoField.val();
                } else {
                    var $mainControl = $(".sf-main-control[data-prefix=" + parentPrefix + "]");
                    return $mainControl.data("runtimeinfo");
                }
            };

            FileLine.prototype.fileDropped = function (e) {
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
                            } else {
                                SF.log("Error " + xhr.statusText);
                            }
                        }
                    };

                    xhr.send(f);
                }
            };

            FileLine.prototype.download = function () {
                var info = this.runtimeInfo();
                if (SF.isEmpty(info.id)) {
                    return;
                }
                var url = this.options.downloadUrl || SF.Urls.downloadFile;
                window.open(url + "?file=" + info.value());
            };

            FileLine.prototype.removeSpecific = function () {
                $(this.pf('DivOld')).hide();
                $(this.pf('DivNew')).show();
            };

            FileLine.prototype.prepareSyncUpload = function () {
                //New file in FileLine but not to be uploaded asyncronously => prepare form for multipart and set runtimeInfo
                $(this.pf(''))[0].setAttribute('value', $(this.pf(''))[0].value);
                var $mform = $('form');
                $mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data');
                this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');
            };

            FileLine.prototype.upload = function () {
                this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');

                var $fileInput = $(this.pf(''));
                $fileInput[0].setAttribute('value', $fileInput[0].value);
                $(this.pf('loading')).show();

                this.createTargetIframe();

                var url = this.options.uploadUrl || SF.Urls.uploadFile;

                var $fileForm = $('<form></form>').attr('method', 'post').attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data').attr('target', SF.compose(this.options.prefix, "frame")).attr('action', url).hide().appendTo($('body'));

                var $divNew = $(this.pf("DivNew"));
                var $clonedDivNew = $divNew.clone(true);
                $divNew.after($clonedDivNew).appendTo($fileForm);

                var $parentPrefix = $("<input />").attr("type", "hidden").attr("name", "fileParentRuntimeInfo").val(this.getParentRuntimeInfo($divNew.attr("data-parent-prefix"))).addClass("sf-file-parent-prefix").appendTo($fileForm);

                var $tabId = $("#" + SF.Keys.tabId).clone().appendTo($fileForm);
                var $antiForgeryToken = $("input[name=" + SF.Keys.antiForgeryToken + "]").clone().appendTo($fileForm);

                $fileForm.submit().remove();
            };

            FileLine.prototype.createTargetIframe = function () {
                var name = SF.compose(this.options.prefix, "frame");
                return $("<iframe id='" + name + "' name='" + name + "' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>").appendTo($("body"));
            };

            FileLine.prototype.onChanged = function () {
                if (this.options.asyncUpload) {
                    this.upload();
                } else {
                    this.prepareSyncUpload();
                }
            };

            FileLine.prototype.updateButtonsDisplay = function (hasEntity) {
            };
            return FileLine;
        })(SF.EntityBase);
    })(SF.Files || (SF.Files = {}));
    var Files = SF.Files;
})(SF || (SF = {}));
//# sourceMappingURL=SF_Files.js.map
