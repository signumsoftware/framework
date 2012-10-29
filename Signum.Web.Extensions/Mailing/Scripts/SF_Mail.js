var SF = SF || {};

SF.Mail = (function () {
    var $bodyPreview = $("#newsBodyContent");

    if ($bodyPreview != null) {
        var content = $("#htmlBodyContent").val();
        $bodyPreview.contents().find("html").html(content);
        $bodyPreview.height($bodyPreview.contents().find("html").height());
    }

    var $newsPreviewContentButton = $("#newsPreviewContentButton");
    $newsPreviewContentButton.click(function () {
        var $newsBodyContentPreview = $("#newsBodyContentPreview");
        var doc = $newsBodyContentPreview[0].document;
        if ($newsBodyContentPreview[0].contentDocument)
            doc = $newsBodyContentPreview[0].contentDocument; // For NS6
        else if ($newsBodyContentPreview[0].contentWindow)
            doc = $newsBodyContentPreview[0].contentWindow.document; // For IE5.5 and IE6
        doc.open();
        doc.writeln($(".sf-email-htmlwrite").val());
        doc.close();
        $("#newsPreviewContent").show();
        $newsBodyContentPreview.height($newsBodyContentPreview.contents().find("html").height());
        $("#newsEditContent").hide();
    });

    var $newsEditContentButton = $("#newsEditContentButton");
    $newsEditContentButton.click(function () {
        $("#newsPreviewContent").hide();
        $("#newsEditContent").show();
    });

    var $messagePreviewContentButton = $("sf-email-messagePreviewContentButton");
    $messagePreviewContentButton.click(function () {
        var $container = $messagePreviewContentButton.closest(".sf-email-messageContainer");
        var $htmlBody = $container.find(".sf-email-sf-email-htmlBody");
        var doc = $htmlBody[0].document;
        if ($htmlBody[0].contentDocument)
            doc = $htmlBody[0].contentDocument; // For NS6
        else if ($htmlBody[0].contentWindow)
            doc = $htmlBody[0].contentWindow.document; // For IE5.5 and IE6
        doc.open();
        doc.writeln($(".sf-email-htmlwrite").val());
        doc.close();
        $container.find(".sf-email-messagePreviewContent").show();
        $htmlBody.height($htmlBody.contents().find("html").height());
        $container.find(".sf-email-messageEditContent").hide();
    });

    var $insertMasterTemplateTokenButton = $("#insertMasterTemplateTokenButton");
    $insertMasterTemplateTokenButton.click(function () {
        //var selected = $("#sfLiterals").find(":selected").val();
        insertToken("@[content]");
    });

    var $control = function () {
        return $('#Text');
    };

    var insertToken = function (token) {
        if (token == "") {
            alert(lang.signum.TheTokenIsNull);
            return;
        }
        var $message = $control();
        $message.val($message.val() + token);
    };



})();