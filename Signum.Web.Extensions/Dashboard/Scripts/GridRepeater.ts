/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")


function _prefix(repeaterItem: Element) {
        return (<HTMLElement>repeaterItem).id.parent("sfRepeaterItem")
}

function _get(repeaterItem: Element, field: string) : string {
    var prefix = _prefix(repeaterItem);

    return prefix.child(field).get(repeaterItem).val();
}

function _set(repeaterItem: Element, field: string, value: string) : void {
    var prefix = _prefix(repeaterItem);

    prefix.child(field).get(repeaterItem).val(value);
}

function _overlaps(
    a: { startColumn: number; columns: number },
    b: { startColumn: number; columns: number }): boolean {

    if ((a.startColumn + a.columns) <= b.startColumn)
        return false;

    if ((b.startColumn + b.columns) <= a.startColumn)
        return false;

    return true;
}

export class GridRepeater extends Lines.EntityRepeater {
    static key_gridRepeaterItemClass = "sf-grid-element";

    static gridClasses =
    "col-sm-1 col-sm-2 col-sm-3 col-sm-4 col-sm-5 col-sm-6 " +
    "col-sm-7 col-sm-8 col-sm-9 col-sm-10 col-sm-11 col-sm-12 " +
    "col-sm-offset-1 col-sm-offset-2 col-sm-offset-3 col-sm-offset-4 col-sm-offset-5 col-sm-offset-6 " +
    "col-sm-offset-7 col-sm-offset-8 col-sm-offset-9 col-sm-offset-10 col-sm-offset-11 col-sm-offset-0";


    _create() {
        super._create();

        this.setupResizer();
        this.setupMover();
        this.setupRemove();
    }


    dragMode: string;

    setupResizer(){
        var container = this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get();

        var currentElement: JQuery;
        var currentRow: JQuery;
        var currentClass: string;

        container.on("dragstart", ".sf-rightHandle, .sf-leftHandle", e => {
            var de = <DragEvent><Event>e.originalEvent;

            var handler = $(e.currentTarget); 
            currentElement = handler.closest(".sf-grid-element");
            currentRow = currentElement.closest(".items-row");
            currentClass = handler.attr("class");
            de.dataTransfer.effectAllowed = "move";
            de.dataTransfer.setData("Text", "Text");
            this.dragMode = "resize";
        }); 

        container.on("dragover", ".items-row", e=> {
            var de = <DragEvent><Event>e.originalEvent;
            de.preventDefault();
            if (!currentElement || !currentRow || currentRow[0] != e.currentTarget || this.dragMode != "resize") {
                de.dataTransfer.dropEffect = "none";
                return;
            }
         
            de.dataTransfer.dropEffect = "move";

            var row = $(e.currentTarget);

            var isRight = currentClass == "sf-rightHandle"; 

            var offsetX = (de.pageX + (isRight ? 15 : -15)) - row.offset().left;
            var col = Math.round((offsetX / row.width()) * 12);

            if (isRight) {
                col = Math.min(col, this.maxLimit(currentElement));
                var startColumn = parseInt(_get(currentElement[0], "StartColumn"));
                _set(currentElement[0], "Columns", Math.max(1, col - startColumn).toString());
            } else {
                col = Math.max(col, this.minLimit(currentElement));
                var startColumn = parseInt(_get(currentElement[0], "StartColumn"));
                var columns = parseInt(_get(currentElement[0], "Columns"));
                var endColumn = startColumn + columns;
                col = Math.min(col, endColumn - 1);
                _set(currentElement[0], "StartColumn", col.toString());
                _set(currentElement[0], "Columns", (endColumn - col).toString());
            }

            this.redrawColumns(currentRow)
        }); 
    }

    setupMover() {
        var container = this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get();

        var currentElement: JQuery;
        var currentRow: JQuery;
        var dx : number;

        container.on("dragstart", ".panel-heading", e => {
            var de = <DragEvent><Event>e.originalEvent;

            var handler = $(e.currentTarget);
            currentElement = handler.closest(".sf-grid-element");
            currentRow = currentElement.closest(".items-row");
            de.dataTransfer.effectAllowed = "move";
            de.dataTransfer.setData("Text", "Text");

            dx = de.pageX - currentElement.offset().left;
            this.dragMode = "move";

            container.addClass("sf-dragging");
        });

        container.on("dragend", ".panel-heading", e => {
            container.removeClass("sf-dragging");
        });

        container.on("dragover", ".items-row", e=> {
            var de = <DragEvent><Event>e.originalEvent;
            de.preventDefault();
            if (!currentElement || !currentRow || this.dragMode != "move") {
                de.dataTransfer.dropEffect = "none";
                return;
            }
            de.dataTransfer.dropEffect = "move";

            var row = $(e.currentTarget);

            var offsetX = (de.pageX - dx) - row.offset().left;
            var startCol = Math.round((offsetX / row.width()) * 12);

            var cols = parseInt(_get(currentElement[0], "Columns"))

            var newPart = {
                startColumn: startCol,
                columns: cols,
            }; 

            if (newPart.startColumn < 0 || 12 < newPart.columns + newPart.startColumn)
                return;

            var current = row.find("." + GridRepeater.key_gridRepeaterItemClass).toArray()
                .filter(e=> e != currentElement[0])
                .map(e=> ({
                    startColumn: parseInt(_get(e, "StartColumn")),
                    columns: parseInt(_get(e, "Columns")),
                }));

            if (current.some(a=> _overlaps(a, newPart)))
                return; 

            _set(currentElement[0], "StartColumn", startCol.toString());

            if (currentRow[0] != row[0]) {
                currentElement.detach();
                row.append(currentElement);
                this.redrawColumns(row);
                this.redrawColumns(currentRow);

                this.saveRows();
            }
            else {
                this.redrawColumns(row);
            }
        });

        container.on("dragenter", ".separator-row", e=> {
            var de = <DragEvent><Event>e.originalEvent;
            de.preventDefault();
            if (!currentElement || !currentRow || this.dragMode != "move") {
                de.dataTransfer.dropEffect = "none";
                return;
            }

            de.dataTransfer.dropEffect = "move";

            $(e.currentTarget).addClass("sf-over");
        }); 

        container.on("dragover", ".separator-row", e=> {
            var de = <DragEvent><Event>e.originalEvent;
            de.preventDefault();
            if (!currentElement || !currentRow || this.dragMode != "move") {
                de.dataTransfer.dropEffect = "none";
                return;
            }

            de.dataTransfer.dropEffect = "move";
        }); 

        container.on("dragleave", ".separator-row", e=> {
            var de = <DragEvent><Event>e.originalEvent;
            de.preventDefault();
            if (!currentElement || !currentRow || this.dragMode != "move") {
                de.dataTransfer.dropEffect = "none";
                return;
            }

            de.dataTransfer.dropEffect = "move";

            $(e.currentTarget).removeClass("sf-over");
        }); 

        container.on("drop", ".separator-row", e=> {
            var de = <DragEvent><Event>e.originalEvent;
            de.preventDefault();
            if (!currentElement || !currentRow || this.dragMode != "move") {
                de.dataTransfer.dropEffect = "none";
                return;
            }

            var separator = $(e.currentTarget);

            var newRow = $("<div>").addClass("row items-row").insertBefore(separator);

            newRow.before($("<div>").addClass("row separator-row"));

            currentElement.detach();

            newRow.append(currentElement);

            _set(currentElement[0], "Columns", "12");
            _set(currentElement[0], "StartColumn", "0");

            this.saveRows();
        }); 
    }

    setupRemove() {
        this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().on("click", ".sf-grid-element  > .panel > .panel-heading > .sf-remove", e => {
            this.removeItem_click((<HTMLElement>e.currentTarget).id.parent("btnRemove"));
        }); 
    }

    removeEntitySpecific(itemPrefix: string) {

        itemPrefix.child(Lines.EntityRepeater.key_repeaterItem).get().remove();

        this.saveRows();
    }

  



    maxLimit(element : JQuery) {
        var next = element.next();

        if (next.length)
            return parseInt(_get(next[0], "StartColumn"));

        return 12; 
    }

    minLimit(element: JQuery) {
        var prev = element.prev();

        if (prev.length) {
            return parseInt(_get(prev[0], "StartColumn")) + parseInt(_get(prev[0], "Columns"));
        }

        return 0;
    }

    getItems() {
        return this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().find("." + GridRepeater.key_gridRepeaterItemClass);
    }

    //The hiddens rules over DOM to simplify non-accumulative start-column (instead of offsets)
    redrawColumns(row: JQuery) {
        var elements = row.find("." + GridRepeater.key_gridRepeaterItemClass).toArray();

        var actual = elements.map(a=> parseInt(_get(a, "StartColumn")));
        var ordered = actual.orderBy(a=> a);

        if (actual.join(",") != ordered.join(",")) {
            $(elements).detach();
            elements = elements.orderBy(a=> parseInt(_get(a, "StartColumn")));
        }

        var prevEndColumn = 0; 
        elements.forEach((elem) => {
            var cols = parseInt(_get(elem, "Columns")); 
            var startCol = parseInt(_get(elem, "StartColumn"));

            var newClass = "col-sm-" + cols + " col-sm-offset-" + (startCol - prevEndColumn);
            var $elem = $(elem);

            if (!$elem.hasClass(newClass))
                $elem.removeClass(GridRepeater.gridClasses).addClass(newClass);

            prevEndColumn = cols + startCol
        }); 

        if (actual.join(",") != ordered.join(","))
            row.append(elements);
    }

    //The DOM rules over the hiddens to simplify row re-indexing
    saveRows() {
        this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().children(".items-row").each((i, e)=> {
            var row = $(e);

            if (row.children().length)
                return;

            row.prev().remove();
            row.remove();
        });

        this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().children(".items-row").each((index, row) => {
            $("." + GridRepeater.key_gridRepeaterItemClass, row).each((_, elem) => {
                _set(elem, "Row", index.toString())
            }); 
        }); 
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {

        var eHtml = <Entities.EntityHtml>entityValue;

        this.prefix.child(Lines.EntityTabRepeater.key_itemsContainer).get()
            .append($("<div>").addClass("row items-row")
                .append(eHtml.html));

        this.prefix.child(Lines.EntityTabRepeater.key_itemsContainer).get()
            .append($("<div>").addClass("row separator-row"));

        this.saveRows();

        eHtml.html = null;
    }

    getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }
}



