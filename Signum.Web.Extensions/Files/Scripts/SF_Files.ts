/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/references.ts"/>
module SF.Files {

    export interface FileLineOptions extends EntityBaseOptions {
        downloadUrl?: string;
        uploadUrl?: string;
        uploadDroppedUrl?: string;
        asyncUpload?: boolean;
        dragAndDrop?: boolean;
    }

    export interface FileAsyncUploadResult {
        FileName: string;
        FullWebPath: string;
        RuntimeInfo: string;
        EntityState: string;
    }

    once("SF-fileLine", () =>
        $.fn.fileLine = function (opt: FileLineOptions) {
            var fl = new FileLine(this, opt);
        });

    export class FileLine extends EntityBase {
        options: FileLineOptions;
        constructor(element: JQuery, _options: FileLineOptions) {
            super(element, _options);
        }

        _create() {
            if (this.options.dragAndDrop == null || this.options.dragAndDrop == true)
                FileLine.initDragDrop($(this.pf("DivNew")),
                    e=> this.fileDropped(e));
        }

        static initDragDrop($div: JQuery, onDropped: (e:DragEvent) => void ) {
            if (window.File && window.FileList && window.FileReader && new XMLHttpRequest().upload) {
                var self = this;
                var $fileDrop = $("<div></div>").addClass("sf-file-drop").html("drag a file here")
                    .on("dragover", function (e) { FileLine.fileDropHover(e, true); })
                    .on("dragleave", function (e) { FileLine.fileDropHover(e, false); })
                    .appendTo($div);
                $fileDrop[0].addEventListener("drop", function (e) {
                    FileLine.fileDropHover(e, false);
                    onDropped(e);
                }, false);
            }
        }

        static fileDropHover(e, toggle : boolean) {
            e.stopPropagation();
            e.preventDefault();
            $(e.target).toggleClass("sf-file-drop-over", toggle);
        }

        getParentRuntimeInfo(parentPrefix : string) {
            var $runtimeInfoField = new SF.RuntimeInfo(parentPrefix).find();
            if ($runtimeInfoField.length > 0) {
                return $runtimeInfoField.val();
            }
            else { //popup
                var $mainControl = $(".sf-main-control[data-prefix=" + parentPrefix + "]");
                return $mainControl.data("runtimeinfo");
            }
        }

        

        uploadAsync(f: File, onLoaded?: (result: FileAsyncUploadResult) => void) {
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
                        var result = <FileAsyncUploadResult>JSON.parse(xhr.responseText);

                        self.onUploaded(result.FileName, result.FullWebPath, result.RuntimeInfo, result.EntityState);

                        if (onLoaded != undefined)
                            onLoaded(result);
                    }
                    else {
                        SF.log("Error " + xhr.statusText);
                    }
                }
            };

            xhr.send(f);
        }

        fileDropped(e: DragEvent) {
            var files = e.dataTransfer.files;
            e.stopPropagation();
            e.preventDefault();

            this.uploadAsync(files[0]);
        }


        removeSpecific() {
            $(this.pf('DivOld')).hide();
            $(this.pf('DivNew')).show();
        }

        prepareSyncUpload() {
            //New file in FileLine but not to be uploaded asyncronously => prepare form for multipart and set runtimeInfo
            $(this.pf(''))[0].setAttribute('value', (<HTMLInputElement>$(this.pf(''))[0]).value);
            var $mform = $('form');
            $mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data');
            this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');
        }

        upload() {
            this.runtimeInfo().setEntity(this.staticInfo().singleType(), '');

            var $fileInput = $(this.pf(''));
            $fileInput[0].setAttribute('value', (<HTMLInputElement>$fileInput[0]).value);
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
        }

        createTargetIframe() {
            var name = SF.compose(this.options.prefix, "frame");
            return $("<iframe id='" + name + "' name='" + name + "' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>")
                .appendTo($("body"));
        }


        onUploaded(fileName: string, link: string, runtimeInfo: string, entityState : string) {
            $(this.pf(Keys.loading)).hide();
            $(this.pf(Keys.toStr)).html(fileName);
            $(this.pf(Keys.link)).html(fileName).attr("download", fileName).attr("href", link);
            this.runtimeInfo().find().val(runtimeInfo);
            $(this.pf(Keys.entityState)).val(entityState);

            $(this.pf("DivNew")).hide();
            $(this.pf("DivOld")).show();
            $(this.pf("btnRemove")).show();
            $(this.pf("frame")).remove();
        }

        onChanged() {
            if (this.options.asyncUpload) {
                this.upload();
            }
            else {
                this.prepareSyncUpload();
            }
        }

        updateButtonsDisplay(hasEntity) {

        }
    }
}
interface Window {
    File: any;
    FileList: any;
    FileReader: any;
}
