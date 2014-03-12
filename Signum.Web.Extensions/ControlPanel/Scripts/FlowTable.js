/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function init($containerTable) {
        createDraggables($containerTable.find(".sf-ftbl-part"));
        createDroppables($containerTable.find(".sf-ftbl-droppable"));

        $(document).on("click", ".sf-ftbl-part-header .sf-remove", function () {
            $(this).closest(".sf-ftbl-part-container").html("");
        });
    }
    exports.init = init;

    function setDraggingState(active) {
        if (active) {
            $(".sf-ftbl-column").addClass("sf-ftbl-dragging");
        } else {
            $(".sf-ftbl-dragging").removeClass("sf-ftbl-dragging");

            //as I clone parts it's not done automatically:
            $(".ui-draggable-dragging").removeClass("ui-draggable-dragging");
        }
    }

    function restoreDroppableSize($droppable) {
        $droppable.css({
            width: "inherit",
            height: "20px"
        });
    }

    function updateRowAndColIndexes($column) {
        var partRowClass = "sf-ftbl-part-row";
        var partColumnClass = "sf-ftbl-part-col";

        var column = $column.attr("data-column");

        $column.find(".sf-ftbl-part").each(function (index) {
            var $part = $(this);
            $part.find("." + partRowClass).val(index);
            $part.find("." + partColumnClass).val(column);
        });
    }

    function createDraggables($target) {
        //$target.draggable({
        //    handle: ".sf-ftbl-part-header",
        //    revert: "invalid",
        //    start: function (event, ui) { setDraggingState(true); },
        //    stop: function (event, ui) { setDraggingState(false); }
        //});
    }

    function createDroppables($target) {
        //$target.droppable({
        //    hoverClass: "ui-state-highlight sf-ftbl-droppable-active",
        //    tolerance: "pointer",
        //    over: function (event, ui) {
        //        var $draggedContainer = ui.draggable.closest(".sf-ftbl-part-container");
        //        $(this).css({
        //            width: $draggedContainer.width(),
        //            height: $draggedContainer.height()
        //        });
        //    },
        //    out: function (event, ui) {
        //        restoreDroppableSize($(this));
        //    },
        //    drop: function (event, ui) {
        //        var $dragged = <JQuery>ui.draggable;
        //        var $startContainer = $dragged.closest(".sf-ftbl-part-container");
        //        var $targetPlaceholder = $(this); //droppable
        //        var $targetCol = $targetPlaceholder.closest(".sf-ftbl-column");
        //        var $originCol = $startContainer.closest(".sf-ftbl-column");
        //        //update html (drag new position is only visual)
        //        var $newDroppable = $("<div></div>").addClass("sf-ftbl-droppable");
        //        var $clonedPart = $startContainer.clone();
        //        $targetPlaceholder.after($newDroppable).after($clonedPart);
        //        $(".sf-ftbl-part").css({ top: 0, left: 0 });
        //        //clear old elements
        //        $startContainer.next(".sf-ftbl-droppable").remove();
        //        $startContainer.html("").remove(); //empty before removing to force jquery ui clear current draggable bindings
        //        //set all row and col number for target column parts
        //        updateRowAndColIndexes($targetCol);
        //        //set all row and col number for origin column parts if it's different from target column
        //        if ($originCol.index() != $targetCol.index()) {
        //            updateRowAndColIndexes($originCol);
        //        }
        //        //create draggable and droppable of new elements
        //        createDroppables($newDroppable);
        //        createDraggables($clonedPart.find(".sf-ftbl-part"));
        //        setDraggingState(false);
        //        restoreDroppableSize($targetPlaceholder);
        //    }
        //});
    }
});
//# sourceMappingURL=FlowTable.js.map
