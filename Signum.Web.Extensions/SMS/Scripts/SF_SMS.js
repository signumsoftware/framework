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
            loadLists($('#sfCharactersLeft').attr("data-url"));
            remainingCharacters();
            fillLiterals();

            $('textarea#Message').keyup(function () {
                remainingCharacters();
            });

            $('#sfRemoveNoSMSChars').click(function () {
                var $textarea = $control();
                $.ajax({
                    dataType: "text",
                    url: $(this).attr("data-url"),
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
            return $('#Message');
        };

        var editable = function () {
            return $control().length > 0;
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

        var loadLists = function (url) {
            $.ajax({
                url: url,
                data: {},
                async: false,
                success: function (data) {
                    SMSMaxTextLength = data.smsLength;
                    SMSWarningTextLength = data.smsWarningLength;
                    normalCharacters = data.normalChar;
                    doubleCharacters = data.doubleChar;
                    $('#sfCharsLeft').html(SMSMaxTextLength);
                }
            });
        };

        var remainingCharacters = function () {
            var $textarea = $control();
            var $remainingChars = $('#sfCharsLeft');
            var $remainingCharacters = $('#sfCharactersLeft > p');

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
            $message.val($message.val() + selected);
        };

        return {
            init: init,
            fillLiterals: fillLiterals
        };
    })();

    $(function () { SF.SMS.init(); });
}
