var SF = SF || {};

SF.ControlPanel = (function () {
    $(function () {
        $(".sf-cp-part").draggable({
            snap: ".sf-cp-part-container",
            start: function (event, ui) { $(".sf-cp-part-container").addClass("sf-cp-dragging"); },
            stop: function (event, ui) { $(".sf-cp-part-container").removeClass("sf-cp-dragging"); }
        });

        $(".sf-cp-part-container").droppable({
            hoverClass: "ui-state-active"
        });
    });
})();