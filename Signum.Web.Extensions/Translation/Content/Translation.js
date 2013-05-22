$(function () {
    var url = $("#results").attr("pluralAndGender");

    $("#results").on("change", "textarea[name$='.Description'], select[name$='.Description']", function () {
        var name = $(this).attr("name");
        $.post(url, { name: name, text: $(this).val() }, function (data) {
            $("[name='" + name.replace(".Description", ".PluralDescription") + "']").val(data.plural);
            $("[name='" + name.replace(".Description", ".Gender") + "']").val(data.gender);
        });
    });
});