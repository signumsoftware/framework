/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Lines"], function(require, exports, Lines) {
    function _prefix(repeaterItem) {
        return repeaterItem.id.parent("sfRepeaterItem");
    }

    function _get(repeaterItem, field) {
        var prefix = _prefix(repeaterItem);

        return prefix.child(field).get(repeaterItem).val();
    }

    function _set(repeaterItem, field, value) {
        var prefix = _prefix(repeaterItem);

        prefix.child(field).get(repeaterItem).val(value);
    }

    function _overlaps(a, b) {
        if ((a.startColumn + a.columns) <= b.startColumn)
            return false;

        if ((b.startColumn + b.columns) <= a.startColumn)
            return false;

        return true;
    }

    var GridRepeater = (function (_super) {
        __extends(GridRepeater, _super);
        function GridRepeater() {
            _super.apply(this, arguments);
        }
        GridRepeater.prototype._create = function () {
            _super.prototype._create.call(this);

            this.setupResizer();
            this.setupMover();
            this.setupRemove();
        };

        GridRepeater.prototype.setupResizer = function () {
            var _this = this;
            var container = this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get();

            var currentElement;
            var currentRow;
            var currentClass;

            container.on("dragstart", ".sf-rightHandle, .sf-leftHandle", function (e) {
                var de = e.originalEvent;

                var handler = $(e.currentTarget);
                currentElement = handler.closest(".sf-grid-element");
                currentRow = currentElement.closest(".items-row");
                currentClass = handler.attr("class");
                de.dataTransfer.effectAllowed = "move";
                de.dataTransfer.setData("Text", "Text");
                _this.dragMode = "resize";
            });

            container.on("dragover", ".items-row", function (e) {
                var de = e.originalEvent;
                de.preventDefault();
                if (!currentElement || !currentRow || currentRow[0] != e.currentTarget || _this.dragMode != "resize") {
                    de.dataTransfer.dropEffect = "none";
                    return;
                }

                de.dataTransfer.dropEffect = "move";

                var row = $(e.currentTarget);

                var isRight = currentClass == "sf-rightHandle";

                var offsetX = (de.pageX + (isRight ? 15 : -15)) - row.offset().left;
                var col = Math.round((offsetX / row.width()) * 12);

                if (isRight) {
                    col = Math.min(col, _this.maxLimit(currentElement));
                    var startColumn = parseInt(_get(currentElement[0], "StartColumn"));
                    _set(currentElement[0], "Columns", Math.max(1, col - startColumn).toString());
                } else {
                    col = Math.max(col, _this.minLimit(currentElement));
                    var startColumn = parseInt(_get(currentElement[0], "StartColumn"));
                    var columns = parseInt(_get(currentElement[0], "Columns"));
                    var endColumn = startColumn + columns;
                    col = Math.min(col, endColumn - 1);
                    _set(currentElement[0], "StartColumn", col.toString());
                    _set(currentElement[0], "Columns", (endColumn - col).toString());
                }

                _this.redrawColumns(currentRow);
            });
        };

        GridRepeater.prototype.setupMover = function () {
            var _this = this;
            var container = this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get();

            var currentElement;
            var currentRow;
            var dx;

            container.on("dragstart", ".panel-heading", function (e) {
                var de = e.originalEvent;

                var handler = $(e.currentTarget);
                currentElement = handler.closest(".sf-grid-element");
                currentRow = currentElement.closest(".items-row");
                de.dataTransfer.effectAllowed = "move";
                de.dataTransfer.setData("Text", "Text");

                dx = de.pageX - currentElement.offset().left;
                _this.dragMode = "move";

                container.addClass("sf-dragging");
            });

            container.on("dragend", ".panel-heading", function (e) {
                container.removeClass("sf-dragging");
            });

            container.on("dragover", ".items-row", function (e) {
                var de = e.originalEvent;
                de.preventDefault();
                if (!currentElement || !currentRow || _this.dragMode != "move") {
                    de.dataTransfer.dropEffect = "none";
                    return;
                }
                de.dataTransfer.dropEffect = "move";

                var row = $(e.currentTarget);

                var offsetX = (de.pageX - dx) - row.offset().left;
                var startCol = Math.round((offsetX / row.width()) * 12);

                var cols = parseInt(_get(currentElement[0], "Columns"));

                var newPart = {
                    startColumn: startCol,
                    columns: cols
                };

                if (newPart.startColumn < 0 || 12 < newPart.columns + newPart.startColumn)
                    return;

                var current = row.find("." + GridRepeater.key_gridRepeaterItemClass).toArray().filter(function (e) {
                    return e != currentElement[0];
                }).map(function (e) {
                    return ({
                        startColumn: parseInt(_get(e, "StartColumn")),
                        columns: parseInt(_get(e, "Columns"))
                    });
                });

                if (current.some(function (a) {
                    return _overlaps(a, newPart);
                }))
                    return;

                _set(currentElement[0], "StartColumn", startCol.toString());

                if (currentRow[0] != row[0]) {
                    currentElement.detach();
                    row.append(currentElement);
                    _this.redrawColumns(row);
                    _this.redrawColumns(currentRow);

                    _this.saveRows();
                } else {
                    _this.redrawColumns(row);
                }
            });

            container.on("dragenter", ".separator-row", function (e) {
                var de = e.originalEvent;
                de.preventDefault();
                if (!currentElement || !currentRow || _this.dragMode != "move") {
                    de.dataTransfer.dropEffect = "none";
                    return;
                }

                de.dataTransfer.dropEffect = "move";

                $(e.currentTarget).addClass("sf-over");
            });

            container.on("dragover", ".separator-row", function (e) {
                var de = e.originalEvent;
                de.preventDefault();
                if (!currentElement || !currentRow || _this.dragMode != "move") {
                    de.dataTransfer.dropEffect = "none";
                    return;
                }

                de.dataTransfer.dropEffect = "move";
            });

            container.on("dragleave", ".separator-row", function (e) {
                var de = e.originalEvent;
                de.preventDefault();
                if (!currentElement || !currentRow || _this.dragMode != "move") {
                    de.dataTransfer.dropEffect = "none";
                    return;
                }

                de.dataTransfer.dropEffect = "move";

                $(e.currentTarget).removeClass("sf-over");
            });

            container.on("drop", ".separator-row", function (e) {
                var de = e.originalEvent;
                de.preventDefault();
                if (!currentElement || !currentRow || _this.dragMode != "move") {
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

                _this.saveRows();
            });
        };

        GridRepeater.prototype.setupRemove = function () {
            var _this = this;
            this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().on("click", ".sf-grid-element  > .panel > .panel-heading > .sf-remove", function (e) {
                _this.removeItem_click(e.currentTarget.id.parent("btnRemove"));
            });
        };

        GridRepeater.prototype.removeEntitySpecific = function (itemPrefix) {
            itemPrefix.child(Lines.EntityRepeater.key_repeaterItem).get().remove();

            this.saveRows();
        };

        GridRepeater.prototype.maxLimit = function (element) {
            var next = element.next();

            if (next.length)
                return parseInt(_get(next[0], "StartColumn"));

            return 12;
        };

        GridRepeater.prototype.minLimit = function (element) {
            var prev = element.prev();

            if (prev.length) {
                return parseInt(_get(prev[0], "StartColumn")) + parseInt(_get(prev[0], "Columns"));
            }

            return 0;
        };

        GridRepeater.prototype.getItems = function () {
            return this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().find("." + GridRepeater.key_gridRepeaterItemClass);
        };

        //The hiddens rules over DOM to simplify non-accumulative start-column (instead of offsets)
        GridRepeater.prototype.redrawColumns = function (row) {
            var elements = row.find("." + GridRepeater.key_gridRepeaterItemClass).toArray();

            var actual = elements.map(function (a) {
                return parseInt(_get(a, "StartColumn"));
            });
            var ordered = actual.orderBy(function (a) {
                return a;
            });

            if (actual.join(",") != ordered.join(",")) {
                $(elements).detach();
                elements = elements.orderBy(function (a) {
                    return parseInt(_get(a, "StartColumn"));
                });
            }

            var prevEndColumn = 0;
            elements.forEach(function (elem) {
                var cols = parseInt(_get(elem, "Columns"));
                var startCol = parseInt(_get(elem, "StartColumn"));

                var newClass = "col-sm-" + cols + " col-sm-offset-" + (startCol - prevEndColumn);
                var $elem = $(elem);

                if (!$elem.hasClass(newClass))
                    $elem.removeClass(GridRepeater.gridClasses).addClass(newClass);

                prevEndColumn = cols + startCol;
            });

            if (actual.join(",") != ordered.join(","))
                row.append(elements);
        };

        //The DOM rules over the hiddens to simplify row re-indexing
        GridRepeater.prototype.saveRows = function () {
            this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().children(".items-row").each(function (i, e) {
                var row = $(e);

                if (row.children().length)
                    return;

                row.prev().remove();
                row.remove();
            });

            this.prefix.child(Lines.EntityRepeater.key_itemsContainer).get().children(".items-row").each(function (index, row) {
                $("." + GridRepeater.key_gridRepeaterItemClass, row).each(function (_, elem) {
                    _set(elem, "Row", index.toString());
                });
            });
        };

        GridRepeater.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var eHtml = entityValue;

            this.prefix.child(Lines.EntityTabRepeater.key_itemsContainer).get().append($("<div>").addClass("row items-row").append(eHtml.html));

            this.prefix.child(Lines.EntityTabRepeater.key_itemsContainer).get().append($("<div>").addClass("row separator-row"));

            this.saveRows();

            eHtml.html = null;
        };

        GridRepeater.prototype.getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };
        GridRepeater.key_gridRepeaterItemClass = "sf-grid-element";

        GridRepeater.gridClasses = "col-sm-1 col-sm-2 col-sm-3 col-sm-4 col-sm-5 col-sm-6 " + "col-sm-7 col-sm-8 col-sm-9 col-sm-10 col-sm-11 col-sm-12 " + "col-sm-offset-1 col-sm-offset-2 col-sm-offset-3 col-sm-offset-4 col-sm-offset-5 col-sm-offset-6 " + "col-sm-offset-7 col-sm-offset-8 col-sm-offset-9 col-sm-offset-10 col-sm-offset-11 col-sm-offset-0";
        return GridRepeater;
    })(Lines.EntityRepeater);
    exports.GridRepeater = GridRepeater;
});
//# sourceMappingURL=GridRepeater.js.map
