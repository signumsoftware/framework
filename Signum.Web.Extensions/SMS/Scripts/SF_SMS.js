var SF = SF || {};

SF.SMS = (function () {
    var SMSMaxTextLength;
    var SMSWarningTextLength;

    var normalCharacters;
    var doubleCharacters;

    var charactersToEnd = function ($textarea) {
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

    function loadLists(url) {
        $.ajax({
            url: url,
            data: {},
            success: function (data) {
                SMSMaxTextLength = data.smsLength;
                SMSWarningTextLength = data.smsWarningLength;
                normalCharacters = data.normalChar;
                doubleCharacters = data.doubleChar;
                $('#sfCharsLeft').html(SMSMaxTextLength);
            }
        });
    };

    $(function () {
        loadLists($('#sfCharactersLeft').attr("data-url"));
    });

    $('textarea#Message').keyup(function () {
        var $textarea = $('#Message');
        var $charsLeft = $('#sfCharsLeft');
        var $charactersLeft = $('#sfCharactersLeft > p');

        var numberCharsLeft = charactersToEnd($textarea);
        $charsLeft.html(numberCharsLeft);

        $charactersLeft.removeClass('sf-sms-no-more-chars').removeClass('sf-sms-warning');
        $textarea.removeClass('sf-sms-red');
        $charsLeft.removeClass('sf-sms-highlight');

        if (numberCharsLeft < 0) {
            $charactersLeft.addClass('sf-sms-no-more-chars');
            $charsLeft.addClass('sf-sms-highlight');
            $textarea.addClass('sf-sms-red');
        }
        else if (numberCharsLeft < SMSWarningTextLength) {
            $charactersLeft.addClass('sf-sms-warning');
            $charsLeft.addClass('sf-sms-highlight');
        }
    });
})();
