/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")

export interface EntityBaseOptions {
    prefix: string;
    partialViewName: string;
    template?: string;
    autoCompleteUrl?: string;

    types: string[];
    typeNiceNames: string[];
    isEmbedded: boolean;
    isReadonly: boolean;
    rootType?: string;
    propertyRoute?: string;
}

export class EntityBase {
    options: EntityBaseOptions;
    element: JQuery;
    autoCompleter: EntityAutocompleter;

    entityChanged: () => void;
    removing: (prefix: string) => Promise<boolean>;
    creating: (prefix: string) => Promise<Entities.EntityValue>;
    finding: (prefix: string) => Promise<Entities.EntityValue>;
    viewing: (entityHtml: Entities.EntityHtml) => Promise<Entities.EntityValue>;

    constructor(element: JQuery, options: EntityBaseOptions) {
        this.element = element;
        this.element.data("SF-control", this);
        this.options = options;
        var temp = $(this.pf(Entities.Keys.template));

        if (temp.length > 0)
            this.options.template = temp.html().replaceAll("<scriptX", "<script").replaceAll("</scriptX", "</script");

        this._create();
    }

    public ready() {

        this.element.SFControlFullfill(this); 
    }

    static key_entity = "sfEntity";

    _create() {
        var $txt = $(this.pf(Entities.Keys.toStr) + ".sf-entity-autocomplete");
        if ($txt.length > 0) {
            this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete,
                term => ({ types: this.options.types, l: 5, q: term }));

            this.setupAutocomplete($txt);
        }
    }
    
    runtimeInfoHiddenElement(itemPrefix?: string) : JQuery {
        return $(this.pf(Entities.Keys.runtimeInfo));
    }

    pf(s) {
        return "#" + SF.compose(this.options.prefix, s);
    }

    containerDiv(itemPrefix?: string) {
        var containerDivId = this.pf(EntityBase.key_entity);
        if ($(containerDivId).length == 0)
            this.runtimeInfoHiddenElement().after(SF.hiddenDiv(containerDivId.after('#'), ""));

        return $(containerDivId);
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {

        var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

        if (runtimeInfo == null)
            return null;

        var div = this.containerDiv();

        var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo,
            this.getToString(),
            this.getLink());

        result.html = div.children();

        div.html(null);

        return result;
    }

    getLink(itemPrefix?: string) : string {
        return null;
    }

    getToString(itemPrefix?: string): string {
        return null;
    }


    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        //virtual function
    }

    setEntity(entityValue: Entities.EntityValue, itemPrefix?: string) {

        this.setEntitySpecific(entityValue)

        if (entityValue) 
            entityValue.assertPrefixAndType(this.options.prefix, this.options.types);
        

        SF.triggerNewContent(this.containerDiv().html(entityValue == null ? null : (<Entities.EntityHtml>entityValue).html));
        Entities.RuntimeInfo.setFromPrefix(this.options.prefix, entityValue == null ? null : entityValue.runtimeInfo);
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

    typeChooser(): Promise<string> {
        return Navigator.typeChooser(this.options.prefix,
            this.options.types.map((t, i) => ({ type: t, toStr: this.options.typeNiceNames[i] })));
    }

    singleType(): string {
        if (this.options.types.length != 1)
            throw new Error("There are {0} types in {1}".format(this.options.types.length, this.options.prefix));

        return this.options.types[0]; 
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            var newEntity = this.options.template ? this.getEmbeddedTemplate(prefix) :
                new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

            return Navigator.viewPopup(newEntity, this.defaultViewOptions());
        });
    }

    getEmbeddedTemplate(itemPrefix?: string): Entities.EntityHtml {
        if (!this.options.template)
            throw new Error("no template in " + this.options.prefix);

        var result = new Entities.EntityHtml(this.options.prefix,
            new Entities.RuntimeInfo(this.singleType(), null, true));

        result.loadHtml(this.options.template);

        return result;
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
            return this.finding(prefix);

        return this.typeChooser().then(type=> {
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
            readOnly: this.options.isReadonly,
            partialViewName: this.options.partialViewName,
            validationOptions: {
                rootType: this.options.rootType,
                propertyRoute: this.options.propertyRoute,
            }
        };
    }

    updateButtonsDisplay() {

        var hasEntity = !!Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

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

export interface EntityAutocompleter {
    getResults(term: string): Promise<Entities.EntityValue[]>;
}

export interface AutocompleteResult {
    id: number;
    text: string;
    type: string;
    link: string;
}

export class AjaxEntityAutocompleter implements EntityAutocompleter {

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
                success: function (data: AutocompleteResult[]) {
                    this.lastXhr = null;
                    resolve(data.map(item=> new Entities.EntityValue(new Entities.RuntimeInfo(item.type, item.id, false), item.text, item.link)));
                }
            });
        });
    }

}

export class EntityLine extends EntityBase {

    getLink(itemPrefix?: string): string {
        return $(this.pf(Entities.Keys.link)).attr("href");
    }

    getToString(itemPrefix?: string): string {
        return $(this.pf(Entities.Keys.link)).text();
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
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
            var o = c.children("option[value='" + entityValue.runtimeInfo.key() + "']");
            if (o.length == 1)
                o.html(entityValue.toStr);
            else
                c.add($("<option value='{0}'/>".format(entityValue.runtimeInfo.key())).text(entityValue.toStr));

            c.val(entityValue.runtimeInfo.key());
        }
    }

    getToString(itemPrefix?: string): string {
        return this.combo().children("option[value='" + this.combo().val() + "']").text();
    }

    combo_selected() {
        var val = this.combo().val();

        var ri = Entities.RuntimeInfo.fromKey(val);

        this.setEntity(ri == null ? null : new Entities.EntityValue(ri, this.getToString()));
    }
}

export interface EntityLineDetailOptions extends EntityBaseOptions {
    detailDiv: string;
}

export class EntityLineDetail extends EntityBase {

    options: EntityLineDetailOptions;

    constructor(element: JQuery, options: EntityLineDetailOptions) {
        super(element, options);
    }

    containerDiv(itemPrefix?: string) {
        return $("#" + this.options.detailDiv);
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        if (entityValue == null)
            return;

        if (!entityValue.isLoaded())
            throw new Error("EntityLineDetail requires a loaded Entities.EntityHtml, consider calling Navigator.loadPartialView");
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;
      
            var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

            return Navigator.requestPartialView(newEntity, this.defaultViewOptions());
        });
    }

    find_click(): Promise<void> {
        return this.onFinding(this.options.prefix).then(result => {
            if (result == null)
                return null;

            if (result.isLoaded())
                return Promise.resolve(<Entities.EntityHtml>result);

            return Navigator.requestPartialView(new Entities.EntityHtml(this.options.prefix, result.runtimeInfo), this.defaultViewOptions());
        }).then(result => {
                if (result)
                    this.setEntity(result);
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

    runtimeInfo(itemPrefix?: string) :JQuery {
        return $("#" + SF.compose(itemPrefix, Entities.Keys.runtimeInfo));
    }

    containerDiv(itemPrefix?: string): JQuery {
        var containerDivId = "#" + SF.compose(itemPrefix, EntityList.key_entity);
        if ($(containerDivId).length == 0)
            this.runtimeInfo(itemPrefix).after(SF.hiddenDiv(containerDivId.after("#"), ""));

        return $(containerDivId);
    }

    getEmbeddedTemplate(itemPrefix?: string) {
        if (!this.options.template)
            throw new Error("no template in " + this.options.prefix);

        var result = new Entities.EntityHtml(itemPrefix,
            new Entities.RuntimeInfo(this.singleType(), null, true));

        var replaced = this.options.template.replace(new RegExp(SF.compose(this.options.prefix, "0"), "gi"), itemPrefix)

        result.loadHtml(replaced);

        return result;
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {
        var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(itemPrefix);

        var div = this.containerDiv(itemPrefix);

        var result = new Entities.EntityHtml(itemPrefix, runtimeInfo,
            this.getToString(itemPrefix),
            this.getLink(itemPrefix));

        result.html = div.children();

        div.html(null);

        return result;
    }

    setEntity(entityValue: Entities.EntityValue, itemPrefix?: string) {
        if (entityValue == null)
            throw new Error("entityValue is mandatory on setEntityItem");

        this.setEntitySpecific(entityValue, itemPrefix)

        entityValue.assertPrefixAndType(itemPrefix, this.options.types);

        if (entityValue.isLoaded())
            SF.triggerNewContent(this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html));

        Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    create_click(): Promise<void> {
        var itemPrefix = this.getNextPrefix();
        return this.onCreating(itemPrefix).then(entity => {
            if (entity)
                this.addEntity(entity, itemPrefix);
        });
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        //virtual
    }

    addEntity(entityValue: Entities.EntityValue, itemPrefix: string) {
        if (entityValue == null)
            throw new Error("entityValue is mandatory on setEntityItem");

        this.addEntitySpecific(entityValue, itemPrefix);

        if (entityValue)
            entityValue.assertPrefixAndType(itemPrefix, this.options.types);

        if (entityValue.isLoaded())
            SF.triggerNewContent(this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html));
        Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged();
        }
    }

    removeEntitySpecific(itemPrefix: string) {
        //virtual
    }

    removeEntity(itemPrefix: string) {
        this.removeEntitySpecific(itemPrefix);

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

    getPrefixes(): string[] {
        return this.getItems().toArray()
            .map((e: HTMLElement) => e.id.before("_" + this.itemSuffix()));
    }

    getRuntimeInfos(): Entities.RuntimeInfo[] {
        return this.getPrefixes().map(p=> Entities.RuntimeInfo.getFromPrefix(p));
    }

    getNextPrefix(inc: number = 0): string {

        var indices = this.getItems().toArray()
            .map((e: HTMLElement) => parseInt(e.id.after(this.options.prefix + "_").before("_" + this.itemSuffix())));

        var next: number = indices.length == 0 ? inc :
            (Math.max.apply(null, indices) + 1 + inc);

        return SF.compose(this.options.prefix, next.toString());
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
            return this.findingMany(prefix);

        return this.typeChooser().then(type=> {
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

    getToString(itemPrefix?: string): string {
        return $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).text();
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).text(entityValue.toStr);
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

    removeEntitySpecific(itemPrefix: string) {
        $("#" + SF.compose(itemPrefix, Entities.Keys.runtimeInfo)).remove();
        $("#" + SF.compose(itemPrefix, Entities.Keys.toStr)).remove();
        $("#" + SF.compose(itemPrefix, EntityList.key_entity)).remove();
        $("#" + SF.compose(itemPrefix, EntityList.key_indexes)).remove();
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

    remove_click() {
        return super.remove_click().then(()=>this.stageCurrentSelected())
    }

    create_click() {
        return super.create_click().then(() => this.stageCurrentSelected())
    }

    find_click() {
        return super.find_click().then(() => this.stageCurrentSelected())
    }

    stageCurrentSelected() {
        var selPrefix = this.selectedItemPrefix();

        var detailDiv = $("#" + this.options.detailDiv)

        var children = detailDiv.children();

        if (children.length != 0) {
            var itemPrefix = children[0].id.before("_" + EntityListDetail.key_entity);
            if (selPrefix == itemPrefix) {
                children.show();
                return;
            }
            children.hide()
                this.runtimeInfo(itemPrefix).after(children);
        }

        var selContainer = this.containerDiv(selPrefix);

        if (selContainer.children().length > 0) {
            detailDiv.append(selContainer);
            selContainer.show();
        } else {
            var entity = new Entities.EntityHtml(selPrefix, Entities.RuntimeInfo.getFromPrefix(selPrefix), null, null);

            Navigator.requestPartialView(entity, this.defaultViewOptions()).then(e=> {
                selContainer.html(e.html);
                detailDiv.append(selContainer);
                selContainer.show();
            });
        }
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

            return Navigator.requestPartialView(newEntity, this.defaultViewOptions());
        });
    }
}

export class EntityRepeater extends EntityListBase {
    static key_itemsContainer = "sfItemsContainer";
    static key_repeaterItem = "sfRepeaterItem";
    static key_repeaterItemClass = "sf-repeater-element";

    itemSuffix() {
        return EntityRepeater.key_repeaterItem;
    }


    getItems() {
        return $(this.pf(EntityRepeater.key_itemsContainer) + " > ." + EntityRepeater.key_repeaterItemClass);
    }

    removeEntitySpecific(itemPrefix: string) {
        $("#" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem)).remove(); 
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var fieldSet = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" +
            "<legend>" +
            (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "") +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.moveUp + "</span>") : "") +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.moveDown + "</span>") : "") +
            "</legend>" +
            SF.hiddenInput(SF.compose(itemPrefix, EntityListBase.key_indexes), this.getNextPosIndex()) +
            SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) +
            "<div id='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_entity) + "' class='sf-line-entity'>" +
            "</div>" + //sfEntity
            "</fieldset>"
            );

        $(this.pf(EntityRepeater.key_itemsContainer)).append(fieldSet);

        SF.triggerNewContent(fieldSet); 
    }

    private getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    private getRemoving(itemPrefix) {
        return this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
    }

    private getMovingUp(itemPrefix) {
        return this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
    }

    private getMovingDown(itemPrefix) {
        return this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
    }

    remove_click(): Promise<void> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    removeItem_click(itemPrefix: string): Promise<void> {
        return this.onRemove(itemPrefix).then(result=> {
            if (result)
                this.removeEntity(itemPrefix);
        });
    }

    onCreating(prefix: string): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser().then(type=> {
            if (type == null)
                return null;

            var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type, null, true));

            return Navigator.requestPartialView(newEntity, this.defaultViewOptions());
        });
    }

    find_click(): Promise<void> {
        return this.onFindingMany(this.options.prefix)
            .then(result => 
            {
                if (!result)
                    return;

                Promise.all(result
                    .map((e, i) => ({ entity: e, prefix : this.getNextPrefix(i) }))
                    .map(t => {
                        var promise = t.entity.isLoaded() ? Promise.resolve(<Entities.EntityHtml>t.entity) :
                            Navigator.requestPartialView(new Entities.EntityHtml(t.prefix, t.entity.runtimeInfo), this.defaultViewOptions())

                        return promise.then(ev=> this.addEntity(ev, t.prefix));
                }));
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
        var link = $('#' + SF.compose(itemPrefix, Entities.Keys.link));
        link.text(entityValue.toStr);
        if (this.options.navigate)
            link.attr("href", entityValue.link);
    }

    getLink(itemPrefix?: string): string {
        return $('#' + SF.compose(itemPrefix, Entities.Keys.link)).attr("hef");
    }

    getToString(itemPrefix?: string): string {
        return $('#' + SF.compose(itemPrefix, Entities.Keys.link)).text();
    }

    removeEntitySpecific(itemPrefix: string){
        $("#" + SF.compose(itemPrefix, EntityStrip.key_stripItem)).remove();
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" +
            SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), this.getNextPosIndex()) +
            SF.hiddenInput(SF.compose(itemPrefix, Entities.Keys.runtimeInfo), null) +
            (this.options.navigate ?
            ("<a class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, Entities.Keys.link) + "' href='" + entityValue.link + "' title='" + lang.signum.navigate + "'>" + entityValue.toStr + "</a>") :
            ("<span class='sf-entitStrip-link' id='" + SF.compose(itemPrefix, Entities.Keys.link) + "'>" + entityValue.toStr + "</span>")) +
            "<span class='sf-button-container'>" + (
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "w" : "n") + "' data-text='false'>" + lang.signum.moveUp + "</span>") : "") +
            (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-" + (this.options.vertical ? "e" : "s") + "' data-text='false'>" + lang.signum.moveDown + "</span>") : "") +
            (this.options.view ? ("<a id='" + SF.compose(itemPrefix, "btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this.getView(itemPrefix) + "\" class='sf-line-button sf-view' data-icon='ui-icon-circle-arrow-e' data-text='false'>" + lang.signum.view + "</a>") : "") +
            (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "")) +
            "</span>" +
            "<div id='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_entity) + "' style='display:none'></div>" +
            "</li>"
            );

        $(this.pf(EntityStrip.key_itemsContainer) + " ." + EntityStrip.key_input).before(li);

        SF.triggerNewContent(li);
    }

    private getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    private getRemoving(itemPrefix: string) {
        return this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "');";
    }

    private getView(itemPrefix: string) {
        return this.getRepeaterCall() + ".view_click('" + itemPrefix + "');";
    }

    private getMovingUp(itemPrefix: string) {
        return this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');";
    }

    private getMovingDown(itemPrefix: string) {
        return this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');";
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

