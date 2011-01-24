/// <reference path="jquery-1.4.min.vsdoc.js" />"

function magicRadios($ctx) {
    var updateBackground = function() {
        $(this).toggleClass("cbDisabled", !$(":radio", this).attr("checked"));
    };

    var $links = $ctx.find(".ruleTable .cbLink");
    
    $links.find("input:radio").hide();
    $links.each(updateBackground);

    $links.live("click", function() {
        var $tr = $(this).parent("td").parent("tr");
        var radio = $(":radio", this);
        radio.attr("checked", true);
        $(".overriden", $tr).attr("checked", radio.val() != $("input[name$=Base]", $tr).val());
        $(".cbLink", $tr).each(updateBackground);
    });
}

function magicCheckBoxes($ctx) {
    var updateBackground = function() {
        $(this).toggleClass("cbDisabled", !$(":checkbox", this).attr("checked"));
    };

    var $links = $ctx.find(".ruleTable .cbLink");

    $links.find("input:checkbox").hide();
    $links.each(updateBackground);

    $links.bind("mousedown", function() { this.onselectstart = function() { return false }; });
    $links.live("click", function(evt) {
        var $tr = $(this).parent("td").parent("tr");

        var cb = $(":checkbox", this);

        if (!evt.shiftKey) {
            $(".cbLink :checkbox", $tr).attr("checked", false);
            cb.attr("checked", true);
        } else {
            var num = $(".cbLink :checkbox:checked", $tr).length
            if (!cb.attr("checked") && num == 1)
                cb.attr("checked", true);
            else if (cb.attr("checked") && num >= 2)
                cb.attr("checked", false);
        }

        var total = $.map($(".cbLink :checkbox:checked", $tr), function(a) { return $(a).attr("tag"); }).join(",");

        $(".overriden", $tr).attr("checked", total != $("input[name$=Base]", $tr).val());
        $(".cbLink", $tr).each(updateBackground);
    });
}

function treeView() 
{
    $(".ruleTable a.namespace").live("click", function() {
        var tv = $(".tvExpandedLast,.tvClosedLast", this).toggleClass("tvExpandedLast").toggleClass("tvClosedLast");
        var tv = $(".tvExpanded,.tvClosed", this).toggleClass("tvExpanded").toggleClass("tvClosed");
        var ns = $("span.namespace", this).html();
        $(".ruleTable tr").filter(function() {
            return $("td > span.namespace", this).html() == ns;
        }).toggle();
        return false;
    });
}

function openDialog(controllerUrl, data) {
    var navigator = new SF.ViewNavigator({
            controllerUrl: controllerUrl,
            requestExtraJsonData: data,
            type: 'PropertyRulePack',
            onLoaded: function(divId) {
            magicRadios($("#" + divId));
        } 
         });
    navigator.createSave();
}

function postDialog(controllerUrl, prefix) {
    new SF.PartialValidator({ controllerUrl: controllerUrl, prefix: prefix }).trySave();
}