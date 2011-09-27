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

})();