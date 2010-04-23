function CallServer(urlController, prefix) {
    var ids = GetSelectedElements(prefix);
    if (ids == "") return;
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfIds=" + ids,
        async: false,
        dataType: "html",
        success: function(msg) {
            window.alert(msg);
        }
    });
}

function CloseChooser(urlController, onOk, onCancel, prefix) {
    var container = $('#' + prefix.compose("externalPopupDiv")).parent();
    $('#' + prefix.compose(sfBtnCancel)).click();
    $.ajax({
        type: "POST",
        url: urlController,
        data: "sfOnOk=" + singleQuote(onOk) + qp("sfOnCancel", singleQuote(onCancel)) + qp(sfPrefix, prefix),
        async: false,
        dataType: "html",
        success: function(msg) {
            container.html(msg);
            ShowPopup(prefix, container[0].id, "modalBackground", "panelPopup");
        }
    });
}

function QuickLinkClickServerAjax(urlController, findOptionsRaw, prefix) {
    var newPrefix = "New".compose(prefix);
    $.ajax({
        type: "POST",
        url: urlController,
        data: findOptionsRaw + qp(sfPrefix, newPrefix),
        async: false,
        dataType: "html",
        success: function(msg) {
            if (msg.indexOf("ModelState") > 0) {
                eval('var result=' + msg);
                var modelState = result["ModelState"];
                ShowErrorMessages(newPrefix, modelState, true, "*");
            }
            else {
                $('#' + prefix.compose("divASustituir")).html(msg);
                ShowPopup(newPrefix, prefix.compose("divASustituir"), "modalBackground", "panelPopup");
                $('#' + newPrefix.compose(sfBtnCancel)).click(function() { $('#' + prefix.compose("divASustituir")).html("") });
            }
        }
    });
}