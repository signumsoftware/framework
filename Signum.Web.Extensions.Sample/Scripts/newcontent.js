$(function () {
    $("body").bind("sf-new-content", function (e) {
        var $newContent = $(e.target);

        SF.NewContentProcessor.defaultButtons($newContent);
        SF.NewContentProcessor.defaultDatepicker($newContent);
        SF.NewContentProcessor.defaultTabs($newContent);
        SF.NewContentProcessor.defaultDropdown($newContent);
        SF.NewContentProcessor.defaultAutocomplete($newContent);
    });

    $("body").trigger("sf-new-content");
});