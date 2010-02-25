var popup = function(_visualOptions) {
    log("popup");
    this.visualOptions = $.extend({
        fade: false
    }, _visualOptions);

    this.show = function(idExternalPopupDiv) {
        log("popup show (" + idExternalPopupDiv + ")");
        var externalPopupDiv = $("#" + idExternalPopupDiv);
        externalPopupDiv.show();
        var modalBackground = $("#" + idExternalPopupDiv + " .popupBackground");

        var otherPopups = $(".popupWindow");
        var maxZindex = 100;
        otherPopups.each(function() {
        var zindex = $('#' + this.id).css('z-index');
            if (zindex > maxZindex)
                maxZindex = zindex;
        });

        modalBackground.width('500%').height('500%').css('z-index', parseInt(maxZindex) + 1);
        //Read offsetWidth and offsetHeight after display=block or otherwise it's 0
        var panelPopup = $("#" + idExternalPopupDiv + " .popupWindow");
        //panelPopup.attr('z-index', parseInt(maxZindex) + 2);
        var popup2 = panelPopup[0];
        log(popup2.id);
        var parentDiv = externalPopupDiv.parent();
        var popupWidth = popup2.offsetWidth;
        var bodyWidth = document.body.clientWidth;
        var left = Math.max((bodyWidth - popupWidth) / 2, 10) + "px";
        var popupHeight = popup2.offsetHeight;
        var bodyHeight = document.documentElement.clientHeight;
        var top = Math.max((bodyHeight - popupHeight) / 2, 10) + "px";

        externalPopupDiv.hide();
        popup2.style.left = left;
        popup2.style.top = top;
        popup2.style.minWidth = popupWidth + "px";
        popup2.style.maxWidth = "95%";
        popup2.style.zIndex = parseInt(maxZindex) + 2;
        
        var maxPercentageWidth = 0.95;
        popup2.style.maxWidth = (maxPercentageWidth * 100) + "%";
        popup2.style.maxHeight = $(window).height() + "px";
        if ($(popup2).children(".searchControl").length)
        {
            $(popup2).children(".searchControl").first().css({"maxHeight": $(window).height() - 100, "overflow": "auto"});
        }
        popup2.style.minWidth = ((popupWidth > (maxPercentageWidth * 100)) ? (maxPercentageWidth * 100) : popupWidth) + "px";

        if ($("#" + idExternalPopupDiv + " :file").length > 0)
            popup2.style.minWidth = "500px";

        externalPopupDiv.show('fast');
        modalBackground[0].style.left = 0;
        modalBackground.css('filter', 'alpha(opacity=40)')
        if (this.visualOptions.fade)
            modalBackground.fadeIn('slow');
        else
            modalBackground.show();
    };
};

