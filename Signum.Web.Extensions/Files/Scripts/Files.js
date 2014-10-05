/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Lines"], function(require, exports, Entities, Lines) {
    (function (DownloadBehaviour) {
        DownloadBehaviour[DownloadBehaviour["SaveAs"] = 0] = "SaveAs";
        DownloadBehaviour[DownloadBehaviour["View"] = 1] = "View";
        DownloadBehaviour[DownloadBehaviour["None"] = 2] = "None";
    })(exports.DownloadBehaviour || (exports.DownloadBehaviour = {}));
    var DownloadBehaviour = exports.DownloadBehaviour;

    once("SF-fileLine", function () {
        return $.fn.fileLine = function (opt) {
            var fl = new FileLine(this, opt);
        };
    });

    var FileLine = (function (_super) {
        __extends(FileLine, _super);
        function FileLine(element, _options) {
            _super.call(this, element, _options);
        }
        FileLine.prototype._create = function () {
            var _this = this;
            if (this.options.dragAndDrop == null || this.options.dragAndDrop == true)
                FileLine.initDragDrop(this.prefix.child("DivNew").get(), function (e) {
                    return _this.fileDropped(e);
                });

            this.prefix.child("sfFile").get().on("change", function (ev) {
                return _this.onChanged(ev);
            });
        };

        FileLine.initDragDrop = function ($div, onDropped) {
            if (window.File && window.FileList && window.FileReader && new XMLHttpRequest().upload) {
                var self = this;
                var $fileDrop = $("<div></div>").addClass("sf-file-drop").html("drag a file here").on("dragover", function (e) {
                    FileLine.fileDropHover(e, true);
                }).on("dragleave", function (e) {
                    FileLine.fileDropHover(e, false);
                }).appendTo($div);
                $fileDrop[0].addEventListener("drop", function (e) {
                    FileLine.fileDropHover(e, false);
                    onDropped(e);
                }, false);
            }
        };

        FileLine.fileDropHover = function (e, toggle) {
            e.stopPropagation();
            e.preventDefault();
            $(e.target).toggleClass("sf-file-drop-over", toggle);
        };

        FileLine.prototype.uploadAsync = function (f, customizeXHR) {
            this.prefix.child('loading').get().show();
            Entities.RuntimeInfo.setFromPrefix(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true));

            var fileName = f.name;

            var $divNew = this.prefix.child("DivNew").get();

            var xhr = new XMLHttpRequest();
            xhr.open("POST", this.options.uploadDroppedUrl || SF.Urls.uploadDroppedFile, true);
            xhr.setRequestHeader("X-FileName", fileName);
            xhr.setRequestHeader("X-Prefix", this.options.prefix);
            xhr.setRequestHeader("X-" + this.options.prefix.child(Entities.Keys.runtimeInfo), Entities.RuntimeInfo.getFromPrefix(this.options.prefix).toString());
            xhr.setRequestHeader("X-sfFileType", this.options.fileType);
            xhr.setRequestHeader("X-sfTabId", $("#sfTabId").val());

            var extraParams = {};
            SF.addAjaxExtraParameters(extraParams);
            if (extraParams != {}) {
                for (var key in extraParams) {
                    if (extraParams.hasOwnProperty(key)) {
                        xhr.setRequestHeader(key, extraParams[key]);
                    }
                }
            }

            var self = this;
            xhr.onload = function (e) {
                var result = JSON.parse(xhr.responseText);

                self.onUploaded(result.FileName, result.FullWebPath, result.RuntimeInfo, result.EntityState);
            };

            xhr.onerror = function (e) {
                SF.log("Error " + xhr.statusText);
            };

            if (customizeXHR != null)
                customizeXHR(xhr);

            xhr.send(f);
        };

        FileLine.prototype.fileDropped = function (e) {
            var files = e.dataTransfer.files;
            e.stopPropagation();
            e.preventDefault();

            this.uploadAsync(files[0]);
        };

        FileLine.prototype.prepareSyncUpload = function () {
            //New file in FileLine but not to be uploaded asyncronously => prepare form for multipart and set runtimeInfo
            this.prefix.get()[0].setAttribute('value', this.prefix.get()[0].value);
            var $mform = $('form');
            $mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data');
            Entities.RuntimeInfo.setFromPrefix(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true));
        };

        FileLine.prototype.upload = function () {
            Entities.RuntimeInfo.setFromPrefix(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true));

            var $fileInput = this.prefix.get();
            $fileInput[0].setAttribute('value', $fileInput[0].value);
            this.prefix.child('loading').get().show();

            this.createTargetIframe();

            var url = this.options.uploadUrl || SF.Urls.uploadFile;

            var $fileForm = $('<form></form>').attr('method', 'post').attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data').attr('target', this.options.prefix.child("frame")).attr('action', url).hide().appendTo($('body'));

            var $divNew = this.prefix.child("DivNew").get();
            var $clonedDivNew = $divNew.clone(true);
            $divNew.after($clonedDivNew).appendTo($fileForm); //if not attached to our DOM first there are problems with filename

            $("<input type='hidden' name='" + this.options.prefix + "_sfFileType' value='" + this.options.fileType + "'/>").appendTo($fileForm);

            var extraParams = {};
            SF.addAjaxExtraParameters(extraParams);
            if (extraParams != {}) {
                for (var key in extraParams) {
                    if (extraParams.hasOwnProperty(key)) {
                        $("<input type='hidden' />").attr("name", key).val(extraParams[key]).appendTo($fileForm);
                    }
                }
            }

            var $tabId = $("#" + Entities.Keys.tabId).clone().appendTo($fileForm);
            var $antiForgeryToken = $("input[name=" + Entities.Keys.antiForgeryToken + "]").clone().appendTo($fileForm);

            $fileForm.submit().remove();
        };

        FileLine.prototype.createTargetIframe = function () {
            var name = this.options.prefix.child("frame");
            return $("<iframe id='" + name + "' name='" + name + "' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>").appendTo($("body"));
        };

        FileLine.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            this.prefix.child(Entities.Keys.loading).get().hide();
            this.prefix.child("sfFile").get().val("");
            if (entityValue) {
                this.prefix.child(Entities.Keys.toStr).tryGet().html(entityValue.toStr);
                this.prefix.child(Entities.Keys.link).tryGet().html(entityValue.toStr).attr("href", entityValue.link);

                if (this.options.download == 0 /* SaveAs */)
                    this.prefix.child(Entities.Keys.link).tryGet().attr("download", entityValue.toStr);
            } else {
                this.prefix.child(Entities.Keys.toStr).tryGet().html("");
                this.prefix.child(Entities.Keys.link).tryGet().html("").removeAttr("download").removeAttr("href");
            }
        };

        FileLine.prototype.onUploaded = function (fileName, link, runtimeInfo, entityState) {
            this.setEntity(new Entities.EntityValue(Entities.RuntimeInfo.parse(runtimeInfo), fileName, link));

            this.prefix.child(Entities.Keys.entityState).tryGet().val(entityState);

            this.prefix.child("frame").tryGet().remove();
        };

        FileLine.prototype.onChanged = function (e) {
            if (this.options.asyncUpload) {
                this.upload();
            } else {
                this.prepareSyncUpload();
            }
        };

        FileLine.prototype.updateButtonsDisplay = function () {
            var hasEntity = !!Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

            this.prefix.child('DivOld').get().toggle(hasEntity);
            this.prefix.child('DivNew').get().toggle(!hasEntity);

            this.prefix.child("btnRemove").tryGet().toggle(hasEntity);
        };

        FileLine.prototype.getLink = function (itemPrefix) {
            return this.prefix.child(Entities.Keys.link).get().attr("href") || null;
        };

        FileLine.prototype.getToString = function (itemPrefix) {
            return this.prefix.child(Entities.Keys.link).get().text();
        };
        return FileLine;
    })(Lines.EntityBase);
    exports.FileLine = FileLine;
});
//# sourceMappingURL=Files.js.map
