/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")

export interface EntityBaseOptions {
    prefix: string;
    partialViewName: string;
}

export class EntityBase {
    options: EntityBaseOptions;
    element: JQuery;
    autoCompleter: EntityAutoCompleter;

    entityChanged: () => any;
    removing: (prefix: string) => Promise<boolean>;
    creating: (prefix: string) => Promise<Entities.EntityValue>;
    finding: (prefix: string) => Promise<Entities.EntityValue>;
    viewing: (entityHtml: Entities.EntityHtml) => Promise<Entities.EntityValue>;

    constructor(element: JQuery, _options: EntityBaseOptions) {
        this.element = element;
        this.element.data("SF-control", this);
        this.options = $.extend({
            prefix: "",
            partialViewName: "",
        }, _options);

        this._create();

        this.element.trigger("SF-ready");
    }

    static key_entity = "sfEntity";

    _create() {
        var $txt = $(this.pf(Entities.Keys.toStr) + ".sf-entity-autocomplete");
        if ($txt.length > 0) {
            var data = $txt.data();

            this.autoCompleter = new AjaxEntityAutoCompleter(SF.Urls.autocomplete,
                term => ({ types: this.staticInfo().types(), l: 5, q: term }));

            this.setupAutocomplete($txt);
        }
    }


    runtimeInfo(itemPrefix?: string) {
        return new Entities.RuntimeInfoElement(this.options.prefix);
    }

    staticInfo() {
        return new Entities.StaticInfo(this.options.prefix);
    }

    pf(s) {
        return "#" + SF.compose(this.options.prefix, s);
    }

    containerDiv(itemPrefix?: string) {
        var containerDivId = this.pf(EntityBase.key_entity);
        if ($(containerDivId).length == 0)
            this.runtimeInfo().getElem().after(SF.hiddenDiv(containerDivId, ""));

        return $(containerDivId);
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {

        var runtimeInfo = this.runtimeInfo().value();

        if (runtimeInfo == null)
            return null;

        var div = this.containerDiv();

        var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo, null, null);

        result.html = div.children();

        div.html(null);

        return result;
    }


    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        //virtual function
    }

    setEntity(entityValue: Entities.EntityValue, itemPrefix?: string) {


        this.setEntitySpecific(entityValue)

            if (entityValue) {
            entityValue.assertPrefixAndType(this.options.prefix, this.staticInfo());
        }

        SF.triggerNewContent(this.containerDiv().html(entityValue == null ? null : (<Entities.EntityHtml>entityValue).html));
        this.runtimeInfo().setValue(entityValue == null ? null : entityValue.runtimeInfo);

        if (entityValue == null) {
            Validator.cleanError($(this.pf(Entities.Keys.toStr)).val(""));
            Validator.cleanError($(this.pf(Entities.Keys.link)).val("").html(""));
        }

        this.updateButtonsDisplay();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    remove_click(): Promise<void> {
        return this.onRemove(this.options.prefix).then(result=> {
            if (result)
                this.setEntity(null);
        });
    }

    onRemove(prefix: string): Promise<boolean> {
        if (this.removing != null)
            return this.removing(prefix);

        return Promise.resolve(true);
    }

    create_click() : Promise<void> {
        return this.onCreating(this.options.prefix).then(result => {
            if (result)
                this.setEntity(result);
        });
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        Navigator.typeChooser(this.staticInfo()).then(type=> {
            if (type == null)
                return null;

            var newEntity = new Entities.EntityHtml(this.options.prefix, new Entities.RuntimeInfoValue(type, null));

            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template))
                newEntity.html = $(template);

            return Navigator.viewPopup(newEntity, this.defaultViewOptions());
        });
    }

    getEmbeddedTemplate(itemPrefix?: string) {
        return window[SF.compose(this.options.prefix, "sfTemplate")];
    }

    view_click(): Promise<void> {
        var entityHtml = this.extractEntityHtml();

        return this.onViewing(entityHtml).then(result=> {
            if (result)
                this.setEntity(result);
            else
                this.setEntity(entityHtml); //previous entity passed by reference
        });
    }

    onViewing(entityHtml: Entities.EntityHtml): Promise<Entities.EntityValue> {
        if (this.viewing != null)
            return this.viewing(entityHtml);

        return Navigator.viewPopup(entityHtml, this.defaultViewOptions());
    }

    find_click(): Promise<void> {
        return this.onFinding(this.options.prefix).then(result => {
            if (result)
                this.setEntity(result);
        });
    }

    onFinding(prefix: string): Promise<Entities.EntityValue> {
        if (this.finding != null)
            return this.finding(this.options.prefix);

        return Navigator.typeChooser(this.staticInfo()).then(type=> {
            if (type == null)
                return null;

            return Finder.find({
                webQueryName: type,
                prefix: prefix,
            });
        });
    }

    defaultViewOptions(): Navigator.ViewPopupOptions {
        return {
            readOnly: this.staticInfo().isReadOnly(),
            partialViewName: this.options.partialViewName
        };
    }

    updateButtonsDisplay() {

        var hasEntity = !!this.runtimeInfo().value;

        $(this.pf("btnCreate")).toggle(!hasEntity);
        $(this.pf("btnFind")).toggle(!hasEntity);
        $(this.pf("btnRemove")).toggle(hasEntity);
        $(this.pf("btnView")).toggle(hasEntity);
        $(this.pf(Entities.Keys.link)).toggle(hasEntity);
        $(this.pf(Entities.Keys.toStr)).toggle(!hasEntity);
    }

    setupAutocomplete($txt) {

        var auto = $txt.autocomplete({
            delay: 200,
            source: (request, response) => {
                this.autoCompleter.getResults(request.term).then(entities=> {
                    response(entities.map(e=> ({ label: e.toStr, value: e })));
                });
            },
            focus: (event, ui) => {
                $txt.val(ui.item.value.text);
                return false;
            },
            select: (event, ui) => {
                this.onAutocompleteSelected(ui.item.value);
                this.setEntity(ui.item.value);
                event.preventDefault();
            },
        });

        auto.data("uiAutocomplete")._renderItem = (ul, item) => {
            var val = <Entities.EntityValue>item.value

                return $("<li>")
                .attr("data-type", val.runtimeInfo.type)
                .attr("data-id", val.runtimeInfo.id)
                .append($("<a>").text(item.label))
                .appendTo(ul);
        };
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        throw new Error("onAutocompleteSelected is abstract");
    }
}

export interface EntityAutoCompleter {
    getResults(term: string): Promise<Entities.EntityValue[]>;
}

export class AjaxEntityAutoCompleter implements EntityAutoCompleter {

    controllerUrl: string;

    getData: (term: string) => any;

    constructor(controllerUrl: string, getData: (term: string) => any) {
        this.controllerUrl = controllerUrl;
        this.getData = getData;
    }

    lastXhr: JQueryXHR; //To avoid previous requests results to be shown

    getResults(term: string): Promise<Entities.EntityValue[]> {
        if (this.lastXhr)
            this.lastXhr.abort();

        return new Promise<Entities.EntityValue[]>((resolve, failure) => {
            this.lastXhr = $.ajax({
                url: this.controllerUrl,
                data: this.getData(term),
                success: function (data: any[]) {
                    this.lastXhr = null;
                    resolve(data.map(item=> new Entities.EntityValue(new Entities.RuntimeInfoValue(item.type, parseInt(item.id)), item.toStr, item.link)));
                }
            });
        });
    }

}

export class EntityLine extends EntityBase {

    setEntitySpecific(entityValue: Entities.EntityValue) {
        var link = $(this.pf(Entities.Keys.link));
        link.text(entityValue == null ? null : entityValue.toStr);
        if (link.filter('a').length !== 0)
            link.attr('href', entityValue == null ? null : entityValue.link);
        $(this.pf(Entities.Keys.toStr)).val('');
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        this.setEntity(entityValue);
    }
}

export class EntityCombo extends EntityBase {

    static key_combo = "sfCombo";

    combo() {
        return $(this.pf(EntityCombo.key_combo));
    }

    setEntitySpecific(entityValue: Entities.EntityValue) {
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
    }

    combo_selected() {
        var val = this.combo().val();

        var ri = Entities.RuntimeInfoValue.fromKey(val);

        this.setEntity(ri == null ? null : new Entities.EntityValue(ri));
    }
}

export interface EntityDetailOptions extends EntityBaseOptions {
    detailDiv: string;
}

export class EntityLineDetail extends EntityBase {

    options: EntityDetailOptions;

    constructor(element: JQuery, options: EntityDetailOptions) {
        super(element, options);
    }

    containerDiv(itemPrefix?: string) {
        return $("#" + this.options.detailDiv);
    }

    setEntitySpecific(entityValue: Entities.EntityValue) {
        if (entityValue == null)
            return;

        if (!entityValue.isLoaded())
            throw new Error("EntityLineDetail requires a loaded Entities.EntityHtml, consider calling Navigator.loadPartialView");
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        Navigator.typeChooser(this.staticInfo()).then(type=> {
            if (type == null)
                return null;

            var newEntity = new Entities.EntityHtml(this.options.prefix, new Entities.RuntimeInfoValue(type, null));

            var template = this.getEmbeddedTemplate();
            if (!SF.isEmpty(template)) {
                newEntity.html = $(template);
                return Promise.resolve(newEntity);
            }

            return Navigator.requestPartialView(newEntity, this.defaultViewOptions());
        });
    }

    onFinding(prefix: string): Promise<Entities.EntityValue> {
        return super.onFinding(prefix).then(entity => {
            if (entity == null)
                return null;

            if ((<Entities.EntityHtml>entity).html != null)
                return Promise.resolve(entity);

            return Navigator.requestPartialView(new Entities.EntityHtml(this.options.prefix, entity.runtimeInfo), this.defaultViewOptions());
        });
    }
}



export interface EntityListBaseOptions extends EntityBaseOptions {
    maxElements?: number;
    remove?: boolean;
    reorder?: boolean;
}

export class EntityListBase extends EntityBase {
    static key_indexes = "sfIndexes";

    options: EntityListBaseOptions;
    finding: (prefix: string) => Promise<Entities.EntityValue>;  // DEPRECATED!
    findingMany: (prefix: string) => Promise<Entities.EntityValue[]>;

    constructor(element: JQuery, options: EntityListBaseOptions) {
        super(element, options);
    }

    runtimeInfo(itemPrefix?: string) {
        return new Entities.RuntimeInfoElement(itemPrefix);
    }

    containerDiv(itemPrefix?: string): JQuery {
        var containerDivId = SF.compose(itemPrefix, EntityList.key_entity);
        if ($(containerDivId).length == 0)
            this.runtimeInfo(itemPrefix).getElem().after(SF.hiddenDiv(containerDivId, ""));

        return $(containerDivId);
    }



    getEmbeddedTemplate(itemPrefix?: string) {
        var template = super.getEmbeddedTemplate();
        if (SF.isEmpty(template))
            return template;

        template = template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix);
        return template;
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {
        var runtimeInfo = this.runtimeInfo(itemPrefix).value();

        var div = this.containerDiv(itemPrefix);

        var result = new Entities.EntityHtml(itemPrefix, runtimeInfo, null, null);

        result.html = div.children();

        div.html(null);

        return result;
    }

    setEntity(entityValue: Entities.EntityValue, itemPrefix?: string) {
        if (entityValue == null)
            throw new Error("entityValue is mandatory on setEntityItem");

        this.setEntitySpecific(entityValue)

            if (entityValue)
            entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

        if (entityValue.isLoaded())
            SF.triggerNewContent(this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html));

        this.runtimeInfo(itemPrefix).setValue(entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }



    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        //virtual
    }

    addEntity(entityValue: Entities.EntityValue, itemPrefix: string) {
        if (entityValue == null)
            throw new Error("entityValue is mandatory on setEntityItem");

        this.addEntitySpecific(entityValue, itemPrefix);

        if (entityValue)
            entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

        if (entityValue.isLoaded())
            SF.triggerNewContent(this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html));
        this.runtimeInfo(itemPrefix).setValue(entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    removeEntitySpecific(prefix: string) {
        //virtual
    }

    removeEntity(prefix: string) {
        this.updateButtonsDisplay();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    itemSuffix(): string {
        throw new Error("itemSuffix is abstract");
    }

    getItems(): JQuery {
        throw new Error("getItems is abstract");
    }

    getNextPrefix(): string {
        var lastIndex = Math.max.apply(null, this.getItems().toArray()
            .map((e: HTMLElement) => parseInt(e.id.after(this.options.prefix + "_").before("_" + this.itemSuffix()))));

            return SF.compose(this.options.prefix, lastIndex + 1)
        }

    getLastPosIndex(): number {
        var $last = this.getItems().filter(":last");
        if ($last.length == 0) {
            return -1;
        }

        var lastId = $last[0].id;
        var lastPrefix = lastId.substring(0, lastId.indexOf(this.itemSuffix()) - 1);

        return this.getPosIndex(lastPrefix);
    }

    getNextPosIndex(): string {
        return ";" + (this.getLastPosIndex() + 1).toString();
    }

    canAddItems() {
        return SF.isEmpty(this.options.maxElements) || this.getItems().length < this.options.maxElements;
    }

    find_click(): Promise<void>{
        return this.onFindingMany(this.options.prefix).then(result => {
            if (result)
                result.forEach(ev=> this.addEntity(ev, this.getNextPrefix()));
        });
    }

    onFinding(prefix: string): Promise<Entities.EntityValue> {
        throw new Error("onFinding is deprecated in EntityListBase");
    }

    onFindingMany(prefix: string): Promise<Entities.EntityValue[]> {
        if (this.findingMany != null)
            return this.findingMany(this.options.prefix);

        return Navigator.typeChooser(this.staticInfo()).then(type=> {
            if (type == null)
                return null;

            return Finder.findMany({
                webQueryName: type,
                prefix: prefix,
            });
        });
    }

    moveUp(itemPrefix: string) {

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
    }

    moveDown(itemPrefix: string) {

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
    }

    getPosIndex(itemPrefix: string) {
        return parseInt($("#" + SF.compose(itemPrefix, EntityListBase.key_indexes)).val().after(";"));
    }

    setPosIndex(itemPrefix: string, newIndex: number) {
        var $indexes = $("#" + SF.compose(itemPrefix, EntityListBase.key_indexes));
        $indexes.val($indexes.val().before(";") + ";" + newIndex.toString());
    }
}

export class EntityList extends EntityListBase {

    static key_list = "sfList";

    itemSuffix() {
        return Entities.Keys.toStr;
    }

    updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
        $('#' + SF.compose(itemPrefix, Entities.Keys.toStr)).html(newToStr);
    }

    selectedItemPrefix(): string {
        var $items = this.getItems().filter(":selected");;
        if ($items.length == 0) {
            return null;
        }

        var nameSelected = $items[0].id;
        return nameSelected.before("_" + this.itemSuffix());
    }

    getItems(): JQuery {
        return $(this.pf(EntityList.key_list) + " > option");
    }

    view_click(): Promise<void> {
        var selectedItemPrefix = this.selectedItemPrefix();

        var entityHtml = this.extractEntityHtml(selectedItemPrefix);

        return this.onViewing(entityHtml).then(result=> {
            if (result)
                this.setEntity(result, selectedItemPrefix);
            else
                this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
        });
    }

    create_click(): Promise<void> {
        var prefix = this.getNextPrefix();
        return this.onCreating(prefix).then(entity => {
            if (entity)
                this.addEntity(entity, prefix);
        });
    }

    updateButtonsDisplay() {
        var hasElements = this.getItems().length > 0;
        $(this.pf("btnRemove")).toggle(hasElements);
        $(this.pf("btnView")).toggle(hasElements);
        $(this.pf("btnUp")).toggle(hasElements);
        $(this.pf("btnDown")).toggle(hasElements);

        var canAdd = this.canAddItems();

        $(this.pf("btnCreate")).toggle(canAdd);
        $(this.pf("btnFind")).toggle(canAdd);
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var $table = $("#" + this.options.prefix + "> .sf-field-list > .sf-field-list-table");

        $table.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), this.getNextPosIndex()));

        $table.before(SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));

        $table.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

        var select = $(this.pf(EntityList.key_list));
        select.append("\n<option id='" + SF.compose(itemPrefix, Entities.Keys.toStr) + "' name='" + SF.compose(itemPrefix, Entities.Keys.toStr) + "' value='' class='sf-value-line'>" + entityValue.toStr + "</option>");
        select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
        select.children('option:last').attr('selected', true);
    }

    remove_click(): Promise<void> {
        var selectedItemPrefix = this.selectedItemPrefix();
        return this.onRemove(selectedItemPrefix).then(result=> {
            if (result)
                this.removeEntity(selectedItemPrefix);
        });
    }

    removeEntitySpecific(prefix: string) {
        $("#" + SF.compose(prefix, Entities.Keys.runtimeInfo)).remove();
        $("#" + SF.compose(prefix, Entities.Keys.toStr)).remove();
        $("#" + SF.compose(prefix, EntityList.key_entity)).remove();
        $("#" + SF.compose(prefix, EntityList.key_indexes)).remove();
    }

    moveUp_click() {
        this.moveUp(this.selectedItemPrefix());
    }

    moveDown_click() {
        this.moveDown(this.selectedItemPrefix());
    }
}

export interface EntityListDetailOptions extends EntityListBaseOptions {
    detailDiv: string;
}

export class EntityListDetail extends EntityList {

    options: EntityListDetailOptions;

    constructor(element: JQuery, options: EntityListDetailOptions) {
        super(element, options);
    }

    selection_Changed() {
        this.stageCurrentSelected();
    }

    stageCurrentSelected() {
        var selPrefix = this.selectedItemPrefix();

        var detailDiv = $("#" + this.options.detailDiv)

            var children = detailDiv.children();

        if (children.length != 0) {
            var prefix = children[0].id.before("_" + EntityListDetail.key_entity);
            if (selPrefix == prefix) {
                children.show();
                return;
            }
            children.hide()
                this.runtimeInfo(prefix).$elem.after(children);
        }

        var selContainer = this.containerDiv(selPrefix);

        if (selContainer.children().length == 0) {
            detailDiv.append(selContainer);
            selContainer.show();
        } else {
            var entity = new Entities.EntityHtml(selPrefix, this.runtimeInfo(selPrefix).value(), null, null);

            Navigator.requestPartialView(entity, this.defaultViewOptions()).then(e=> {
                selContainer.html(e.html);
                detailDiv.append(selContainer);
            });
        }
    }
}

export class EntityRepeater extends EntityListBase {
    static key_itemsContainer = "sfItemsContainer";
    static key_repeaterItem = "sfRepeaterItem";
    static key_repeaterItemClass = "sf-repeater-element";
    static key_link = "sfLink";

    itemSuffix() {
        return EntityRepeater.key_repeaterItem;
    }


    getItems() {
        return $(this.pf(EntityRepeater.key_itemsContainer) + " > ." + EntityRepeater.key_repeaterItemClass);
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var $div = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" +
            "<legend>" +
            (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "") +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.moveUp + "</span>") : "") +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.moveDown + "</span>") : "") +
            "</legend>" +
            SF.hiddenInput(SF.compose(itemPrefix, EntityListBase.key_indexes), this.getNextPosIndex()) +
            SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) +
            "<div id='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' class='sf-line-entity'>" +
            "</div>" + //sfEntity
            "</fieldset>"
            );

        $(this.pf(EntityRepeater.key_itemsContainer)).append($div);
    }

    _getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    _getRemoving(itemPrefix) {
        return this._getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
    }

    _getMovingUp(itemPrefix) {
        return this._getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
    }

    _getMovingDown(itemPrefix) {
        return this._getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
    }

    remove_click(): Promise<void> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    removeItem_click(itemPrefix: string): Promise<void> {
        return this.onRemove(itemPrefix).then(result=> {
            if (result)
                this.removeEntity(itemPrefix);
        });
    }

    updateButtonsDisplay() {
        var canAdd = this.canAddItems();

        $(this.pf("btnCreate")).toggle(canAdd);
        $(this.pf("btnFind")).toggle(canAdd);
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

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        $('#' + SF.compose(itemPrefix, Entities.Keys.link)).html(entityValue.toStr);
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var $li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" +
            SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), this.getNextPosIndex()) +
            SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) +
            (this.options.navigate ?
            ("<a class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "' href='" + entityValue.link + "' title='" + lang.signum.navigate + "'>" + entityValue.toStr + "</a>") :
            ("<span class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, EntityStrip.key_link) + "'>" + entityValue.toStr + "</span>")) +
            "<span class='sf-button-container'>" + (
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "w" : "n") + "' data-text='false'>" + lang.signum.moveUp + "</span>") : "") +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "e" : "s") + "' data-text='false'>" + lang.signum.moveDown + "</span>") : "") +
            (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this._getView(itemPrefix) + "\" class='sf-line-button sf-view' data-icon='ui-icon-circle-arrow-e' data-text='false'>" + lang.signum.view + "</a>") : "") +
            (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "")) +
            "</span>" +
            "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" +
            "</li>"
            );

        $(this.pf(EntityStrip.key_itemsContainer) + " ." + EntityStrip.key_input).before($li);
    }

    _getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    _getRemoving(itemPrefix: string) {
        return this._getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
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

    remove_click(): Promise<void> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    removeItem_click(itemPrefix: string): Promise<void> {
        return this.onRemove(itemPrefix).then(result=> {
            if (result)
                this.removeEntity(itemPrefix);
        });
    }

    view_click(): Promise<void>{ throw new Error("remove_click is deprecated in EntityRepeater"); }

    viewItem_click(itemPrefix: string): Promise<void>{
        var entityHtml = this.extractEntityHtml(itemPrefix);

        return this.onViewing(entityHtml).then(result=> {
            if (result)
                this.setEntity(result, itemPrefix);
            else
                this.setEntity(entityHtml, itemPrefix); //previous entity passed by reference
        });
    }

    updateButtonsDisplay() {
        var canAdd = this.canAddItems();

        $(this.pf("btnCreate")).toggle(canAdd);
        $(this.pf("btnFind")).toggle(canAdd);
        $(this.pf("sfToStr")).toggle(canAdd);
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        this.addEntity(entityValue, this.getNextPrefix());
    }
}

