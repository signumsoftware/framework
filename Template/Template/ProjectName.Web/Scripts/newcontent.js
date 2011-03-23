$(function () {
    $("body").bind("sf-new-content", function (e) {
        var $newContent = $(e.target);

        SF.NewContentProcessor.defaultButtons($newContent);
        SF.NewContentProcessor.defaultDatepicker($newContent);
        SF.NewContentProcessor.defaultDropdown($newContent);
        SF.NewContentProcessor.defaultAutocomplete($newContent);
        SF.NewContentProcessor.defaultPlaceholder($newContent);
        SF.NewContentProcessor.defaultTabs($newContent);
    });

    $("body").trigger("sf-new-content");
});