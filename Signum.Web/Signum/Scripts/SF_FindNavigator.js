/// <reference path="references.ts"/>
var SF;
(function (SF) {
    (function (FindNavigator) {
        once("SF-searchControl", function () {
            return $.fn.searchControl = function (opt) {
                new SF.SearchControl(this, opt);
            };
        });

        function getFor(prefix) {
            return $("#" + SF.compose(prefix, "sfSearchControl")).data("SF-control");
        }
        FindNavigator.getFor = getFor;

        function openFinder(findOptions) {
            var self = this;
            $.ajax({
                url: findOptions.openFinderUrl || (SF.isEmpty(findOptions.prefix) ? SF.Urls.find : SF.Urls.partialFind),
                data: this.requestDataForOpenFinder(findOptions),
                async: false,
                success: function (popupHtml) {
                    var divId = SF.compose(findOptions.prefix, "Temp");
                    $("body").append(SF.hiddenDiv(divId, popupHtml));
                    SF.triggerNewContent($("#" + divId));
                    $.extend(self.getFor(findOptions.prefix).options, findOptions); //Copy all properties (i.e. onOk was not transmitted)
                    $("#" + divId).popup({
                        onOk: function () {
                            self.getFor(findOptions.prefix).onSearchOk();
                        },
                        onCancel: function () {
                            self.getFor(findOptions.prefix).onSearchCancel();
                        }
                    });
                }
            });
        }
        FindNavigator.openFinder = openFinder;

        function requestDataForOpenFinder(findOptions) {
            var requestData = {
                webQueryName: findOptions.webQueryName,
                elems: findOptions.elems,
                allowMultiple: findOptions.allowMultiple,
                prefix: findOptions.prefix
            };

            if (findOptions.navigate == false) {
                requestData["navigate"] = findOptions.navigate;
            }
            if (findOptions.searchOnLoad == true) {
                requestData["searchOnLoad"] = findOptions.searchOnLoad;
            }
            if (findOptions.filterMode != null) {
                requestData["filterMode"] = findOptions.filterMode;
            }
            if (!findOptions.create) {
                requestData["create"] = findOptions.create;
            }
            if (!findOptions.allowChangeColumns) {
                requestData["allowChangeColumns"] = findOptions.allowChangeColumns;
            }
            if (findOptions.filters != null) {
                requestData["filters"] = findOptions.filters;
            }
            if (findOptions.orders != null) {
                requestData["orders"] = this.serializeOrders(findOptions.orders);
            }
            if (findOptions.columns != null) {
                requestData["columns"] = findOptions.columns;
            }
            if (findOptions.columnMode != null) {
                requestData["columnMode"] = findOptions.columnMode;
            }

            return requestData;
        }
        FindNavigator.requestDataForOpenFinder = requestDataForOpenFinder;

        function serializeOrders(orderArray) {
            var currOrders = orderArray.join(";");
            if (!SF.isEmpty(currOrders)) {
                currOrders += ";";
            }
            return currOrders;
        }
        FindNavigator.serializeOrders = serializeOrders;

        function newSubTokensCombo(webQueryName, prefix, index, controllerUrl, requestExtraJsonData) {
            var $selectedCombo = $("#" + SF.compose(prefix, "ddlTokens_" + index));
            if ($selectedCombo.length == 0) {
                return;
            }

            this.clearChildSubtokenCombos($selectedCombo, prefix, index);

            var $container = $selectedCombo.closest(".sf-search-control");
            if ($container.length > 0) {
                $container.trigger("sf-new-subtokens-combo", $selectedCombo.attr("id"));
            } else {
                $selectedCombo.trigger("sf-new-subtokens-combo", $selectedCombo.attr("id"));
            }

            var $selectedOption = $selectedCombo.children("option:selected");
            if ($selectedOption.val() == "") {
                return;
            }

            var serializer = new SF.Serializer().add({
                webQueryName: webQueryName,
                tokenName: this.constructTokenName(prefix),
                index: index,
                prefix: prefix
            });
            if (!SF.isEmpty(requestExtraJsonData)) {
                serializer.add(requestExtraJsonData);
            }

            var self = this;
            $.ajax({
                url: controllerUrl || SF.Urls.subTokensCombo,
                data: serializer.serialize(),
                dataType: "html",
                success: function (newCombo) {
                    if (newCombo != "<span>no-results</span>") {
                        $("#" + SF.compose(prefix, "ddlTokens_" + index)).after(newCombo);
                    }
                }
            });
        }
        FindNavigator.newSubTokensCombo = newSubTokensCombo;
        ;

        function clearChildSubtokenCombos($selectedCombo, prefix, index) {
            $selectedCombo.siblings("select,span").filter(function () {
                var elementId = $(this).attr("id");
                if (typeof elementId == "undefined") {
                    return false;
                }
                if ((elementId.indexOf(SF.compose(prefix, "ddlTokens_")) != 0) && (elementId.indexOf(SF.compose(prefix, "lblddlTokens_")) != 0)) {
                    return false;
                }
                var currentIndex = elementId.substring(elementId.lastIndexOf("_") + 1, elementId.length);
                return parseInt(currentIndex) > index;
            }).remove();
        }
        FindNavigator.clearChildSubtokenCombos = clearChildSubtokenCombos;

        function constructTokenName(prefix) {
            var tokenName = "";
            var stop = false;
            for (var i = 0; !stop; i++) {
                var currSubtoken = $("#" + SF.compose(prefix, "ddlTokens_" + i));
                if (currSubtoken.length > 0)
                    tokenName = SF.compose(tokenName, currSubtoken.val(), ".");
                else
                    stop = true;
            }
            return tokenName;
        }
        FindNavigator.constructTokenName = constructTokenName;

        function deleteFilter(elem) {
            var $tr = $(elem).closest("tr");
            if ($tr.find("select[disabled]").length > 0) {
                return;
            }

            if ($tr.siblings().length == 0) {
                var $filterList = $tr.closest(".sf-filters-list");
                $filterList.find(".sf-explanation").show();
                $filterList.find("table").hide();
            }

            $tr.remove();
        }
        FindNavigator.deleteFilter = deleteFilter;
    })(SF.FindNavigator || (SF.FindNavigator = {}));
    var FindNavigator = SF.FindNavigator;

    (function (ColumnOptionsMode) {
        ColumnOptionsMode[ColumnOptionsMode["Add"] = 0] = "Add";
        ColumnOptionsMode[ColumnOptionsMode["Remove"] = 1] = "Remove";
        ColumnOptionsMode[ColumnOptionsMode["Replace"] = 2] = "Replace";
    })(SF.ColumnOptionsMode || (SF.ColumnOptionsMode = {}));
    var ColumnOptionsMode = SF.ColumnOptionsMode;

    var FilterMode = (function () {
        function FilterMode() {
        }
        FilterMode.Visible = "Visible";
        FilterMode.Hidden = "Hidden";
        FilterMode.AlwaysHidden = "AlwaysHidden";
        FilterMode.OnlyResults = "OnlyResults";
        return FilterMode;
    })();
    SF.FilterMode = FilterMode;

    var SearchControl = (function () {
        function SearchControl(element, _options) {
            this.keys = {
                elems: "sfElems",
                page: "sfPage",
                pagination: "sfPaginationMode"
            };
            this.searchOnLoadFinished = false;
            element.data("SF-control", this);

            this.element = element;

            this.options = $.extend({
                allowChangeColumns: true,
                allowOrder: true,
                allowMultiple: true,
                columnMode: "Add",
                columns: null,
                create: true,
                elems: null,
                selectedItemsContextMenu: true,
                filterMode: "Visible",
                filters: null,
                navigate: true,
                openFinderUrl: null,
                onCancelled: null,
                onOk: null,
                onOkClosed: null,
                orders: [],
                prefix: "",
                searchOnLoad: false,
                webQueryName: null
            }, _options);

            this._create();
        }
        SearchControl.prototype.pf = function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        };

        SearchControl.prototype.tempDivId = function () {
            return SF.compose(this.options.prefix, "Temp");
        };

        SearchControl.prototype.closeMyOpenedCtxMenu = function () {
            if (this.element.find(".sf-search-ctxmenu-overlay").length > 0) {
                $('.sf-search-ctxmenu-overlay').remove();
                return false;
            }

            return true;
        };

        SearchControl.prototype._create = function () {
            var self = this;

            var $tblResults = self.element.find(".sf-search-results-container");

            if (this.options.allowOrder) {
                $tblResults.on("click", "th:not(.sf-th-entity):not(.sf-th-selection),th:not(.sf-th-entity):not(.sf-th-selection) span,th:not(.sf-th-entity):not(.sf-th-selection) .sf-header-droppable", function (e) {
                    if (e.target != this || $(this).closest(".sf-search-ctxmenu").length > 0) {
                        return;
                    }
                    self.newSortOrder($(e.target).closest("th"), e.shiftKey);
                    self.search();
                    return false;
                });
            }

            if (this.options.allowChangeColumns || (this.options.filterMode != SF.FilterMode[SF.FilterMode.AlwaysHidden] && this.options.filterMode != "OnlyResults")) {
                $tblResults.on("contextmenu", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
                    if (!self.closeMyOpenedCtxMenu()) {
                        return false;
                    }
                    self.headerContextMenu(e);
                    return false;
                });
            }

            if (this.options.allowChangeColumns) {
                $tblResults.on("click", ".sf-search-ctxitem.sf-remove-column > span", function () {
                    var $elem = $(this).closest("th");
                    $('.sf-search-ctxmenu-overlay').remove();

                    self.removeColumn($elem);
                    return false;
                });

                $tblResults.on("click", ".sf-search-ctxitem.sf-edit-column > span", function () {
                    var $elem = $(this).closest("th");
                    $('.sf-search-ctxmenu-overlay').remove();

                    self.editColumn($elem);
                    return false;
                });

                this.createMoveColumnDragDrop();
            }

            if (this.options.filterMode != "AlwaysHidden" && this.options.filterMode != "OnlyResults") {
                $tblResults.on("contextmenu", "td:not(.sf-td-no-results):not(.sf-td-multiply,.sf-search-footer-pagination)", function (e) {
                    if (!self.closeMyOpenedCtxMenu()) {
                        return false;
                    }

                    var $td = $(this).closest("td");

                    var $tr = $td.closest("tr");
                    var $currentRowSelector = $tr.find(".sf-td-selection");
                    if ($currentRowSelector.filter(":checked").length == 0) {
                        self.changeRowSelection($(self.pf("sfSearchControl .sf-td-selection:checked")), false);
                        self.changeRowSelection($currentRowSelector, true);
                    }

                    var index = $td.index();
                    var $th = $td.closest("table").find("th").eq(index);
                    if ($th.hasClass('sf-th-selection') || $th.hasClass('sf-th-entity')) {
                        if (self.options.selectedItemsContextMenu == true) {
                            self.entityContextMenu(e);
                        }
                    } else {
                        self.cellContextMenu(e);
                    }
                    return false;
                });

                $tblResults.on("click", ".sf-search-ctxitem.sf-quickfilter > span", function () {
                    var $elem = $(this).closest("td");
                    $('.sf-search-ctxmenu-overlay').remove();
                    self.quickFilterCell($elem);
                });

                $tblResults.on("click", ".sf-search-ctxitem.sf-quickfilter-header > span", function () {
                    var $elem = $(this).closest("th");
                    $('.sf-search-ctxmenu-overlay').remove();
                    self.quickFilterHeader($elem);
                    return false;
                });
            }

            if (this.options.filterMode != "OnlyResults") {
                $tblResults.on("click", ".sf-pagination-button", function () {
                    $(self.pf(self.keys.page)).val($(this).attr("data-page"));
                    self.search();
                });

                $tblResults.on("change", ".sf-pagination-size", function () {
                    if ($(this).find("option:selected").val() == "All") {
                        self.clearResults();
                    } else {
                        self.search();
                    }
                });

                $tblResults.on("change", ".sf-td-selection", function () {
                    self.changeRowSelection($(this), $(this).filter(":checked").length > 0);
                });

                $(this.pf("sfFullScreen")).on("mousedown", function (e) {
                    e.preventDefault();
                    self.fullScreen(e);
                });

                this.element.on("sf-new-subtokens-combo", function (event) {
                    var args = [];
                    for (var _i = 0; _i < (arguments.length - 1); _i++) {
                        args[_i] = arguments[_i + 1];
                    }
                    self.newSubTokensComboAdded($("#" + args[0]));
                });

                this.element.find(".sf-tm-selected").click(function () {
                    if (!self.closeMyOpenedCtxMenu()) {
                        return false;
                    }

                    self.ctxMenuInDropdown($(this).closest(".sf-dropdown"));
                });
            }

            $tblResults.on("selectstart", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
                return false;
            });

            if (this.options.searchOnLoad) {
                this.searchOnLoad();
            }
        };

        SearchControl.prototype.changeRowSelection = function ($rowSelectors, select) {
            $rowSelectors.prop("checked", select);
            $rowSelectors.closest("tr").toggleClass("ui-state-active", select);

            var $control = $(this.pf("sfSearchControl"));

            var selected = $control.find(".sf-td-selection:checked").length;
            $control.find(".sf-tm-selected > .ui-button-text").html(lang.signum.searchControlMenuSelected + " (" + selected + ")");
        };

        SearchControl.prototype.createCtxMenu = function ($rightClickTarget) {
            var left = $rightClickTarget.position().left + ($rightClickTarget.outerWidth() / 2);
            var top = $rightClickTarget.position().top + ($rightClickTarget.outerHeight() / 2);

            var $cmenu = $("<div class='ui-state-default sf-search-ctxmenu'></div>");
            $cmenu.css({
                left: left,
                top: top,
                zIndex: '101'
            });

            var $ctxMenuOverlay = $('<div class="sf-search-ctxmenu-overlay"></div>').click(function (e) {
                var $clickTarget = $(e.target);
                if ($clickTarget.hasClass("sf-search-ctxitem") || $clickTarget.parent().hasClass("sf-search-ctxitem"))
                    $cmenu.hide();
                else
                    $('.sf-search-ctxmenu-overlay').remove();
            }).append($cmenu);

            return $ctxMenuOverlay;
        };

        SearchControl.prototype.headerContextMenu = function (e) {
            var $th = $(e.target).closest("th");
            var $menu = this.createCtxMenu($th);

            var $itemContainer = $menu.find(".sf-search-ctxmenu");
            if (this.options.filterMode != "AlwaysHidden" && this.options.filterMode != "OnlyResults") {
                $itemContainer.append("<div class='sf-search-ctxitem sf-quickfilter-header'><span>" + lang.signum.addFilter + "</span></div>");
            }

            if (this.options.allowChangeColumns) {
                $itemContainer.append("<div class='sf-search-ctxitem sf-edit-column'><span>" + lang.signum.editColumnName + "</span></div>").append("<div class='sf-search-ctxitem sf-remove-column'><span>" + lang.signum.removeColumn + "</span></div>");
            }

            $th.append($menu);
            return false;
        };

        SearchControl.prototype.cellContextMenu = function (e) {
            var $td = $(e.target);
            var $menu = this.createCtxMenu($td);

            $menu.find(".sf-search-ctxmenu").html("<div class='sf-search-ctxitem sf-quickfilter'><span>" + lang.signum.addFilter + "</span></div>");

            $td.append($menu);
            return false;
        };

        SearchControl.prototype.requestDataForContextMenu = function () {
            return {
                liteKeys: this.element.find(".sf-td-selection:checked").closest("tr").map(function () {
                    return $(this).data("entity");
                }).toArray().join(","),
                webQueryName: this.options.webQueryName,
                prefix: this.options.prefix,
                implementationsKey: $(this.pf("sfEntityTypeNames")).val()
            };
        };

        SearchControl.prototype.entityContextMenu = function (e) {
            var $td = $(e.target).closest("td");
            $td.addClass("sf-ctxmenu-active");

            var $menu = this.createCtxMenu($td);
            var $itemContainer = $menu.find(".sf-search-ctxmenu");

            $.ajax({
                url: SF.Urls.selectedItemsContextMenu,
                data: this.requestDataForContextMenu(),
                success: function (items) {
                    $itemContainer.html(items);
                    $td.append($menu);
                    SF.triggerNewContent($menu);
                }
            });

            return false;
        };

        SearchControl.prototype.ctxMenuInDropdown = function ($dropdown) {
            if ($dropdown.hasClass("sf-open")) {
                var requestData = this.requestDataForContextMenu();
                if (SF.isEmpty(requestData.implementationsKey)) {
                    return;
                }

                var loadingClass = "sf-tm-selected-loading";

                var $ul = $dropdown.children(".sf-menu-button");
                $ul.html($("<li></li>").addClass(loadingClass).html($("<span></span>").addClass("sf-query-button").html(lang.signum.loading)));

                $.ajax({
                    url: SF.Urls.selectedItemsContextMenu,
                    data: requestData,
                    success: function (items) {
                        $ul.find("li").removeClass(loadingClass).html(items);
                        SF.triggerNewContent($ul);
                    }
                });
            }
        };

        SearchControl.prototype.fullScreen = function (evt) {
            var url = this.element.attr("data-find-url") + this.requestDataForSearchInUrl();
            if (evt.ctrlKey || evt.which == 2) {
                window.open(url);
            } else if (evt.which == 1) {
                window.location.href = url;
            }
        };

        SearchControl.prototype.search = function () {
            var $searchButton = $(this.pf("qbSearch"));
            $searchButton.addClass("sf-searching");
            var self = this;
            $.ajax({
                url: SF.Urls.search,
                data: this.requestDataForSearch(),
                success: function (r) {
                    var $tbody = self.element.find(".sf-search-results-container tbody");
                    if (!SF.isEmpty(r)) {
                        $tbody.html(r);
                        SF.triggerNewContent(self.element.find(".sf-search-results-container tbody"));
                    } else {
                        $tbody.html("");
                    }
                    $searchButton.removeClass("sf-searching");
                }
            });
        };

        SearchControl.prototype.requestDataForSearch = function () {
            var requestData = new Object();
            requestData["webQueryName"] = this.options.webQueryName;
            requestData["pagination"] = $(this.pf(this.keys.pagination)).val();
            requestData["elems"] = $(this.pf(this.keys.elems)).val();
            requestData["page"] = ($(this.pf(this.keys.page)).val() || "1");
            requestData["allowMultiple"] = this.options.allowMultiple;
            requestData["navigate"] = this.options.navigate;
            requestData["filters"] = this.serializeFilters();
            requestData["filterMode"] = this.options.filterMode;
            requestData["orders"] = this.serializeOrders();
            requestData["columns"] = this.serializeColumns();
            requestData["columnMode"] = 'Replace';

            requestData["prefix"] = this.options.prefix;
            return requestData;
        };

        SearchControl.prototype.requestDataForSearchInUrl = function () {
            var url = "?pagination=" + $(this.pf(this.keys.pagination)).val() + "&elems=" + $(this.pf(this.keys.elems)).val() + "&page=" + $(this.pf(this.keys.page)).val() + "&filters=" + this.serializeFilters() + "&filterMode=Visible" + "&orders=" + this.serializeOrders() + "&columns=" + this.serializeColumns() + "&columnMode=Replace" + "&navigate=" + this.options.navigate;

            if (!this.options.allowMultiple) {
                url += "&allowMultiple=" + this.options.allowMultiple;
            }

            return url;
        };

        SearchControl.prototype.serializeFilters = function () {
            var result = "", self = this;
            $(this.pf("tblFilters > tbody > tr")).each(function () {
                result += self.serializeFilter($(this)) + ";";
            });
            return result;
        };

        SearchControl.prototype.serializeFilter = function ($filter) {
            var id = $filter[0].id;
            var index = id.substring(id.lastIndexOf("_") + 1, id.length);

            var selector = $(SF.compose(this.pf("ddlSelector"), index) + " option:selected", $filter);
            var value = $(SF.compose(this.pf("value"), index), $filter).val();

            var valBool = $("input:checkbox[id=" + SF.compose(SF.compose(this.options.prefix, "value"), index) + "]", $filter);
            if (valBool.length > 0) {
                value = valBool[0].checked;
            } else {
                var info = new SF.RuntimeInfo(SF.compose(SF.compose(this.options.prefix, "value"), index));
                if (info.find().length > 0) {
                    value = info.entityType() + ";" + info.id();
                    if (value == ";") {
                        value = "";
                    }
                }

                //Encode value CSV-ish style
                var hasQuote = value.indexOf("\"") != -1;
                if (hasQuote || value.indexOf(",") != -1 || value.indexOf(";") != -1) {
                    if (hasQuote) {
                        value = value.replace(/"/g, "\"\"");
                    }
                    value = "\"" + value + "\"";
                }
            }

            return $filter.find("td:nth-child(2) > :hidden").val() + "," + selector.val() + "," + value;
        };

        SearchControl.prototype.serializeOrders = function () {
            return SF.FindNavigator.serializeOrders(this.options.orders);
        };

        SearchControl.prototype.serializeColumns = function () {
            var result = "";
            var self = this;
            $(this.pf("tblResults thead tr th:not(.sf-th-entity):not(.sf-th-selection)")).each(function () {
                var $this = $(this);
                var token = $this.find("input:hidden").val();
                var displayName = $this.text().trim();
                if (token == displayName) {
                    result += token;
                } else {
                    result += token + "," + displayName;
                }
                result += ";";
            });
            return result;
        };

        SearchControl.prototype.onSearchOk = function () {
            var self = this;
            this.hasSelectedItems(function (items) {
                var doDefault = (self.options.onOk != null) ? self.options.onOk(items) : true;
                if (doDefault != false) {
                    $('#' + self.tempDivId()).remove();
                    if (self.options.onOkClosed != null) {
                        self.options.onOkClosed();
                    }
                }
            });
        };

        SearchControl.prototype.onSearchCancel = function () {
            $('#' + this.tempDivId()).remove();
            if (this.options.onCancelled != null) {
                this.options.onCancelled();
            }
        };

        SearchControl.prototype.hasSelectedItems = function (onSuccess) {
            var items = this.selectedItems();
            if (items.length == 0) {
                SF.Notify.info(lang.signum.noElementsSelected);
                return;
            }
            onSuccess(items);
        };

        SearchControl.prototype.hasSelectedItem = function (onSuccess) {
            var items = this.selectedItems();
            if (items.length == 0) {
                SF.Notify.info(lang.signum.noElementsSelected);
                return;
            } else if (items.length > 1) {
                SF.Notify.info(lang.signum.onlyOneElement);
                return;
            }
            onSuccess(items[0]);
        };

        SearchControl.prototype.selectedItems = function () {
            var items = [];
            var selected = $("input:checkbox[name^=" + SF.compose(this.options.prefix, "rowSelection") + "]:checked");
            if (selected.length == 0)
                return items;

            var self = this;
            selected.each(function (i, v) {
                var parts = v.value.split("__");
                var item = {
                    id: parts[0],
                    type: parts[1],
                    key: parts[1] + ";" + parts[0],
                    toStr: parts[2],
                    link: $(this).parent().next().children('a').attr('href')
                };
                items.push(item);
            });

            return items;
        };

        SearchControl.prototype.splitSelectedKeys = function () {
            var selected = this.selectedItems();
            if (selected.length < 1) {
                return '';
            } else {
                return selected.map(function (item) {
                    return item.key;
                }).join(',');
            }
        };

        SearchControl.prototype.newSortOrder = function ($th, multiCol) {
            var columnName = $th.find("input:hidden").val();
            var currentOrders = this.options.orders;

            var indexCurrOrder = $.inArray(columnName, currentOrders);
            var newOrder = "";
            if (indexCurrOrder === -1) {
                indexCurrOrder = $.inArray("-" + columnName, currentOrders);
            } else {
                newOrder = "-";
            }

            if (!multiCol) {
                this.element.find(".sf-search-results-container th").removeClass("sf-header-sort-up sf-header-sort-down");
                this.options.orders = [newOrder + columnName];
            } else {
                if (indexCurrOrder !== -1) {
                    this.options.orders[indexCurrOrder] = newOrder + columnName;
                } else {
                    this.options.orders.push(newOrder + columnName);
                }
            }

            if (newOrder == "-")
                $th.removeClass("sf-header-sort-down").addClass("sf-header-sort-up");
            else
                $th.removeClass("sf-header-sort-up").addClass("sf-header-sort-down");
        };

        SearchControl.prototype.addColumn = function () {
            if (!this.options.allowChangeColumns || $(this.pf("tblFilters tbody")).length == 0) {
                throw "Adding columns is not allowed";
            }

            var tokenName = SF.FindNavigator.constructTokenName(this.options.prefix);
            if (SF.isEmpty(tokenName)) {
                return;
            }

            var prefixedTokenName = SF.compose(this.options.prefix, tokenName);
            if ($(this.pf("tblResults thead tr th[id=\"" + prefixedTokenName + "\"]")).length > 0) {
                return;
            }

            var $tblHeaders = $(this.pf("tblResults thead tr"));

            var self = this;
            $.ajax({
                url: $(this.pf("btnAddColumn")).attr("data-url"),
                data: { "webQueryName": this.options.webQueryName, "tokenName": tokenName },
                async: false,
                success: function (columnNiceName) {
                    $tblHeaders.append("<th class='ui-state-default'>" + "<div class='sf-header-droppable sf-header-droppable-right'></div>" + "<div class='sf-header-droppable sf-header-droppable-left'></div>" + "<input type=\"hidden\" value=\"" + tokenName + "\" />" + "<span>" + columnNiceName + "</span></th>");
                    var $newTh = $tblHeaders.find("th:last");
                    self.createMoveColumnDragDrop($newTh, $newTh.find(".sf-header-droppable"));
                }
            });
        };

        SearchControl.prototype.editColumn = function ($th) {
            var colName = $th.text().trim();

            var popupPrefix = SF.compose(this.options.prefix, "newName");

            var divId = "columnNewName";
            var $div = $("<div id='" + divId + "'></div>");
            $div.html("<p>" + lang.signum.enterTheNewColumnName + "</p>").append("<br />").append("<input type='text' value='" + colName + "' />").append("<br />").append("<br />").append("<input type='button' id='" + SF.compose(popupPrefix, "btnOk") + "' class='sf-button sf-ok-button' value='OK' />");

            var $tempContainer = $("<div></div>").append($div);

            new SF.ViewNavigator({
                onOk: function () {
                    $th.find("span").html($("#columnNewName > input:text").val());
                },
                prefix: popupPrefix
            }).showViewOk($tempContainer.html());
        };

        SearchControl.prototype.moveColumn = function ($source, $target, before) {
            if (before) {
                $target.before($source);
            } else {
                $target.after($source);
            }

            $source.removeAttr("style"); //remove absolute positioning
            this.clearResults();
            this.createMoveColumnDragDrop();
        };

        SearchControl.prototype.createMoveColumnDragDrop = function ($draggables, $droppables) {
            $draggables = $draggables || $(this.pf("tblResults") + " th:not(.sf-th-entity):not(.sf-th-selection)");
            $droppables = $droppables || $(this.pf("tblResults") + " .sf-header-droppable");

            $draggables.draggable({
                revert: "invalid",
                axis: "x",
                opacity: 0.5,
                distance: 8,
                cursor: "move"
            });
            $draggables.removeAttr("style"); //remove relative positioning

            var self = this;
            $droppables.droppable({
                hoverClass: "sf-header-droppable-active",
                tolerance: "pointer",
                drop: function (event, ui) {
                    var $dragged = ui.draggable;

                    var $targetPlaceholder = $(this);
                    var $targetCol = $targetPlaceholder.closest("th");

                    self.moveColumn($dragged, $targetCol, $targetPlaceholder.hasClass("sf-header-droppable-left"));
                }
            });
        };

        SearchControl.prototype.removeColumn = function ($th) {
            $th.remove();
            this.clearResults();
        };

        SearchControl.prototype.clearResults = function () {
            var $tbody = $(this.pf("tblResults tbody"));
            $tbody.find("tr:not('.sf-search-footer')").remove();
            $tbody.prepend($("<tr></tr>").append($("<td></td>").attr("colspan", $tbody.find(".sf-search-footer td").attr("colspan"))));
        };

        SearchControl.prototype.toggleFilters = function () {
            var $toggler = this.element.find(".sf-filters-header");
            this.element.find(".sf-filters").toggle();
            $toggler.toggleClass('close');
            if ($toggler.hasClass('close')) {
                $toggler.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-n").addClass("ui-icon-triangle-1-e");
                $toggler.find(".ui-button-text").html(lang.signum.showFilters);
            } else {
                $toggler.find(".ui-button-icon-primary").removeClass("ui-icon-triangle-1-e").addClass("ui-icon-triangle-1-n");
                $toggler.find(".ui-button-text").html(lang.signum.hideFilters);
            }
            return false;
        };

        SearchControl.prototype.addFilter = function (url, requestExtraJsonData) {
            var tableFilters = $(this.pf("tblFilters tbody"));
            if (tableFilters.length == 0) {
                throw "Adding filters is not allowed";
            }

            var tokenName = SF.FindNavigator.constructTokenName(this.options.prefix);
            if (SF.isEmpty(tokenName)) {
                return;
            }

            var serializer = new SF.Serializer().add({
                webQueryName: this.options.webQueryName,
                tokenName: tokenName,
                index: this.newFilterRowIndex(),
                prefix: this.options.prefix
            });
            if (!SF.isEmpty(requestExtraJsonData)) {
                serializer.add(requestExtraJsonData);
            }

            var self = this;
            $.ajax({
                url: url || SF.Urls.addFilter,
                data: serializer.serialize(),
                async: false,
                success: function (filterHtml) {
                    var $filterList = self.element.closest(".sf-search-control").find(".sf-filters-list");
                    $filterList.find(".sf-explanation").hide();
                    $filterList.find("table").show();

                    tableFilters.append(filterHtml);
                    SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
                }
            });
        };

        SearchControl.prototype.newFilterRowIndex = function () {
            var lastRow = $(this.pf("tblFilters tbody tr:last"));
            if (lastRow.length == 1) {
                return parseInt(lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length)) + 1;
            }
            return 0;
        };

        SearchControl.prototype.newSubTokensComboAdded = function ($selectedCombo) {
            var $btnAddFilter = $(this.pf("btnAddFilter"));
            var $btnAddColumn = $(this.pf("btnAddColumn"));

            var self = this;
            var $selectedOption = $selectedCombo.children("option:selected");
            $selectedCombo.attr("title", $selectedOption.attr("title"));
            $selectedCombo.attr("style", $selectedOption.attr("style"));
            if ($selectedOption.val() == "") {
                var $prevSelect = $selectedCombo.prev("select");
                if ($prevSelect.length == 0) {
                    this.changeButtonState($btnAddFilter, lang.signum.selectToken);
                    this.changeButtonState($btnAddColumn, lang.signum.selectToken);
                } else {
                    var $prevSelectedOption = $prevSelect.find("option:selected");
                    this.changeButtonState($btnAddFilter, $prevSelectedOption.attr("data-filter"), function () {
                        self.addFilter();
                    });
                    this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () {
                        self.addColumn();
                    });
                }
                return;
            }

            this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () {
                self.addFilter();
            });
            this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () {
                self.addColumn();
            });
        };

        SearchControl.prototype.changeButtonState = function ($button, disablingMessage, enableCallback) {
            var hiddenId = $button.attr("id") + "temp";
            if (typeof disablingMessage != "undefined") {
                $button.addClass("ui-button-disabled").addClass("ui-state-disabled").addClass("sf-disabled").attr("disabled", "disabled").attr("title", disablingMessage);
                $button.unbind('click').bind('click', function (e) {
                    e.preventDefault();
                    return false;
                });
            } else {
                var self = this;
                $button.removeClass("ui-button-disabled").removeClass("ui-state-disabled").removeClass("sf-disabled").prop("disabled", null).attr("title", "");
                $button.unbind('click').bind('click', enableCallback);
            }
        };

        SearchControl.prototype.quickFilter = function (value, tokenName) {
            var tableFilters = $(this.pf("tblFilters tbody"));
            if (tableFilters.length === 0) {
                return;
            }

            var params = {
                "value": value,
                "webQueryName": this.options.webQueryName,
                "tokenName": tokenName,
                "prefix": this.options.prefix,
                "index": this.newFilterRowIndex()
            };

            var self = this;
            $.ajax({
                url: SF.Urls.quickFilter,
                data: params,
                async: false,
                success: function (filterHtml) {
                    var $filterList = self.element.find(".sf-filters-list");
                    $filterList.find(".sf-explanation").hide();
                    $filterList.find("table").show();

                    tableFilters.append(filterHtml);
                    SF.triggerNewContent($(self.pf("tblFilters tbody tr:last")));
                }
            });
        };

        SearchControl.prototype.quickFilterCell = function ($elem) {
            var value = $elem.data("value");
            if (typeof value == "undefined") {
                value = $elem.html().trim();
            }

            var cellIndex = $elem[0].cellIndex;
            var tokenName = $($($elem.closest(".sf-search-results")).find("th")[cellIndex]).children("input:hidden").val();

            this.quickFilter(value, tokenName);
        };

        SearchControl.prototype.quickFilterHeader = function ($elem) {
            this.quickFilter("", $elem.find("input:hidden").val());
        };

        SearchControl.prototype.create = function (viewOptions) {
            var self = this;
            var type = this.getEntityType(function (type) {
                self.typedCreate($.extend({
                    type: type
                }, viewOptions || {}));
            });
        };

        SearchControl.prototype.getEntityType = function (_onTypeFound) {
            var typeStr = $(this.pf(SF.Keys.entityTypeNames)).val();
            var types = typeStr.split(",");
            if (types.length == 1) {
                return _onTypeFound(types[0]);
            }
            SF.openTypeChooser(this.options.prefix, _onTypeFound, { types: typeStr });
        };

        SearchControl.prototype.typedCreate = function (viewOptions) {
            viewOptions.prefix = viewOptions.prefix || this.options.prefix;
            if (SF.isEmpty(viewOptions.prefix)) {
                var fullViewOptions = this.viewOptionsForSearchCreate(viewOptions);
                new SF.ViewNavigator(fullViewOptions).navigate();
            } else {
                var fullViewOptions = this.viewOptionsForSearchPopupCreate(viewOptions);
                new SF.ViewNavigator(fullViewOptions).createSave();
            }
        };

        SearchControl.prototype.viewOptionsForSearchCreate = function (viewOptions) {
            return $.extend({
                controllerUrl: SF.Urls.create
            }, viewOptions);
        };

        SearchControl.prototype.viewOptionsForSearchPopupCreate = function (viewOptions) {
            return $.extend({
                controllerUrl: SF.Urls.popupNavigate,
                requestExtraJsonData: this.requestDataForSearchPopupCreate()
            }, viewOptions);
        };

        SearchControl.prototype.requestDataForSearchPopupCreate = function () {
            return {
                filters: this.serializeFilters(),
                webQueryName: this.options.webQueryName
            };
        };

        SearchControl.prototype.toggleSelectAll = function () {
            var select = $(this.pf("cbSelectAll:checked"));
            $(this.pf("sfSearchControl .sf-td-selection")).prop('checked', (select.length > 0) ? true : false);
        };

        SearchControl.prototype.searchOnLoad = function () {
            var btnSearchId = SF.compose(this.options.prefix, "qbSearch");
            var $button = $("#" + btnSearchId);
            var self = this;
            var makeSearch = function () {
                if (!self.searchOnLoadFinished) {
                    $button.click();
                    self.searchOnLoadFinished = true;
                }
            };

            var $tabContainer = $button.closest(".sf-tabs");
            if ($tabContainer.length == 0) {
                makeSearch();
            } else {
                var self = this;
                $tabContainer.bind("tabsactivate", function (evt, ui) {
                    if ($(ui.newPanel).find(self.element).length > 0) {
                        makeSearch();
                    }
                });
            }
        };
        return SearchControl;
    })();
    SF.SearchControl = SearchControl;
})(SF || (SF = {}));
//# sourceMappingURL=SF_FindNavigator.js.map
