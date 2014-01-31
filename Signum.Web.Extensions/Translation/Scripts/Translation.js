/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports"], function(require, exports) {
    function pluralAndGender() {
        var url = $("#results").attr("data-pluralAndGender");

        $("#results").on("change", "textarea[name$='.Description'], select[name$='.Description']", function () {
            var name = $(this).attr("name");
            $.post(url, { name: name, text: $(this).val() }, function (data) {
                $("[name='" + name.replace(".Description", ".PluralDescription") + "']").val(data.plural);
                $("[name='" + name.replace(".Description", ".Gender") + "']").val(data.gender);
            });
        });
    }
    exports.pluralAndGender = pluralAndGender;

    function editAndRemember(remember) {
        $("a.edit").bind("click", function () {
            var select = $(this).parent().find("select");
            var input = $("<textarea/>").attr("type", "text").attr("name", select.attr("name")).val(select.val()).attr("style", "width:90%");
            select.after(input);

            $(this).remove();

            if (!remember) {
                select.remove();
            } else {
                var selectName = select.attr("name");

                input.after($("<button/>").click(exports.onFeedbackClick).text(lang.translation.RememberChange));

                select.attr('name', selectName + "#Automatic");
                select.hide();
            }

            return false;
        });
    }
    exports.editAndRemember = editAndRemember;

    function onFeedbackClick(e) {
        e.preventDefault();

        var $this = $(this);

        var original = $this.parent().find("select").val();
        var fixed = $this.parent().find("textarea").val();

        while (original.length > 0 && fixed.length > 0 && original.charAt(0) == fixed.charAt(0)) {
            original = original.substring(1);
            fixed = fixed.substring(1);
        }

        while (original.length > 0 && fixed.length > 0 && original.charAt(original.length - 1) == fixed.charAt(fixed.length - 1)) {
            original = original.substring(0, original.length - 2);
            fixed = fixed.substring(0, fixed.length - 2);
        }

        var wrong;
        do {
            wrong = prompt(lang.translation.WrongTranslationToSubstitute, original);
            if (wrong == null)
                return;
        } while(wrong.length == 0);

        var right;
        do {
            right = prompt(lang.translation.RightTranslation, fixed);

            if (right == null)
                return;
        } while(right.length == 0);

        var $results = $this.parents("#results");

        $.post($results.attr("data-feedback"), { culture: $results.attr("data-culture"), wrong: wrong, right: right }, function (data) {
        });

        return;
    }
    exports.onFeedbackClick = onFeedbackClick;
});
//# sourceMappingURL=Translation.js.map
