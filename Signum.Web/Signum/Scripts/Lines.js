/// <reference path="globals.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", "Entities", "Validator", "Navigator", "Finder"], function(require, exports, Entities, Validator, Navigator, Finder) {
    var EntityBase = (function () {
        function EntityBase(element, _options) {
            this.element = element;
            this.element.data("SF-control", this);
            this.options = $.extend({
                prefix: "",
                partialViewName: ""
            }, _options);

            this._create();

            this.element.trigger("SF-ready");
        }
        EntityBase.prototype._create = function () {
            var _this = this;
            var $txt = $(this.pf(Entities.Keys.toStr) + ".sf-entity-autocomplete");
            if ($txt.length > 0) {
                var data = $txt.data();

                this.autoCompleter = new AjaxEntityAutoCompleter(SF.Urls.autocomplete, function (term) {
                    return ({ types: _this.staticInfo().types(), l: 5, q: term });
                });

                this.setupAutocomplete($txt);
            }
        };

        EntityBase.prototype.runtimeInfo = function (itemPrefix) {
            return new Entities.RuntimeInfoElement(this.options.prefix);
        };

        EntityBase.prototype.staticInfo = function () {
            return new Entities.StaticInfo(this.options.prefix);
        };

        EntityBase.prototype.pf = function (s) {
            return "#" + SF.compose(this.options.prefix, s);
        };

        EntityBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = this.pf(EntityBase.key_entity);
            if ($(containerDivId).length == 0)
                this.runtimeInfo().getElem().after(SF.hiddenDiv(containerDivId, ""));

            return $(containerDivId);
        };

        EntityBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = this.runtimeInfo().value();

            if (runtimeInfo == null)
                return null;

            var div = this.containerDiv();

            var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo, null, null);

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityBase.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            //virtual function
        };

        EntityBase.prototype.setEntity = function (entityValue, itemPrefix) {
            this.setEntitySpecific(entityValue);

            if (entityValue) {
                entityValue.assertPrefixAndType(this.options.prefix, this.staticInfo());
            }

            SF.triggerNewContent(this.containerDiv().html(entityValue == null ? null : entityValue.html));
            this.runtimeInfo().setValue(entityValue == null ? null : entityValue.runtimeInfo);

            if (entityValue == null) {
                Validator.cleanError($(this.pf(Entities.Keys.toStr)).val(""));
                Validator.cleanError($(this.pf(Entities.Keys.link)).val("").html(""));
            }

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityBase.prototype.remove_click = function () {
            var _this = this;
            return this.onRemove(this.options.prefix).then(function (result) {
                if (result)
                    _this.setEntity(null);
            });
        };

        EntityBase.prototype.onRemove = function (prefix) {
            if (this.removing != null)
                return this.removing(prefix);

            return Promise.resolve(true);
        };

        EntityBase.prototype.create_click = function () {
            var _this = this;
            return this.onCreating(this.options.prefix).then(function (result) {
                if (result)
                    _this.setEntity(result);
            });
        };

        EntityBase.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            Navigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new Entities.EntityHtml(_this.options.prefix, new Entities.RuntimeInfoValue(type, null));

                var template = _this.getEmbeddedTemplate();
                if (!SF.isEmpty(template))
                    newEntity.html = $(template);

                return Navigator.viewPopup(newEntity, _this.defaultViewOptions());
            });
        };

        EntityBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            return window[SF.compose(this.options.prefix, "sfTemplate")];
        };

        EntityBase.prototype.view_click = function () {
            var _this = this;
            var entityHtml = this.extractEntityHtml();

            return this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result);
                else
                    _this.setEntity(entityHtml); //previous entity passed by reference
            });
        };

        EntityBase.prototype.onViewing = function (entityHtml) {
            if (this.viewing != null)
                return this.viewing(entityHtml);

            return Navigator.viewPopup(entityHtml, this.defaultViewOptions());
        };

        EntityBase.prototype.find_click = function () {
            var _this = this;
            return this.onFinding(this.options.prefix).then(function (result) {
                if (result)
                    _this.setEntity(result);
            });
        };

        EntityBase.prototype.onFinding = function (prefix) {
            if (this.finding != null)
                return this.finding(this.options.prefix);

            return Navigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                return Finder.find({
                    webQueryName: type,
                    prefix: prefix
                });
            });
        };

        EntityBase.prototype.defaultViewOptions = function () {
            return {
                readOnly: this.staticInfo().isReadOnly(),
                partialViewName: this.options.partialViewName
            };
        };

        EntityBase.prototype.updateButtonsDisplay = function () {
            var hasEntity = !!this.runtimeInfo().value;

            $(this.pf("btnCreate")).toggle(!hasEntity);
            $(this.pf("btnFind")).toggle(!hasEntity);
            $(this.pf("btnRemove")).toggle(hasEntity);
            $(this.pf("btnView")).toggle(hasEntity);
            $(this.pf(Entities.Keys.link)).toggle(hasEntity);
            $(this.pf(Entities.Keys.toStr)).toggle(!hasEntity);
        };

        EntityBase.prototype.setupAutocomplete = function ($txt) {
            var _this = this;
            var auto = $txt.autocomplete({
                delay: 200,
                source: function (request, response) {
                    _this.autoCompleter.getResults(request.term).then(function (entities) {
                        response(entities.map(function (e) {
                            return ({ label: e.toStr, value: e });
                        }));
                    });
                },
                focus: function (event, ui) {
                    $txt.val(ui.item.value.text);
                    return false;
                },
                select: function (event, ui) {
                    _this.onAutocompleteSelected(ui.item.value);
                    _this.setEntity(ui.item.value);
                    event.preventDefault();
                }
            });

            auto.data("uiAutocomplete")._renderItem = function (ul, item) {
                var val = item.value;

                return $("<li>").attr("data-type", val.runtimeInfo.type).attr("data-id", val.runtimeInfo.id).append($("<a>").text(item.label)).appendTo(ul);
            };
        };

        EntityBase.prototype.onAutocompleteSelected = function (entityValue) {
            throw new Error("onAutocompleteSelected is abstract");
        };
        EntityBase.key_entity = "sfEntity";
        return EntityBase;
    })();
    exports.EntityBase = EntityBase;

    var AjaxEntityAutoCompleter = (function () {
        function AjaxEntityAutoCompleter(controllerUrl, getData) {
            this.controllerUrl = controllerUrl;
            this.getData = getData;
        }
        AjaxEntityAutoCompleter.prototype.getResults = function (term) {
            var _this = this;
            if (this.lastXhr)
                this.lastXhr.abort();

            return new Promise(function (resolve, failure) {
                _this.lastXhr = $.ajax({
                    url: _this.controllerUrl,
                    data: _this.getData(term),
                    success: function (data) {
                        this.lastXhr = null;
                        resolve(data.map(function (item) {
                            return new Entities.EntityValue(new Entities.RuntimeInfoValue(item.type, parseInt(item.id)), item.toStr, item.link);
                        }));
                    }
                });
            });
        };
        return AjaxEntityAutoCompleter;
    })();
    exports.AjaxEntityAutoCompleter = AjaxEntityAutoCompleter;

    once("SF-entityLine", function () {
        return $.fn.entityLine = function (opt) {
            return new EntityLine(this, opt);
        };
    });

    var EntityLine = (function (_super) {
        __extends(EntityLine, _super);
        function EntityLine() {
            _super.apply(this, arguments);
        }
        EntityLine.prototype.setEntitySpecific = function (entityValue) {
            var link = $(this.pf(Entities.Keys.link));
            link.text(entityValue == null ? null : entityValue.toStr);
            if (link.filter('a').length !== 0)
                link.attr('href', entityValue == null ? null : entityValue.link);
            $(this.pf(Entities.Keys.toStr)).val('');
        };

        EntityLine.prototype.onAutocompleteSelected = function (entityValue) {
            this.setEntity(entityValue);
        };
        return EntityLine;
    })(EntityBase);
    exports.EntityLine = EntityLine;

    once("SF-entityCombo", function () {
        return $.fn.entityCombo = function (opt) {
            return new EntityCombo(this, opt);
        };
    });

    var EntityCombo = (function (_super) {
        __extends(EntityCombo, _super);
        function EntityCombo() {
            _super.apply(this, arguments);
        }
        EntityCombo.prototype.combo = function () {
            return $(this.pf(EntityCombo.key_combo));
        };

        EntityCombo.prototype.setEntitySpecific = function (entityValue) {
            var c = this.combo();

            if (entityValue == null)
                c.val(null);
            else {
                var o = c.children("option[value=" + entityValue.runtimeInfo.key() + "]");
                if (o.length > 1)
                    o.html(entityValue.toStr);
                else
                    c.add($("<option value='{0}'/>".format(entityValue.runtimeInfo.key())).text(entityValue.toStr));

                c.val(entityValue.runtimeInfo.key());
            }
        };

        EntityCombo.prototype.combo_selected = function () {
            var val = this.combo().val();

            var ri = Entities.RuntimeInfoValue.fromKey(val);

            this.setEntity(ri == null ? null : new Entities.EntityValue(ri));
        };
        EntityCombo.key_combo = "sfCombo";
        return EntityCombo;
    })(EntityBase);
    exports.EntityCombo = EntityCombo;

    once("SF-entityLineDetail", function () {
        return $.fn.entityLineDetail = function (opt) {
            return new EntityLineDetail(this, opt);
        };
    });

    var EntityLineDetail = (function (_super) {
        __extends(EntityLineDetail, _super);
        function EntityLineDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityLineDetail.prototype.containerDiv = function (itemPrefix) {
            return $("#" + this.options.detailDiv);
        };

        EntityLineDetail.prototype.setEntitySpecific = function (entityValue) {
            if (entityValue == null)
                return;

            if (!entityValue.isLoaded())
                throw new Error("EntityLineDetail requires a loaded Entities.EntityHtml, consider calling Navigator.loadPartialView");
        };

        EntityLineDetail.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            Navigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                var newEntity = new Entities.EntityHtml(_this.options.prefix, new Entities.RuntimeInfoValue(type, null));

                var template = _this.getEmbeddedTemplate();
                if (!SF.isEmpty(template)) {
                    newEntity.html = $(template);
                    return Promise.resolve(newEntity);
                }

                return Navigator.requestPartialView(newEntity, _this.defaultViewOptions());
            });
        };

        EntityLineDetail.prototype.onFinding = function (prefix) {
            var _this = this;
            return _super.prototype.onFinding.call(this, prefix).then(function (entity) {
                if (entity == null)
                    return null;

                if (entity.html != null)
                    return Promise.resolve(entity);

                return Navigator.requestPartialView(new Entities.EntityHtml(_this.options.prefix, entity.runtimeInfo), _this.defaultViewOptions());
            });
        };
        return EntityLineDetail;
    })(EntityBase);
    exports.EntityLineDetail = EntityLineDetail;

    var EntityListBase = (function (_super) {
        __extends(EntityListBase, _super);
        function EntityListBase(element, options) {
            _super.call(this, element, options);
        }
        EntityListBase.prototype.runtimeInfo = function (itemPrefix) {
            return new Entities.RuntimeInfoElement(itemPrefix);
        };

        EntityListBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = SF.compose(itemPrefix, EntityList.key_entity);
            if ($(containerDivId).length == 0)
                this.runtimeInfo(itemPrefix).getElem().after(SF.hiddenDiv(containerDivId, ""));

            return $(containerDivId);
        };

        EntityListBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            var template = _super.prototype.getEmbeddedTemplate.call(this);
            if (SF.isEmpty(template))
                return template;

            template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix);
            return template;
        };

        EntityListBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = this.runtimeInfo(itemPrefix).value();

            var div = this.containerDiv(itemPrefix);

            var result = new Entities.EntityHtml(itemPrefix, runtimeInfo, null, null);

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityListBase.prototype.setEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.setEntitySpecific(entityValue);

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html(entityValue.html));

            this.runtimeInfo(itemPrefix).setValue(entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            //virtual
        };

        EntityListBase.prototype.addEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.addEntitySpecific(entityValue, itemPrefix);

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html(entityValue.html));
            this.runtimeInfo(itemPrefix).setValue(entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.removeEntitySpecific = function (prefix) {
            //virtual
        };

        EntityListBase.prototype.removeEntity = function (prefix) {
            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.itemSuffix = function () {
            throw new Error("itemSuffix is abstract");
        };

        EntityListBase.prototype.getItems = function () {
            throw new Error("getItems is abstract");
        };

        EntityListBase.prototype.getNextPrefix = function () {
            var _this = this;
            var lastIndex = Math.max.apply(null, this.getItems().toArray().map(function (e) {
                return parseInt(e.id.after(_this.options.prefix + "_").before("_" + _this.itemSuffix()));
            }));

            return SF.compose(this.options.prefix, lastIndex + 1);
        };

        EntityListBase.prototype.getLastPosIndex = function () {
            var $last = this.getItems().filter(":last");
            if ($last.length == 0) {
                return -1;
            }

            var lastId = $last[0].id;
            var lastPrefix = lastId.substring(0, lastId.indexOf(this.itemSuffix()) - 1);

            return this.getPosIndex(lastPrefix);
        };

        EntityListBase.prototype.getNextPosIndex = function () {
            return ";" + (this.getLastPosIndex() + 1).toString();
        };

        EntityListBase.prototype.canAddItems = function () {
            return SF.isEmpty(this.options.maxElements) || this.getItems().length < this.options.maxElements;
        };

        EntityListBase.prototype.find_click = function () {
            var _this = this;
            return this.onFindingMany(this.options.prefix).then(function (result) {
                if (result)
                    result.forEach(function (ev) {
                        return _this.addEntity(ev, _this.getNextPrefix());
                    });
            });
        };

        EntityListBase.prototype.onFinding = function (prefix) {
            throw new Error("onFinding is deprecated in EntityListBase");
        };

        EntityListBase.prototype.onFindingMany = function (prefix) {
            if (this.findingMany != null)
                return this.findingMany(this.options.prefix);

            return Navigator.typeChooser(this.staticInfo()).then(function (type) {
                if (type == null)
                    return null;

                return Finder.findMany({
                    webQueryName: type,
                    prefix: prefix
                });
            });
        };

        EntityListBase.prototype.moveUp = function (itemPrefix) {
            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(itemPrefix, suffix));
            var $itemPrev = $item.prev();

            if ($itemPrev.length == 0) {
                return;
            }

            var itemPrevPrefix = $itemPrev[0].id.before("_" + suffix);

            var prevNewIndex = this.getPosIndex(itemPrevPrefix);
            this.setPosIndex(itemPrefix, prevNewIndex);
            this.setPosIndex(itemPrevPrefix, prevNewIndex + 1);

            $item.insertBefore($itemPrev);
        };

        EntityListBase.prototype.moveDown = function (itemPrefix) {
            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(itemPrefix, suffix));
            var $itemNext = $item.next();

            if ($itemNext.length == 0) {
                return;
            }

            var itemNextPrefix = $itemNext[0].id.before("_" + suffix);

            var nextNewIndex = this.getPosIndex(itemNextPrefix);
            this.setPosIndex(itemPrefix, nextNewIndex);
            this.setPosIndex(itemNextPrefix, nextNewIndex - 1);

            $item.insertAfter($itemNext);
        };

        EntityListBase.prototype.getPosIndex = function (itemPrefix) {
            return parseInt($("#" + SF.compose(itemPrefix, EntityListBase.key_indexes)).val().after(";"));
        };

        EntityListBase.prototype.setPosIndex = function (itemPrefix, newIndex) {
            var $indexes = $("#" + SF.compose(itemPrefix, EntityListBase.key_indexes));
            $indexes.val($indexes.val().before(";") + ";" + newIndex.toString());
        };
        EntityListBase.key_indexes = "sfIndexes";
        return EntityListBase;
    })(EntityBase);
    exports.EntityListBase = EntityListBase;

    once("SF-entityList", function () {
        return $.fn.entityList = function (opt) {
            return new EntityList(this, opt);
        };
    });

    var EntityList = (function (_super) {
        __extends(EntityList, _super);
        function EntityList() {
            _super.apply(this, arguments);
        }
        EntityList.prototype.itemSuffix = function () {
            return Entities.Keys.toStr;
        };

        EntityList.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            $('#' + SF.compose(itemPrefix, Entities.Keys.toStr)).html(newToStr);
        };

        EntityList.prototype.selectedItemPrefix = function () {
            var $items = this.getItems().filter(":selected");
            ;
            if ($items.length == 0) {
                return null;
            }

            var nameSelected = $items[0].id;
            return nameSelected.before("_" + this.itemSuffix());
        };

        EntityList.prototype.getItems = function () {
            return $(this.pf(EntityList.key_list) + " > option");
        };

        EntityList.prototype.view_click = function () {
            var _this = this;
            var selectedItemPrefix = this.selectedItemPrefix();

            var entityHtml = this.extractEntityHtml(selectedItemPrefix);

            return this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result, selectedItemPrefix);
                else
                    _this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
            });
        };

        EntityList.prototype.create_click = function () {
            var _this = this;
            var prefix = this.getNextPrefix();
            return this.onCreating(prefix).then(function (entity) {
                if (entity)
                    _this.addEntity(entity, prefix);
            });
        };

        EntityList.prototype.updateButtonsDisplay = function () {
            var hasElements = this.getItems().length > 0;
            $(this.pf("btnRemove")).toggle(hasElements);
            $(this.pf("btnView")).toggle(hasElements);
            $(this.pf("btnUp")).toggle(hasElements);
            $(this.pf("btnDown")).toggle(hasElements);

            var canAdd = this.canAddItems();

            $(this.pf("btnCreate")).toggle(canAdd);
            $(this.pf("btnFind")).toggle(canAdd);
        };

        EntityList.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var $table = $("#" + this.options.prefix + "> .sf-field-list > .sf-field-list-table");

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), this.getNextPosIndex()));

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));

            $table.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

            var select = $(this.pf(EntityList.key_list));
            select.append("\n<option id='" + SF.compose(itemPrefix, Entities.Keys.toStr) + "' name='" + SF.compose(itemPrefix, Entities.Keys.toStr) + "' value='' class='sf-value-line'>" + entityValue.toStr + "</option>");
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
            select.children('option:last').attr('selected', true);
        };

        EntityList.prototype.remove_click = function () {
            var _this = this;
            var selectedItemPrefix = this.selectedItemPrefix();
            return this.onRemove(selectedItemPrefix).then(function (result) {
                if (result)
                    _this.removeEntity(selectedItemPrefix);
            });
        };

        EntityList.prototype.removeEntitySpecific = function (prefix) {
            $("#" + SF.compose(prefix, Entities.Keys.runtimeInfo)).remove();
            $("#" + SF.compose(prefix, Entities.Keys.toStr)).remove();
            $("#" + SF.compose(prefix, EntityList.key_entity)).remove();
            $("#" + SF.compose(prefix, EntityList.key_indexes)).remove();
        };

        EntityList.prototype.moveUp_click = function () {
            this.moveUp(this.selectedItemPrefix());
        };

        EntityList.prototype.moveDown_click = function () {
            this.moveDown(this.selectedItemPrefix());
        };
        EntityList.key_list = "sfList";
        return EntityList;
    })(EntityListBase);
    exports.EntityList = EntityList;

    once("SF-entityListDetail", function () {
        return $.fn.entityListDetail = function (opt) {
            return new EntityListDetail(this, opt);
        };
    });

    var EntityListDetail = (function (_super) {
        __extends(EntityListDetail, _super);
        function EntityListDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityListDetail.prototype.selection_Changed = function () {
            this.stageCurrentSelected();
        };

        EntityListDetail.prototype.stageCurrentSelected = function () {
            var selPrefix = this.selectedItemPrefix();

            var detailDiv = $("#" + this.options.detailDiv);

            var children = detailDiv.children();

            if (children.length != 0) {
                var prefix = children[0].id.before("_" + EntityListDetail.key_entity);
                if (selPrefix == prefix) {
                    children.show();
                    return;
                }
                children.hide();
                this.runtimeInfo(prefix).$elem.after(children);
            }

            var selContainer = this.containerDiv(selPrefix);

            if (selContainer.children().length == 0) {
                detailDiv.append(selContainer);
                selContainer.show();
            } else {
                var entity = new Entities.EntityHtml(selPrefix, this.runtimeInfo(selPrefix).value(), null, null);

                Navigator.requestPartialView(entity, this.defaultViewOptions()).then(function (e) {
                    selContainer.html(e.html);
                    detailDiv.append(selContainer);
                });
            }
        };
        return EntityListDetail;
    })(EntityList);
    exports.EntityListDetail = EntityListDetail;

    once("SF-entityRepeater", function () {
        return $.fn.entityRepeater = function (opt) {
            return new EntityRepeater(this, opt);
        };
    });

    var EntityRepeater = (function (_super) {
        __extends(EntityRepeater, _super);
        function EntityRepeater() {
            _super.apply(this, arguments);
        }
        EntityRepeater.prototype.itemSuffix = function () {
            return EntityRepeater.key_repeaterItem;
        };

        EntityRepeater.prototype.getItems = function () {
            return $(this.pf(EntityRepeater.key_itemsContainer) + " > ." + EntityRepeater.key_repeaterItemClass);
        };

        EntityRepeater.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var $div = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" + "<legend>" + (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.moveUp + "</span>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.moveDown + "</span>") : "") + "</legend>" + SF.hiddenInput(SF.compose(itemPrefix, EntityListBase.key_indexes), this.getNextPosIndex()) + SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) + "<div id='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' class='sf-line-entity'>" + "</div>" + "</fieldset>");

            $(this.pf(EntityRepeater.key_itemsContainer)).append($div);
        };

        EntityRepeater.prototype._getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityRepeater.prototype._getRemoving = function (itemPrefix) {
            return this._getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
        };

        EntityRepeater.prototype._getMovingUp = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        };

        EntityRepeater.prototype._getMovingDown = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        };

        EntityRepeater.prototype.remove_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityRepeater.prototype.removeItem_click = function (itemPrefix) {
            var _this = this;
            return this.onRemove(itemPrefix).then(function (result) {
                if (result)
                    _this.removeEntity(itemPrefix);
            });
        };

        EntityRepeater.prototype.updateButtonsDisplay = function () {
            var canAdd = this.canAddItems();

            $(this.pf("btnCreate")).toggle(canAdd);
            $(this.pf("btnFind")).toggle(canAdd);
        };
        EntityRepeater.key_itemsContainer = "sfItemsContainer";
        EntityRepeater.key_repeaterItem = "sfRepeaterItem";
        EntityRepeater.key_repeaterItemClass = "sf-repeater-element";
        EntityRepeater.key_link = "sfLink";
        return EntityRepeater;
    })(EntityListBase);
    exports.EntityRepeater = EntityRepeater;

    once("SF-entityStrip", function () {
        return $.fn.entityStrip = function (opt) {
            return new EntityStrip(this, opt);
        };
    });

    var EntityStrip = (function (_super) {
        __extends(EntityStrip, _super);
        function EntityStrip(element, options) {
            _super.call(this, element, options);
        }
        EntityStrip.prototype.itemSuffix = function () {
            return EntityStrip.key_stripItem;
        };

        EntityStrip.prototype.getItems = function () {
            return $(this.pf(EntityStrip.key_itemsContainer) + " > ." + EntityStrip.key_stripItemClass);
        };

        EntityStrip.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            $('#' + SF.compose(itemPrefix, Entities.Keys.link)).html(entityValue.toStr);
        };

        EntityStrip.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var $li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" + SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), this.getNextPosIndex()) + SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) + (this.options.navigate ? ("<a class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "' href='" + entityValue.link + "' title='" + lang.signum.navigate + "'>" + entityValue.toStr + "</a>") : ("<span class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "'>" + entityValue.toStr + "</span>")) + "<span class='sf-button-container'>" + ((this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "w" : "n") + "' data-text='false'>" + lang.signum.moveUp + "</span>") : "") + (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "e" : "s") + "' data-text='false'>" + lang.signum.moveDown + "</span>") : "") + (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this._getView(itemPrefix) + "\" class='sf-line-button sf-view' data-icon='ui-icon-circle-arrow-e' data-text='false'>" + lang.signum.view + "</a>") : "") + (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "")) + "</span>" + "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" + "</li>");

            $(this.pf(EntityStrip.key_itemsContainer) + " ." + EntityStrip.key_input).before($li);
        };

        EntityStrip.prototype._getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityStrip.prototype._getRemoving = function (itemPrefix) {
            return this._getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
        };

        EntityStrip.prototype._getView = function (itemPrefix) {
            return this._getRepeaterCall() + ".view('" + itemPrefix + "');";
        };

        EntityStrip.prototype._getMovingUp = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        };

        EntityStrip.prototype._getMovingDown = function (itemPrefix) {
            return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        };

        EntityStrip.prototype.remove_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityStrip.prototype.removeItem_click = function (itemPrefix) {
            var _this = this;
            return this.onRemove(itemPrefix).then(function (result) {
                if (result)
                    _this.removeEntity(itemPrefix);
            });
        };

        EntityStrip.prototype.view_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityStrip.prototype.viewItem_click = function (itemPrefix) {
            var _this = this;
            var entityHtml = this.extractEntityHtml(itemPrefix);

            return this.onViewing(entityHtml).then(function (result) {
                if (result)
                    _this.setEntity(result, itemPrefix);
                else
                    _this.setEntity(entityHtml, itemPrefix); //previous entity passed by reference
            });
        };

        EntityStrip.prototype.updateButtonsDisplay = function () {
            var canAdd = this.canAddItems();

            $(this.pf("btnCreate")).toggle(canAdd);
            $(this.pf("btnFind")).toggle(canAdd);
            $(this.pf("sfToStr")).toggle(canAdd);
        };

        EntityStrip.prototype.onAutocompleteSelected = function (entityValue) {
            this.addEntity(entityValue, this.getNextPrefix());
        };
        EntityStrip.key_itemsContainer = "sfItemsContainer";
        EntityStrip.key_stripItem = "sfStripItem";
        EntityStrip.key_stripItemClass = "sf-strip-element";
        EntityStrip.key_link = "sfLink";
        EntityStrip.key_input = "sf-strip-input";
        return EntityStrip;
    })(EntityList);
    exports.EntityStrip = EntityStrip;
});
//# sourceMappingURL=Lines.js.map
