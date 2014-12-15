/// <reference path="../../../../Framework/Signum.Web/Signum/Scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")


export function initReplacements() {
    var self = this;

    $(".sf-word-template-container").on("click", ".sf-word-inserttoken", function () {
        var tokenName = Finder.QueryTokenBuilder.constructTokenName($(this).data("prefix"));
        if (SF.isEmpty(tokenName)) {
            return;
        }

        var tokenTag = constructTokenTag(tokenName, $(this).data("block"));

        window.prompt("Copy to clipboard: Ctrl+C, Enter", tokenTag);

    });

$(".sf-word-template-container").on("sf-new-subtokens-combo", "select", function (event, ...idSelectedCombo) {
        newSubTokensComboAdded.call(self, $("#" + idSelectedCombo[0]));
    });
}

function constructTokenTag(tokenName: string, block?: string) {
    if (typeof block == "undefined") {
        return "@[" + tokenName + "]";
    }
    else if (block === "if") {
        return "@if[" + tokenName + "]\r\n@else\r\n@endif";
    }
    else if (block === "foreach") {
        return "@foreach[" + tokenName + "]\r\n@endforeach";
    }
    else if (block === "any") {
        return "@any[" + tokenName + "=value]\r\n@notany\r\n@endany";
    }
    else {
        throw "invalid block name";
    }
};

export function newSubTokensComboAdded($selectedCombo: JQuery) {
    var $btnInsertBasic = $(".sf-word-inserttoken-basic");
    var $btnInsertIf = $(".sf-word-inserttoken-if");
    var $btnInsertForeach = $(".sf-word-inserttoken-foreach");
    var $btnInsertAny = $(".sf-word-inserttoken-any");

    var $selectedOption = $selectedCombo.children("option:selected");
    $selectedCombo.attr("title", $selectedOption.attr("title"));
    $selectedCombo.attr("style", $selectedOption.attr("style"));
    if ($selectedOption.val() == "") {
        var $prevSelect = $selectedCombo.prev("select");
        if ($prevSelect.length == 0) {
            changeButtonState($btnInsertBasic, lang.signum.selectToken);
            changeButtonState($btnInsertIf, lang.signum.selectToken);
            changeButtonState($btnInsertForeach, lang.signum.selectToken);
            changeButtonState($btnInsertAny, lang.signum.selectToken);
        }
        else {
            changeButtonState($btnInsertBasic);
            var $prevSelectedOption = $prevSelect.find("option:selected");
            changeButtonState($btnInsertIf, $prevSelectedOption.attr("data-if"));
            changeButtonState($btnInsertForeach, $prevSelectedOption.attr("data-foreach"));
            changeButtonState($btnInsertAny, $prevSelectedOption.attr("data-any"));
        }
        return;
    }

    changeButtonState($btnInsertBasic);
    changeButtonState($btnInsertIf, $selectedOption.attr("data-if"));
    changeButtonState($btnInsertForeach, $selectedOption.attr("data-foreach"));
    changeButtonState($btnInsertAny, $selectedOption.attr("data-any"));
};

function changeButtonState($button: JQuery, disablingMessage?: string) {
    var hiddenId = $button.attr("id") + "temp";
    if (typeof disablingMessage != "undefined") {
        $button.attr("disabled", "disabled").attr("title", disablingMessage);
        $button.unbind('click').bind('click', function (e) { e.preventDefault(); return false; });
    }
    else {
        $button.attr("disabled", null).attr("title", "");
        $button.unbind('click');
    }
}

export function createWordReportFromTemplate(options: Operations.EntityOperationOptions, event: MouseEvent, findOptions: Finder.FindOptions, url: string, contextual: boolean) {
    Finder.find(findOptions).then(entity => {
        if (entity == null)
            return;

        Entities.EntityValue

        options.requestExtraJsonData = { keys: entity.runtimeInfo.key() };

        options.controllerUrl = url;

        if (contextual)
            return Operations.constructFromDefaultContextual(options, event);
        else
            return Operations.constructFromDefault(options, event);
    });
}

export function createWordReportFromEntity(options: Operations.EntityOperationOptions, event: MouseEvent, title: string, chooserOptions: Navigator.ChooserOption[] , url: string, contextual: boolean) {
    Navigator.chooser(options.prefix + "_choose", title, chooserOptions).then(opt => {
        if (opt == null)
            return;

        options.requestExtraJsonData = { keys: opt.value };

        options.controllerUrl = url;

        if (contextual)
            return Operations.constructFromDefaultContextual(options, event);
        else
            return Operations.constructFromDefault(options, event);
    });
}

