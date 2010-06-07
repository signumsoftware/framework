var FindNavigator = function(_findOptions) {
    this.findOptions = $.extend({
        prefix: "",
        queryUrlName: null,
        searchOnLoad: false,
        allowMultiple: null,
        create: true,
        view: true,
        top: null,
        filters: null,
        filterMode: null,
        navigatorControllerUrl: "Signum/PartialFind",
        searchControllerUrl: "Signum/Search",
        onOk: null,
        onCancelled: null,
        onOkClosed: null,
        async: true
    }, _findOptions);
};

FindNavigator.prototype = {

    pf: function(s) {
        return "#" + this.findOptions.prefix.compose(s);
    },

    tempDivId: function() {
        return this.findOptions.prefix + "Temp";
    },

    openFinder: function() {
        log("FindNavigator openFinder");
        var self = this;
        $.ajax({
            type: "POST",
            url: this.findOptions.navigatorControllerUrl,
            data: this.requestData(),
            async: false,
            dataType: "html",
            success: function(popupHtml) {
                $('#divASustituir').after(hiddenDiv(self.tempDivId(), popupHtml));
                new popup().show(self.tempDivId());
                $(self.pf(sfBtnOk)).unbind('click').click(function() { self.onSearchOk(); });
                $(self.pf(sfBtnCancel)).unbind('click').click(function() { self.onSearchCancel(); });
            }
        });
    },

    selectedItems: function() {
        log("FindNavigator selectedItems");
        var items = new Array();
        var selected = $("input:radio[name=" + this.findOptions.prefix.compose("rowSelection") + "]:checked, input:checkbox[name^=" + this.findOptions.prefix.compose("rowSelection") + "]:checked");
        if (selected.length == 0)
            return items;

        var self = this;
        selected.each(function(i) {
            var currentItem = this.value;
            var __index = currentItem.indexOf("__");
            var __index2 = currentItem.indexOf("__", __index + 2);
            var item = new Object();
            item.id = currentItem.substring(0, __index);
            item.type = currentItem.substring(__index + 2, __index2);
            item.toStr = currentItem.substring(__index2 + 2, currentItem.length);
            item.link = $('#' + this.id).parent().next(self.pf('tdResults')).children('a').attr('href');
            items[i] = item;
        });

        return items;
    },

    splitSelectedIds: function() {
        log("FindNavigator splitSelectedIds");
        var selected = this.selectedItems();
        var result = "";
        $(selected).each(function(i, value) {
            result += value.id + ",";
        });
        if (!empty(result))
            result = result.substring(0, result.length - 1);
        return result;
    },

    search: function() {
        //	var async = concurrentSearch[prefix + "btnSearch"];
        //	if (async) concurrentSearch[prefix + "btnSearch"]=false;
        var btnSearch = $(this.pf("btnSearch"));
        btnSearch.toggleClass('loading').val(lang['searching']);
        var self = this;
        $.ajax({
            type: "POST",
            url: this.findOptions.searchControllerUrl,
            data: this.requestData(),
            async: this.findOptions.async,
            dataType: "html",
            success: function(resultsHtml) {
                $(self.pf("divResults")).html(resultsHtml);
            }
        });
        btnSearch.val(lang['buscar']).toggleClass('loading');
    },

    requestData: function() {
        var requestData = new Object();
        requestData[sfQueryUrlName] = ((empty(this.findOptions.queryUrlName)) ? $(this.pf(sfQueryUrlName)).val() : this.findOptions.queryUrlName);
        requestData[sfTop] = empty(this.findOptions.top) ? $(this.pf(sfTop)).val() : this.findOptions.top;
        requestData[sfAllowMultiple] = (this.findOptions.allowMultiple == undefined) ? $(this.pf(sfAllowMultiple)).val() : this.findOptions.allowMultiple;

        var canView = $(this.pf(sfView)).val();
        requestData[sfView] = (!this.findOptions.view) ? false : (empty(canView) ? this.findOptions.view : canView);
        requestData["sfSearchOnLoad"] = this.findOptions.searchOnLoad;

        if (this.findOptions.async)
            requestData["sfAsync"] = this.findOptions.async;

        if (this.findOptions.filterMode != null)
            requestData["sfFilterMode"] = this.findOptions.filterMode;

        if (!this.findOptions.create)
            requestData["sfCreate"] = this.findOptions.create;

        var currentfilters = this.serializeFilters();
        if (!empty(currentfilters))
            $.extend(requestData, currentfilters);
        else if (!empty(this.findOptions.filters)) {
            var filterArray = this.findOptions.filters.split("&");
            for (var i = 0, l = filterArray.length; i < l; i++) {
                var pair = filterArray[i];
                if (!empty(pair)) {
                    pair = pair.split("=");
                    if (pair.length == 2)
                        requestData[pair[0]] = pair[1];
                }
            }
        }

        requestData[sfPrefix] = this.findOptions.prefix;

        return requestData;
    },

    serializeFilters: function() {
        var result = "";
        var self = this;
        $(this.pf("tblFilters > tbody > tr")).each(function() {
            result = $.extend(result, self.serializeFilter(this.id.substring(this.id.lastIndexOf("_") + 1, this.id.length)));
        });
        return result;
    },

    serializeFilter: function(index) {
        var tds = $(this.pf("trFilter").compose(index) + " td");
        var columnName = tds[0].id.substring(tds[0].id.indexOf("__") + 2, tds[0].id.length);
        var selector = $(this.pf("ddlSelector").compose(index) + " option:selected");
        var value = $(this.pf("value").compose(index)).val();

        var valBool = $("input:checkbox[id=" + this.findOptions.prefix.compose("value").compose(index) + "]"); //it's a checkbox
        if (valBool.length > 0) value = valBool[0].checked;

        var info = RuntimeInfoFor(this.findOptions.prefix.compose("value").compose(index));
        if (info.find().length > 0) //If it's a Lite, the value is the Id
            value = info.id() + ";" + info.runtimeType();

        var filter = new Object();
        filter["cn" + index] = columnName;
        filter["sel" + index] = selector.val();
        filter["val" + index] = value;
        return filter;
    },

    onSearchOk: function() {
        log("FindNavigator onSearchOk");
        var selected = this.selectedItems();
        if (selected.length == 0)
            return;
        var doDefault = (this.findOptions.onOk != null) ? this.findOptions.onOk(selected) : true;
        if (doDefault != false) {
            $('#' + this.tempDivId()).remove();
            if (this.findOptions.onOkClosed != null)
                this.findOptions.onOkClosed();
        }
    },

    onSearchCancel: function() {
        log("FindNavigator onSearchCancel");
        $('#' + this.tempDivId()).remove();
        if (this.findOptions.onCancelled != null)
            this.findOptions.onCancelled();
    },

    newFilterRowIndex: function() {
        log("FindNavigator newFilterRowIndex");
        var lastRow = $(this.pf("tblFilters tbody tr:last"));
        var lastRowIndex = -1;
        if (lastRow.length == 1)
            lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length);
        return parseInt(lastRowIndex) + 1;
    },

    addFilter: function() {
        log("FindNavigator addFilter");

        var tableFilters = $(this.pf("tblFilters tbody"));
        if (tableFilters.length == 0)
            throw "Adding filters is not allowed";

        var tokenName = this.constructTokenName();
        if (empty(tokenName)) return;

        var queryUrlName = ((empty(this.findOptions.queryUrlName)) ? $(this.pf(sfQueryUrlName)).val() : this.findOptions.queryUrlName);

        var self = this;
        $.ajax({
            type: "POST",
            url: "Signum/AddFilter",
            data: { "sfQueryUrlName": queryUrlName, "tokenName": tokenName, "index": this.newFilterRowIndex(), "prefix": this.findOptions.prefix },
            async: false,
            dataType: "html",
            success: function(filterHtml) {
                $(self.pf("filters-list .explanation")).hide();
                $(self.pf("filters-list table")).show('fast');
                tableFilters.append(filterHtml);
                $(self.pf("btnClearAllFilters")).show();
            }
        });
    },

    newSubTokensCombo: function(index) {
        log("FindNavigator newSubTokensCombo");
        var selectedColumn = $(this.pf("ddlTokens_" + index));
        if (selectedColumn.length == 0) return;

        //Clear child subtoken combos
        var self = this;
        $("select")
        .filter(function() { return $(this).attr("id").indexOf(self.findOptions.prefix.compose("ddlTokens_")) == 0; })
        .filter(function() {
            var currentId = $(this).attr("id");
            var lastSeparatorIndex = currentId.lastIndexOf("_");
            var currentIndex = currentId.substring(lastSeparatorIndex + 1, currentId.length);
            return parseInt(currentIndex) > index;
        })
        .remove();

        if (selectedColumn.children("option:selected").val() == "") return;

        var tokenName = this.constructTokenName();
        var queryUrlName = ((empty(this.findOptions.queryUrlName)) ? $(this.pf(sfQueryUrlName)).val() : this.findOptions.queryUrlName);

        $.ajax({
            type: "POST",
            url: "Signum/NewSubTokensCombo",
            data: { "sfQueryUrlName": queryUrlName, "tokenName": tokenName, "index": index, "prefix": this.findOptions.prefix },
            async: false,
            dataType: "html",
            success: function(newCombo) {
                $(self.pf("ddlTokens_" + index)).after(newCombo);
            }
        });
    },

    constructTokenName: function() {
        log("FindNavigator constructTokenName");
        var tokenName = "";
        var stop = false;
        for (i = 0; !stop; i++) {
            var currSubtoken = $(this.pf("ddlTokens_" + i));
            if (currSubtoken.length > 0)
                tokenName = tokenName.compose(currSubtoken.val(), ".");
            else
                stop = true;
        }
        return tokenName;
    },

    quickFilter: function(idTD) {
        log("FindNavigator quickFilter");
        var tableFilters = $(this.pf("tblFilters tbody"));
        if (tableFilters.length == 0)
            return;
        var params;
        var ahref = $('#' + idTD + ' a');
        if (ahref.length == 0)
            params = { "isLite": "false", "sfValue": $('#' + idTD).html() };
        else {
            var route = ahref.attr("href");
            var separator = route.indexOf("/");
            var lastSeparator = route.lastIndexOf("/");
            params = { "isLite": "true", "typeUrlName": route.substring(separator + 1, lastSeparator), "sfId": route.substring(lastSeparator + 1, route.length) };
        }
        var idTDnoPrefix = idTD.substring(this.findOptions.prefix.length, idTD.length);
        var colIndex = parseInt(idTDnoPrefix.substring(idTDnoPrefix.indexOf("_") + 1, idTDnoPrefix.length)) - 1
        var self = this;
        $.ajax({
            type: "POST",
            url: "Signum/QuickFilter",
            data: $.extend(params, { "sfQueryUrlName": $(this.pf(sfQueryUrlName)).val(), "sfColIndex": colIndex, "prefix": this.findOptions.prefix, "index": this.newFilterRowIndex() }),
            async: false,
            dataType: "html",
            success: function(filterHtml) {
                $(self.pf("filters-list .explanation")).hide();
                $(self.pf("filters-list table")).show('fast');
                tableFilters.append(filterHtml);
            }
        });
    },

    deleteFilter: function(index) {
        log("FindNavigator deleteFilter");
        var tr = $(this.pf("trFilter_" + index))
        if (tr.length == 0) return;

        if ($(this.pf("trFilter").compose(index) + " select[disabled]").length == 0) tr.remove();
        if ($(this.pf("tblFilters tbody tr")).length == 0) {
            $(this.pf("filters-list .explanation")).show();
            $(this.pf("filters-list table")).hide('fast');
            $(this.pf("btnClearAllFilters")).hide();
        }
    },

    clearAllFilters: function() {
        log("FindNavigator clearAllFilters");
        var self = this;
        $(this.pf("tblFilters > tbody > tr")).each(function(index) {
            self.deleteFilter(this.id.substring(this.id.lastIndexOf("_") + 1, this.id.length));
        });
    },

    requestDataForSearchPopupCreate: function() {
        var requestData = this.serializeFilters();
        var requestData = $.extend(requestData, { sfQueryUrlName: ((empty(this.findOptions.queryUrlName)) ? $(this.pf(sfQueryUrlName)).val() : this.findOptions.queryUrlName) });
        return requestData;
    },

    viewOptionsForSearchCreate: function(_viewOptions) {
        log("FindNavigator viewOptionsForSearchCreate");
        if (this.findOptions.prefix != _viewOptions.prefix)
            throw "FindOptions prefix and ViewOptions prefix don't match";
        _viewOptions.prefix = "New".compose(_viewOptions.prefix);
        var self = this;
        return $.extend({
            type: $(this.pf(sfEntityTypeName)).val(),
            containerDiv: null,
            onCancelled: null,
            controllerUrl: empty(this.findOptions.prefix) ? "Signum/Create" : "Signum/PopupCreate"
        }, _viewOptions);
    },

    viewOptionsForSearchPopupCreate: function(_viewOptions) {
        log("FindNavigator viewOptionsForSearchPopupCreate");
        if (this.findOptions.prefix != _viewOptions.prefix)
            throw "FindOptions prefix and ViewOptions prefix don't match";
        _viewOptions.prefix = "New".compose(_viewOptions.prefix);
        var self = this;
        return $.extend({
            type: $(this.pf(sfEntityTypeName)).val(),
            containerDiv: null,
            requestExtraJsonData: this.requestDataForSearchPopupCreate(),
            onCancelled: null,
            controllerUrl: empty(this.findOptions.prefix) ? "Signum/Create" : "Signum/PopupCreate"
        }, _viewOptions);
    }
};

function OpenFinder(_findOptions) {
    new FindNavigator(_findOptions).openFinder();
}

function Search(_findOptions) {
    new FindNavigator(_findOptions).search();
}

function SelectedItems(_findOptions) {
    return new FindNavigator(_findOptions).selectedItems();
}

function SplitSelectedIds(_findOptions) {
    return new FindNavigator(_findOptions).splitSelectedIds();
}

function HasSelectedItems(_findOptions, onSuccess) {
    log("FindNavigator HasSelectedItems");
    var items = SelectedItems(_findOptions);
    if (items.length == 0) {
        NotifyInfo(lang['noElementsSelected']);
        return;
    }
    onSuccess(items);
}

function AddFilter(prefix) {
    new FindNavigator({ prefix: prefix }).addFilter();
}

function NewSubTokensCombo(prefix, index) {
    new FindNavigator({ prefix: prefix }).newSubTokensCombo(index);
}

function QuickFilter(prefix, idTd) {
    new FindNavigator({ prefix: prefix }).quickFilter(idTd);
}

function DeleteFilter(prefix, index) {
    new FindNavigator({ prefix: prefix }).deleteFilter(index);
}

function ClearAllFilters(prefix) {
    new FindNavigator({ prefix: prefix }).clearAllFilters();
}

function SearchCreate(viewOptions){
    var findNavigator = new FindNavigator({prefix: viewOptions.prefix});
    if (empty(viewOptions.prefix)) {
        var viewOptions = findNavigator.viewOptionsForSearchCreate(viewOptions);
        new ViewNavigator(viewOptions).navigate();
    }
    else{
        var viewOptions = findNavigator.viewOptionsForSearchPopupCreate(viewOptions);
        new ViewNavigator(viewOptions).createSave();
    }
}

function toggleVisibility(elementId) {
	$('#' + elementId).toggle();
}

function toggleFilters(id) {
    var elem = $('#' + id + " .filters-header");
    var R = elem.attr('rev');
    var D = $('#' + R);
    D.toggle('fast');
    $('#' + id + ' .filters').toggle('fast');
    elem.toggleClass('close');
    if (elem.hasClass('close')) elem.html('Mostrar filtros'); else elem.html('Ocultar filtros');
    return false;
}

var asyncSearchFinished = new Array();
function SearchOnLoad(btnSearchId) {    
    var $button = $("#" + btnSearchId);
    var makeSearch = function() {
        if (!asyncSearchFinished[btnSearchId]) {
            $button.click();
            asyncSearchFinished[btnSearchId] = true;
        }
    };
    
    if ($button.is(':visible')) {
        makeSearch();
    }
    else {
        var $tabContainer = $button.parents(".tabs").first();
        if ($tabContainer.length) {
            $tabContainer.find("a").click(
                function() {                    
                    if ($button.is(':visible')) makeSearch();
                });
        } else{
            makeSearch();
        }                
    }
}