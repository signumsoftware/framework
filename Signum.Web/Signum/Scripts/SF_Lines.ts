/// <reference path="references.ts"/>

interface JQuery {
    SFControl<T>(): T;
}

module SF {

    once("SF-control", () => {
        jQuery.fn.SFControl = function () {
            return this.data("SF-control");
        };
    });


    export interface EntityBaseOptions {
        prefix: string;
        partialViewName: string;
        onEntityChanged: () => any
    }

    export interface EntityData {
        id?: string;
        runtimeInfo?: string;
        type: string;
        toStr: string;
        link?: string;
        key?: string;
    }

    export class EntityBase {
        options: EntityBaseOptions;
        element: JQuery;
        constructor(element: JQuery, _options: EntityBaseOptions) {
            this.element = element;
            this.element.data("SF-control", this);
            this.options = $.extend({
                prefix: "",
                partialViewName: "",
                onEntityChanged: null
            }, _options);

            this._create();

            this.element.trigger("SF-ready");
        }

        static key_entity = "sfEntity";


        _create() {
            var $txt = $(this.pf(SF.Keys.toStr) + ".sf-entity-autocomplete");
            if ($txt.length > 0) {
                var data = $txt.data();
                this.entityAutocomplete($txt, { delay: 200, types: data.types, url: data.url || SF.Urls.autocomplete, count: 5 });
            }
        }


        runtimeInfo() {
            return new SF.RuntimeInfo(this.options.prefix);
        }

        staticInfo() {
            return new SF.StaticInfo(this.options.prefix);
        }

        pf(s) {
            return "#" + SF.compose(this.options.prefix, s);
        }


        checkValidation(validatorOptions: PartialValidationOptions, itemPrefix?: string) {
            if (typeof validatorOptions == "undefined" || typeof validatorOptions.type == "undefined") {
                throw "validatorOptions.type must be supplied to checkValidation";
            }

            var info = this.runtimeInfo();
            $.extend(validatorOptions, {
                prefix: this.options.prefix,
                id: (info.find().length !== 0) ? info.id() : ''
            });
            var validator = new SF.PartialValidator(validatorOptions);
            var validatorResult = validator.validate();
            if (!validatorResult.isValid) {
                if (!confirm(lang.signum.popupErrors)) {
                    $.extend(validatorResult, { acceptChanges: false });
                    return validatorResult;
                }
                else {
                    validator.showErrors(validatorResult.modelState, true);
                }
            }
            this.updateLinks(validatorResult.newToStr, validatorResult.newLink);
            $.extend(validatorResult, { acceptChanges: true });
            return validatorResult;
        }

        updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
            //Abstract function
        }


        fireOnEntityChanged(hasEntity: boolean) {
            this.updateButtonsDisplay(hasEntity);
            if (!SF.isEmpty(this.options.onEntityChanged)) {
                this.options.onEntityChanged();
            }
        }

        remove(itemPrefix?: string) {
            $(this.pf(SF.Keys.toStr)).val("").removeClass(SF.Validator.inputErrorClass);
            $(this.pf(SF.Keys.link)).val("").html("").removeClass(SF.Validator.inputErrorClass);
            this.runtimeInfo().removeEntity();

            this.removeSpecific();
            this.fireOnEntityChanged(false);
        }

        removeSpecific() {
            throw new Error("removeSpecific is abstract");
        }

        getEntityType(_onTypeFound) {
            var types = this.staticInfo().types().split(",");
            if (types.length == 1) {
                return _onTypeFound(types[0]);
            }

            SF.openTypeChooser(this.options.prefix, _onTypeFound);
        }

        create(_viewOptions: ViewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedCreate($.extend({ type: type }, _viewOptions));
            });
        }

        typedCreate(_viewOptions: ViewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityBase typedCreate. Call create instead";
            }
            if (_viewOptions.navigate) {
                window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + _viewOptions.type, "_blank");
                return;
            }
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) {
                new SF.ViewNavigator(viewOptions).showCreateOk(template);
            }
            else {
                new SF.ViewNavigator(viewOptions).createOk();
            }
        }


        viewOptionsForCreating(viewOptions: ViewOptions): ViewOptions {
            throw new Error("viewOptionsForCreating is abstract");
        }

        viewOptionsForViewing(_viewOptions: ViewOptions, itemPrefix?: string): ViewOptions {
            throw new Error("viewOptionsForViewing is abstract");
        }

        onFindingOk(selectedItems: Array<EntityData>, _viewOptions?: ViewOptions) {
            throw new Error("onFindingOk is abstract");
        }

        getEmbeddedTemplate(viewOptions?: ViewOptions) {
            return window[SF.compose(this.options.prefix, "sfTemplate")];
        }

        find(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions));
            });
        }

        typedFind(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in EBaseline typedFind. Call find instead";
            }
            var findOptions = this.createFindOptions(_findOptions);
            SF.FindNavigator.openFinder(findOptions);
        }

        createFindOptions(findOptions: FindOptions, _viewOptions?: ViewOptions): FindOptions {
            throw new Error("removeSpecific is abstract");
        }

        extraJsonParams(itemPrefix?: string) {
            var extraParams: any = {};

            var staticInfo = this.staticInfo();

            //If Embedded Entity => send propertyInfo
            if (staticInfo.isEmbedded()) {
                extraParams.rootType = staticInfo.rootType();
                extraParams.propertyRoute = staticInfo.propertyRoute();
            }

            if (staticInfo.isReadOnly()) {
                extraParams.readOnly = true;
            }

            return extraParams;
        }

        updateButtonsDisplay(hasEntity: boolean) {
            var btnCreate = $(this.pf("btnCreate"));
            var btnRemove = $(this.pf("btnRemove"));
            var btnFind = $(this.pf("btnFind"));
            var btnView = $(this.pf("btnView"));
            var link = $(this.pf(SF.Keys.link));
            var txt = $(this.pf(SF.Keys.toStr));

            if (hasEntity == true) {
                if (link.html() == "")
                    link.html("&nbsp;");
                if (link.length > 0) {
                    txt.hide();
                    link.show();
                }
                else
                    txt.show();
                btnCreate.hide(); btnFind.hide();
                btnRemove.show(); btnView.show();
            }
            else {
                if (link.length > 0) {
                    link.hide();
                    txt.show();
                }
                else
                    txt.hide();
                btnRemove.hide(); btnView.hide();
                btnCreate.show(); btnFind.show();
            }
        }


        entityAutocomplete($elem, options) {
            var lastXhr; //To avoid previous requests results to be shown
            var self = this;
            var auto = $elem.autocomplete({
                delay: options.delay || 200,

                source: function (request, response) {
                    if (lastXhr)
                        lastXhr.abort();
                    lastXhr = $.ajax({
                        url: options.url,
                        data: { types: options.types, l: options.count || 5, q: request.term },
                        success: function (data) {
                            lastXhr = null;
                            response($.map(data, function (item) {
                                    return {
                                    label: item.text,
                                    value: item
                                }
                                }));
                        }
                    });
                },
                focus: function (event, ui) {
                    $elem.val(ui.item.value.text);
                    return false;
                },
                select: function (event, ui) {
                    var controlId = $elem.attr("id");
                    var prefix = controlId.substr(0, controlId.indexOf(SF.Keys.toStr) - 1);
                    self.onAutocompleteSelected(controlId, ui.item.value);
                    event.preventDefault();
                },
            });

            auto.data("uiAutocomplete")._renderItem = function (ul, item) {
                return $("<li>")
                    .attr("data-type", item.value.type)
                    .attr("data-id", item.value.id)
                    .append($("<a>").text(item.label))
                    .appendTo(ul);
            };
        }

        onAutocompleteSelected(controlId: string, data: any) {
            throw new Error("onAutocompleteSelected is abstract");
        }
    }

    once("SF-entityLine", () =>
        $.fn.entityLine = function (opt: EntityBaseOptions) {
            new EntityLine(this, opt);
        });

    export class EntityLine extends EntityBase {

        updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
            var link = $(this.pf(SF.Keys.link));
            link.html(newToStr);
            if (link.filter('a').length !== 0)
                link.attr('href', newLink);
            $(this.pf(SF.Keys.toStr)).val('');
        }

        view(_viewOptions?: ViewOptions) {
            var viewOptions = this.viewOptionsForViewing(_viewOptions);
            new SF.ViewNavigator(viewOptions).viewOk();
        }

        viewOptionsForViewing(_viewOptions: ViewOptions, itemPrefix?: string): ViewOptions {
            var self = this;
            var info = this.runtimeInfo();
            return $.extend({
                containerDiv: SF.compose(this.options.prefix, EntityBase.key_entity),
                onOk: function () { return self.onViewingOk(_viewOptions.validationOptions); },
                onOkClosed: function () { self.fireOnEntityChanged(true); },
                type: info.entityType(),
                id: info.id(),
                prefix: this.options.prefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        }

        onViewingOk(validatorOptions: ValidationOptions) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: this.runtimeInfo().entityType()
            });
            return this.checkValidation(valOptions).acceptChanges;
        }

        viewOptionsForCreating(_viewOptions: ViewOptions): ViewOptions {
            var self = this;
            return $.extend({
                onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type); },
                onOkClosed: function () { self.fireOnEntityChanged(true); },
                prefix: this.options.prefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        }

        newEntity(clonedElements: JQuery, item: EntityData) {
            var info = this.runtimeInfo();
            if (typeof item.runtimeInfo != "undefined") {
                info.find().val(item.runtimeInfo);
            }
            else {
                info.setEntity(item.type, item.id || '');
            }

            if ($(this.pf(EntityBase.key_entity)).length == 0) {
                info.find().after(SF.hiddenDiv(SF.compose(this.options.prefix, EntityBase.key_entity), ""));
            }
            $(this.pf(EntityBase.key_entity)).append(clonedElements);

            this.updateLinks(item.toStr, item.link);
        }

        onCreatingOk(clonedElements: JQuery, validatorOptions: ValidationOptions, entityType: string, itemPrefix?: string) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: entityType
            });
            var validatorResult = this.checkValidation(valOptions);
            if (validatorResult.acceptChanges) {
                var runtimeInfo;
                var $mainControl = $(".sf-main-control[data-prefix=" + this.options.prefix + "]");
                if ($mainControl.length > 0) {
                    runtimeInfo = $mainControl.data("runtimeinfo");
                }
                this.newEntity(clonedElements, {
                    runtimeInfo: runtimeInfo,
                    type: entityType,
                    toStr: validatorResult.newToStr,
                    link: validatorResult.newLink
                });
            }
            return validatorResult.acceptChanges;
        }

        createFindOptions(_findOptions: FindOptions, _viewOptions?: ViewOptions): FindOptions {
            var self = this;
            return $.extend({
                prefix: this.options.prefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems); },
                onOkClosed: function () { self.fireOnEntityChanged(true); }
            }, _findOptions);
        }

        onFindingOk(selectedItems: Array<EntityData>, _viewOptions?: ViewOptions) {
            if (selectedItems == null || selectedItems.length != 1) {
                window.alert(lang.signum.onlyOneElement);
                return false;
            }
            this.newEntity(null, selectedItems[0]);
            return true;
        }

        onAutocompleteSelected(controlId: string, data: any) {
            var selectedItems = [{
                id: data.id,
                type: data.type,
                toStr: data.text,
                link: data.link
            }];
            this.onFindingOk(selectedItems);
            this.fireOnEntityChanged(true);
        }


        removeSpecific() {
            $(this.pf(EntityLine.key_entity)).remove();
        }
    }

    once("SF-entityCombo", () =>
        $.fn.entityCombo = function (opt: EntityBaseOptions) {
            var sc = new EntityCombo(this, opt);
        });

    export class EntityCombo extends EntityBase {

        static key_entity = "sfEntity";
        static key_combo = "sfCombo";

        updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
            $("#" + this.options.prefix + " option:selected").html(newToStr);
        }

        selectedValue() {

            var selected = $(this.pf(EntityCombo.key_combo) + " > option:selected");
            if (selected.length === 0) {
                return null;
            }
            var fullValue = selected.val();
            var separator = fullValue.indexOf(";");
            var value: any = {};
            if (separator === -1) {
                value.entityType = SF.isEmpty(fullValue) ? "" : this.staticInfo().singleType();
                value.id = fullValue;
            }
            else {
                value.entityType = fullValue.substring(0, separator);
                value.id = fullValue.substring(separator + 1, fullValue.length);
            }
            return value;
        }

        setSelected() {
            var newValue = this.selectedValue(),
                newEntityType = "",
                newId = "",
                newEntity = newValue !== null && !SF.isEmpty(newValue.id);

            if (newEntity) {
                newEntityType = newValue.entityType;
                newId = newValue.id;
            }
            var runtimeInfo = this.runtimeInfo();
            runtimeInfo.setEntity(newEntityType, newId);
            $(this.pf(EntityBase.key_entity)).html(''); //Clean
            this.fireOnEntityChanged(newEntity);
        }

        view(_viewOptions?: ViewOptions) {
            var viewOptions = this.viewOptionsForViewing(_viewOptions);
            if (viewOptions.navigate) {
                var runtimeInfo = this.runtimeInfo();
                if (!SF.isEmpty(runtimeInfo.id())) {
                    window.open(viewOptions.controllerUrl.substring(0, viewOptions.controllerUrl.lastIndexOf("/") + 1) + runtimeInfo.entityType() + "/" + runtimeInfo.id(), "_blank");
                }
            }
            else {
                new SF.ViewNavigator(viewOptions).viewOk();
            }
        }
    }

    export interface EntityBaseDetailOptions extends EntityBaseOptions {
        detailDiv: string;
    }

    once("SF-entityLineDetail", () =>
        $.fn.entityLineDetail = function (opt: EntityBaseDetailOptions) {
            new EntityLineDetail(this, opt);
        });

    export class EntityLineDetail extends EntityBase {

        options: EntityBaseDetailOptions;

        constructor(element: JQuery, options: EntityBaseDetailOptions) {
            super(element, options);
        }

        typedCreate(_viewOptions: ViewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityLineDetail typedCreate. Call create instead";
            }
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) { //Template pre-loaded: EmbeddedEntity
                $('#' + viewOptions.containerDiv).html(template);
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
            else {
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($("#" + this.options.detailDiv));
            }
            this.onCreated(viewOptions.type);
        }

        viewOptionsForCreating(_viewOptions: ViewOptions): ViewOptions {
            return $.extend({
                containerDiv: this.options.detailDiv,
                prefix: this.options.prefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams()
            }, _viewOptions);
        }

        newEntity(entityType: string) {
            this.runtimeInfo().setEntity(entityType, '');
        }

        onCreated(entityType: string) {
            this.newEntity(entityType);
            this.fireOnEntityChanged(true);
        }

        find(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        }

        typedFind(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in entityLineDetail typedFind. Call find instead";
            }
            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        }

        createFindOptions(_findOptions: FindOptions, _viewOptions?: ViewOptions): FindOptions {
            var self = this;
            return $.extend({
                prefix: this.options.prefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); },
                onOkClosed: function () { self.fireOnEntityChanged(true); }
            }, _findOptions);
        }

        onFindingOk(selectedItems: Array<EntityData>, _viewOptions?: ViewOptions) {
            if (selectedItems == null || selectedItems.length != 1) {
                window.alert(lang.signum.onlyOneElement);
                return false;
            }

            this.runtimeInfo().setEntity(selectedItems[0].type, selectedItems[0].id);

            //View result in the detailDiv
            var viewOptions = this.viewOptionsForCreating($.extend(_viewOptions, { type: selectedItems[0].type, id: selectedItems[0].id }));
            new SF.ViewNavigator(viewOptions).viewEmbedded();
            SF.triggerNewContent($("#" + this.options.detailDiv));

            return true;
        }

        removeSpecific() {
            $("#" + this.options.detailDiv).html("");
        }
    }

    once("SF-entityList", () =>
        $.fn.entityList = function (opt: EntityBaseOptions) {
            new EntityList(this, opt);
        });

    export class EntityList extends EntityBase {
        static key_indexes = "sfIndexes";
        static key_list = "sfList";

        itemSuffix() {
            return SF.Keys.toStr;
        }

        updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
            $('#' + SF.compose(itemPrefix, SF.Keys.toStr)).html(newToStr);
        }

        extraJsonParams(itemPrefix?: string) {
            var extraParams: any = {};

            var staticInfo = this.staticInfo();

            //If Embedded Entity => send propertyRoute
            if (staticInfo.isEmbedded()) {
                extraParams.rootType = staticInfo.rootType();
                extraParams.propertyRoute = staticInfo.propertyRoute();
            }

            if (staticInfo.isReadOnly()) {
                extraParams.readOnly = true;
            }

            return extraParams;
        }

        itemRuntimeInfo(itemPrefix: string) {
            return new SF.RuntimeInfo(itemPrefix);
        }


        selectedItemPrefix(): string {
            var $items = this.getItems();
            if ($items.length == 0) {
                return null;
            }
            var selected = $items.filter(":selected");
            if (selected.length == 0) {
                return null;
            }
            var nameSelected = selected[0].id;
            return nameSelected.substr(0, nameSelected.indexOf(SF.Keys.toStr) - 1);
        }

        getItems() {
            return $(this.pf(EntityList.key_list) + " > option");
        }

        getLastPrefixIndex() {
            var lastPrefixIndex = -1;
            var $items = this.getItems();
            var self = this;
            $items.each(function () {
                var currId = this.id;
                var currPrefixIndex = parseInt(currId.substring(self.options.prefix.length + 1, currId.indexOf(self.itemSuffix()) - 1), 10);
                if (currPrefixIndex > lastPrefixIndex) {
                    lastPrefixIndex = currPrefixIndex;
                }
            });
            return lastPrefixIndex;
        }

        getNewIndex(itemPrefix: string) {
            return parseInt($("#" + SF.compose(itemPrefix, EntityList.key_indexes)).val().split(";")[1]);
        }

        setNewIndex(itemPrefix: string, newIndex: number) {
            var $indexes = $("#" + SF.compose(itemPrefix, EntityList.key_indexes));
            var indexes = $indexes.val().split(";");
            $indexes.val(indexes[0].toString() + ";" + newIndex.toString());
        }

        getLastNewIndex() {
            var $last = this.getItems().filter(":last");
            if ($last.length == 0) {
                return -1;
            }

            var lastId = $last[0].id;
            var lastPrefix = lastId.substring(0, lastId.indexOf(this.itemSuffix()) - 1);

            return this.getNewIndex(lastPrefix);
        }

        checkValidation(validatorOptions: PartialValidationOptions, itemPrefix?: string) {
            if (typeof validatorOptions == "undefined" || typeof validatorOptions.type == "undefined") {
                throw "validatorOptions.type must be supplied to checkValidation";
            }

            var info = this.itemRuntimeInfo(itemPrefix);
            $.extend(validatorOptions, {
                prefix: itemPrefix,
                id: (info.find().length > 0) ? info.id() : ''
            });

            var validator = new SF.PartialValidator(validatorOptions);
            var validatorResult = validator.validate();
            if (!validatorResult.isValid) {
                if (!confirm(lang.signum.popupErrors)) {
                    $.extend(validatorResult, { acceptChanges: false });
                    return validatorResult;
                }
                else
                    validator.showErrors(validatorResult.modelState, true);
            }
            this.updateLinks(validatorResult.newToStr, validatorResult.newLink, itemPrefix);
            $.extend(validatorResult, { acceptChanges: true });
            return validatorResult;
        }

        getEmbeddedTemplate(): string {
            var template = window[SF.compose(this.options.prefix, "sfTemplate")];
            if (!SF.isEmpty(template)) {
                var newPrefixIndex = this.getLastPrefixIndex() + 1;
                var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
                template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix);
            }
            return template;
        }

        viewOptionsForCreating(_viewOptions: ViewOptions): ViewOptions {
            var self = this;
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            return $.extend({
                onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type, itemPrefix); },
                onCancelled: null,
                controllerUrl: null,
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        }

        onCreatingOk(clonedElements: JQuery, validatorOptions: PartialValidationOptions, entityType: string, itemPrefix?: string) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: entityType
            });
            var validatorResult = this.checkValidation(valOptions, itemPrefix);
            if (validatorResult.acceptChanges) {
                var runtimeInfo;
                var $mainControl = $(".sf-main-control[data-prefix=" + itemPrefix + "]");
                if ($mainControl.length > 0) {
                    runtimeInfo = $mainControl.data("runtimeinfo");
                }
                this.newListItem(clonedElements, itemPrefix, { runtimeInfo: runtimeInfo, type: entityType, toStr: validatorResult.newToStr });
            }
            return validatorResult.acceptChanges;
        }

        newListItem(clonedElements: any, itemPrefix: string, item: EntityData) {
            var $table = $("#" + this.options.prefix + "> .sf-field-list > .sf-field-list-table");

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), ";" + (this.getLastNewIndex() + 1).toString()));

            var itemInfoValue = item.runtimeInfo || this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o', null);
            $table.before(SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue));

            $table.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

            $('#' + SF.compose(itemPrefix, EntityList.key_entity)).append(clonedElements);

            var select = $(this.pf(EntityList.key_list));
            if (SF.isEmpty(item.toStr)) {
                item.toStr = "&nbsp;";
            }
            select.append("\n<option id='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' name='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' value='' class='sf-value-line'>" + item.toStr + "</option>");
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
            select.children('option:last').attr('selected', true);
            this.fireOnEntityChanged(false);
        }

        view(_viewOptions: ViewOptions) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.viewInIndex(_viewOptions, selectedItemPrefix);
        }

        viewInIndex(_viewOptions: ViewOptions, selectedItemPrefix: string) {
            var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
            if (viewOptions.navigate) {
                var itemInfo = this.itemRuntimeInfo(selectedItemPrefix);
                if (!SF.isEmpty(itemInfo.id())) {
                    window.open(_viewOptions.controllerUrl.substring(0, _viewOptions.controllerUrl.lastIndexOf("/") + 1) + itemInfo.entityType() + "/" + itemInfo.id(), "_blank");
                }
                return;
            }
            new SF.ViewNavigator(viewOptions).viewOk();
        }

        viewOptionsForViewing(_viewOptions: ViewOptions, itemPrefix?: string): ViewOptions {
            var self = this;
            var info = this.itemRuntimeInfo(itemPrefix);
            return $.extend({
                containerDiv: SF.compose(itemPrefix, EntityList.key_entity),
                onOk: function () { return self.onViewingOk(_viewOptions.validationOptions, itemPrefix); },
                onOkClosed: function () { self.fireOnEntityChanged(false); },
                onCancelled: null,
                controllerUrl: null,
                type: info.entityType(),
                id: info.id(),
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        }

        onViewingOk(validatorOptions: ValidationOptions, itemPrefix) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: this.itemRuntimeInfo(itemPrefix).entityType()
            });
            var validatorResult = this.checkValidation(valOptions, itemPrefix);
            return validatorResult.acceptChanges;
        }

        createFindOptions(_findOptions: FindOptions, _viewOptions?: ViewOptions): FindOptions {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems); }
            }, _findOptions);
        }

        onFindingOk(selectedItems: Array<EntityData>, _viewOptions?: ViewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                self.newListItem(null, itemPrefix, item);
            });
            return true;
        }

        foreachNewItem(selectedItems: Array<EntityData>, itemAction: (item: EntityData, itemPrefix: string) => void) {
            var lastPrefixIndex = this.getLastPrefixIndex();
            for (var i = 0, l = selectedItems.length; i < l; i++) {
                var item = selectedItems[i];
                lastPrefixIndex++;
                var itemPrefix = SF.compose(this.options.prefix, lastPrefixIndex.toString());
                itemAction(item, itemPrefix);
            }
        }

        remove(itemPrefix?: string) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.removeInIndex(selectedItemPrefix);
        }

        removeInIndex(selectedItemPrefix: string) {
            $.each([SF.Keys.runtimeInfo, SF.Keys.toStr, EntityList.key_entity, EntityList.key_indexes], function (i, key) {
                $("#" + SF.compose(selectedItemPrefix, key)).remove();
            });
            this.fireOnEntityChanged(false);
        }

        updateButtonsDisplay() {
            var hasElements = this.getItems().length > 0;
            $(this.pf("btnRemove")).toggle(hasElements);
            $(this.pf("btnView")).toggle(hasElements);
            $(this.pf("btnUp")).toggle(hasElements);
            $(this.pf("btnDown")).toggle(hasElements);
        }

        moveUp(selectedItemPrefix: string) {
            if (typeof selectedItemPrefix == "undefined") {
                selectedItemPrefix = this.selectedItemPrefix();
            }

            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
            var $itemPrev = $item.prev();

            if ($itemPrev.length == 0) {
                return;
            }

            var itemPrevId = $itemPrev[0].id;
            var itemPrevPrefix = itemPrevId.substring(0, itemPrevId.indexOf(suffix) - 1);

            var prevNewIndex = this.getNewIndex(itemPrevPrefix);
            this.setNewIndex(selectedItemPrefix, prevNewIndex);
            this.setNewIndex(itemPrevPrefix, prevNewIndex + 1);

            $item.insertBefore($itemPrev);
        }

        moveDown(selectedItemPrefix: string) {
            if (typeof selectedItemPrefix == "undefined") {
                selectedItemPrefix = this.selectedItemPrefix();
            }

            var suffix = this.itemSuffix();
            var $item = $("#" + SF.compose(selectedItemPrefix, suffix));
            var $itemNext = $item.next();

            if ($itemNext.length == 0) {
                return;
            }

            var itemNextId = $itemNext[0].id;
            var itemNextPrefix = itemNextId.substring(0, itemNextId.indexOf(suffix) - 1);

            var nextNewIndex = this.getNewIndex(itemNextPrefix);
            this.setNewIndex(selectedItemPrefix, nextNewIndex);
            this.setNewIndex(itemNextPrefix, nextNewIndex - 1);

            $item.insertAfter($itemNext);
        }

    }

    once("SF-entityListDetail", () =>
        $.fn.entityListDetail = function (opt: EntityBaseDetailOptions) {
            new EntityListDetail(this, opt);
        });

    export class EntityListDetail extends EntityList {

        options: EntityBaseDetailOptions;

        constructor(element: JQuery, options: EntityBaseDetailOptions) {
            super(element, options);
        }

        typedCreate(_viewOptions: ViewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityListDetail typedCreate. Call create instead";
            }
            this.restoreCurrent();
            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) {
                $('#' + viewOptions.containerDiv).html(template);
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
            else {
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
            this.onItemCreated(viewOptions);
        }

        viewOptionsForCreating(_viewOptions: ViewOptions): ViewOptions {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            return $.extend({
                containerDiv: this.options.detailDiv,
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        }

        getVisibleItemPrefix() {
            var detail = $('#' + this.options.detailDiv);
            var firstId = detail.find(":input[id^=" + this.options.prefix + "]:first");
            if (firstId.length === 0) {
                return null;
            }
            var id = firstId[0].id;
            var nextSeparator = id.indexOf("_", this.options.prefix.length + 1);
            return id.substring(0, nextSeparator);
        }

        restoreCurrent() {
            var itemPrefix = this.getVisibleItemPrefix();
            if (!SF.isEmpty(itemPrefix)) {
                $('#' + SF.compose(itemPrefix, EntityBase.key_entity))
                    .html('')
                    .append(SF.cloneContents(this.options.detailDiv));
            }
        }

        onItemCreated(viewOptions: ViewOptions) {
            if (SF.isEmpty(viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityListDetail onItemCreated. Call create instead";
            }

            var itemPrefix = viewOptions.prefix;
            this.newListItem(null, itemPrefix, { type: viewOptions.type, toStr: null });
        }

        view(_viewOptions: ViewOptions) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.viewInIndex(_viewOptions, selectedItemPrefix);
        }

        viewInIndex(_viewOptions: ViewOptions, selectedItemPrefix: string) {
            this.restoreCurrent();
            if (this.isLoaded(selectedItemPrefix)) {
                this.cloneAndShow(selectedItemPrefix);
            }
            else {
                var viewOptions = this.viewOptionsForViewing(_viewOptions, selectedItemPrefix);
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($('#' + viewOptions.containerDiv));
            }
        }

        viewOptionsForViewing(_viewOptions: ViewOptions, itemPrefix?: string): ViewOptions {
            var self = this;
            var info = this.itemRuntimeInfo(itemPrefix);
            return $.extend({
                containerDiv: this.options.detailDiv,
                type: info.entityType(),
                id: info.id(),
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        }

        isLoaded(selectedItemPrefix: string) {
            return !SF.isEmpty($('#' + SF.compose(selectedItemPrefix, EntityBase.key_entity)).html());
        }

        cloneAndShow(selectedItemPrefix: string) {
            $('#' + this.options.detailDiv)
                .html('')
                .append(SF.cloneContents(SF.compose(selectedItemPrefix, EntityBase.key_entity)));

            $('#' + SF.compose(selectedItemPrefix, EntityBase.key_entity))
                .html('');
        }

        find(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        }

        typedFind(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in entityListDetail typedFind. Call find instead";
            }

            this.restoreCurrent();
            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        }

        createFindOptions(_findOptions: FindOptions, _viewOptions?: ViewOptions): FindOptions {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems); }
            }, _findOptions);
        }

        onFindingOk(selectedItems: Array<EntityData>, _viewOptions?: ViewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                self.newListItem(null, itemPrefix, item);
            });
            //View result in the detailDiv
            $(this.pf(EntityList.key_list)).dblclick();
            return true;
        }

        remove(itemPrefix?: string) {
            var selectedItemPrefix = this.selectedItemPrefix();
            if (SF.isEmpty(selectedItemPrefix)) {
                return;
            }
            this.edlineRemoveInIndex(selectedItemPrefix);
        }

        edlineRemoveInIndex(itemPrefix) {
            var currentVisible = this.getVisibleItemPrefix();
            if (!SF.isEmpty(currentVisible) && currentVisible == itemPrefix)
                $('#' + this.options.detailDiv).html('');
            this.removeInIndex(itemPrefix);
        }
    }

    export interface EntityRepeaterOptions extends EntityBaseOptions {
        maxElements?: number;
        remove: boolean;
        reorder: boolean;
    }

    once("SF-entityRepeater", () =>
        $.fn.entityRepeater = function (opt: EntityRepeaterOptions) {
            new EntityRepeater(this, opt);
        });

    export class EntityRepeater extends EntityList {
        static key_itemsContainer = "sfItemsContainer";
        static key_repeaterItem = "sfRepeaterItem";
        static key_repeaterItemClass = "sf-repeater-element";
        static key_link = "sfLink";

        options: EntityRepeaterOptions;

        constructor(element: JQuery, options: EntityRepeaterOptions) {
            super(element, options);
        }

        itemSuffix() {
            return EntityRepeater.key_repeaterItem;
        }


        getItems() {
            return $(this.pf(EntityRepeater.key_itemsContainer) + " > ." + EntityRepeater.key_repeaterItemClass);
        }

        canAddItems() {
            if (!SF.isEmpty(this.options.maxElements)) {
                if (this.getItems().length >= +this.options.maxElements) {
                    return false;
                }
            }
            return true;
        }

        typedCreate(_viewOptions: ViewOptions) {
            if (SF.isEmpty(_viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityRepeater typedCreate. Call create instead";
            }
            if (!this.canAddItems()) {
                return;
            }

            var viewOptions = this.viewOptionsForCreating(_viewOptions);
            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) { //Template pre-loaded (Embedded Entity): It will be created with "_0" itemprefix => replace it with the current one
                template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), viewOptions.prefix);
                this.onItemCreated(template, viewOptions);
            }
            else {
                var self = this;
                new SF.ViewNavigator(viewOptions).createEmbedded(function (newHtml) {
                    self.onItemCreated(newHtml, viewOptions);
                });
            }
        }

        viewOptionsForCreating(_viewOptions: ViewOptions): ViewOptions {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            return $.extend({
                containerDiv: "",
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        }

        onItemCreated(newHtml, viewOptions: ViewOptions) {
            if (SF.isEmpty(viewOptions.type)) {
                throw "ViewOptions type parameter must not be null in entityRepeater onItemCreated";
            }

            var itemPrefix = viewOptions.prefix;
            this.newRepItem(newHtml, itemPrefix, { type: viewOptions.type });
        }

        newRepItem(newHtml, itemPrefix, item) {
            var itemInfoValue = this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o', null);
            var $div = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" +
                "<legend>" +
                (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "") +
                (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.moveUp + "</span>") : "") +
                (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.moveDown + "</span>") : "") +
                "</legend>" +
                SF.hiddenInput(SF.compose(itemPrefix, EntityRepeater.key_indexes), ";" + (this.getLastNewIndex() + 1).toString()) +
                SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue) +
                "<div id='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' class='sf-line-entity'>" +
                "</div>" + //sfEntity
                "</fieldset>"
                );

            $(this.pf(EntityRepeater.key_itemsContainer)).append($div);
            $("#" + SF.compose(itemPrefix, EntityRepeater.key_entity)).html(newHtml);
            SF.triggerNewContent($("#" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem)));
            this.fireOnEntityChanged(false);
        }

        _getRepeaterCall() {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        }

        _getRemoving(itemPrefix) {
            return this._getRepeaterCall() + ".remove('" + itemPrefix + "');";
        }

        _getMovingUp(itemPrefix) {
            return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        }

        _getMovingDown(itemPrefix) {
            return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        }

        viewOptionsForViewing(_viewOptions: ViewOptions, itemPrefix?: string): ViewOptions { //Used in onFindingOk
            return $.extend({
                containerDiv: SF.compose(itemPrefix, EntityBase.key_entity),
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        }

        find(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        }

        typedFind(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in ERep typedFind. Call find instead";
            }
            if (!this.canAddItems()) {
                return;
            }

            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        }

        createFindOptions(_findOptions: FindOptions, _viewOptions?: ViewOptions): FindOptions {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); }
            }, _findOptions);
        }

        onFindingOk(selectedItems: Array<EntityData>, _viewOptions?: ViewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                if (!self.canAddItems()) {
                    return;
                }

                self.newRepItem('', itemPrefix, item);

                //View results in the repeater
                var viewOptions = self.viewOptionsForViewing($.extend(_viewOptions, { type: item.type, id: item.id }), itemPrefix);
                new SF.ViewNavigator(viewOptions).viewEmbedded();
                SF.triggerNewContent($(SF.compose(itemPrefix, EntityRepeater.key_entity)));
            });
            return true;
        }

        remove(itemPrefix?: string) {
            $('#' + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem)).remove();
            this.fireOnEntityChanged(false);
        }

        updateButtonsDisplay() {
            var $buttons = $(this.pf("btnFind"), this.pf("btnCreate"));
            if (this.canAddItems()) {
                $buttons.show();
            }
            else {
                $buttons.hide();
            }
        }
    }

    export interface EntityStripOptions extends EntityBaseOptions {
        maxElements?: number;
        remove?: boolean;
        vertical?: boolean;
        reorder?: boolean;
        view?: boolean;
        navigate?: boolean;
    }

    once("SF-entityStrip", () =>
        $.fn.entityStrip = function (opt: EntityStripOptions) {
            new EntityStrip(this, opt);
        });

    export class EntityStrip extends EntityList {
        static key_itemsContainer = "sfItemsContainer";
        static key_stripItem = "sfStripItem";
        static key_stripItemClass = "sf-strip-element";
        static key_link = "sfLink";
        static key_input = "sf-strip-input";

        options: EntityStripOptions;

        constructor(element: JQuery, options: EntityStripOptions) {
            super(element, options);
        }

        itemSuffix() {
            return EntityStrip.key_stripItem;
        }

        getItems() {
            return $(this.pf(EntityStrip.key_itemsContainer) + " > ." + EntityStrip.key_stripItemClass);
        }

        canAddItems() {
            if (!SF.isEmpty(this.options.maxElements)) {
                if (this.getItems().length >= +this.options.maxElements) {
                    return false;
                }
            }
            return true;
        }

        viewOptionsForCreating(_viewOptions: ViewOptions): ViewOptions {
            var self = this;
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            return $.extend({
                onOk: function (clonedElements) { return self.onCreatingOk(clonedElements, _viewOptions.validationOptions, _viewOptions.type, itemPrefix); },
                onCancelled: null,
                controllerUrl: null,
                prefix: itemPrefix,
                partialViewName: this.options.partialViewName,
                requestExtraJsonData: this.extraJsonParams(itemPrefix)
            }, _viewOptions);
        }

        onCreatingOk(clonedElements: JQuery, validatorOptions: PartialValidationOptions, entityType: string, itemPrefix?: string) {
            var valOptions = $.extend(validatorOptions || {}, {
                type: entityType
            });
            var validatorResult = this.checkValidation(valOptions, itemPrefix);
            if (validatorResult.acceptChanges) {
                var runtimeInfo;
                var $mainControl = $(".sf-main-control[data-prefix=" + itemPrefix + "]");
                if ($mainControl.length > 0) {
                    runtimeInfo = $mainControl.data("runtimeinfo");
                }
                this.newStripItem(clonedElements, itemPrefix, { runtimeInfo: runtimeInfo, type: entityType, toStr: validatorResult.newToStr, link: validatorResult.newLink });
            }
            return validatorResult.acceptChanges;
        }

        newStripItem(newHtml: JQuery, itemPrefix: string, item: EntityData) {
            var itemInfoValue = item.runtimeInfo || this.itemRuntimeInfo(itemPrefix).createValue(item.type, item.id || '', typeof item.id == "undefined" ? 'n' : 'o', null);
            var $li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" +
                SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), ";" + (this.getLastNewIndex() + 1).toString()) +
                SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), itemInfoValue) +
                (this.options.navigate ?
                ("<a class='sf-value-line' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "' href='" + item.link + "' title='" + lang.signum.navigate + "'>" + item.toStr + "</a>") :
                ("<span class='sf-value-line' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "'>" + item.toStr + "</span>")) +
                "<span class='sf-button-container'>" + (
                (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "w" : "n") + "' data-text='false'>" + lang.signum.moveUp + "</span>") : "") +
                (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "e" : "s") + "' data-text='false'>" + lang.signum.moveDown + "</span>") : "") +
                (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this._getView(itemPrefix) + "\" class='sf-line-button sf-view' data-icon='ui-icon-circle-arrow-e' data-text='false'>" + lang.signum.view + "</a>") : "") +
                (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "")) +
                "</span>" +
                (!SF.isEmpty(newHtml) ? "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" : "") +
                "</li>"
                );

            $(this.pf(EntityStrip.key_itemsContainer) + " ." + EntityStrip.key_input).before($li);
            if (!SF.isEmpty(newHtml))
                $("#" + SF.compose(itemPrefix, EntityStrip.key_entity)).html(newHtml);
            SF.triggerNewContent($("#" + SF.compose(itemPrefix, EntityStrip.key_stripItem)));
            this.fireOnEntityChanged(false);
        }

        _getRepeaterCall() {
            return "$('#" + this.options.prefix + "').data('SF-control')";
        }

        _getRemoving(itemPrefix: string) {
            return this._getRepeaterCall() + ".remove('" + itemPrefix + "');";
        }

        _getView(itemPrefix: string) {
            return this._getRepeaterCall() + ".view('" + itemPrefix + "');";
        }

        _getMovingUp(itemPrefix: string) {
            return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
        }

        _getMovingDown(itemPrefix: string) {
            return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
        }


        find(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            var _self = this;
            var type = this.getEntityType(function (type) {
                _self.typedFind($.extend({ webQueryName: type }, _findOptions), _viewOptions);
            });
        }

        typedFind(_findOptions: FindOptions, _viewOptions?: ViewOptions) {
            if (SF.isEmpty(_findOptions.webQueryName)) {
                throw "FindOptions webQueryName parameter must not be null in ERep typedFind. Call find instead";
            }
            if (!this.canAddItems()) {
                return;
            }

            var findOptions = this.createFindOptions(_findOptions, _viewOptions);
            SF.FindNavigator.openFinder(findOptions);
        }

        createFindOptions(_findOptions: FindOptions, _viewOptions?: ViewOptions): FindOptions {
            var newPrefixIndex = this.getLastPrefixIndex() + 1;
            var itemPrefix = SF.compose(this.options.prefix, newPrefixIndex.toString());
            var self = this;
            return $.extend({
                prefix: itemPrefix,
                onOk: function (selectedItems) { return self.onFindingOk(selectedItems, _viewOptions); }
            }, _findOptions);
        }

        onFindingOk(selectedItems: Array<EntityData>, _viewOptions?: ViewOptions) {
            if (selectedItems == null || selectedItems.length == 0) {
                throw "No item was returned from Find Window";
            }
            var self = this;
            this.foreachNewItem(selectedItems, function (item, itemPrefix) {
                if (!self.canAddItems()) {
                    return;
                }

                self.newStripItem(null, itemPrefix, item);
            });
            return true;
        }

        remove(itemPrefix?: string) {
            $('#' + SF.compose(itemPrefix, EntityStrip.key_stripItem)).remove();
            this.fireOnEntityChanged(false);
        }

        view(_viewOptions: ViewOptions, itemPrefix?: string) {
            this.viewInIndex(_viewOptions || {}, itemPrefix);
        }


        updateButtonsDisplay() {
            var $buttons = $(this.pf("btnFind") + ", " + this.pf("btnCreate") + ", " + this.pf("sfToStr"));
            if (this.canAddItems()) {
                $buttons.show();
            }
            else {
                $buttons.hide();
            }
        }

        updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
            $('#' + SF.compose(itemPrefix, SF.Keys.link)).html(newToStr);
        }

        onAutocompleteSelected(controlId: string, data: any) {
            var selectedItems = [{
                id: data.id,
                type: data.type,
                toStr: data.text,
                link: data.link
            }];
            this.onFindingOk(selectedItems);
            $("#" + controlId).val("");
            this.fireOnEntityChanged(true);
        }
    }

    export function getInfoParams(prefix) {
        return $("#" + SF.compose(prefix, SF.Keys.runtimeInfo) + ", #" + SF.compose(prefix, EntityList.key_indexes));
    };
}

