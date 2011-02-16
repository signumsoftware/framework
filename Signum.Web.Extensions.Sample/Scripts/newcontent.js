$(function () {
    $("body").bind("sf-new-content", function (e) {
        var $newContent = $(e.target);
        //buttons
        $newContent.find(".sf-entity-button, .sf-query-button, .sf-line-button, .sf-chooser-button").each(function (i, val) {
            var $txt = $(val);
            var data = $txt.data();
            $txt.button({ text: (!("text" in data) || SF.isTrue(data.text)), icons: { primary: data.icon, secondary: data["icon-secondary"]} });
        });

        //datepicker
        $newContent.find(".sf-datepicker").each(function (i, val) {
            var $txt = $(val);
            $txt.datepicker(jQuery.extend({}, SF.Locale.defaultDatepickerOptions, { dateFormat: $txt.attr("data-format") }));
        });

        //dropdown
        $newContent.find(".sf-dropdown .sf-menu-button")
            .addClass("ui-autocomplete ui-menu ui-widget ui-widget-content ui-corner-all")
            .find("li")
            .addClass("ui-menu-item")
            .find("a")
            .addClass("ui-corner-all");

        //autocomplete
        $newContent.find(".sf-entity-autocomplete").each(function (i, val) {
            var $txt = $(val);
            var data = $txt.data();
            SF.entityAutocomplete($txt, { delay: 200, types: data.types, url: data.url, count: 5 });
        });

        //input placeholder
        $newContent.find('input[placeholder], textarea[placeholder]').placeholder();

        //tabs
        $newContent.find(".sf-tabs").tabs();
    });

    $("body").trigger("sf-new-content");
});