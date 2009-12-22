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
    var container = $('#' + prefix + "externalPopupDiv").parent();
    $('#' + prefix + sfBtnCancel).click();
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
    var newPrefix = prefix + "New";
    $.ajax({
        type: "POST",
        url: urlController,
        data: findOptionsRaw + qp(sfPrefix, newPrefix) + qp("prefixEnd", "S"),
        async: false,
        dataType: "html",
        success: function(msg) {
            if (msg.indexOf("ModelState") > 0) {
                eval('var result=' + msg);
                var modelState = result["ModelState"];
                ShowErrorMessages(newPrefix, modelState, true, "*");
            }
            else {
                $('#' + prefix + "divASustituir").html(msg);
                ShowPopup(newPrefix, prefix + "divASustituir", "modalBackgroundS", "panelPopupS");
                $('#' + newPrefix + sfBtnCancelS).click(function() { $('#' + prefix + "divASustituir").html("") });
            }
        }
    });
}