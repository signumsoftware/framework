/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>


export function pluralAndGender() {
    var url = $("#results").attr("data-pluralAndGender");

    $("#results").on("change", "textarea[name$='.Description'], select[name$='.Description']", function () {
        var name = $(this).attr("name");
        $.post(url, { name: name, text: $(this).val() }, function (data) {
            $("[name='" + name.replace(".Description", ".PluralDescription") + "']").val(data.plural);
            $("[name='" + name.replace(".Description", ".Gender") + "']").val(data.gender);
        });
    });
}



export function fixTextAreas() {
    $("textarea").each((i, e) => fixTextArea(<HTMLTextAreaElement>e));
}

function fixTextArea(area: HTMLTextAreaElement) {
    area.style.height = "1px";
    area.style.height = area.scrollHeight + "px";
}

export function editAndRemember(remember: boolean) {

    $("a.edit").bind("click", function () {
        var select = $(this).parent().find("select");
        var input = $("<textarea/>").attr("type", "text")
            .attr("name", select.attr("name"))
            .val(select.val())
            .attr("style", "width:90%");
        select.after(input);

        fixTextArea(<HTMLTextAreaElement>input[0]);
        
        $(this).remove();

        if (!remember) {
            select.remove();
        }
        else {
            var selectName = select.attr("name");

            input.after($("<button/>")
                .click(onFeedbackClick)
                .text(lang.translation.RememberChange));

            select.attr('name', selectName + "#Automatic");
            select.hide();
        }

        return false;
    });

    $("button.rememberChange").bind("click", onFeedbackClick); 
}

function isSpace(str: string) {
    return str == " " ||
        str == "\t" ||
        str == "\n" ||
        str == "\r" ||
        str == "." ||
        str == "," ||
        str == ";" ||
        str == ":"; 
}


function onFeedbackClick(e: MouseEvent) {
    e.preventDefault();

    var $this = $(this);

    var textArea = $this.parent().find("textarea");

    var original = <string>textArea.filter("[disabled]").val();
    var fixed = <string>textArea.not("[disabled]").val();

    
    var word = "";
    while (original.length > 0 && fixed.length > 0 && original.charAt(0) == fixed.charAt(0)) {

        if (isSpace(original.charAt(0)))
            word = "";
        else
            word += original.charAt(0);

        original = original.substring(1);
        fixed = fixed.substring(1);
    }

    original = word + original;
    fixed = word + fixed;

    var word = "";
    while (original.length > 0 && fixed.length > 0 && original.charAt(original.length - 1) == fixed.charAt(fixed.length - 1)) {

        if (isSpace(original.charAt(original.length - 1)))
            word = "";
        else
            word = original.charAt(original.length - 1) + word;

        original = original.substring(0, original.length - 1);
        fixed = fixed.substring(0, fixed.length - 1);
    }

    original = original + word;
    fixed = fixed + word;

    var wrong;
    do {
        wrong = prompt(lang.translation.WrongTranslationToSubstitute, original);
        if (wrong == null)
            return;
    } while (wrong.length == 0);


    var right;
    do {
        right = prompt(lang.translation.RightTranslation, fixed);

        if (right == null)
            return;
    } while (right.length == 0);

    var $results = $this.parents("#results");

    $.post($results.attr("data-feedback"), { culture: $results.attr("data-culture"), wrong: wrong, right: right }, function (data) {

    });

    return;
}
