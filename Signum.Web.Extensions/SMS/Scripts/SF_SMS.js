var SKI = SKI || {};

SKI.User = (function () {
    var SMSMaxTextLength = 0;
    var SMSWarningTextLength = 0;

    var normalCharacters = new Array();
    var doubleCharacters = new Array();

    var characterstoend = function (textarea) {
        var $block = $(textarea);
        var text = $block.val();
        var count = text.length;
        var maxLength = SMSMaxTextLength;

        for (var l = 0; l < text.length; l++) {
            if (normalCharacters.indexOf(text.charAt(l)) == -1) {
                if (doubleCharacters.indexOf(text.charAt(l)) != -1) {
                    count++;
                } else {
                    maxLength = 60;
                    count = text.length;
                    break;
                }
            }
        }
        return SMSMaxTextLength - count;
    };

    function LoadLists(dir) {
        $.ajax({
            url: dir,
            data: {},
            success: function (data) {
                SMSMaxTextLength = data.smsLength;
                SMSWarningTextLength = data.smsWarningLength;
                normalCharacters = data.normalChar;
                doubleCharacters = data.doubleChar;
                $('#numberofchar').html(characterstoend('#Message'));
            }
        });
    };

    $(function () {
        LoadLists($('#charactersleft').attr("data-url"));
    });

    $('textarea#Message').keyup(function () {
        var charscounted = characterstoend('#Message');
        $('#numberofchar').html(characterstoend('#Message'));

        $('#charactersleft > p').removeClass();
        $('#Message').removeClass();
        
        if (charscounted == 0) {
            $('#charactersleft > p').addClass('nomorechars');
        } else {
            if (charscounted < SMSWarningTextLength) {
                $('#charactersleft > p').addClass('warningchars'); backgroundred
                $('#Message').addClass('backgroundred');
            }
        }

    });

    return {
        characterstoend: characterstoend
    };

})();
