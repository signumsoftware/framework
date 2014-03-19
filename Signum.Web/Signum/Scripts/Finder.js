/// <reference path="globals.ts"/>
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Navigator"], function(require, exports, Entities, Navigator) {
    (function (FilterOperation) {
        FilterOperation[FilterOperation["EqualTo"] = 0] = "EqualTo";
        FilterOperation[FilterOperation["DistinctTo"] = 1] = "DistinctTo";
        FilterOperation[FilterOperation["GreaterThan"] = 2] = "GreaterThan";
        FilterOperation[FilterOperation["GreaterThanOrEqual"] = 3] = "GreaterThanOrEqual";
        FilterOperation[FilterOperation["LessThan"] = 4] = "LessThan";
        FilterOperation[FilterOperation["LessThanOrEqual"] = 5] = "LessThanOrEqual";
        FilterOperation[FilterOperation["Contains"] = 6] = "Contains";
        FilterOperation[FilterOperation["StartsWith"] = 7] = "StartsWith";
        FilterOperation[FilterOperation["EndsWith"] = 8] = "EndsWith";
        FilterOperation[FilterOperation["Like"] = 9] = "Like";
        FilterOperation[FilterOperation["NotContains"] = 10] = "NotContains";
        FilterOperation[FilterOperation["NotStartsWith"] = 11] = "NotStartsWith";
        FilterOperation[FilterOperation["NotEndsWith"] = 12] = "NotEndsWith";
        FilterOperation[FilterOperation["NotLike"] = 13] = "NotLike";
        FilterOperation[FilterOperation["IsIn"] = 14] = "IsIn";
    })(exports.FilterOperation || (exports.FilterOperation = {}));
    var FilterOperation = exports.FilterOperation;

    (function (FilterMode) {
        FilterMode[FilterMode["Visible"] = 0] = "Visible";
        FilterMode[FilterMode["Hidden"] = 1] = "Hidden";
        FilterMode[FilterMode["AlwaysHidden"] = 2] = "AlwaysHidden";
        FilterMode[FilterMode["OnlyResults"] = 3] = "OnlyResults";
    })(exports.FilterMode || (exports.FilterMode = {}));
    var FilterMode = exports.FilterMode;

    (function (OrderType) {
        OrderType[OrderType["Ascending"] = 0] = "Ascending";
        OrderType[OrderType["Descending"] = 1] = "Descending";
    })(exports.OrderType || (exports.OrderType = {}));
    var OrderType = exports.OrderType;

    (function (ColumnOptionsMode) {
        ColumnOptionsMode[ColumnOptionsMode["Add"] = 0] = "Add";
        ColumnOptionsMode[ColumnOptionsMode["Remove"] = 1] = "Remove";
        ColumnOptionsMode[ColumnOptionsMode["Replace"] = 2] = "Replace";
    })(exports.ColumnOptionsMode || (exports.ColumnOptionsMode = {}));
    var ColumnOptionsMode = exports.ColumnOptionsMode;

    function getFor(prefix) {
        return $("#" + SF.compose(prefix, "sfSearchControl")).SFControl();
    }
    exports.getFor = getFor;

    function findMany(findOptions) {
        findOptions.allowSelection = true;
        findOptions.multipleSelection = true;
        return findInternal(findOptions);
    }
    exports.findMany = findMany;

    function find(findOptions) {
        findOptions.allowSelection = true;
        findOptions.multipleSelection = false;
        return findInternal(findOptions).then(function (array) {
            return array == null ? null : array[0];
        });
    }
    exports.find = find;

    (function (RequestType) {
        RequestType[RequestType["QueryRequest"] = 0] = "QueryRequest";
        RequestType[RequestType["FindOptions"] = 1] = "FindOptions";
        RequestType[RequestType["FullScreen"] = 2] = "FullScreen";
    })(exports.RequestType || (exports.RequestType = {}));
    var RequestType = exports.RequestType;

    function findInternal(findOptions) {
        return SF.ajaxPost({
            url: findOptions.openFinderUrl || SF.Urls.partialFind,
            data: exports.requestDataForOpenFinder(findOptions, false)
        }).then(function (modalDivHtml) {
            var modalDiv = $(modalDivHtml);

            var okButtonId = SF.compose(findOptions.prefix, "btnOk");

            var items;
            return Navigator.openModal(modalDiv, function (button) {
                if (button.id != okButtonId)
                    return Promise.resolve(true);

                return exports.getFor(findOptions.prefix).then(function (sc) {
                    items = sc.selectedItems();
                    if (items.length == 0) {
                        SF.Notify.info(lang.signum.noElementsSelected);
                        return false;
                    }

                    if (items.length > 1 && !findOptions.multipleSelection) {
                        SF.Notify.info(lang.signum.onlyOneElement);
                        return false;
                    }

                    return true;
                });
            }).then(function (pair) {
                return pair.button.id == okButtonId ? items : null;
            });
        });
    }

    function explore(findOptions) {
        return SF.ajaxPost({
            url: findOptions.openFinderUrl || SF.Urls.partialFind,
            data: exports.requestDataForOpenFinder(findOptions, true)
        }).then(function (modalDivHtml) {
            return Navigator.openModal($(modalDivHtml));
        }).then(function () {
            return null;
        });
    }
    exports.explore = explore;

    function requestDataForOpenFinder(findOptions, isExplore) {
        var requestData = {
            webQueryName: findOptions.webQueryName,
            elems: findOptions.elems,
            allowSelection: findOptions.allowSelection,
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
            requestData["filters"] = findOptions.filters.map(function (f) {
                return f.columnName + "," + FilterOperation[f.operation] + "," + SearchControl.encodeCSV(f.value);
            }).join(";"); //List of filter names "token1,operation1,value1;token2,operation2,value2"
        }
        if (findOptions.orders != null) {
            requestData["orders"] = serializeOrders(findOptions.orders);
        }
        if (findOptions.columns != null) {
            requestData["columns"] = findOptions.columns.map(function (c) {
                return c.columnName + "," + c.displayName;
            }).join(";"); //List of column names "token1,displayName1;token2,displayName2"
        }
        if (findOptions.columnMode != null) {
            requestData["columnMode"] = findOptions.columnMode;
        }

        requestData["isExplore"] = isExplore;

        return requestData;
    }
    exports.requestDataForOpenFinder = requestDataForOpenFinder;

    function serializeOrders(orders) {
        return orders.map(function (f) {
            return (f.orderType == 0 /* Ascending */ ? "" : "-") + f.columnName;
        }).join(";");
    }

    function deleteFilter(trId) {
        var $tr = $("tr#" + trId);
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
    exports.deleteFilter = deleteFilter;

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
                allowSelection: true,
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
                orders: [],
                prefix: "",
                searchOnLoad: false,
                webQueryName: null
            }, _options);

            this._create();
        }
        SearchControl.prototype.ready = function () {
            this.element.SFControlFullfill(this);
        };

        SearchControl.prototype.pf = function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        };

        SearchControl.prototype._create = function () {
            var _this = this;
            var self = this;

            this.filterBuilder = new FilterBuilder($(this.pf("tblFilterBuilder")), this.options.prefix, this.options.webQueryName, SF.Urls.addFilter);

            this.filterBuilder.addColumnClicked = function () {
                return _this.addColumn();
            };

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

            if (this.options.allowChangeColumns || (this.options.filterMode != 2 /* AlwaysHidden */ && this.options.filterMode != 3 /* OnlyResults */)) {
                $tblResults.on("contextmenu", "th:not(.sf-th-entity):not(.sf-th-selection)", function (e) {
                    self.headerContextMenu(e);
                    return false;
                });
            }

            if (this.options.allowChangeColumns) {
                $tblResults.on("click", ".sf-search-ctxitem.sf-remove-column > span", function () {
                    var $elem = $(this).closest("th");

                    self.removeColumn($elem);
                    return false;
                });

                $tblResults.on("click", ".sf-search-ctxitem.sf-edit-column > span", function () {
                    var $elem = $(this).closest("th");

                    self.editColumn($elem);
                    return false;
                });

                this.createMoveColumnDragDrop();
            }

            if (this.options.filterMode != 2 /* AlwaysHidden */ && this.options.filterMode != 3 /* OnlyResults */) {
                $tblResults.on("contextmenu", "td:not(.sf-td-no-results):not(.sf-td-multiply,.sf-search-footer-pagination)", function (e) {
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

            if (this.options.filterMode != 3 /* OnlyResults */) {
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

                this.element.find(this.pf("btnSelected")).click(function () {
                    self.ctxMenuInDropdown();
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
            $rowSelectors.closest("tr").toggleClass("active", select);

            var selected = this.element.find(".sf-td-selection:checked").length;

            this.element.find(this.pf("btnSelectedSpan")).text(selected);
            var btn = this.element.find(this.pf("btnSelected"));
            if (selected == 0)
                btn.attr("disabled", "disabled");
            else
                btn.removeAttr("disabled");
        };

        SearchControl.prototype.ctxMenuInDropdown = function () {
            var $dropdown = $(this.pf("btnSelectedDropDown"));

            if (!$dropdown.closest(".btn-group").hasClass("open")) {
                var loadingClass = "sf-tm-selected-loading";

                $dropdown.html($("<li></li>").addClass(loadingClass).html($("<span></span>").addClass("sf-query-button").html(lang.signum.loading)));

                $.ajax({
                    url: SF.Urls.selectedItemsContextMenu,
                    data: this.requestDataForContextMenu(),
                    success: function (items) {
                        $dropdown.html(items);
                    }
                });
            }
        };

        SearchControl.prototype.createCtxMenu = function (e) {
            var $cmenu = $("<ul class='dropdown-menu'></ul>");
            $cmenu.css({
                left: e.pageX,
                top: e.pageY,
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
            var $menu = this.createCtxMenu(e);

            var $itemContainer = $menu.find(".sf-search-ctxmenu");
            if (this.options.filterMode != 2 /* AlwaysHidden */ && this.options.filterMode != 3 /* OnlyResults */) {
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
            var $menu = this.createCtxMenu(e);

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
                implementationsKey: $(this.pf(Entities.Keys.entityTypeNames)).val()
            };
        };

        SearchControl.prototype.entityContextMenu = function (e) {
            var $td = $(e.target).closest("td");

            var $menu = this.createCtxMenu(e);
            var $itemContainer = $menu.find(".sf-search-ctxmenu");

            $.ajax({
                url: SF.Urls.selectedItemsContextMenu,
                data: this.requestDataForContextMenu(),
                success: function (items) {
                    $itemContainer.html(items);
                    $td.append($menu);
                }
            });

            return false;
        };

        SearchControl.prototype.fullScreen = function (evt) {
            var urlParams = this.requestDataForSearchInUrl();

            var url = this.element.attr("data-find-url") + "?" + urlParams;
            if (evt.ctrlKey || evt.which == 2) {
                window.open(url);
            } else if (evt.which == 1) {
                window.location.href = url;
            }
        };

        SearchControl.prototype.search = function () {
            var $searchButton = $(this.pf("qbSearch"));
            $searchButton.addClass("sf-searching");
            var count = parseInt($searchButton.attr("data-searchCount")) || 0;
            var self = this;
            $.ajax({
                url: SF.Urls.search,
                data: this.requestDataForSearch(0 /* QueryRequest */),
                success: function (r) {
                    var $tbody = self.element.find(".sf-search-results-container tbody");
                    if (!SF.isEmpty(r)) {
                        $tbody.html(r);
                    } else {
                        $tbody.html("");
                    }
                    $searchButton.removeClass("sf-searching");
                    $searchButton.attr("data-searchCount", count + 1);
                }
            });
        };

        SearchControl.prototype.requestDataForSearchInUrl = function () {
            var form = this.requestDataForSearch(2 /* FullScreen */);

            return $.param(form);
        };

        SearchControl.prototype.requestDataForSearch = function (type) {
            var requestData = {};
            if (type != 2 /* FullScreen */)
                requestData["webQueryName"] = this.options.webQueryName;

            requestData["pagination"] = $(this.pf(this.keys.pagination)).val();
            requestData["elems"] = $(this.pf(this.keys.elems)).val();
            requestData["page"] = ($(this.pf(this.keys.page)).val() || "1");
            requestData["allowSelection"] = this.options.allowSelection;
            requestData["navigate"] = this.options.navigate;
            requestData["filters"] = this.filterBuilder.serializeFilters();

            if (type != 2 /* FullScreen */)
                requestData["filterMode"] = this.options.filterMode;

            requestData["orders"] = this.serializeOrders();
            requestData["columns"] = this.serializeColumns();
            requestData["columnMode"] = 'Replace';

            requestData["prefix"] = this.options.prefix;
            return requestData;
        };

        SearchControl.encodeCSV = function (value) {
            if (!value)
                return "";

            var hasQuote = value.indexOf("\"") != -1;
            if (hasQuote || value.indexOf(",") != -1 || value.indexOf(";") != -1) {
                if (hasQuote)
                    value = value.replace(/"/g, "\"\"");
                return "\"" + value + "\"";
            }

            return value;
        };

        SearchControl.prototype.serializeOrders = function () {
            return serializeOrders(this.options.orders);
        };

        SearchControl.prototype.serializeColumns = function () {
            var self = this;
            return $(this.pf("tblResults thead tr th:not(.sf-th-entity):not(.sf-th-selection)")).toArray().map(function (th) {
                var $this = $(th);
                var token = $this.find("input:hidden").val();
                var displayName = $this.text().trim();
                if (token == displayName)
                    return token;
                else
                    return token + "," + displayName;
            }).join(";");
        };

        SearchControl.getSelectedItems = function (prefix) {
            return $("input:checkbox[name^=" + SF.compose(prefix, "rowSelection") + "]:checked").toArray().map(function (v) {
                var parts = v.value.split("__");
                return new Entities.EntityValue(new Entities.RuntimeInfo(parts[1], parseInt(parts[0]), false), parts[2], $(v).parent().next().children('a').attr('href'));
            });
        };

        SearchControl.liteKeys = function (values) {
            return values.map(function (v) {
                return v.runtimeInfo.key();
            }).join(",");
        };

        SearchControl.prototype.selectedItems = function () {
            return SearchControl.getSelectedItems(this.options.prefix);
        };

        SearchControl.prototype.selectedItemsLiteKeys = function () {
            return SearchControl.liteKeys(this.selectedItems());
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

        SearchControl.prototype.selectedKeys = function () {
            return this.selectedItems().map(function (item) {
                return item.runtimeInfo.key();
            }).join(',');
        };

        SearchControl.prototype.newSortOrder = function ($th, multiCol) {
            var columnName = $th.find("input:hidden").val();

            var cols = this.options.orders.filter(function (o) {
                return o.columnName == columnName;
            });
            var col = cols.length == 0 ? null : cols[0];

            var oposite = col == null ? 0 /* Ascending */ : col.orderType == 0 /* Ascending */ ? 1 /* Descending */ : 0 /* Ascending */;
            var $sort = $th.find("span.sf-header-sort");
            if (!multiCol) {
                this.element.find("span.sf-header-sort").removeClass("asc desc l0 l1 l2 l3");
                $sort.addClass(oposite == 0 /* Ascending */ ? "asc" : "desc");
                this.options.orders = [{ columnName: columnName, orderType: oposite }];
            } else {
                if (col !== null) {
                    col.orderType = oposite;
                    $sort.removeClass("asc desc").addClass(oposite == 0 /* Ascending */ ? "asc" : "desc");
                } else {
                    this.options.orders.push({ columnName: columnName, orderType: oposite });
                    $sort.addClass(oposite == 0 /* Ascending */ ? "asc" : "desc").addClass("l" + (this.options.orders.length - 1 % 4));
                }
            }
        };

        SearchControl.prototype.addColumn = function () {
            if (!this.options.allowChangeColumns || $(this.pf("tblFilters tbody")).length == 0) {
                throw "Adding columns is not allowed";
            }

            var tokenName = QueryTokenBuilder.constructTokenName(this.options.prefix);
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
            var colName = $th.find("span").text().trim();

            Navigator.valueLineBox({
                prefix: SF.compose(this.options.prefix, "newName"),
                title: lang.signum.editColumnName,
                message: lang.signum.enterTheNewColumnName,
                value: colName,
                type: 4 /* TextBox */
            }).then(function (result) {
                if (result)
                    $th.find("span").text(result);
            });
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
            //$draggables.draggable({
            //    revert: "invalid",
            //    axis: "x",
            //    opacity: 0.5,
            //    distance: 8,
            //    cursor: "move"
            //});
            //$draggables.removeAttr("style"); //remove relative positioning
            //var self = this;
            //$droppables.droppable({
            //    hoverClass: "sf-header-droppable-active",
            //    tolerance: "pointer",
            //    drop: function (event, ui) {
            //        var $dragged = ui.draggable;
            //        var $targetPlaceholder = $(this); //droppable
            //        var $targetCol = $targetPlaceholder.closest("th");
            //        self.moveColumn($dragged, $targetCol, $targetPlaceholder.hasClass("sf-header-droppable-left"));
            //    }
            //});
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
            $toggler.toggleClass('active');
            return false;
        };

        SearchControl.prototype.quickFilterCell = function ($elem) {
            var value = $elem.data("value");
            if (typeof value == "undefined")
                value = $elem.html().trim();

            var cellIndex = $elem[0].cellIndex;
            var tokenName = $($($elem.closest(".sf-search-results")).find("th")[cellIndex]).children("input:hidden").val();

            this.filterBuilder.addFilter(tokenName, value);
        };

        SearchControl.prototype.quickFilterHeader = function ($elem) {
            this.filterBuilder.addFilter($elem.find("input:hidden").val(), "");
        };

        SearchControl.prototype.create_click = function () {
            this.onCreate();
        };

        SearchControl.prototype.onCreate = function () {
            var _this = this;
            if (this.creating != null)
                this.creating();
            else
                this.getEntityType().then(function (type) {
                    if (type == null)
                        return;

                    var runtimeInfo = new Entities.RuntimeInfo(type, null, true);
                    if (SF.isEmpty(_this.options.prefix))
                        Navigator.navigate(runtimeInfo, false);

                    var requestData = _this.requestDataForSearchPopupCreate();

                    Navigator.navigatePopup(new Entities.EntityHtml(SF.compose(_this.options.prefix, "Temp"), runtimeInfo), { requestExtraJsonData: requestData });
                });
        };

        SearchControl.prototype.getEntityType = function () {
            var names = $(this.pf(Entities.Keys.entityTypeNames)).val().split(",");
            var niceNames = $(this.pf(Entities.Keys.entityTypeNiceNames)).val().split(",");

            var options = names.map(function (p, i) {
                return ({
                    type: p,
                    toStr: niceNames[i]
                });
            });
            if (options.length == 1) {
                return Promise.resolve(options[0].type);
            }
            return Navigator.chooser(this.options.prefix, lang.signum.chooseAType, options).then(function (o) {
                return o == null ? null : o.type;
            });
        };

        SearchControl.prototype.requestDataForSearchPopupCreate = function () {
            return {
                filters: this.filterBuilder.serializeFilters(),
                webQueryName: this.options.webQueryName
            };
        };

        SearchControl.prototype.toggleSelectAll = function () {
            var select = $(this.pf("cbSelectAll:checked"));
            this.changeRowSelection($(this.pf("sfSearchControl .sf-td-selection")), (select.length > 0) ? true : false);
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
            if ($tabContainer.length == 0 || this.element.is(":visible")) {
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
    exports.SearchControl = SearchControl;

    var FilterBuilder = (function () {
        function FilterBuilder(element, prefix, webQueryName, url) {
            var _this = this;
            this.element = element;
            this.prefix = prefix;
            this.webQueryName = webQueryName;
            this.url = url;
            this.element.on("sf-new-subtokens-combo", function (event) {
                var args = [];
                for (var _i = 0; _i < (arguments.length - 1); _i++) {
                    args[_i] = arguments[_i + 1];
                }
                _this.newSubTokensComboAdded($("#" + args[0]));
            });
        }
        FilterBuilder.prototype.pf = function (s) {
            return "#" + SF.compose(this.prefix, s);
        };

        FilterBuilder.prototype.newSubTokensComboAdded = function ($selectedCombo) {
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
                        self.addFilterClicked();
                    });
                    this.changeButtonState($btnAddColumn, $prevSelectedOption.attr("data-column"), function () {
                        self.addColumnClicked();
                    });
                }
                return;
            }

            this.changeButtonState($btnAddFilter, $selectedOption.attr("data-filter"), function () {
                self.addFilterClicked();
            });
            this.changeButtonState($btnAddColumn, $selectedOption.attr("data-column"), function () {
                self.addColumnClicked();
            });
        };

        FilterBuilder.prototype.changeButtonState = function ($button, disablingMessage, enableCallback) {
            if (!$button)
                return;

            var hiddenId = $button.attr("id") + "temp";
            if (typeof disablingMessage != "undefined") {
                $button.attr("disabled", "disabled").attr("title", disablingMessage);
                $button.unbind('click').bind('click', function (e) {
                    e.preventDefault();
                    return false;
                });
            } else {
                var self = this;
                $button.removeAttr("disabled").attr("title", "");
                $button.unbind('click').bind('click', enableCallback);
            }
        };

        FilterBuilder.prototype.addFilterClicked = function () {
            var tokenName = QueryTokenBuilder.constructTokenName(this.prefix);
            if (SF.isEmpty(tokenName)) {
                return;
            }

            this.addFilter(tokenName, null);
        };

        FilterBuilder.prototype.addFilter = function (tokenName, value) {
            var tableFilters = $(this.pf("tblFilters tbody"));
            if (tableFilters.length == 0) {
                throw "Adding filters is not allowed";
            }

            var data = {
                webQueryName: this.webQueryName,
                tokenName: tokenName,
                value: value,
                index: this.newFilterRowIndex(),
                prefix: this.prefix
            };

            var self = this;
            $.ajax({
                url: this.url,
                data: data,
                async: false,
                success: function (filterHtml) {
                    var $filterList = self.element.find(".sf-filters-list");
                    $filterList.find(".sf-explanation").hide();
                    $filterList.find("table").show();

                    tableFilters.append(filterHtml);
                }
            });
        };

        FilterBuilder.prototype.newFilterRowIndex = function () {
            var lastRow = $(this.pf("tblFilters tbody tr:last"));
            if (lastRow.length == 1) {
                return parseInt(lastRow[0].id.substr(lastRow[0].id.lastIndexOf("_") + 1, lastRow[0].id.length)) + 1;
            }
            return 0;
        };

        FilterBuilder.prototype.serializeFilters = function () {
            var _this = this;
            return $(this.pf("tblFilters > tbody > tr")).toArray().map(function (f) {
                var $filter = $(f);

                var id = $filter[0].id;
                var index = id.afterLast("_");

                var selector = $(SF.compose(_this.pf("ddlSelector"), index) + " option:selected", $filter);

                var value = _this.encodeValue($filter, index);

                return $filter.find("td:nth-child(2) > :hidden").val() + "," + selector.val() + "," + value;
            }).join(";");
        };

        FilterBuilder.prototype.encodeValue = function ($filter, index) {
            var valBool = $("input:checkbox[id=" + SF.compose(this.prefix, "value", index) + "]", $filter);
            if (valBool.length > 0)
                return valBool[0].checked;

            var infoElem = $("#" + SF.compose(this.prefix, "value", index, Entities.Keys.runtimeInfo));
            if (infoElem.length > 0) {
                var val = Entities.RuntimeInfo.parse(infoElem.val());
                return SearchControl.encodeCSV(val == null ? null : val.key());
            }

            return SearchControl.encodeCSV($(SF.compose(this.pf("value"), index), $filter).val());
        };
        return FilterBuilder;
    })();
    exports.FilterBuilder = FilterBuilder;

    (function (QueryTokenBuilder) {
        function init(containerId, webQueryName, controllerUrl, requestExtraJsonData) {
            $("#" + containerId).on("change", "select", function () {
                tokenChanged($(this), webQueryName, controllerUrl, requestExtraJsonData);
            });
        }
        QueryTokenBuilder.init = init;

        function tokenChanged($selectedCombo, webQueryName, controllerUrl, requestExtraJsonData) {
            var prefix = $selectedCombo.attr("id").before("ddlTokens_");
            if (prefix.endsWith("_"))
                prefix = prefix.substr(0, prefix.length - 1);

            var index = parseInt($selectedCombo.attr("id").after("ddlTokens_"));

            clearChildSubtokenCombos($selectedCombo, prefix, index);
            $selectedCombo.trigger("sf-new-subtokens-combo", $selectedCombo.attr("id"));

            var $selectedOption = $selectedCombo.children("option:selected");
            if ($selectedOption.val() == "") {
                return;
            }

            var tokenName = constructTokenName(prefix);

            var data = $.extend({
                webQueryName: webQueryName,
                tokenName: tokenName,
                index: index,
                prefix: prefix
            }, requestExtraJsonData);

            $.ajax({
                url: controllerUrl,
                data: data,
                dataType: "html",
                success: function (newHtml) {
                    $selectedCombo.parent().html(newHtml);
                }
            });
        }
        QueryTokenBuilder.tokenChanged = tokenChanged;
        ;

        function clearChildSubtokenCombos($selectedCombo, prefix, index) {
            $selectedCombo.next("select,input[type=hidden]").remove();
        }
        QueryTokenBuilder.clearChildSubtokenCombos = clearChildSubtokenCombos;

        function constructTokenName(prefix) {
            var tokenName = "";
            var stop = false;
            for (var i = 0; ; i++) {
                var currSubtoken = $("#" + SF.compose(prefix, "ddlTokens_" + i));
                if (currSubtoken.length == 0)
                    break;

                var part = currSubtoken.val();
                tokenName = !tokenName ? part : !part ? tokenName : (tokenName + "." + part);
            }
            return tokenName;
        }
        QueryTokenBuilder.constructTokenName = constructTokenName;
    })(exports.QueryTokenBuilder || (exports.QueryTokenBuilder = {}));
    var QueryTokenBuilder = exports.QueryTokenBuilder;
});
//# sourceMappingURL=Finder.js.map
