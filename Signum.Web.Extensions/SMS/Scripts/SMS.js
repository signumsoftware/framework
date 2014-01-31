/// <reference path="../../../../Framework/Signum.Web/Signum/Headers/jquery/jquery.d.ts"/>
/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities"], function(require, exports, Entities) {
    var SMSMaxTextLength;
    var SMSWarningTextLength;

    var normalCharacters;
    var doubleCharacters;

    function init(removeCharactersUrl, getDictionariesUrl) {
        if (!editable()) {
            return;
        }
        $.ajax({
            url: getDictionariesUrl,
            data: {},
            async: false,
            success: function (data) {
                SMSMaxTextLength = data.smsLength;
                SMSWarningTextLength = data.smsWarningLength;
                normalCharacters = data.normalChar;
                doubleCharacters = data.doubleChar;
                $('.sf-sms-chars-left:visible').html(SMSMaxTextLength);
            }
        });

        var $textAreasPresent = $(".sf-sms-msg-text");
        for (var i = 0; i < $textAreasPresent.length; i++) {
            remainingCharacters($($textAreasPresent[i]));
        }

        exports.fillLiterals();

        $(document).on('keyup', 'textarea.sf-sms-msg-text', function () {
            remainingCharacters();
        });

        $(document).on('click', '.sf-sms-remove-chars', function () {
            var $textarea = $control();
            $.ajax({
                dataType: "text",
                url: removeCharactersUrl,
                data: { text: $textarea.val() },
                success: function (result) {
                    $textarea.val(result);
                    remainingCharacters();
                }
            });
        });

        $("#sfLiterals").dblclick(function () {
            insertLiteral();
        });

        $("#sfInsertLiteral").click(function () {
            insertLiteral();
        });
    }

    function $control() {
        return $('.sf-sms-msg-text:visible');
    }

    function editable() {
        return $control().length > 0 || $(".sf-sms-template-messages").length > 0;
    }

    function charactersToEnd($textarea) {
        if (!editable()) {
            return;
        }
        var text = $textarea.val();
        var count = text.length;
        var maxLength = SMSMaxTextLength;
        for (var l = 0; l < text.length; l++) {
            var current = text.charAt(l);
            if (normalCharacters.indexOf(current) == -1) {
                if (doubleCharacters.indexOf(current) != -1) {
                    count++;
                } else {
                    maxLength = 60;
                    count = text.length;
                    break;
                }
            }
        }
        return maxLength - count;
    }

    function remainingCharacters($textarea) {
        $textarea = $textarea || $control();
        var $remainingChars = $textarea.closest(".sf-sms-edit-container").find('.sf-sms-chars-left');
        var $remainingCharacters = $textarea.closest(".sf-sms-edit-container").find('.sf-sms-characters-left > p');

        var numberCharsLeft = charactersToEnd($textarea);
        $remainingChars.html(numberCharsLeft.toString());

        $remainingCharacters.removeClass('sf-sms-no-more-chars').removeClass('sf-sms-warning');
        $textarea.removeClass('sf-sms-red');
        $remainingChars.removeClass('sf-sms-highlight');

        if (numberCharsLeft < 0) {
            $remainingCharacters.addClass('sf-sms-no-more-chars');
            $remainingChars.addClass('sf-sms-highlight');
            $textarea.addClass('sf-sms-red');
        } else if (numberCharsLeft < SMSWarningTextLength) {
            $remainingCharacters.addClass('sf-sms-warning');
            $remainingChars.addClass('sf-sms-highlight');
        }
    }

    function fillLiterals() {
        var $combo = $(".sf-associated-type");
        var prefix = $combo.attr("data-control-id");
        var url = $combo.attr("data-url");
        var $list = $("#sfLiterals");
        if ($list.length == 0) {
            return;
        }
        var runtimeInfo = new Entities.RuntimeInfoElement(prefix);
        if (SF.isEmpty(runtimeInfo.value)) {
            $list.html("");
            return;
        }
        $.ajax({
            url: url,
            data: runtimeInfo.value().toString(),
            success: function (data) {
                $list.html("");
                for (var i = 0; i < data.literals.length; i++) {
                    $list.append($("<option>").val(data.literals[i]).html(data.literals[i]));
                }
                remainingCharacters();
            }
        });
    }
    exports.fillLiterals = fillLiterals;

    function insertLiteral() {
        var selected = $("#sfLiterals").find(":selected").val();
        if (selected == "") {
            alert("No element selected");
            return;
        }
        var $message = $control();
        $message.val($message.val().substring(0, $message[0].selectionStart) + selected + $message.val().substring($message[0].selectionEnd));
    }
});
//# sourceMappingURL=SMS.js.map
