var SF = SF || {};

SF.ControlPanel = (function () {
    var partRowClass = "sf-cp-part-row";
    var partColumnClass = "sf-cp-part-col";

    var destroyDragAndDrop = function () {
        $(".sf-cp-part").draggable("destroy");
        $(".sf-cp-part-container").droppable("destroy");
    };

    var createDragAndDrop = function () {
        $(".sf-cp-part").draggable({
            handle: ".sf-cp-part-header",
            snap: ".sf-cp-part-container",
            revert: "invalid",
            start: function (event, ui) { $(".sf-cp-part-container").addClass("sf-cp-dragging"); },
            stop: function (event, ui) { $(".sf-cp-part-container").removeClass("sf-cp-dragging"); }
        });

        $(".sf-cp-part-container").droppable({
            hoverClass: "ui-state-active",
            tolerance: "pointer",
            drop: function (event, ui) {
                var $container = $(this); //droppable
                var $draggedPart = ui.draggable;
                var $targetPart = $container.find(".sf-cp-part").not($draggedPart);

                //swap row and col indexes in hidden fields
                var $targetRow = $targetPart.find("." + partRowClass);
                var $targetColumn = $targetPart.find("." + partColumnClass);

                var $draggedRow = $draggedPart.find("." + partRowClass);
                var $draggedColumn = $draggedPart.find("." + partColumnClass);

                var targetRow = $targetRow.val();
                var targetColumn = $targetColumn.val();
                $targetRow.val($draggedRow.val());
                $targetColumn.val($draggedColumn.val());
                $draggedRow.val(targetRow);
                $draggedColumn.val(targetColumn);

                //empty search-controls - searchonload will refresh it
                $(".sf-search-results tbody tr").remove();

                //drag has been only visual, now update html 
                //cache and empty both variables before setting new html (for searchcontrol parts searchonload collission fix)
                var $targetNewContainer = $draggedPart.closest(".sf-cp-part-container");
                var $draggedNewContainer = $targetPart.closest(".sf-cp-part-container");
                
                var targetOldHtml = $targetNewContainer.html();
                var draggedOldHtml = $draggedNewContainer.html();
                $targetNewContainer.html('');
                $draggedNewContainer.html('');
                $("#sfCpContainer").find("td#sfPanelPart_" + $draggedRow.val() + "_" + $draggedColumn.val()).html(targetOldHtml);
                $("#sfCpContainer").find("td#sfPanelPart_" + $targetRow.val() + "_" + $targetColumn.val()).html(draggedOldHtml);
                //$targetNewContainer.html(draggedOldHtml);
                //$draggedNewContainer.html(targetOldHtml);

                $(".sf-cp-part").css({ top: 0, left: 0 });

                destroyDragAndDrop();
                createDragAndDrop();
            }
        });
    };

    $(function () { createDragAndDrop(); })

    /*var toggleFillColumn = function (cbFillId, colInputId) {
    if ($("#" + cbFillId + ":checked").length > 0) {
    $('#' + colInputId).val(1).attr('disabled', 'disabled');
    }
    else {
    $('#' + colInputId).removeAttr('disabled');
    }
    };

    return {
    toggleFillColumn: toggleFillColumn
    };*/
})();