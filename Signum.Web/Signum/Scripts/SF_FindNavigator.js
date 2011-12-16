"use strict";

SF.registerModule("FindNavigator", function () {

    SF.FindNavigator = function (_findOptions) {
        this.findOptions = $.extend({
            prefix: "",
            webQueryName: null,
            searchOnLoad: false,
            allowMultiple: null,
            create: true,
            view: true,
            elems: null,
            filters: null, //List of filter names "token1,operation1,value1;token2,operation2,value2"
            filterMode: null,
            orders: null, //A Json array like ["columnName1","-columnName2"] => will order by columnname1 asc, then by columnname2 desc
            columns: null, //List of column names "token1,displayName1;token2,displayName2"
            columnMode: null,
            allowUserColumns: null,
            navigatorControllerUrl: null,
            searchControllerUrl: null,
            onOk: null,
            onCancelled: null,
            onOkClosed: null,
            async: true
        }, _findOptions);
    };

    SF.FindNavigator.prototype = {

        webQueryName: "sfWebQueryName",
        elems: "sfElems",
        page: "sfPage",
        allowMultiple: "sfAllowMultiple",
        view: "sfView",
        orders: "sfOrders",
        filterMode: "sfFilterMode",

        pf: function (s) {
            return "#" + SF.compose(this.findOptions.prefix, s);
        },

        control: function () {
            return $(".sf-search-control[data-prefix='" + this.findOptions.prefix + "']");
        },

        tempDivId: function () {
            return SF.compose(this.findOptions.prefix, "Temp");
        },

        initialize: function () {
            var self = this;

            var closeMyOpenedCtxMenu = function (target) {
                if ($(target).hasClass("sf-search-ctxmenu-overlay") || $(target).parents().hasClass("sf-search-ctxmenu-overlay")) {
                    $('.sf-search-ctxmenu-overlay').remove();
                    return false;
                }
                return true;
            };

            $(this.pf("tblResults") + " th:not(.th-col-entity):not(.th-col-selection)")
            .live('click', function (e) {
                self.newSortOrder($(e.target), e.shiftKey);
                self.search();
                return false;
            })
            .live('contextmenu', function (e) {
                if (!closeMyOpenedCtxMenu(e.target)) {
                    return false;
                }
                self.headerContextMenu(e);
                return false;
            })
            .live('mousedown', function (e) {
                this.onselectstart = function () { return false };
                return false;
            });

            $(this.pf("tblResults td:not(.sf-td-no-results):not(.sf-td-multiply,.sf-search-footer-pagination)")).live('contextmenu', function (e) {
                if (!closeMyOpenedCtxMenu(e.target)) {
                    return false;
                }

                var $this = $(this);
                var index = $this.index();
                var $th = $this.closest("table").find("th").eq(index);
                if ($th.hasClass('th-col-selection'))
                    return false;
                if ($th.hasClass('th-col-entity'))
                    self.control().trigger('entity-cell-ctx-menu', [this, self]);
                else
                    self.cellContextMenu(e);
                return false;
            });

            $(this.pf("tblResults") + " .sf-search-ctxitem.quickfilter").live('click', function () {
                SF.log("contextmenu item click");
                var $elem = $(this).closest("td");
                $('.sf-search-ctxmenu-overlay').remove();

                var quickFilterUrl = self.control().data("quickfilter-url");
                if (quickFilterUrl == undefined)
                    return false; //QuickFilters not active            

                self.quickFilterCell($elem, quickFilterUrl);
            });

            $(this.pf("tblResults") + " .sf-search-ctxitem.quickfilter-header").live('click', function () {
                SF.log("contextmenu item click");
                var $elem = $(this).closest("th");
                $('.sf-search-ctxmenu-overlay').remove();

                var quickFilterUrl = self.control().data("quickfilter-url");
                if (quickFilterUrl == undefined)
                    return false; //QuickFilters not active            

                self.quickFilterHeader($elem, quickFilterUrl);
                return false;
            });

            $(this.pf("tblResults") + " .sf-search-ctxitem.remove-column").live('click', function () {
                SF.log("contextmenu item click");
                var $elem = $(this).closest("th");
                $('.sf-search-ctxmenu-overlay').remove();

                self.removeColumn($elem);
                return false;
            });

            $(this.pf("tblResults") + " .sf-search-ctxitem.edit-column").live('click', function () {
                SF.log("contextmenu item click");
                var $elem = $(this).closest("th");
                $('.sf-search-ctxmenu-overlay').remove();

                self.editColumn($elem);
                return false;
            });

            $(this.pf("tblResults") + " .move-column-left," + this.pf("tblResults") + " .move-column-right").live('click', function (e) {
                SF.log("contextmenu item click");
                var $elem = $(this).closest("th");
                $('.sf-search-ctxmenu-overlay').remove();

                self.moveColumn($elem, e);
                return false;
            });

            $(this.pf("tblResults") + " .sf-pagination-button").live('click', function () {
                SF.log("pagination button click");
                $(self.pf(self.page)).val($(this).attr("data-page"));
                self.search();
            });

            $(this.pf("sfElems")).live('change', function () {
                SF.log("page size changed");
                self.search();
            });
        },

        createCtxMenu: function ($rightClickTarget) {
            var $cmenu = $("<div class='sf-search-ctxmenu'></div>");
            $cmenu.css({
                left: $rightClickTarget.position().left + ($rightClickTarget.outerWidth() / 2),
                top: $rightClickTarget.position().top + ($rightClickTarget.outerHeight() / 2),
                zIndex: '101'
            });

            var $ctxMenuOverlay = $('<div class="sf-search-ctxmenu-overlay"></div>').click(function (e) {
                SF.log("contextmenu click");
                var $clickTarget = $(e.target);
                if ($clickTarget.hasClass("sf-search-ctxitem") || $clickTarget.parent().hasClass("sf-search-ctxitem"))
                    $cmenu.hide();
                else
                    $('.sf-search-ctxmenu-overlay').remove();
            }).append($cmenu);

            return $ctxMenuOverlay;
        },

        headerContextMenu: function (e) {
            SF.log("headerContextmenu");

            var $th = $(e.target).closest("th");
            var $menu = this.createCtxMenu($th);

            var $itemContainer = $menu.find(".sf-search-ctxmenu");
            $itemContainer.append("<div class='sf-search-ctxitem quickfilter-header'>" + lang.signum.addFilter + "</div>")
                .append("<div class='sf-search-ctxitem edit-column'>" + lang.signum.editColumnName + "</div>")
                .append("<div class='sf-search-ctxitem remove-column'>" + lang.signum.removeColumn + "</div>");

            var thIndex = $th.index();
            var extraCols = this.control().find(".th-col-entity,.th-col-selection");

            if (thIndex > extraCols.length) {
                $itemContainer.append("<div class='sf-search-ctxitem move-column-left'>" + lang.signum.reorderColumn_MoveLeft + "</div>")
            }
            if (thIndex < $th.parent().children("th").length - 1) {
                $itemContainer.append("<div class='sf-search-ctxitem move-column-right'>" + lang.signum.reorderColumn_MoveRight + "</div>");
            }

            $th.append($menu);

            return false;
        },

        cellContextMenu: function (e) {
            SF.log("cellContextmenu");

            var $td = $(e.target);
            var $menu = this.createCtxMenu($td);

            $menu.find(".sf-search-ctxmenu")
                .html("<div class='sf-search-ctxitem quickfilter'>" + lang.signum.addFilter + "</div>");

            $td.append($menu);

            return false;
        },

        openFinder: function () {
            SF.log("FindNavigator openFinder");
            var self = this;
            $.ajax({
                url: this.findOptions.navigatorControllerUrl,
                data: this.requestDataForOpenFinder(),
                async: false,
                success: function (popupHtml) {
                    var divId = self.tempDivId();
                    $("body").append(SF.hiddenDiv(divId, popupHtml));
                    SF.triggerNewContent($("#" + divId));
                    $("#" + divId).popup({
                        onOk: function () { self.onSearchOk(); },
                        onCancel: function () { self.onSearchCancel(); }
                    });
                }
            });
        },

        hasSelectedItems: function (onSuccess) {
            SF.log("FindNavigator hasSelectedItems");
            var items = this.selectedItems();
            if (items.length == 0) {
                SF.Notify.info(lang.signum.noElementsSelected);
                return;
            }
            onSuccess(items);
        },

        selectedItems: function () {
            SF.log("FindNavigator selectedItems");
            var items = [];
            var selected = $("input:radio[name=" + SF.compose(this.findOptions.prefix, "rowSelection") + "]:checked, input:checkbox[name^=" + SF.compose(this.findOptions.prefix, "rowSelection") + "]:checked");
            if (selected.length == 0)
                return items;

            var self = this;
            selected.each(function (i, v) {
                var parts = v.value.split("__");
                var item = {
                    id: parts[0],
                    type: parts[1],
                    toStr: parts[2],
                    link: $(this).parent().next().children('a').attr('href')
                };
                items.push(item);
            });

            return items;
        },

        splitSelectedIds: function () {
            SF.log("FindNavigator splitSelectedIds");
            var selected = this.selectedItems();
            var result = [];
            for (var i = 0, l = selected.length; i < l; i++) {
                result.push(selected[i].id + ",");
            }

            if (result.length) {
                var result2 = result.join('');
                return result2.substring(0, result2.length - 1);
            }
            return '';
        },

        search: function () {
            var $btnSearch = $(this.pf("qbSearch"));
            $btnSearch.toggleClass("sf-loading").val(lang.signum.searching);

            var self = this;
            $.ajax({
                url: (SF.isEmpty(this.findOptions.searchControllerUrl) ? this.control().attr("data-search-url") : this.findOptions.searchControllerUrl),
                data: this.requestDataForSearch(),
                async: this.findOptions.async,
                success: function (r) {
                    var idBtnSearch = $btnSearch.attr('id');
                    if (SF.FindNavigator.asyncSearchFinished[idBtnSearch])
                        SF.FindNavigator.asyncSearchFinished[idBtnSearch] = false;
                    $btnSearch.val(lang.signum.search).toggleClass("sf-loading");
                    var $control = self.control();
                    var $tbody = $control.find(".sf-search-results-container tbody");

                    if (!SF.isEmpty(r)) {
                        $tbody.html(r);
                        SF.triggerNewContent($control.find(".sf-search-results-container tbody"));
                    }
                    else {
                        $tbody.html("");
                    }
                },
                error: function () {
                    $btnSearch.val(lang.signum.search).toggleClass('loading');
                }
            });

        },

        requestDataForSearch: function () {
            var requestData = new Object();
            requestData["webQueryName"] = $(this.pf(this.webQueryName)).val();
            requestData["elems"] = $(this.pf(this.elems)).val();
            requestData["page"] = $(this.pf(this.page)).val();
            requestData["allowMultiple"] = $(this.pf(this.allowMultiple)).val();

            var canView = $(this.pf(this.view)).val();
            requestData["view"] = (SF.isEmpty(canView) ? true : canView);

            requestData["filters"] = this.serializeFilters();
            requestData["filterMode"] = $(this.pf(this.filterMode)).val();
            requestData["orders"] = this.serializeOrders();
            requestData["columns"] = this.serializeColumns();
            requestData["columnMode"] = 'Replace';

            requestData["prefix"] = this.findOptions.prefix;
            return requestData;
        },

        requestDataForOpenFinder: function () {
            var requestData = {};
            requestData["webQueryName"] = this.findOptions.webQueryName;
            requestData["elems"] = this.findOptions.elems;
            requestData["allowMultiple"] = this.findOptions.allowMultiple;
            if (this.findOptions.view == false)
                requestData["view"] = this.findOptions.view;
            if (this.findOptions.searchOnLoad == true)
                requestData["searchOnLoad"] = this.findOptions.searchOnLoad;

            if (this.findOptions.async)
                requestData["async"] = this.findOptions.async;

            if (this.findOptions.filterMode != null)
                requestData["filterMode"] = this.findOptions.filterMode;

            if (!this.findOptions.create)
                requestData["create"] = this.findOptions.create;

            if (this.findOptions.filters != null)
                requestData["filters"] = this.findOptions.filters;
            if (this.findOptions.orders != null)
                requestData["orders"] = this.findOptions.orders;
            if (this.findOptions.columns != null)
                requestData["columns"] = this.findOptions.columns;
            if (this.findOptions.columnMode != null)
                requestData["columnMode"] = this.findOptions.columnMode;

            requestData["prefix"] = this.findOptions.prefix;

            return requestData;
        },

        serializeFilters: function () {
            var result = "", self = this;
            $(this.pf("tblFilters > tbody > tr")).each(function () {
                result += self.serializeFilter($(this)) + ";";
            });
            return result;
        },

        serializeFilter: function ($filter) {
            var id = $filter[0].id;
            var index = id.substring(id.lastIndexOf("_") + 1, id.length);

            var selector = $(SF.compose(this.pf("ddlSelector"), index) + " option:selected", $filter);
            var value = $(SF.compose(this.pf("value"), index), $filter).val();

            var valBool = $("input:checkbox[id=" + SF.compose(SF.compose(this.findOptions.prefix, "value"), index) + "]", $filter); //it's a checkbox
            if (valBool.length > 0) {
                value = valBool[0].checked;
            }
            else {
                var info = new SF.RuntimeInfo(SF.compose(SF.compose(this.findOptions.prefix, "value"), index));
                if (info.find().length > 0) { //If it's a Lite, the value is the Id
                    value = info.runtimeType() + ";" + info.id();
                    if (value == ";") {
                        value = "";
                    }
                }

                //Encode value CSV-ish style
                var hasQuote = value.indexOf("\"") != -1;
                if (hasQuote || value.indexOf(",") != -1 || value.indexOf(";") != -1) {
                    if (hasQuote)
                        value = value.replace(/"/g, "\"\"");
                    value = "\"" + value + "\"";
                }
            }

            return $filter.find("td:nth-child(2) > :hidden").val() + "," + selector.val() + "," + value;
        },

        serializeOrders: function () {
            var currOrder = $(this.pf(this.orders)).val();
            if (SF.isEmpty(currOrder))
                return "";
            return currOrder.replace(/"/g, "");
        },

        newSortOrder: function ($th, multiCol) {
            SF.log("FindNavigator sort");
            var columnName = $th.find("input:hidden").val();

            this.findOptions.searchControllerUrl = this.control().data("search-url");

            var currOrderArray = [];
            var currOrder = $(this.pf(this.orders)).val();
            if (!SF.isEmpty(currOrder))
                currOrderArray = currOrder.split(";");

            var found = false;
            var currIndex;
            var oldOrder = "";
            for (currIndex = 0; currIndex < currOrderArray.length && !found; currIndex++) {
                found = currOrderArray[currIndex] == columnName;
                if (found) {
                    oldOrder = "";
                    break;
                }
                found = currOrderArray[currIndex] == "-" + columnName;
                if (found) {
                    oldOrder = "-";
                    break;
                }
            }
            var newOrder = found ? (oldOrder == "" ? "-" : "") : "";
            currOrder = $(this.pf(this.orders));
            if (!multiCol) {
                this.control().find(".sf-search-results-container th").removeClass("sf-header-sort-up sf-header-sort-down");
                currOrder.val(newOrder + columnName + ";");
            }
            else {
                if (found)
                    currOrderArray[currIndex] = newOrder + columnName;
                else
                    currOrderArray[currOrderArray.length] = newOrder + columnName;
                var currOrderStr = "";
                for (var i = 0; i < currOrderArray.length; i++) {
                    if (!SF.isEmpty(currOrderArray[i])) {
                        currOrderStr += currOrderArray[i] + ";";
                    }
                }
                currOrder.val(currOrderStr);
            }

            if (newOrder == "-")
                $th.removeClass("sf-header-sort-down").addClass("sf-header-sort-up");
            else
                $th.removeClass("sf-header-sort-up").addClass("sf-header-sort-down");
        },

        serializeColumns: function () {
            SF.log("FindNavigator serializeColumns");
            var result = "";
            var self = this;
            $(this.pf("tblResults thead tr th:not(.th-col-entity):not(.th-col-selection)")).each(function () {
                var $this = $(this);
                var token = $this.find("input:hidden").val();
                var displayName = $this.text().trim();
                if (token == displayName) {
                    result += token;
                }
                else {
                    result += token + "," + displayName;
                }
                result += ";";
            });
            return result;
        },

        onSearchOk: function () {
            SF.log("FindNavigator onSearchOk");
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

        onSearchCancel: function () {
            SF.log("FindNavigator onSearchCancel");
            $('#' + this.tempDivId()).remove();
            if (this.findOptions.onCancelled != null)
                this.findOptions.onCancelled();
        },

        addColumn: function () {
            SF.log("FindNavigator addColumn");

            if (SF.isFalse(this.findOptions.allowUserColumns) || $(this.pf("tblFilters tbody")).length == 0)
                throw "Adding columns is not allowed";

            var tokenName = this.constructTokenName();
            if (SF.isEmpty(tokenName)) return;

            var prefixedTokenName = SF.compose(this.findOptions.prefix, tokenName);
            if ($(this.pf("tblResults thead tr th[id=\"" + prefixedTokenName + "\"]")).length > 0) return;

            var $tblHeaders = $(this.pf("tblResults thead tr"));

            var webQueryName = ((SF.isEmpty(this.findOptions.webQueryName)) ? $(this.pf(this.webQueryName)).val() : this.findOptions.webQueryName);
            var self = this;
            $.ajax({
                url: $(this.pf("btnAddColumn")).attr("data-url"),
                data: { "webQueryName": webQueryName, "tokenName": tokenName },
                async: false,
                success: function (columnNiceName) {
                    $tblHeaders.append("<th class='ui-state-default'><input type=\"hidden\" value=\"" + tokenName + "\" />" + columnNiceName + "</th>");
                }
            });
        },

        editColumn: function ($th) {
            SF.log("FindNavigator editColumn");

            var $colTokenHidden = $th.find("input:hidden");
            var colName = $th.text().trim();

            var popupPrefix = SF.compose(this.findOptions.prefix, "newName");

            var divId = "columnNewName";
            var $div = $("<div id='" + divId + "'></div>");
            $div.html("<p>" + lang.signum.enterTheNewColumnName + "</p>")
                .append("<br />")
                .append("<input type='text' value='" + colName + "' />")
                .append("<br />").append("<br />")
                .append("<input type='button' id='" + SF.compose(popupPrefix, "btnOk") + "' class='sf-button sf-ok-button' value='OK' />");

            var $tempContainer = $("<div></div>").append($div);

            new SF.ViewNavigator({
                onOk: function () { $th.html($("#columnNewName > input:text").val()).append($colTokenHidden); },
                prefix: popupPrefix
            }).showViewOk($tempContainer.html());
        },

        moveColumn: function ($th, e) {
            SF.log("FindNavigator moveColumn");
            var $target = $(e.target);
            var thIndex = $th.index();
            var $ths = $th.parent().children("th");
            if ($target.hasClass("move-column-left")) {
                var $prevTh = $($ths[thIndex - 1]);
                $th.detach();
                $prevTh.before($th);
            }
            else if ($target.hasClass("move-column-right")) {
                var $nextTh = $($ths[thIndex + 1]);
                $th.detach();
                $nextTh.after($th);
            }
            else {
                throw "No direction was given to FindNavigator moveColumn";
            }
            var $tbody = $(this.pf("tblResults tbody"));
            $tbody.find("tr:not('.sf-search-footer')").remove();
            $tbody.prepend($("<tr></tr>").append($("<td></td>").attr("colspan", $tbody.find(".sf-search-footer td").attr("colspan"))));
        },

        removeColumn: function ($th) {
            SF.log("FindNavigator removeColumn");

            $th.remove();

            var $tbody = $(this.pf("tblResults tbody"));
            $tbody.find("tr:not('.sf-search-footer')").remove();
            $tbody.prepend($("<tr></tr>").append($("<td></td>").attr("colspan", $tbody.find(".sf-search-footer td").attr("colspan"))));
        },

        toggleFilters: function (elem) {
            var $elem = $(elem);
            $elem.toggleClass('close');
            this.control().find(".sf-filters").toggle();
            if ($elem.hasClass('close')) {
                $elem.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-n").addClass("ui-icon-triangle-1-e");
                $elem.find(".ui-button-text").html(lang.signum.showFilters);
            }
            else {
                $elem.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-e").addClass("ui-icon-triangle-1-n");
                $elem.find(".ui-button-text").html(lang.signum.hideFilters);
            }
            return false;
        },

        addFilter: function (addFilterUrl, requestExtraJsonData) {
            SF.log("FindNavigator addFilter");

            var tableFilters = $(this.pf("tblFilters tbody"));
            if (tableFilters.length == 0)
                throw "Adding filters is not allowed";

            var tokenName = this.constructTokenName();
            if (SF.isEmpty(tokenName)) return;

            var webQueryName = ((SF.isEmpty(this.findOptions.webQueryName)) ? $(this.pf(this.webQueryName)).val() : this.findOptions.webQueryName);

            var serializer = new SF.Serializer().add({
                webQueryName: webQueryName,
                tokenName: tokenName,
                index: this.newFilterRowIndex(),
                prefix: this.findOptions.prefix
            });
            if (!SF.isEmpty(requestExtraJsonData)) {
                serializer.add(requestExtraJsonData);
            }

            var self = this;
            $.ajax({
                url: addFilterUrl || $(this.pf("btnAddFilter")).attr("data-url"),
                data: serializer.serialize(),
                async: false,
                success: function (filterHtml) {
                    var $filterList = self.control().find(".sf-filters-list");
                    $filterList.find(".sf-explanation").hide();
                    $filterList.find("table").show();

                    tableFilters.append(filterHtml);
                    SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
                }
            });
        },

        newFilterRowIndex: function () {
            SF.log("FindNavigator newFilterRowIndex");
            var lastRow = $(this.pf("tblFilters tbody tr:last"));
            var lastRowIndex = -1;
            if (lastRow.length == 1)
                lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length);
            return parseInt(lastRowIndex) + 1;
        },

        newSubTokensCombo: function (index, controllerUrl, requestExtraJsonData) {
            SF.log("FindNavigator newSubTokensCombo");
            var $selectedColumn = $(this.pf("ddlTokens_" + index));
            if ($selectedColumn.length == 0) {
                return;
            }

            var $btnAddFilter = $(this.pf("btnAddFilter"));
            var $btnAddColumn = $(this.pf("btnAddColumn"));

            //Clear child subtoken combos
            var self = this;
            $selectedColumn.siblings("select,span")
                .filter(function () {
                    var elementId = $(this).attr("id");
                    if (typeof elementId == "undefined") {
                        return false;
                    }
                    if ((elementId.indexOf(SF.compose(self.findOptions.prefix, "ddlTokens_")) != 0)
                        && (elementId.indexOf(SF.compose(self.findOptions.prefix, "lblddlTokens_")) != 0)) {
                        return false;
                    }
                    var currentIndex = elementId.substring(elementId.lastIndexOf("_") + 1, elementId.length);
                    return parseInt(currentIndex) > index;
                })
                .remove();

            var $selectedOption = $selectedColumn.children("option:selected");
            if ($selectedOption.val() == "") {
                if (index == 0) {
                    this.changeButtonState($btnAddFilter, lang.signum.selectToken);
                    this.changeButtonState($btnAddColumn, lang.signum.selectToken);
                }
                else {
                    var $prevSelectedOption = $(this.pf("ddlTokens_" + (parseInt(index, 10) - 1))).find("option:selected");
                    this.changeButtonState($btnAddFilter, $prevSelectedOption.attr("data-filter"), function () { self.addFilter(); });
                    this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () { self.addColumn(); });
                }
                return;
            }

            this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () { self.addFilter(); });
            this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () { self.addColumn(); });

            var tokenName = this.constructTokenName();
            var webQueryName = ((SF.isEmpty(this.findOptions.webQueryName)) ? $(this.pf(this.webQueryName)).val() : this.findOptions.webQueryName);

            var serializer = new SF.Serializer().add({
                webQueryName: webQueryName,
                tokenName: tokenName,
                index: index,
                prefix: this.findOptions.prefix
            });
            if (!SF.isEmpty(requestExtraJsonData)) {
                serializer.add(requestExtraJsonData);
            }

            $.ajax({
                url: controllerUrl,
                data: serializer.serialize(),
                async: false,
                success: function (newCombo) {
                    $(self.pf("ddlTokens_" + index)).after(newCombo);
                }
            });
        },

        changeButtonState: function ($button, disablingMessage, enableCallback) {
            var hiddenId = $button.attr("id") + "temp";
            if (typeof disablingMessage != "undefined") {
                $button.addClass("ui-button-disabled").addClass("ui-state-disabled").addClass("sf-disabled").attr("disabled", "disabled").attr("title", disablingMessage);
                $button.unbind('click').bind('click', function (e) { e.preventDefault(); return false; });
            }
            else {
                var self = this;
                $button.removeClass("ui-button-disabled").removeClass("ui-state-disabled").removeClass("sf-disabled").prop("disabled", null).attr("title", "");
                $button.unbind('click').bind('click', enableCallback);
            }
        },

        constructTokenName: function () {
            SF.log("FindNavigator constructTokenName");
            var tokenName = "",
            stop = false;

            for (var i = 0; !stop; i++) {
                var currSubtoken = $(this.pf("ddlTokens_" + i));
                if (currSubtoken.length > 0)
                    tokenName = SF.compose(tokenName, currSubtoken.val(), ".");
                else
                    stop = true;
            }
            return tokenName;
        },

        quickFilter: function (value, tokenName, quickFilterUrl) {
            SF.log("FindNavigator quickFilter");
            var tableFilters = $(this.pf("tblFilters tbody"));
            if (tableFilters.length === 0) {
                return;
            }

            var params = {
                "value": value,
                "webQueryName": $(this.pf(this.webQueryName)).val(),
                "tokenName": tokenName,
                "prefix": this.findOptions.prefix,
                "index": this.newFilterRowIndex()
            };

            var self = this;
            $.ajax({
                url: quickFilterUrl,
                data: params,
                async: false,
                success: function (filterHtml) {
                    var $filterList = self.control().find(".sf-filters-list");
                    $filterList.find(".sf-explanation").hide();
                    $filterList.find("table").show();

                    tableFilters.append(filterHtml);
                    SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
                }
            });
        },

        quickFilterCell: function ($elem, quickFilterUrl) {
            SF.log("FindNavigator quickFilterCell");

            var value;
            var data = $elem.children(".sf-data");
            if (data.length == 0) {
                var cb = $elem.find("input:checkbox");
                if (cb.length == 0)
                    value = $elem.html().trim();
                else
                    value = cb.filter(":checked").length > 0;
            }
            else {
                value = data.val();
            }

            var cellIndex = $elem[0].cellIndex;
            var tokenName = $($($elem.closest(".sf-search-results")).find("th")[cellIndex]).children("input:hidden").val();

            this.quickFilter(value, tokenName, quickFilterUrl);
        },

        quickFilterHeader: function ($elem, quickFilterUrl) {
            SF.log("FindNavigator quickFilterHeader");

            this.quickFilter("", $elem.find("input:hidden").val(), quickFilterUrl);
        },

        deleteFilter: function (elem) {
            var $tr = $(elem).closest("tr");
            if ($tr.find("select[disabled]").length)
                return;

            if ($tr.siblings().length == 0) {
                var $filterList = $tr.closest(".sf-filters-list");
                $filterList.find(".sf-explanation").show();
                $filterList.find("table").hide();
            }

            $tr.remove();
        },

        requestDataForSearchPopupCreate: function () {
            return {
                filters: this.serializeFilters(),
                webQueryName: ((SF.isEmpty(this.findOptions.webQueryName)) ? $(this.pf(this.webQueryName)).val() : this.findOptions.webQueryName)
            };
        },

        viewOptionsForSearchCreate: function (_viewOptions) {
            SF.log("FindNavigator viewOptionsForSearchCreate");
            if (this.findOptions.prefix != _viewOptions.prefix)
                throw "FindOptions prefix and ViewOptions prefix don't match";
            _viewOptions.prefix = SF.compose("New", _viewOptions.prefix);
            var self = this;
            return $.extend({
                containerDiv: null,
                onCancelled: null
            }, _viewOptions);
        },

        viewOptionsForSearchPopupCreate: function (_viewOptions) {
            SF.log("FindNavigator viewOptionsForSearchPopupCreate");
            if (this.findOptions.prefix != _viewOptions.prefix)
                throw "FindOptions prefix and ViewOptions prefix don't match";
            _viewOptions.prefix = SF.compose("New", _viewOptions.prefix);
            var self = this;
            return $.extend({
                containerDiv: null,
                requestExtraJsonData: this.requestDataForSearchPopupCreate(),
                onCancelled: null
            }, _viewOptions);
        },

        getRuntimeType: function (typeChooserUrl, _onTypeFound) {
            SF.log("FindNavigator getRuntimeType");
            var typeStr = $(this.pf(SF.Keys.entityTypeNames)).val();
            var types = typeStr.split(",");
            if (types.length == 1)
                return _onTypeFound(types[0]);

            SF.openTypeChooser(this.findOptions.prefix, _onTypeFound, { controllerUrl: typeChooserUrl, types: typeStr });
        },

        typedCreate: function (viewOptions) {
            SF.log("FindNavigator typedCreate");
            if (SF.isEmpty(viewOptions.prefix)) {
                var fullViewOptions = this.viewOptionsForSearchCreate(viewOptions);
                new SF.ViewNavigator(fullViewOptions).navigate();
            }
            else {
                var saveUrl = this.control().data("popup-save-url");
                var fullViewOptions = this.viewOptionsForSearchPopupCreate(viewOptions);
                new SF.ViewNavigator(fullViewOptions).createSave(saveUrl);
            }
        },

        toggleSelectAll: function () {
            SF.log("FindNavigator toggleSelectAll");
            var select = $(this.pf("cbSelectAll:checked"));
            $("input:checkbox[name^=" + SF.compose(this.findOptions.prefix, "rowSelection") + "]")
        .attr('checked', (select.length > 0) ? true : false);
        }
    };

    SF.FindNavigator.create = function (viewOptions, typeChooserUrl) {
        var findNavigator = new SF.FindNavigator({ prefix: viewOptions.prefix });
        var type = findNavigator.getRuntimeType(typeChooserUrl, function (type) {
            findNavigator.typedCreate($.extend({ type: type }, viewOptions));
        });
    }

    SF.FindNavigator.asyncSearchFinished = new Array();
    SF.FindNavigator.searchOnLoad = function (prefix) {
        var btnSearchId = SF.compose(prefix, "qbSearch");
        var $button = $("#" + btnSearchId);
        var makeSearch = function () {
            if (!SF.FindNavigator.asyncSearchFinished[btnSearchId]) {
                $button.click();
                SF.FindNavigator.asyncSearchFinished[btnSearchId] = true;
            }
        };

        if ($("#" + SF.compose(prefix, "divResults")).is(':visible')) {
            makeSearch();
        }
        else {
            var $tabContainer = $button.closest(".sf-tabs");
            if ($tabContainer.length > 0) {
                $tabContainer.bind("tabsshow", function () {
                    if ($("#" + SF.compose(prefix, "divResults")).is(':visible')) {
                        makeSearch();
                    }
                });
            } else {
                makeSearch();
            }
        }
    };

    $(".sf-subtokens-expander").live('click', function () {
        var $this = $(this);
        $this.next().show().focus().click();
        $this.remove();
    });
});
