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
        $newsBodyContentPreview.contents().find("html").html($(".sf-email-htmlwrite").val());
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