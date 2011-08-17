var SF = SF || {};

SF.ControlPanel = (function () {
    var createDraggables = function () {
        $(".sf-cp-part").draggable({
            handle: ".sf-cp-part-header",
            snap: ".sf-cp-part-container",
            revert: "invalid",
            start: function (event, ui) {
                //if last row has non-empty cells => add another row to allow to add a part to a full column
                var $this = $(this);
                var $table = $this.closest("table");
                var $lastTr = $table.find("tr:last");
                if ($lastTr.find("td").not(":empty").length > 0) {
                    var $lastTd = $lastTr.find("td:last");
                    var newRowIndex = parseInt($lastTd.attr("data-row"), 10) + 1;
                    var numCols = $lastTd.attr("data-column");
                    var $newTr = $("<tr></tr>");
                    for (var i = 1; i <= numCols; i++) {
                        var $td = $("<td></td>").addClass("sf-cp-part-container").attr("data-row", newRowIndex).attr("data-column", i);
                        $newTr.append($td);
                    }
                    $table.append($newTr);
                    createDroppables($this.closest("table").find("tr:last").find("td"));
                }

                $(".sf-cp-part-container").addClass("sf-cp-dragging");
            },
            stop: function (event, ui) { $(".sf-cp-part-container").removeClass("sf-cp-dragging"); }
        });
    };

    var createDroppables = function ($target) {
        $target.droppable({
            hoverClass: "ui-state-highlight",
            tolerance: "pointer",
            drop: function (event, ui) {
                var $dragged = ui.draggable;
                var $startContainer = $dragged.closest(".sf-cp-part-container");

                var $targetContainer = $(this); //droppable
                var $replaced = $targetContainer.find(".sf-cp-part").not($dragged);

                var startRow = $startContainer.attr("data-row");
                var startColumn = $startContainer.attr("data-column");
                var targetRow = $targetContainer.attr("data-row");
                var targetColumn = $targetContainer.attr("data-column");

                // if target container is the same as start container => restore position
                if ((startRow == targetRow) && (startColumn == targetColumn)) {
                    $(".sf-cp-part").css({ top: 0, left: 0 });
                    $(".sf-cp-part-container").removeClass("sf-cp-dragging");
                    return;
                }

                //swap row and col indexes in hidden fields
                var partRowClass = "sf-cp-part-row";
                var partColumnClass = "sf-cp-part-col";

                $dragged.find("." + partRowClass).val(targetRow);
                $dragged.find("." + partColumnClass).val(targetColumn);

                if ($replaced.length > 0) {
                    $replaced.find("." + partRowClass).val(startRow);
                    $replaced.find("." + partColumnClass).val(startColumn);
                }

                //drag has been only visual, now update html 
                var targetOldHtml = $targetContainer.html();
                $targetContainer.html($startContainer.html());
                $startContainer.html(targetOldHtml);
                $(".sf-cp-part").css({ top: 0, left: 0 });

                //update draggable and droppable
                $(".sf-cp-part").draggable("destroy");
                createDraggables();

                var $droppables = $(".sf-cp-part-container");
                $droppables.droppable("destroy");
                createDroppables($droppables);

                $droppables.removeClass("sf-cp-dragging");
            }
        });
    };

    $(".sf-cp-part-header .sf-remove").live("click", function () {
        $(this).closest(".sf-cp-part-container").html("");
    });

    $(function () {
        createDraggables();
        createDroppables($(".sf-cp-part-container"));
    });
})();