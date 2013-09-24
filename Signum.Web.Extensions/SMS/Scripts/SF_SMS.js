var SF = SF || {};

if (typeof SF.SMS != "undefined") {
    SF.SMS.init();
}
else {
    SF.SMS = (function () {
        var SMSMaxTextLength;
        var SMSWarningTextLength;

        var normalCharacters;
        var doubleCharacters;

        var init = function () {
            if (!editable()) {
                return;
            }

            loadLists();

            var $textAreasPresent = $(".sf-sms-msg-text");
            for (var i = 0; i < $textAreasPresent.length; i++) {
                remainingCharacters($($textAreasPresent[i]));
            }

            fillLiterals();

            $(document).on('keyup', 'textarea.sf-sms-msg-text', function () {
                remainingCharacters();
            });

            $(document).on('click', '.sf-sms-remove-chars', function () {
                var $textarea = $control();
                $.ajax({
                    dataType: "text",
                    url: SF.Urls.removeCharacters,
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
        };

        var $control = function () {
            return $('.sf-sms-msg-text:visible');
        };

        var editable = function () {
            return $control().length > 0 || $(".sf-sms-template-messages").length > 0;
        };

        var charactersToEnd = function ($textarea) {
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
                    }
                    else {
                        maxLength = 60;
                        count = text.length;
                        break;
                    }
                }
            }
            return maxLength - count;
        };

        var loadLists = function () {
            $.ajax({
                url: SF.Urls.getDictionaries,
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
        };

        var remainingCharacters = function ($textarea) {
            $textarea = $textarea || $control();
            var $remainingChars = $textarea.closest(".sf-sms-edit-container").find('.sf-sms-chars-left');
            var $remainingCharacters = $textarea.closest(".sf-sms-edit-container").find('.sf-sms-characters-left > p');

            var numberCharsLeft = charactersToEnd($textarea);
            $remainingChars.html(numberCharsLeft);

            $remainingCharacters.removeClass('sf-sms-no-more-chars').removeClass('sf-sms-warning');
            $textarea.removeClass('sf-sms-red');
            $remainingChars.removeClass('sf-sms-highlight');

            if (numberCharsLeft < 0) {
                $remainingCharacters.addClass('sf-sms-no-more-chars');
                $remainingChars.addClass('sf-sms-highlight');
                $textarea.addClass('sf-sms-red');
            }
            else if (numberCharsLeft < SMSWarningTextLength) {
                $remainingCharacters.addClass('sf-sms-warning');
                $remainingChars.addClass('sf-sms-highlight');
            }
        };

        var fillLiterals = function () {
            var $combo = $(".sf-associated-type");
            var prefix = $combo.attr("data-control-id");
            var url = $combo.attr("data-url");
            var $list = $("#sfLiterals");
            if ($list.length == 0) {
                return;
            }
            var runtimeInfo = new SF.RuntimeInfo(prefix);
            if (SF.isEmpty(runtimeInfo.entityType())) {
                $list.html("");
                return;
            }
            $.ajax({
                url: url,
                data: runtimeInfo.find().serialize(),
                success: function (data) {
                    $list.html("");
                    for (var i = 0; i < data.literals.length; i++) {
                        $list.append($("<option>").val(data.literals[i]).html(data.literals[i]));
                    }
                    remainingCharacters();
                }
            });
        };

        var insertLiteral = function () {
            var selected = $("#sfLiterals").find(":selected").val();
            if (selected == "") {
                alert("No element selected");
                return;
            }
            var $message = $control();
            $message.val(
                $message.val().substring(0, $message[0].selectionStart) +
                selected +
                $message.val().substring($message[0].selectionEnd)
                );
        };

        return {
            init: init,
            fillLiterals: fillLiterals
        };
    })();

    SF.SMS.init();
}
