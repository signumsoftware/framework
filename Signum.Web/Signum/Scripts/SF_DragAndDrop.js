var elemWidth, elemHeight, clientWidth, clientHeight, outside = false;
var element = null;
var lastMouseX;
var lastMouseY;
var allowInitiallyOutside = true;

function isOutside() {
    return element.offsetLeft < 0 || element.offsetTop < 0 || element.offsetLeft + elemWidth > clientWidth || element.offsetTop + elemHeight > clientHeight;
}

function getMousePosition(e) {
    var posx = 0;
    var posy = 0;

    if (window.event) {
        posx = window.event.clientX + document.documentElement.scrollLeft + document.body.scrollLeft;
        posy = window.event.clientY + document.documentElement.scrollTop + document.body.scrollTop;
    } else {
        posx = e.clientX + window.scrollX;
        posy = e.clientY + window.scrollY;
    }

    return {
        'x': posx,
        'y': posy
    };
}

function comienzoMovimiento(event, id) {
    element = document.getElementById(id);
    element.style.cursor = "move";
    elemWidth = element.offsetWidth;
    elemHeight = element.offsetHeight;
    clientWidth = document.documentElement.clientWidth;
    clientHeight = document.documentElement.clientHeight;
    outside = isOutside();

    var pos = getMousePosition(event);
    lastMouseX = pos.x;
    lastMouseY = pos.y;
    lastElemTop = element.offsetTop;
    lastElemLeft = element.offsetLeft;

    $(document).bind("mousemove", function(e) {
        var pos = getMousePosition(e);

        var left = ((allowInitiallyOutside && !outside) || !allowInitiallyOutside) ? Math.min(Math.max(lastElemLeft + pos.x - lastMouseX, 0), clientWidth - elemWidth) : lastElemLeft + pos.x - lastMouseX;

        var top = ((allowInitiallyOutside && !outside) || !allowInitiallyOutside) ? Math.min(Math.max(lastElemTop + pos.y - lastMouseY, 0), clientHeight - elemHeight) : lastElemTop + pos.y - lastMouseY;

        element.style.left = left + "px";
        element.style.top = top + "px";
        element.style.width = elemWidth;
        element.style.height = elemHeight;

        outside = isOutside();
        return false;

    }).bind("mouseup", function() {
        $(document).unbind("mousemove").unbind("mouseup");
        element.style.cursor = "auto";
    });

    evitaEventos(event);
}

function evitaEventos(event) {
    // Funcion que evita que se ejecuten eventos adicionales
    if (window.event) {
        window.event.cancelBubble = true;
        window.event.returnValue = false;
    } else event.preventDefault();
}
