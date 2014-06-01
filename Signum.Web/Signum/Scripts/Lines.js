/// <reference path="globals.ts"/>
var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
define(["require", "exports", "Framework/Signum.Web/Signum/Scripts/Entities", "Framework/Signum.Web/Signum/Scripts/Validator", "Framework/Signum.Web/Signum/Scripts/Navigator", "Framework/Signum.Web/Signum/Scripts/Finder"], function(require, exports, Entities, Validator, Navigator, Finder) {
    var EntityBase = (function () {
        function EntityBase(element, options) {
            this.element = element;
            this.element.data("SF-control", this);
            this.options = options;
            this.prefix = this.options.prefix;
            this.hidden = this.prefix.child("hidden").tryGet();
            this.inputGroup = this.prefix.child("inputGroup").tryGet();
            this.shownButton = this.prefix.child("shownButton").tryGet();

            var temp = this.prefix.child(Entities.Keys.template).tryGet();

            if (temp.length > 0) {
                this.options.template = temp.html().replaceAll("<scriptX", "<script").replaceAll("</scriptX", "</script");
                this.options.templateToString = temp.attr("data-toString");
            }

            this.fixInputGroup();

            this._create();
        }
        EntityBase.prototype.ready = function () {
            this.element.SFControlFullfill(this);
        };

        EntityBase.prototype._create = function () {
        };

        EntityBase.prototype.runtimeInfoHiddenElement = function (itemPrefix) {
            return this.prefix.child(Entities.Keys.runtimeInfo).get();
        };

        EntityBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = this.prefix.child(EntityBase.key_entity);
            var result = containerDivId.tryGet();
            if (result.length)
                return result;

            return SF.hiddenDiv(containerDivId, "").insertAfter(this.runtimeInfoHiddenElement());
        };

        EntityBase.prototype.getRuntimeInfo = function () {
            return Entities.RuntimeInfo.getFromPrefix(this.options.prefix);
        };

        EntityBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

            if (runtimeInfo == null)
                return null;

            var div = this.containerDiv();

            var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo, this.getToString(), this.getLink());

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityBase.prototype.getLink = function (itemPrefix) {
            return null;
        };

        EntityBase.prototype.getToString = function (itemPrefix) {
            return null;
        };

        EntityBase.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            //virtual function
        };

        EntityBase.prototype.setEntity = function (entityValue, itemPrefix) {
            this.setEntitySpecific(entityValue);

            if (entityValue)
                entityValue.assertPrefixAndType(this.options.prefix, this.options.types);

            this.containerDiv().html(entityValue == null ? null : entityValue.html);
            Entities.RuntimeInfo.setFromPrefix(this.options.prefix, entityValue == null ? null : entityValue.runtimeInfo);
            if (entityValue == null) {
                Validator.cleanHasError(this.element);
            }

            this.updateButtonsDisplay();
            this.notifyChanges(true);
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityBase.prototype.notifyChanges = function (setHasChanges) {
            if (setHasChanges)
                SF.setHasChanges(this.element);

            this.element.attr("changes", (parseInt(this.element.attr("changes")) || 0) + 1);
        };

        EntityBase.prototype.remove_click = function () {
            var _this = this;
            return this.onRemove(this.options.prefix).then(function (result) {
                if (result) {
                    _this.setEntity(null);
                    return _this.options.prefix;
                }

                return null;
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
                if (result) {
                    _this.setEntity(result);
                    return _this.options.prefix;
                }

                _this.notifyChanges(false);
                return null;
            });
        };

        EntityBase.prototype.typeChooser = function (filter) {
            return Navigator.typeChooser(this.options.prefix, this.options.types.filter(filter));
        };

        EntityBase.prototype.singleType = function () {
            if (this.options.types.length != 1)
                throw new Error("There are {0} types in {1}".format(this.options.types.length, this.options.prefix));

            return this.options.types[0].name;
        };

        EntityBase.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            return this.typeChooser(function (ti) {
                return ti.creable;
            }).then(function (type) {
                if (!type)
                    return null;

                return type.preConstruct().then(function (extra) {
                    if (!extra)
                        return null;

                    var newEntity = _this.options.template ? _this.getEmbeddedTemplate(prefix) : new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                    return Navigator.viewPopup(newEntity, _this.defaultViewOptions(extra));
                });
            });
        };

        EntityBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            if (!this.options.template)
                throw new Error("no template in " + this.options.prefix);

            var result = new Entities.EntityHtml(this.options.prefix, new Entities.RuntimeInfo(this.singleType(), null, true), this.options.templateToString);

            result.loadHtml(this.options.template);

            return result;
        };

        EntityBase.prototype.view_click = function () {
            var _this = this;
            var entityHtml = this.extractEntityHtml();

            return this.onViewing(entityHtml).then(function (result) {
                if (result) {
                    _this.setEntity(result);
                    return _this.options.prefix;
                } else {
                    _this.setEntity(entityHtml); //previous entity passed by reference
                    return null;
                }
            });
        };

        EntityBase.prototype.onViewing = function (entityHtml) {
            if (this.viewing != null)
                return this.viewing(entityHtml);

            return Navigator.viewPopup(entityHtml, this.defaultViewOptions(null));
        };

        EntityBase.prototype.find_click = function () {
            var _this = this;
            return this.onFinding(this.options.prefix).then(function (result) {
                if (result) {
                    _this.setEntity(result);
                    return _this.options.prefix;
                }

                _this.notifyChanges(false);
                return null;
            });
        };

        EntityBase.prototype.onFinding = function (prefix) {
            if (this.finding != null)
                return this.finding(prefix);

            return this.typeChooser(function (ti) {
                return ti.findable;
            }).then(function (type) {
                if (!type)
                    return null;

                return Finder.find({
                    webQueryName: type.name,
                    prefix: prefix
                });
            });
        };

        EntityBase.prototype.defaultViewOptions = function (extraJsonData) {
            return {
                readOnly: this.options.isReadonly,
                partialViewName: this.options.partialViewName,
                validationOptions: {
                    rootType: this.options.rootType,
                    propertyRoute: this.options.propertyRoute
                },
                requestExtraJsonData: extraJsonData
            };
        };

        EntityBase.prototype.updateButtonsDisplay = function () {
            var hasEntity = !!Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

            this.visibleButton("btnCreate", !hasEntity);
            this.visibleButton("btnFind", !hasEntity);
            this.visibleButton("btnView", hasEntity);
            this.visibleButton("btnRemove", hasEntity);

            this.fixInputGroup();
        };

        EntityBase.prototype.fixInputGroup = function () {
            this.inputGroup.toggleClass("input-group", !!this.shownButton.children().length);
        };

        EntityBase.prototype.visibleButton = function (sufix, visible) {
            var element = this.prefix.child(sufix).tryGet();

            if (!element.length)
                return;

            (visible ? this.shownButton : this.hidden).append(element.detach());
        };

        EntityBase.prototype.setupAutocomplete = function ($txt) {
            var _this = this;
            var handler;
            var auto = $txt.typeahead({
                source: function (query, response) {
                    if (handler)
                        clearTimeout(handler);

                    handler = setTimeout(function () {
                        _this.autoCompleter.getResults(query).then(function (entities) {
                            return response(entities);
                        });
                    }, 300);
                },
                sorter: function (items) {
                    return items;
                },
                matcher: function (item) {
                    return true;
                },
                highlighter: function (item) {
                    return $("<div>").append($("<span>").attr("data-type", item.runtimeInfo.type).attr("data-id", item.runtimeInfo.id).text(item.toStr)).html();
                },
                updater: function (val) {
                    return _this.onAutocompleteSelected(val);
                }
            });
        };

        EntityBase.prototype.onAutocompleteSelected = function (entityValue) {
            throw new Error("onAutocompleteSelected is abstract");
        };
        EntityBase.key_entity = "sfEntity";
        return EntityBase;
    })();
    exports.EntityBase = EntityBase;

    var AjaxEntityAutocompleter = (function () {
        function AjaxEntityAutocompleter(controllerUrl, getData) {
            this.controllerUrl = controllerUrl;
            this.getData = getData;
        }
        AjaxEntityAutocompleter.prototype.getResults = function (term) {
            var _this = this;
            if (this.lastXhr)
                this.lastXhr.abort();

            return new Promise(function (resolve, failure) {
                _this.lastXhr = $.ajax({
                    url: _this.controllerUrl,
                    data: _this.getData(term),
                    success: function (data) {
                        this.lastXhr = null;
                        var entities = data.map(function (item) {
                            return new Entities.EntityValue(new Entities.RuntimeInfo(item.type, item.id, false), item.text, item.link);
                        });
                        resolve(entities);
                    }
                });
            });
        };
        return AjaxEntityAutocompleter;
    })();
    exports.AjaxEntityAutocompleter = AjaxEntityAutocompleter;

    var EntityLine = (function (_super) {
        __extends(EntityLine, _super);
        function EntityLine() {
            _super.apply(this, arguments);
        }
        EntityLine.prototype._create = function () {
            var _this = this;
            var $txt = this.prefix.child(Entities.Keys.toStr).tryGet().filter(".sf-entity-autocomplete");
            if ($txt.length) {
                this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete, function (term) {
                    return ({ types: _this.options.types.map(function (t) {
                            return t.name;
                        }).join(","), l: 5, q: term });
                });

                this.setupAutocomplete($txt);
            }
        };

        EntityLine.prototype.getLink = function (itemPrefix) {
            return this.prefix.child(Entities.Keys.link).get().attr("href");
        };

        EntityLine.prototype.getToString = function (itemPrefix) {
            return this.prefix.child(Entities.Keys.link).get().text();
        };

        EntityLine.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            var link = this.prefix.child(Entities.Keys.link).get();
            link.text(entityValue == null ? null : entityValue.toStr);
            if (link.is('a'))
                link.attr('href', entityValue == null ? null : entityValue.link);
            this.prefix.child(Entities.Keys.toStr).get().val('');

            this.visible(this.prefix.child(Entities.Keys.link).tryGet(), entityValue != null);
            this.visible(this.prefix.get().find("ul.typeahead.dropdown-menu"), entityValue == null);
            this.visible(this.prefix.child(Entities.Keys.toStr).tryGet(), entityValue == null);
        };

        EntityLine.prototype.visible = function (element, visible) {
            if (!element.length)
                return;

            if (visible)
                this.shownButton.before(element.detach());
            else
                this.hidden.append(element.detach());
        };

        EntityLine.prototype.onAutocompleteSelected = function (entityValue) {
            this.setEntity(entityValue);
        };
        return EntityLine;
    })(EntityBase);
    exports.EntityLine = EntityLine;

    var EntityCombo = (function (_super) {
        __extends(EntityCombo, _super);
        function EntityCombo() {
            _super.apply(this, arguments);
        }
        EntityCombo.prototype.combo = function () {
            return this.prefix.child(EntityCombo.key_combo).get();
        };

        EntityCombo.prototype.setEntitySpecific = function (entityValue) {
            var c = this.combo();

            if (entityValue == null)
                c.val(null);
            else {
                var o = c.children("option[value='" + entityValue.runtimeInfo.key() + "']");
                if (o.length == 1)
                    o.html(entityValue.toStr);
                else
                    c.add($("<option value='{0}'/>".format(entityValue.runtimeInfo.key())).text(entityValue.toStr));

                c.val(entityValue.runtimeInfo.key());
            }
        };

        EntityCombo.prototype.getToString = function (itemPrefix) {
            return this.combo().children("option[value='" + this.combo().val() + "']").text();
        };

        EntityCombo.prototype.combo_selected = function () {
            var val = this.combo().val();

            var ri = Entities.RuntimeInfo.fromKey(val);

            this.setEntity(ri == null ? null : new Entities.EntityValue(ri, this.getToString()));
        };
        EntityCombo.key_combo = "sfCombo";
        return EntityCombo;
    })(EntityBase);
    exports.EntityCombo = EntityCombo;

    var EntityLineDetail = (function (_super) {
        __extends(EntityLineDetail, _super);
        function EntityLineDetail(element, options) {
            _super.call(this, element, options);
        }
        EntityLineDetail.prototype.containerDiv = function (itemPrefix) {
            return this.prefix.child("sfDetail").get();
        };

        EntityLineDetail.prototype.fixInputGroup = function () {
        };

        EntityLineDetail.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            if (entityValue == null)
                return;

            if (!entityValue.isLoaded())
                throw new Error("EntityLineDetail requires a loaded Entities.EntityHtml, consider calling Navigator.loadPartialView");
        };

        EntityLineDetail.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            if (this.options.template)
                return Promise.resolve(this.getEmbeddedTemplate(prefix));

            return this.typeChooser(function (t) {
                return t.creable;
            }).then(function (type) {
                if (!type)
                    return null;

                return type.preConstruct().then(function (args) {
                    if (!args)
                        return null;

                    var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                    return Navigator.requestPartialView(newEntity, _this.defaultViewOptions(args));
                });
            });
        };

        EntityLineDetail.prototype.find_click = function () {
            var _this = this;
            return this.onFinding(this.options.prefix).then(function (result) {
                if (result == null) {
                    _this.notifyChanges(false);
                    return null;
                }

                if (result.isLoaded())
                    return Promise.resolve(result);

                return Navigator.requestPartialView(new Entities.EntityHtml(_this.options.prefix, result.runtimeInfo), _this.defaultViewOptions(null));
            }).then(function (result) {
                if (result) {
                    _this.setEntity(result);
                    return _this.options.prefix;
                }

                return null;
            });
        };
        return EntityLineDetail;
    })(EntityBase);
    exports.EntityLineDetail = EntityLineDetail;

    var EntityListBase = (function (_super) {
        __extends(EntityListBase, _super);
        function EntityListBase(element, options) {
            _super.call(this, element, options);
            this.reservedPrefixes = [];
        }
        EntityListBase.prototype.runtimeInfo = function (itemPrefix) {
            return itemPrefix.child(Entities.Keys.runtimeInfo).get();
        };

        EntityListBase.prototype.containerDiv = function (itemPrefix) {
            var containerDivId = itemPrefix.child(EntityList.key_entity);

            var result = containerDivId.tryGet();

            if (result.length)
                return result;

            return SF.hiddenDiv(containerDivId, "").insertAfter(this.runtimeInfo(itemPrefix));
        };

        EntityListBase.prototype.getEmbeddedTemplate = function (itemPrefix) {
            if (!this.options.template)
                throw new Error("no template in " + this.options.prefix);

            var result = new Entities.EntityHtml(itemPrefix, new Entities.RuntimeInfo(this.singleType(), null, true), this.options.templateToString);

            var replaced = this.options.template.replace(new RegExp(this.options.prefix.child("0"), "gi"), itemPrefix);

            result.loadHtml(replaced);

            return result;
        };

        EntityListBase.prototype.extractEntityHtml = function (itemPrefix) {
            var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(itemPrefix);

            var div = this.containerDiv(itemPrefix);

            var result = new Entities.EntityHtml(itemPrefix, runtimeInfo, this.getToString(itemPrefix), this.getLink(itemPrefix));

            result.html = div.children();

            div.html(null);

            return result;
        };

        EntityListBase.prototype.setEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.setEntitySpecific(entityValue, itemPrefix);

            entityValue.assertPrefixAndType(itemPrefix, this.options.types);

            if (entityValue.isLoaded())
                this.containerDiv(itemPrefix).html(entityValue.html);

            Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            this.notifyChanges(true);
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.create_click = function () {
            var _this = this;
            var itemPrefix = this.reserveNextPrefix();
            return this.onCreating(itemPrefix).then(function (entity) {
                if (entity) {
                    _this.addEntity(entity, itemPrefix);
                    return itemPrefix;
                }

                _this.notifyChanges(false);
                return null;
            }).then(function (prefix) {
                _this.freeReservedPrefix(itemPrefix);
                return prefix;
            }, function (error) {
                _this.freeReservedPrefix(itemPrefix);
                throw error;
                return "";
            });
        };

        EntityListBase.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            //virtual
        };

        EntityListBase.prototype.addEntity = function (entityValue, itemPrefix) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.addEntitySpecific(entityValue, itemPrefix);

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.options.types);

            if (entityValue.isLoaded())
                this.containerDiv(itemPrefix).html(entityValue.html);
            Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            this.notifyChanges(true);
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        };

        EntityListBase.prototype.removeEntitySpecific = function (itemPrefix) {
            //virtual
        };

        EntityListBase.prototype.removeEntity = function (itemPrefix) {
            this.removeEntitySpecific(itemPrefix);

            this.updateButtonsDisplay();
            this.notifyChanges(true);
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

        EntityListBase.prototype.getPrefixes = function () {
            var _this = this;
            return this.getItems().toArray().map(function (e) {
                return e.id.before("_" + _this.itemSuffix());
            });
        };

        EntityListBase.prototype.getRuntimeInfos = function () {
            return this.getPrefixes().map(function (p) {
                return Entities.RuntimeInfo.getFromPrefix(p);
            });
        };

        EntityListBase.prototype.reserveNextPrefix = function () {
            var _this = this;
            var currentPrefixes = this.getItems().toArray().map(function (e) {
                return e.id.before("_" + _this.itemSuffix());
            });

            for (var i = 0; ; i++) {
                var newPrefix = this.options.prefix + "_" + i;

                if (this.reservedPrefixes.indexOf(newPrefix) == -1 && currentPrefixes.indexOf(newPrefix) == -1) {
                    this.reservedPrefixes.push(newPrefix);

                    return newPrefix;
                }
            }
        };

        EntityListBase.prototype.freeReservedPrefix = function (itemPrefix) {
            var index = this.reservedPrefixes.indexOf(itemPrefix);
            if (index == -1)
                throw Error("itemPrefix not reserved: " + itemPrefix);

            return this.reservedPrefixes.splice(index, 1);
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
            var prefixes = [];

            return this.onFindingMany(this.options.prefix).then(function (result) {
                if (result) {
                    result.forEach(function (ev) {
                        var pr = _this.reserveNextPrefix();
                        prefixes.push(pr);
                        _this.addEntity(ev, pr);
                    });

                    return prefixes.join(",");
                }

                _this.notifyChanges(false);
                return null;
            }).then(function (prefix) {
                prefixes.forEach(_this.freeReservedPrefix);
                return prefix;
            }, function (error) {
                prefixes.forEach(_this.freeReservedPrefix);
                throw error;
                return "";
            });
        };

        EntityListBase.prototype.onFinding = function (prefix) {
            throw new Error("onFinding is deprecated in EntityListBase");
        };

        EntityListBase.prototype.onFindingMany = function (prefix) {
            if (this.findingMany != null)
                return this.findingMany(prefix);

            return this.typeChooser(function (t) {
                return t.findable;
            }).then(function (type) {
                if (type == null)
                    return null;

                return Finder.findMany({
                    webQueryName: type.name,
                    prefix: prefix
                });
            });
        };

        EntityListBase.prototype.moveUp = function (itemPrefix) {
            var suffix = this.itemSuffix();
            var $item = itemPrefix.child(suffix).get();
            var $itemPrev = $item.prev();

            if ($itemPrev.length == 0) {
                return;
            }

            var itemPrevPrefix = $itemPrev[0].id.parent(suffix);

            var prevNewIndex = this.getPosIndex(itemPrevPrefix);
            this.setPosIndex(itemPrefix, prevNewIndex);
            this.setPosIndex(itemPrevPrefix, prevNewIndex + 1);

            $item.insertBefore($itemPrev);

            this.notifyChanges(true);
        };

        EntityListBase.prototype.moveDown = function (itemPrefix) {
            var suffix = this.itemSuffix();
            var $item = itemPrefix.child(suffix).get();
            var $itemNext = $item.next();

            if ($itemNext.length == 0) {
                return;
            }

            var itemNextPrefix = $itemNext[0].id.parent(suffix);

            var nextNewIndex = this.getPosIndex(itemNextPrefix);
            this.setPosIndex(itemPrefix, nextNewIndex);
            this.setPosIndex(itemNextPrefix, nextNewIndex - 1);

            $item.insertAfter($itemNext);

            this.notifyChanges(true);
        };

        EntityListBase.prototype.getPosIndex = function (itemPrefix) {
            return parseInt(itemPrefix.child(EntityListBase.key_indexes).get().val().after(";"));
        };

        EntityListBase.prototype.setPosIndex = function (itemPrefix, newIndex) {
            var $indexes = itemPrefix.child(EntityListBase.key_indexes).get();
            $indexes.val($indexes.val().before(";") + ";" + newIndex.toString());
        };
        EntityListBase.key_indexes = "sfIndexes";
        return EntityListBase;
    })(EntityBase);
    exports.EntityListBase = EntityListBase;

    var EntityList = (function (_super) {
        __extends(EntityList, _super);
        function EntityList() {
            _super.apply(this, arguments);
        }
        EntityList.prototype._create = function () {
            var _this = this;
            var list = this.prefix.child(EntityList.key_list).get();

            list.change(function () {
                return _this.selection_Changed();
            });

            if (list.height() < this.shownButton.height())
                list.css("min-height", this.shownButton.height());

            this.selection_Changed();
        };

        EntityList.prototype.selection_Changed = function () {
            this.updateButtonsDisplay();
        };

        EntityList.prototype.itemSuffix = function () {
            return Entities.Keys.toStr;
        };

        EntityList.prototype.updateLinks = function (newToStr, newLink, itemPrefix) {
            itemPrefix.child(Entities.Keys.toStr).get().html(newToStr);
        };

        EntityList.prototype.selectedItemPrefix = function () {
            var $items = this.getItems().filter(":selected");
            if ($items.length == 0) {
                return null;
            }

            var nameSelected = $items[0].id;
            return nameSelected.before("_" + this.itemSuffix());
        };

        EntityList.prototype.getItems = function () {
            return this.prefix.child(EntityList.key_list).get().children("option");
        };

        EntityList.prototype.view_click = function () {
            var _this = this;
            var selectedItemPrefix = this.selectedItemPrefix();

            var entityHtml = this.extractEntityHtml(selectedItemPrefix);

            return this.onViewing(entityHtml).then(function (result) {
                if (result) {
                    _this.setEntity(result, selectedItemPrefix);
                    return selectedItemPrefix;
                } else {
                    _this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
                    return null;
                }
            });
        };

        EntityList.prototype.updateButtonsDisplay = function () {
            var canAdd = this.canAddItems();
            this.visibleButton("btnCreate", canAdd);
            this.visibleButton("btnFind", canAdd);

            var hasSelected = this.selectedItemPrefix() != null;
            this.visibleButton("btnView", hasSelected);
            this.visibleButton("btnRemove", hasSelected);
            this.visibleButton("btnUp", hasSelected);
            this.visibleButton("btnDown", hasSelected);

            this.fixInputGroup();
        };

        EntityList.prototype.getToString = function (itemPrefix) {
            return itemPrefix.child(Entities.Keys.toStr).get().text();
        };

        EntityList.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            itemPrefix.child(Entities.Keys.toStr).get().text(entityValue.toStr);
        };

        EntityList.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            this.inputGroup.before(SF.hiddenInput(itemPrefix.child(EntityList.key_indexes), this.getNextPosIndex()));
            this.inputGroup.before(SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));
            this.inputGroup.before(SF.hiddenDiv(itemPrefix.child(EntityList.key_entity), ""));

            var select = this.prefix.child(EntityList.key_list).get();
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select

            $("<option/>").attr("id", itemPrefix.child(Entities.Keys.toStr)).attr("value", "").attr('selected', true).text(entityValue.toStr).appendTo(select);
        };

        EntityList.prototype.remove_click = function () {
            var _this = this;
            var selectedItemPrefix = this.selectedItemPrefix();
            return this.onRemove(selectedItemPrefix).then(function (result) {
                if (result) {
                    var next = _this.getItems().filter(":selected").next();
                    if (next.length == 0)
                        next = _this.getItems().filter(":selected").prev();

                    _this.removeEntity(selectedItemPrefix);

                    next.attr("selected", "selected");
                    _this.selection_Changed();

                    return selectedItemPrefix;
                }

                return null;
            });
        };

        EntityList.prototype.removeEntitySpecific = function (itemPrefix) {
            itemPrefix.child(Entities.Keys.runtimeInfo).get().remove();
            itemPrefix.child(Entities.Keys.toStr).get().remove();
            itemPrefix.child(EntityList.key_entity).tryGet().remove();
            itemPrefix.child(EntityList.key_indexes).tryGet().remove();
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

    var EntityListDetail = (function (_super) {
        __extends(EntityListDetail, _super);
        function EntityListDetail() {
            _super.apply(this, arguments);
        }
        EntityListDetail.prototype.selection_Changed = function () {
            _super.prototype.selection_Changed.call(this);
            this.stageCurrentSelected();
        };

        EntityListDetail.prototype.remove_click = function () {
            var _this = this;
            return _super.prototype.remove_click.call(this).then(function (result) {
                _this.stageCurrentSelected();
                return result;
            });
        };

        EntityListDetail.prototype.create_click = function () {
            var _this = this;
            return _super.prototype.create_click.call(this).then(function (result) {
                _this.stageCurrentSelected();
                return result;
            });
        };

        EntityListDetail.prototype.find_click = function () {
            var _this = this;
            return _super.prototype.find_click.call(this).then(function (result) {
                _this.stageCurrentSelected();
                return result;
            });
        };

        EntityListDetail.prototype.stageCurrentSelected = function () {
            var _this = this;
            var selPrefix = this.selectedItemPrefix();

            var detailDiv = $("#" + this.options.detailDiv);

            var currentChildren = detailDiv.children();
            var currentPrefix = currentChildren.length ? currentChildren[0].id.parent(EntityListDetail.key_entity) : null;
            if (currentPrefix == selPrefix) {
                return;
            }

            var hideCurrent = function () {
                if (currentPrefix) {
                    currentChildren.hide();
                    _this.runtimeInfo(currentPrefix).after(currentChildren);
                }
            };

            if (selPrefix) {
                var selContainer = this.containerDiv(selPrefix);

                var promise = selContainer.children().length ? Promise.resolve(null) : Navigator.requestPartialView(new Entities.EntityHtml(selPrefix, Entities.RuntimeInfo.getFromPrefix(selPrefix), null, null)).then(function (e) {
                    return selContainer.html(e.html);
                });

                promise.then(function () {
                    detailDiv.append(selContainer);
                    selContainer.show();

                    if (currentPrefix) {
                        currentChildren.hide();
                        _this.runtimeInfo(currentPrefix).after(currentChildren);
                    }
                });
            }
            if (currentPrefix) {
                currentChildren.hide();
                this.runtimeInfo(currentPrefix).after(currentChildren);
            }
        };

        EntityListDetail.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            if (this.options.template)
                return Promise.resolve(this.getEmbeddedTemplate(prefix));

            return this.typeChooser(function (t) {
                return t.creable;
            }).then(function (type) {
                if (type == null)
                    return null;

                return type.preConstruct().then(function (args) {
                    if (!args)
                        return null;

                    var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                    return Navigator.requestPartialView(newEntity, _this.defaultViewOptions(args));
                });
            });
        };
        return EntityListDetail;
    })(EntityList);
    exports.EntityListDetail = EntityListDetail;

    var EntityRepeater = (function (_super) {
        __extends(EntityRepeater, _super);
        function EntityRepeater() {
            _super.apply(this, arguments);
        }
        EntityRepeater.prototype.itemSuffix = function () {
            return EntityRepeater.key_repeaterItem;
        };

        EntityRepeater.prototype.fixInputGroup = function () {
        };

        EntityRepeater.prototype.getItems = function () {
            return this.prefix.child(EntityRepeater.key_itemsContainer).get().children("." + EntityRepeater.key_repeaterItemClass);
        };

        EntityRepeater.prototype.removeEntitySpecific = function (itemPrefix) {
            itemPrefix.child(EntityRepeater.key_repeaterItem).get().remove();
        };

        EntityRepeater.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var fieldSet = $("<fieldset id='" + itemPrefix.child(EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" + "<legend><div class='item-group'>" + (this.options.remove ? ("<a id='" + itemPrefix.child("btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');" + "\" class='sf-line-button sf-remove'><span class='glyphicon glyphicon-remove'></span></a>") : "") + (this.options.reorder ? ("<a id='" + itemPrefix.child("btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-up'></span></span></a>") : "") + (this.options.reorder ? ("<a id='" + itemPrefix.child("btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-down'></span></span></a>") : "") + "</div></legend>" + SF.hiddenInput(itemPrefix.child(EntityListBase.key_indexes), this.getNextPosIndex()) + SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), null) + "<div id='" + itemPrefix.child(EntityRepeater.key_entity) + "' class='sf-line-entity'>" + "</div>" + "</fieldset>");

            this.options.prefix.child(EntityRepeater.key_itemsContainer).get().append(fieldSet);
        };

        EntityRepeater.prototype.getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityRepeater.prototype.remove_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityRepeater.prototype.removeItem_click = function (itemPrefix) {
            var _this = this;
            return this.onRemove(itemPrefix).then(function (result) {
                if (result) {
                    _this.removeEntity(itemPrefix);
                    return itemPrefix;
                }
                return null;
            });
        };

        EntityRepeater.prototype.onCreating = function (prefix) {
            var _this = this;
            if (this.creating != null)
                return this.creating(prefix);

            if (this.options.template)
                return Promise.resolve(this.getEmbeddedTemplate(prefix));

            return this.typeChooser(function (t) {
                return t.creable;
            }).then(function (type) {
                if (type == null)
                    return null;

                return type.preConstruct().then(function (args) {
                    if (!args)
                        return null;

                    var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                    return Navigator.requestPartialView(newEntity, _this.defaultViewOptions(args));
                });
            });
        };

        EntityRepeater.prototype.find_click = function () {
            var _this = this;
            return this.onFindingMany(this.options.prefix).then(function (result) {
                if (!result) {
                    _this.notifyChanges(false);
                    return;
                }

                return Promise.all(result.map(function (e) {
                    var itemPrefix = _this.reserveNextPrefix();

                    var promise = e.isLoaded() ? Promise.resolve(e) : Navigator.requestPartialView(new Entities.EntityHtml(itemPrefix, e.runtimeInfo), _this.defaultViewOptions(null));

                    return promise.then(function (ev) {
                        _this.addEntity(ev, itemPrefix);
                        _this.freeReservedPrefix(itemPrefix);
                        return itemPrefix;
                    }, function (error) {
                        return _this.freeReservedPrefix(itemPrefix);
                    });
                })).then(function (result) {
                    return result.join(",");
                });
            });
        };

        EntityRepeater.prototype.updateButtonsDisplay = function () {
            var canAdd = this.canAddItems();

            this.prefix.child("btnCreate").tryGet().toggle(canAdd);
            this.prefix.child("btnFind").tryGet().toggle(canAdd);
        };
        EntityRepeater.key_itemsContainer = "sfItemsContainer";
        EntityRepeater.key_repeaterItem = "sfRepeaterItem";
        EntityRepeater.key_repeaterItemClass = "sf-repeater-element";
        return EntityRepeater;
    })(EntityListBase);
    exports.EntityRepeater = EntityRepeater;

    var EntityTabRepeater = (function (_super) {
        __extends(EntityTabRepeater, _super);
        function EntityTabRepeater() {
            _super.apply(this, arguments);
        }
        EntityTabRepeater.prototype._create = function () {
            _super.prototype._create.call(this);
        };

        EntityTabRepeater.prototype.itemSuffix = function () {
            return EntityTabRepeater.key_repeaterItem;
        };

        EntityTabRepeater.prototype.getItems = function () {
            return this.prefix.child(EntityTabRepeater.key_itemsContainer).get().children("." + EntityTabRepeater.key_repeaterItemClass);
        };

        EntityTabRepeater.prototype.removeEntitySpecific = function (itemPrefix) {
            var li = itemPrefix.child(EntityTabRepeater.key_repeaterItem).get();

            if (li.next().length)
                li.next().find("a").tab("show");
            else if (li.prev().length)
                li.prev().find("a").tab("show");

            li.remove();
            itemPrefix.child(EntityBase.key_entity).get().remove();
        };

        EntityTabRepeater.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var header = $("<li id='" + itemPrefix.child(EntityTabRepeater.key_repeaterItem) + "' class='" + EntityTabRepeater.key_repeaterItemClass + "'>" + "<a data-toggle='tab' href='#" + itemPrefix.child(EntityBase.key_entity) + "' >" + "<span>" + entityValue.toStr + "</span>" + SF.hiddenInput(itemPrefix.child(EntityListBase.key_indexes), this.getNextPosIndex()) + SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), null) + (this.options.reorder ? ("<span id='" + itemPrefix.child("btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-left'></span></span>") : "") + (this.options.reorder ? ("<span id='" + itemPrefix.child("btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-right'></span></span>") : "") + (this.options.remove ? ("<span id='" + itemPrefix.child("btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');" + "\" class='sf-line-button sf-remove' ><span class='glyphicon glyphicon-remove'></span></span>") : "") + "</a>" + "</li>");

            this.prefix.child(EntityTabRepeater.key_itemsContainer).get().append(header);

            var entity = $("<div id='" + itemPrefix.child(EntityTabRepeater.key_entity) + "' class='tab-pane'>" + "</div>");

            this.prefix.child(EntityTabRepeater.key_tabsContainer).get().append(entity);

            header.find("a").tab("show");
        };

        EntityTabRepeater.prototype.getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };
        EntityTabRepeater.key_tabsContainer = "sfTabsContainer";
        return EntityTabRepeater;
    })(EntityRepeater);
    exports.EntityTabRepeater = EntityTabRepeater;

    var EntityStrip = (function (_super) {
        __extends(EntityStrip, _super);
        function EntityStrip(element, options) {
            _super.call(this, element, options);
        }
        EntityStrip.prototype.fixInputGroup = function () {
        };

        EntityStrip.prototype._create = function () {
            var _this = this;
            var $txt = this.prefix.child(Entities.Keys.toStr).get().filter(".sf-entity-autocomplete");
            if ($txt.length) {
                this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete, function (term) {
                    return ({ types: _this.options.types.map(function (t) {
                            return t.name;
                        }).join(","), l: 5, q: term });
                });

                this.setupAutocomplete($txt);
            }
        };

        EntityStrip.prototype.itemSuffix = function () {
            return EntityStrip.key_stripItem;
        };

        EntityStrip.prototype.getItems = function () {
            return this.prefix.child(EntityStrip.key_itemsContainer).get().children("." + EntityStrip.key_stripItemClass);
        };

        EntityStrip.prototype.setEntitySpecific = function (entityValue, itemPrefix) {
            var link = itemPrefix.child(Entities.Keys.link).get();
            link.text(entityValue.toStr);
            if (this.options.navigate)
                link.attr("href", entityValue.link);
        };

        EntityStrip.prototype.getLink = function (itemPrefix) {
            return itemPrefix.child(Entities.Keys.link).get().attr("hef");
        };

        EntityStrip.prototype.getToString = function (itemPrefix) {
            return itemPrefix.child(Entities.Keys.link).get().text();
        };

        EntityStrip.prototype.removeEntitySpecific = function (itemPrefix) {
            itemPrefix.child(EntityStrip.key_stripItem).get().remove();
        };

        EntityStrip.prototype.addEntitySpecific = function (entityValue, itemPrefix) {
            var li = $("<li id='" + itemPrefix.child(EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + " input-group'>" + (this.options.navigate ? ("<a class='sf-entitStrip-link' id='" + itemPrefix.child(Entities.Keys.link) + "' href='" + entityValue.link + "' title='" + lang.signum.navigate + "'>" + entityValue.toStr + "</a>") : ("<span class='sf-entitStrip-link' id='" + itemPrefix.child(Entities.Keys.link) + "'>" + entityValue.toStr + "</span>")) + SF.hiddenInput(itemPrefix.child(EntityStrip.key_indexes), this.getNextPosIndex()) + SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), null) + "<div id='" + itemPrefix.child(EntityStrip.key_entity) + "' style='display:none'></div>" + "<span>" + ((this.options.reorder ? ("<a id='" + itemPrefix.child("btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-" + (this.options.vertical ? "up" : "left") + "'></span></a>") : "") + (this.options.reorder ? ("<a id='" + itemPrefix.child("btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-" + (this.options.vertical ? "down" : "right") + "'></span></a>") : "") + (this.options.view ? ("<a id='" + itemPrefix.child("btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this.getRepeaterCall() + ".view_click('" + itemPrefix + "');" + "\" class='sf-line-button sf-view'><span class='glyphicon glyphicon-arrow-right'></span></a>") : "") + (this.options.remove ? ("<a id='" + itemPrefix.child("btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');" + "\" class='sf-line-button sf-remove'><span class='glyphicon glyphicon-remove'></span></a>") : "")) + "</span>" + "</li>");

            this.prefix.child(EntityStrip.key_itemsContainer).get().find(" ." + EntityStrip.key_input).before(li);
        };

        EntityStrip.prototype.getRepeaterCall = function () {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        };

        EntityStrip.prototype.remove_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityStrip.prototype.removeItem_click = function (itemPrefix) {
            var _this = this;
            return this.onRemove(itemPrefix).then(function (result) {
                if (result) {
                    _this.removeEntity(itemPrefix);
                    return itemPrefix;
                }

                return null;
            });
        };

        EntityStrip.prototype.view_click = function () {
            throw new Error("remove_click is deprecated in EntityRepeater");
        };

        EntityStrip.prototype.viewItem_click = function (itemPrefix) {
            var _this = this;
            var entityHtml = this.extractEntityHtml(itemPrefix);

            return this.onViewing(entityHtml).then(function (result) {
                if (result) {
                    _this.setEntity(result, itemPrefix);
                    return itemPrefix;
                } else {
                    _this.setEntity(entityHtml, itemPrefix); //previous entity passed by reference
                    return null;
                }
            });
        };

        EntityStrip.prototype.updateButtonsDisplay = function () {
            var canAdd = this.canAddItems();

            this.prefix.child("btnCreate").tryGet().toggle(canAdd);
            this.prefix.child("btnFind").tryGet().toggle(canAdd);
            this.prefix.child("sfToStr").tryGet().toggle(canAdd);
        };

        EntityStrip.prototype.onAutocompleteSelected = function (entityValue) {
            var prefix = this.reserveNextPrefix();
            this.addEntity(entityValue, prefix);
            this.prefix.child(Entities.Keys.toStr).get().val('');
            this.freeReservedPrefix(prefix);
        };
        EntityStrip.key_itemsContainer = "sfItemsContainer";
        EntityStrip.key_stripItem = "sfStripItem";
        EntityStrip.key_stripItemClass = "sf-strip-element";
        EntityStrip.key_input = "sf-strip-input";
        return EntityStrip;
    })(EntityList);
    exports.EntityStrip = EntityStrip;
});
//# sourceMappingURL=Lines.js.map
