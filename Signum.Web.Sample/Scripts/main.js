$(function() {
    $.address.change(function(event) {
        if (event.value != "/") {
            $('#content').load($("base").attr("href") + event.value + " #content");
        }
    });

    $('body').delegate("a", "click", function() {
        $.address.value($(this).attr('href'));
        return false;
    });
});