var SKI = SKI || {};

SKI.User = (function () {
    var SMSMaxTextLength = 0;
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
                normalCharacters = data.normalChar;
                doubleCharacters = data.doubleChar;
            }
        });
    };

    $(function () {
        LoadLists($('#charactersleft').attr("data-url"));
        $('#numerocaracteres').html(characterstoend('#Message'));
    });

    $('textarea#Message').keyup(function () {
        var charscounted = characterstoend('#Message');
        $('#numerocaracteres').html(characterstoend('#Message'));
        if (charscounted > SMSMaxTextLength) {
            $('textarea#Message').addClass('toochars');
        }
    });



    return {
        characterstoend: characterstoend
    };

})();
