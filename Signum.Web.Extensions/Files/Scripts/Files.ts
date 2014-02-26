/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")

export interface FileLineOptions extends Lines.EntityBaseOptions {
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

export class FileLine extends Lines.EntityBase {
    options: FileLineOptions;
    constructor(element: JQuery, _options: FileLineOptions) {
        super(element, _options);
    }

    _create() {
        if (this.options.dragAndDrop == null || this.options.dragAndDrop == true)
            FileLine.initDragDrop($(this.pf("DivNew")),
                e=> this.fileDropped(e));


    }

    static initDragDrop($div: JQuery, onDropped: (e: DragEvent) => void) {
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

    static fileDropHover(e, toggle: boolean) {
        e.stopPropagation();
        e.preventDefault();
        $(e.target).toggleClass("sf-file-drop-over", toggle);
    }

    uploadAsync(f: File, customizeXHR?: (xhr: XMLHttpRequest) => void) {
        $(this.pf('loading')).show();
        Entities.RuntimeInfo.setFromPrefix(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true));

        var fileName = f.name;

        var $divNew = $(this.pf("DivNew"));

        var xhr = new XMLHttpRequest();
        xhr.open("POST", this.options.uploadDroppedUrl || SF.Urls.uploadDroppedFile, true);
        xhr.setRequestHeader("X-FileName", fileName);
        xhr.setRequestHeader("X-Prefix", this.options.prefix);
        xhr.setRequestHeader("X-" + SF.compose(this.options.prefix, Entities.Keys.runtimeInfo), Entities.RuntimeInfo.getFromPrefix(this.options.prefix).toString());
        xhr.setRequestHeader("X-sfFileType", $(this.pf("sfFileType")).val());
        xhr.setRequestHeader("X-sfTabId", $("#sfTabId").val());

        var self = this;
        xhr.onload = function (e) {
            var result = <FileAsyncUploadResult>JSON.parse(xhr.responseText);

            self.onUploaded(result.FileName, result.FullWebPath, result.RuntimeInfo, result.EntityState);
        };

        xhr.onerror = function (e) {
            SF.log("Error " + xhr.statusText);
        };

        if (customizeXHR != null)
            customizeXHR(xhr);

        xhr.send(f);
    }

    fileDropped(e: DragEvent) {
        var files = e.dataTransfer.files;
        e.stopPropagation();
        e.preventDefault();

        this.uploadAsync(files[0]);
    }

    prepareSyncUpload() {
        //New file in FileLine but not to be uploaded asyncronously => prepare form for multipart and set runtimeInfo
        $(this.pf(''))[0].setAttribute('value', (<HTMLInputElement>$(this.pf(''))[0]).value);
        var $mform = $('form');
        $mform.attr('enctype', 'multipart/form-data').attr('encoding', 'multipart/form-data');
        Entities.RuntimeInfo.setFromPrefix(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true));
    }

    upload() {
        Entities.RuntimeInfo.setFromPrefix(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true));

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

        var $tabId = $("#" + Entities.Keys.tabId).clone().appendTo($fileForm);
        var $antiForgeryToken = $("input[name=" + Entities.Keys.antiForgeryToken + "]").clone().appendTo($fileForm);

        $fileForm.submit().remove();
    }

    createTargetIframe() {
        var name = SF.compose(this.options.prefix, "frame");
        return $("<iframe id='" + name + "' name='" + name + "' src='about:blank' style='position:absolute;left:-1000px;top:-1000px'></iframe>")
            .appendTo($("body"));
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        $(this.pf(Entities.Keys.loading)).hide();
        if (entityValue) {
            $(this.pf(Entities.Keys.toStr)).html(entityValue.toStr);
            $(this.pf(Entities.Keys.link)).html(entityValue.toStr).attr("download", entityValue.toStr).attr("href", entityValue.link);
        } else {
            $(this.pf(Entities.Keys.toStr)).html("")
            $(this.pf(Entities.Keys.toStr)).html("").attr("download", undefined).attr("href", undefined)
        }
    }

    onUploaded(fileName: string, link: string, runtimeInfo: string, entityState: string) {

        this.setEntity(new Entities.EntityValue(Entities.RuntimeInfo.parse(runtimeInfo), fileName, link));

        $(this.pf(Entities.Keys.entityState)).val(entityState);

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

    updateButtonsDisplay() {
        var hasEntity = !!Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

        $(this.pf('DivOld')).toggle(hasEntity);
        $(this.pf('DivNew')).toggle(!hasEntity);

        $(this.pf("btnRemove")).toggle(hasEntity);
    }

    getLink(itemPrefix?: string): string {
        return $(this.pf(Entities.Keys.link)).attr("href");
    }

    getToString(itemPrefix?: string): string {
        return $(this.pf(Entities.Keys.link)).text();
    }

}

