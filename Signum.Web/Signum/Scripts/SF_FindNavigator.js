if (!FindNavigator && typeof FindNavigator == "undefined") {

    function EntityCellContextMenu(e) {
        log("entity contextmenu");

        var $target = $(e.target);

        var hiddenQueryName = $target.closest(".searchControl").children("input:hidden[id$=sfWebQueryName]");
        var idHiddenQueryName = hiddenQueryName.attr('id');
        var prefix = idHiddenQueryName.substring(0, idHiddenQueryName.indexOf("sfWebQueryName"));
        if (prefix.charAt(prefix.length - 1) == "_")
            prefix = prefix.substring(0, prefix.length - 1);

        var showCtxUrl = window[prefix.compose("EntityContextMenuUrl")];
        if (showCtxUrl == undefined)
            return false; //EntityContextMenu not active

        var $cmenu = $("<div class='searchCtxMenu'></div>");
        $('<div class="searchCtxMenuOverlay"></div>').click(function (e) {
            log("contextmenu click");
            var $target = $(e.target);
            if ($target.hasClass("searchCtxItem") || $target.parent().hasClass("searchCtxItem"))
                $cmenu.hide();
            else
                $('.searchCtxMenuOverlay').remove();
        }).append($cmenu).appendTo($target);
        $cmenu.css({
            left: $target.position().left + ($target.outerWidth() / 2),
            top: $target.position().top + ($target.outerHeight() / 2),
            zIndex: '101'
        }).show();

        $target.addClass("contextmenu-active");
        SF.ajax({
            url: showCtxUrl,
            type: "POST",
            async: true,
            dataType: "html",
            data: { lite: $target.parent().attr('data-entity'), webQueryName: hiddenQueryName.val(), prefix: prefix },
            success: function (items) { $cmenu.html(items); }
        });

        return false;
    }

    function CellContextMenu(e) {
        log("contextmenu");
        var $target = $(e.target);

        var $cmenu = $("<div class='searchCtxMenu'><div class='searchCtxItem quickFilter'><span>Add filter</span></div></div>");
        $('<div class="searchCtxMenuOverlay"></div>').click(function (e) {
            log("contextmenu click");
            var $target = $(e.target);
            if ($target.hasClass("searchCtxItem") || $target.parent().hasClass("searchCtxItem"))
                $cmenu.hide();
            else
                $('.searchCtxMenuOverlay').remove();
        }).append($cmenu).appendTo($target);
        $cmenu.css({
            left: $target.position().left + ($target.outerWidth() / 2),
            top: $target.position().top + ($target.outerHeight() / 2),
            zIndex: '101'
        }).show();

        return false;
    }

    var FindNavigator = function (_findOptions) {
        this.findOptions = $.extend({
            prefix: "",
            webQueryName: null,
            searchOnLoad: false,
            allowMultiple: null,
            create: true,
            view: true,
            top: null,
            filters: null,
            filterMode: null,
            orders: null, //A Json array like ["columnName1","-columnName2"] => will order by columnname1 asc, then by columnname2 desc
            columns: null, //List of column names "columnName1,columnName2"
            columnMode: null,
            allowUserColumns: null,
            navigatorControllerUrl: null,
            searchControllerUrl: null,
            onOk: null,
            onCancelled: null,
            onOkClosed: null,
            async: true
        }, _findOptions);

        this.$control = $(this.pf("divSearchControl"));
    };

    FindNavigator.prototype = {

        pf: function (s) {
            return "#" + this.findOptions.prefix.compose(s);
        },

        tempDivId: function () {
            return this.findOptions.prefix + "Temp";
        },

        initialize: function () {
            var self = this;
            $(this.pf("tblResults") + " th:not(.thRowEntity):not(.thRowSelection)")
            .live('click', function (e) {
                if ($(this).hasClass("columnEditing"))
                    return true;
                Sort(e, window[self.findOptions.prefix.compose("SearchUrl")]);
                return false;
            })
            .live('mousedown', function (e) {
                if ($(this).hasClass("columnEditing"))
                    return true;
                this.onselectstart = function () { return false };
                return false;
            });

            $(this.pf("tblResults td")).live('contextmenu', function (e) {
                if ($(e.target).hasClass("searchCtxMenuOverlay") || $(e.target).parents().hasClass("searchCtxMenuOverlay")) {
                    $('.searchCtxMenuOverlay').remove();
                    return false;
                }

                var $this = $(this);
                var index = $this.index();
                var $th = $this.closest("table").find("th").eq(index);
                if ($th.hasClass('thRowSelection'))
                    return false;
                if ($th.hasClass('thRowEntity'))
                    EntityCellContextMenu(e);
                else
                    CellContextMenu(e);
                return false;
            });

            $(this.pf("tblResults") + " .searchCtxItem.quickFilter").live('click', function () {
                log("contextmenu item click");
                var $elem = $(this).closest("td");
                $('.searchCtxMenuOverlay').remove();

                var quickFilterUrl = window[self.findOptions.prefix.compose("QuickFilterUrl")];
                if (quickFilterUrl == undefined)
                    return false; //QuickFilters not active            

                QuickFilter($elem, quickFilterUrl);
            });
        },

        openFinder: function () {
            log("FindNavigator openFinder");
            var self = this;
            SF.ajax({
                type: "POST",
                url: this.findOptions.navigatorControllerUrl,
                data: this.requestDataForOpenFinder(),
                async: false,
                dataType: "html",
                success: function (popupHtml) {
                    $('#divASustituir').after(hiddenDiv(self.tempDivId(), popupHtml));
                    new popup().show(self.tempDivId());
                    $(self.pf(sfBtnOk)).unbind('click').click(function () { self.onSearchOk(); });
                    $(self.pf(sfBtnCancel)).unbind('click').click(function () { self.onSearchCancel(); });
                }
            });
        },

        selectedItems: function () {
            log("FindNavigator selectedItems");
            var items = [];
            var selected = $("input:radio[name=" + this.findOptions.prefix.compose("rowSelection") + "]:checked, input:checkbox[name^=" + this.findOptions.prefix.compose("rowSelection") + "]:checked");
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
            log("FindNavigator splitSelectedIds");
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
            this.editColumnsFinish();

            var $btnSearch = $(this.pf("btnSearch"));
            $btnSearch.toggleClass('loading').val(lang.signum.searching);

            var self = this;
            SF.ajax({
                type: "POST",
                url: this.findOptions.searchControllerUrl,
                data: this.requestDataForSearch(),
                async: this.findOptions.async,
                dataType: "html",
                success: function (r) {
                    var idBtnSearch = $btnSearch.attr('id');
                    if (asyncSearchFinished[idBtnSearch])
                        asyncSearchFinished[idBtnSearch] = false;
                    $btnSearch.val(lang.signum.search).toggleClass('loading');
                    if (!empty(r))
                        self.$control.find(".divResults tbody").html(r);
                    else {
                        var columns = $(self.pf("divResults th")).length;
                        self.$control.find(".divResults tbody").html("<tr><td colspan=\"" + columns + "\">" + lang.signum.noResults + "</td></tr>")
                    }
                },
                error: function () {
                    $btnSearch.val(lang.signum.search).toggleClass('loading');
                }
            });

        },

        requestDataForSearch: function () {
            var requestData = new Object();
            requestData["webQueryName"] = $(this.pf(sfWebQueryName)).val();
            requestData[sfTop] = $(this.pf(sfTop)).val();
            requestData[sfAllowMultiple] = $(this.pf(sfAllowMultiple)).val();

            var canView = $(this.pf(sfView)).val();
            requestData[sfView] = (empty(canView) ? true : canView);

            var currentfilters = this.serializeFilters();
            if (!empty(currentfilters))
                $.extend(requestData, currentfilters);

            requestData["sfOrderBy"] = this.serializeOrders();
            requestData["sfColumns"] = this.serializeColumns();
            requestData["sfColumnMode"] = 'Replace';

            requestData[sfPrefix] = this.findOptions.prefix;
            return requestData;
        },

        requestDataForOpenFinder: function () {
            var requestData = new Object();
            requestData["webQueryName"] = this.findOptions.webQueryName;
            requestData[sfTop] = this.findOptions.top;
            requestData[sfAllowMultiple] = this.findOptions.allowMultiple;
            if (this.findOptions.view == false)
                requestData[sfView] = this.findOptions.view;
            if (this.findOptions.searchOnLoad == true)
                requestData["sfSearchOnLoad"] = this.findOptions.searchOnLoad;

            if (this.findOptions.async)
                requestData["sfAsync"] = this.findOptions.async;

            if (this.findOptions.filterMode != null)
                requestData["sfFilterMode"] = this.findOptions.filterMode;

            if (!this.findOptions.create)
                requestData["sfCreate"] = this.findOptions.create;

            if (!empty(this.findOptions.filters)) {
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

            if (this.findOptions.orders != null)
                requestData["sfOrderBy"] = this.findOptions.orders;
            if (this.findOptions.columns != null)
                requestData["sfColumns"] = this.findOptions.columns;
            if (this.findOptions.columnMode != null)
                requestData["sfColumnMode"] = this.findOptions.columnMode;

            requestData[sfPrefix] = this.findOptions.prefix;

            return requestData;
        },

        serializeFilters: function () {
            var result = "", self = this;
            $(this.pf("tblFilters > tbody > tr")).each(function () {
                result = $.extend(result,
                self.serializeFilter($(this)));
            });
            return result;
        },

        serializeFilter: function ($filter) {

            var id = $filter[0].id;
            var index = id.substring(id.lastIndexOf("_") + 1, id.length);

            var selector = $(this.pf("ddlSelector").compose(index) + " option:selected", $filter);
            var value = $(this.pf("value").compose(index), $filter).val();

            var valBool = $("input:checkbox[id=" + this.findOptions.prefix.compose("value").compose(index) + "]", $filter); //it's a checkbox

            if (valBool.length > 0) value = valBool[0].checked;

            var info = RuntimeInfoFor(this.findOptions.prefix.compose("value").compose(index));
            if (info.find().length > 0) //If it's a Lite, the value is the Id
                value = info.runtimeType() + ";" + info.id();

            var filter = new Object();
            filter["cn" + index] = $filter.find("td:nth-child(1) > :hidden").val();
            filter["sel" + index] = selector.val();
            filter["val" + index] = value;
            return filter;
        },

        serializeOrders: function () {
            var currOrder = $(this.pf("OrderBy")).val();
            if (empty(currOrder))
                return "";
            return currOrder.replace(/"/g, "");
        },

        setNewSortOrder: function ($th, multiCol) {
            log("FindNavigator sort");
            var columnName = $th.find("input:hidden").val();

            var currOrderArray = [];
            var currOrder = $(this.pf("OrderBy")).val();
            if (!empty(currOrder))
                currOrderArray = currOrder.split(",");

            var found = false;
            var currIndex;
            var oldOrder = "";
            for (var currIndex = 0; currIndex < currOrderArray.length && !found; currIndex++) {
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
            var currOrder = $(this.pf("OrderBy"));
            if (!multiCol) {
                this.$control.find(".divResults th").removeClass("headerSortUp headerSortDown");
                currOrder.val(newOrder + columnName);
            }
            else {
                if (found)
                    currOrderArray[currIndex] = newOrder + columnName;
                else
                    currOrderArray[currOrderArray.length] = newOrder + columnName;
                var currOrderStr = "";
                for (var i = 0; i < currOrderArray.length; i++)
                    currOrderStr = currOrderStr.compose(currOrderArray[i], ",");
                currOrder.val(currOrderStr);
            }

            if (newOrder == "-")
                $th.removeClass("headerSortDown").addClass("headerSortUp");
            else
                $th.removeClass("headerSortUp").addClass("headerSortDown");

            return this;
        },

        serializeColumns: function () {
            log("FindNavigator serializeColumns");
            var result = "";
            var self = this;
            $(this.pf("tblResults thead tr th:not(.thRowEntity):not(.thRowSelection)")).each(function () {
                var $this = $(this);
                result = result.compose($this.find("input:hidden").val() + ";" + $this.text().trim(), ",");
            });
            return result;
        },

        onSearchOk: function () {
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

        onSearchCancel: function () {
            log("FindNavigator onSearchCancel");
            $('#' + this.tempDivId()).remove();
            if (this.findOptions.onCancelled != null)
                this.findOptions.onCancelled();
        },

        addColumn: function (getColumnNameUrl) {
            log("FindNavigator addColumn");

            if (isFalse(this.findOptions.allowUserColumns) || $(this.pf("tblFilters tbody")).length == 0)
                throw "Adding columns is not allowed";

            this.editColumnsFinish();

            var tokenName = this.constructTokenName();
            if (empty(tokenName)) return;

            var prefixedTokenName = this.findOptions.prefix.compose(tokenName);
            if ($(this.pf("tblResults thead tr th[id=\"" + prefixedTokenName + "\"]")).length > 0) return;

            var $tblHeaders = $(this.pf("tblResults thead tr"));

            var webQueryName = ((empty(this.findOptions.webQueryName)) ? $(this.pf(sfWebQueryName)).val() : this.findOptions.webQueryName);
            var self = this;
            SF.ajax({
                type: "POST",
                url: getColumnNameUrl,
                data: { "webQueryName": webQueryName, "tokenName": tokenName },
                async: false,
                dataType: "html",
                success: function (columnNiceName) {
                    $tblHeaders.append("<th><input type=\"hidden\" value=\"" + tokenName + "\" />" + columnNiceName + "</th>");
                    $(self.pf("btnEditColumns")).show();
                }
            });
        },

        editColumns: function () {
            log("FindNavigator editColumns");

            var self = this;
            $(this.pf("tblResults thead tr th:not(.thRowEntity):not(.thRowSelection)")).each(function () {
                var th = $(this);
                th.addClass("columnEditing");
                var hidden = th.find("input:hidden");
                th.html("<input type=\"text\" value=\"" + th.text().trim() + "\" />" +
                    "<br /><a id=\"link-delete-user-col\" onclick=\"DeleteColumn('" + self.findOptions.prefix + "', '" + hidden.val() + "');\">Delete Column</a>")
              .append(hidden);
            });

            $(this.pf("btnEditColumnsFinish")).show();
            $(this.pf("btnEditColumns")).hide();
        },

        editColumnsFinish: function () {
            log("FindNavigator editColumnsFinish");

            var $btnFinish = $(this.pf("btnEditColumnsFinish:visible"));
            if ($btnFinish.length == 0)
                return;

            var self = this;
            $(this.pf("tblResults thead tr th:not(.thRowEntity):not(.thRowSelection)")).each(function () {
                var th = $(this);
                th.removeClass("columnEditing");
                var hidden = th.find("input:hidden");
                var newColName = th.find("input:text").val();
                th.html(newColName).append(hidden);
            });

            $btnFinish.hide();
            $(this.pf("btnEditColumns")).show();
        },

        deleteColumn: function (columnName) {
            log("FindNavigator deleteColumn");

            var self = this;
            $(this.pf("tblResults thead tr th"))
        .filter(function () { return $(this).find("input:hidden[value='" + columnName + "']").length > 0 })
        .remove();

            $(this.pf("tblResults tbody")).html("");

            if ($(this.pf("tblResults thead tr th")).length == 0)
                $(this.pf("btnEditColumnsFinish")).hide();
        },

        addFilter: function (addFilterUrl) {
            log("FindNavigator addFilter");

            var tableFilters = $(this.pf("tblFilters tbody"));
            if (tableFilters.length == 0)
                throw "Adding filters is not allowed";

            var tokenName = this.constructTokenName();
            if (empty(tokenName)) return;

            var webQueryName = ((empty(this.findOptions.webQueryName)) ? $(this.pf(sfWebQueryName)).val() : this.findOptions.webQueryName);

            var self = this;
            SF.ajax({
                type: "POST",
                url: addFilterUrl,
                data: { "webQueryName": webQueryName, "tokenName": tokenName, "index": this.newFilterRowIndex(), "prefix": this.findOptions.prefix },
                async: false,
                dataType: "html",
                success: function (filterHtml) {
                    var $filterList = self.$control.find(".filters-list");
                    $filterList.find(".explanation").hide();
                    $filterList.find("table").show();
                    tableFilters.append(filterHtml);

                    $(self.pf("btnClearAllFilters"), self.$control).show();
                }
            });
        },

        newFilterRowIndex: function () {
            log("FindNavigator newFilterRowIndex");
            var lastRow = $(this.pf("tblFilters tbody tr:last"));
            var lastRowIndex = -1;
            if (lastRow.length == 1)
                lastRowIndex = lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length);
            return parseInt(lastRowIndex) + 1;
        },

        newSubTokensCombo: function (index, controllerUrl) {
            log("FindNavigator newSubTokensCombo");
            var selectedColumn = $(this.pf("ddlTokens_" + index));
            if (selectedColumn.length == 0) return;

            //Clear child subtoken combos
            var self = this;
            $("select,span")
        .filter(function () {
            return ($(this).attr("id").indexOf(self.findOptions.prefix.compose("ddlTokens_")) == 0)
            || ($(this).attr("id").indexOf(self.findOptions.prefix.compose("lblddlTokens_")) == 0)
        })
        .filter(function () {
            var currentId = $(this).attr("id");
            var lastSeparatorIndex = currentId.lastIndexOf("_");
            var currentIndex = currentId.substring(lastSeparatorIndex + 1, currentId.length);
            return parseInt(currentIndex) > index;
        })
        .remove();

            if (selectedColumn.children("option:selected").val() == "") return;

            var tokenName = this.constructTokenName();
            var webQueryName = ((empty(this.findOptions.webQueryName)) ? $(this.pf(sfWebQueryName)).val() : this.findOptions.webQueryName);

            SF.ajax({
                type: "POST",
                url: controllerUrl,
                data: { "webQueryName": webQueryName, "tokenName": tokenName, "index": index, "prefix": this.findOptions.prefix },
                async: false,
                dataType: "html",
                success: function (newCombo) {
                    $(self.pf("ddlTokens_" + index)).after(newCombo);
                }
            });
        },

        constructTokenName: function () {
            log("FindNavigator constructTokenName");
            var tokenName = "",
            stop = false;
            //var $fieldsList = $(".fields-list", this.$control);

            for (var i = 0; !stop; i++) {
                var currSubtoken = $(this.pf("ddlTokens_" + i));
                if (currSubtoken.length > 0)
                    tokenName = tokenName.compose(currSubtoken.val(), ".");
                else
                    stop = true;
            }
            return tokenName;
        },

        quickFilter: function ($elem, quickFilterUrl) {
            log("FindNavigator quickFilter");
            var tableFilters = $(this.pf("tblFilters tbody"));
            if (tableFilters.length == 0)
                return;
            var params;
            var data = $elem.children(".data");
            if (data.length == 0) {
                var cb = $elem.find("input:checkbox");
                if (cb.length == 0)
                    params = { "sfValue": $elem.html().trim() };
                else
                    params = { "sfValue": (cb.filter(":checked").length > 0) };
            }
            else {
                params = { "sfValue": data.val() };
            }

            var cellIndex = $elem[0].cellIndex;

            params = $.extend(params, {
                "webQueryName": $(this.pf(sfWebQueryName)).val(),
                "tokenName": $($($elem.closest(".tblResults")).find("th")[cellIndex]).children("input:hidden").val(),
                "prefix": this.findOptions.prefix,
                "index": this.newFilterRowIndex()
            });

            var self = this;
            SF.ajax({
                type: "POST",
                url: quickFilterUrl,
                data: params,
                async: false,
                dataType: "html",
                success: function (filterHtml) {
                    var $filterList = self.$control.find(".filters-list");
                    $filterList.find(".explanation").hide();
                    $filterList.find("table").show();
                    tableFilters.append(filterHtml);
                    $(self.pf("btnClearAllFilters"), self.$control).show();
                }
            });
        },

        deleteFilter: function (elem) {
            var $tr = $(elem).closest("tr");
            if ($tr.find("select[disabled]").length)
                return;

            if ($tr.siblings().length == 0) {
                var $filterList = $tr.closest(".filters-list");
                $filterList.find(".explanation").show();
                $filterList.find("table").hide();
                $(this.pf("btnClearAllFilters"), this.$control).hide();
            }

            $tr.remove();
        },

        clearAllFilters: function () {
            log("FindNavigator clearAllFilters");

            this.$control.find(".filters-list")
                     .find(".explanation").show().end()
                     .find("table").hide()
                      .find("tbody > tr").remove();

            $(this.pf("btnClearAllFilters"), this.$control).hide();

        },

        requestDataForSearchPopupCreate: function () {
            var requestData = this.serializeFilters();
            var requestData = $.extend(requestData, { webQueryName: ((empty(this.findOptions.webQueryName)) ? $(this.pf(sfWebQueryName)).val() : this.findOptions.webQueryName) });
            return requestData;
        },

        viewOptionsForSearchCreate: function (_viewOptions) {
            log("FindNavigator viewOptionsForSearchCreate");
            if (this.findOptions.prefix != _viewOptions.prefix)
                throw "FindOptions prefix and ViewOptions prefix don't match";
            _viewOptions.prefix = "New".compose(_viewOptions.prefix);
            var self = this;
            return $.extend({
                type: $(this.pf(sfEntityTypeName)).val(),
                containerDiv: null,
                onCancelled: null
            }, _viewOptions);
        },

        viewOptionsForSearchPopupCreate: function (_viewOptions) {
            log("FindNavigator viewOptionsForSearchPopupCreate");
            if (this.findOptions.prefix != _viewOptions.prefix)
                throw "FindOptions prefix and ViewOptions prefix don't match";
            _viewOptions.prefix = "New".compose(_viewOptions.prefix);
            var self = this;
            return $.extend({
                type: $(this.pf(sfEntityTypeName)).val(),
                containerDiv: null,
                requestExtraJsonData: this.requestDataForSearchPopupCreate(),
                onCancelled: null
            }, _viewOptions);
        },

        toggleSelectAll: function () {
            log("FindNavigator toggleSelectAll");
            var select = $(this.pf("cbSelectAll:checked"));
            $("input:checkbox[name^=" + this.findOptions.prefix.compose("rowSelection") + "]")
        .attr('checked', (select.length > 0) ? true : false);
        }
    };

    function InitializeSearchControl(prefix) {
        new FindNavigator({ prefix: prefix }).initialize();
    }

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
            NotifyInfo(lang.signum.noElementsSelected);
            return;
        }
        onSuccess(items);
    }

    function AddColumn(prefix, getColumnNameUrl) {
        new FindNavigator({ prefix: prefix }).addColumn(getColumnNameUrl);
    }

    function EditColumns(prefix) {
        new FindNavigator({ prefix: prefix }).editColumns();
    }

    function EditColumnsFinish(prefix) {
        new FindNavigator({ prefix: prefix }).editColumnsFinish();
    }

    function DeleteColumn(prefix, columnName) {
        new FindNavigator({ prefix: prefix }).deleteColumn(columnName);
    }

    function AddFilter(prefix, addFilterUrl) {
        new FindNavigator({ prefix: prefix }).addFilter(addFilterUrl);
    }

    function NewSubTokensCombo(_findOptions, index, controllerUrl) {
        new FindNavigator(_findOptions).newSubTokensCombo(index, controllerUrl);
    }

    function QuickFilter($elem, quickFilterUrl) {
        var idtblresults = $elem.closest(".tblResults")[0].id;
        var prefix = (idtblresults == "tblResults") ? "" : idtblresults.substring(0, idtblresults.indexOf("tblResults") - 1);
        new FindNavigator({ prefix: prefix }).quickFilter($elem, quickFilterUrl);
    }

    function DeleteFilter(prefix, index) {
        new FindNavigator({ prefix: prefix }).deleteFilter(index);
    }

    function ClearAllFilters(prefix) {
        new FindNavigator({ prefix: prefix }).clearAllFilters();
    }

    function SearchCreate(viewOptions, createUrl) {
        var findNavigator = new FindNavigator({ prefix: viewOptions.prefix });
        if (empty(viewOptions.prefix)) {
            var viewOptions = findNavigator.viewOptionsForSearchCreate(viewOptions);
            new ViewNavigator(viewOptions).navigate();
        }
        else {
            var viewOptions = findNavigator.viewOptionsForSearchPopupCreate(viewOptions);
            new ViewNavigator(viewOptions).createSave();
        }
    }

    function ToggleSelectAll(prefix) {
        var findNavigator = new FindNavigator({ prefix: prefix }).toggleSelectAll();
    }

    function Sort(evt, controllerUrl) {
        var $target = $(evt.target);
        var searchControlDiv = $target.parents(".searchControl");

        var prefix = searchControlDiv[0].id;
        prefix = prefix.substring(0, prefix.indexOf("divSearchControl"));
        if (prefix.lastIndexOf("_") == prefix.length - 1)
            prefix = prefix.substring(0, prefix.length - 1);
        var findNavigator = new FindNavigator({ prefix: prefix, searchControllerUrl: controllerUrl });

        var multiCol = evt.shiftKey;

        findNavigator.setNewSortOrder($target, multiCol).search();
    }

    function toggleVisibility(elementId) {
        $('#' + elementId).toggle();
    }

    function toggleFilters(elem) {
        var $elem = $(elem);
        $elem.toggleClass('close').siblings(".filters").toggle();
        if ($elem.hasClass('close')) $elem.html('Mostrar filtros');
        else $elem.html('Ocultar filtros');
        return false;
    }

    var asyncSearchFinished = new Array();
    function SearchOnLoad(prefix) {
        var btnSearchId = prefix.compose("btnSearch");
        var $button = $("#" + btnSearchId);
        var makeSearch = function () {
            if (!asyncSearchFinished[btnSearchId]) {
                $button.click();
                asyncSearchFinished[btnSearchId] = true;
            }
        };

        if ($("#" + prefix.compose("divResults")).is(':visible')) {
            makeSearch();
        }
        else {
            var $tabContainer = $button.parents(".tabs").first();
            if ($tabContainer.length) {
                $tabContainer.find("a").click(
                function () {
                    if ($("#" + prefix.compose("divResults")).is(':visible')) makeSearch();
                });
            } else {
                makeSearch();
            }
        }
    }

}
