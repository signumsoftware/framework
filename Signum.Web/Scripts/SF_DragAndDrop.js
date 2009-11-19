$(document).ready(function () {
	carga();
});

function carga() {
	posicion = 0;
	// IE
	if (navigator.userAgent.indexOf("MSIE") >= 0) navegador = 0;
	// Otros
	else navegador = 1;
}
var _id;
var $id;
var width, height;
function comienzoMovimiento(event, id) {
	elMovimiento = document.getElementById(id);
	$('#' + id).css("cursor", "move");
	_id = id;
	$id = $('#' + _id);
	width=$id.width();
	height=$id.height();
	// Obtengo la posicion del cursor
	if (navegador == 0) {
		cursorComienzoX = window.event.clientX + document.documentElement.scrollLeft + document.body.scrollLeft;
		cursorComienzoY = window.event.clientY + document.documentElement.scrollTop + document.body.scrollTop;

		document.attachEvent("onmousemove", enMovimiento);
		document.attachEvent("onmouseup", finMovimiento);
	}
	if (navegador == 1) {
		cursorComienzoX = event.clientX + window.scrollX;
		cursorComienzoY = event.clientY + window.scrollY;

		document.addEventListener("mousemove", enMovimiento, true);
		document.addEventListener("mouseup", finMovimiento, true);
	}

	elComienzoX = parseInt(elMovimiento.style.left);
	if (isNaN(elComienzoX)) elComienzoX = 0;
	elComienzoY = parseInt(elMovimiento.style.top);
	if (isNaN(elComienzoY)) elComienzoY = 0;
	// Actualizo el posicion del elemento
	////elMovimiento.style.zIndex = ++posicion;
	evitaEventos(event);
}

function evitaEventos(event) {
	// Funcion que evita que se ejecuten eventos adicionales
	if (navegador == 0) {
		window.event.cancelBubble = true;
		window.event.returnValue = false;
	}
	if (navegador == 1) event.preventDefault();
}

function enMovimiento(event) {
	var xActual, yActual;
	if (navegador == 0) {
		xActual = window.event.clientX + document.documentElement.scrollLeft + document.body.scrollLeft;
		yActual = window.event.clientY + document.documentElement.scrollTop + document.body.scrollTop;
	}
	if (navegador == 1) {
		xActual = event.clientX + window.scrollX;
		yActual = event.clientY + window.scrollY;
	}
    var l = Math.min(Math.max(elComienzoX + xActual - cursorComienzoX,0), document.documentElement.clientWidth-width);
	var t = Math.min(Math.max(elComienzoY + yActual - cursorComienzoY,0), document.documentElement.clientHeight-height);
	elMovimiento.style.left = l + "px";
	elMovimiento.style.top = t + "px";

	evitaEventos(event);
}

function finMovimiento(event) {
	if (navegador == 0) {
		document.detachEvent("onmousemove", enMovimiento);
		document.detachEvent("onmouseup", finMovimiento);
	}
	if (navegador == 1) {
		document.removeEventListener("mousemove", enMovimiento, true);
		document.removeEventListener("mouseup", finMovimiento, true);
	}
	$id.css("cursor", "auto");
}