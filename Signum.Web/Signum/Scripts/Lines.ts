/// <reference path="globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")

export interface EntityBaseOptions {
    prefix: string;
    partialViewName?: string;
    template?: string;
    templateToString?: string;

    autoCompleteUrl?: string;

    types: Entities.TypeInfo[]; 

    create?: boolean;
    remove?: boolean;
    find?: boolean;
    view?: boolean;
    navigate?: boolean;
    isEmbedded: boolean;
    isReadonly: boolean;
    rootType?: string;
    propertyRoute?: string;
}

export class EntityBase {
    prefix: string;
    options: EntityBaseOptions;
    element: JQuery;
    hidden: JQuery;
    inputGroup: JQuery;
    shownButton: JQuery;
    autoCompleter: EntityAutocompleter;

    entityChanged: (entityValue: Entities.EntityValue, itemPrefix?: string) => void;
    removing: (prefix: string, event: MouseEvent) => Promise<boolean>;
    creating: (prefix: string, event: MouseEvent) => Promise<Entities.EntityValue>;
    finding: (prefix: string, event: MouseEvent) => Promise<Entities.EntityValue>;
    viewing: (entityHtml: Entities.EntityHtml, event: MouseEvent) => Promise<Entities.EntityValue>;

    constructor(element: JQuery, options: EntityBaseOptions) {
        this.element = element;
        this.element.data("SF-control", this);
        this.options = options;
        this.prefix = this.options.prefix;
        this.hidden =  this.prefix.child("hidden").tryGet();
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

    public ready() {

        this.element.SFControlFullfill(this);
    }

    static key_entity = "sfEntity";

    _create() { //abstract

    }

    runtimeInfoHiddenElement(itemPrefix?: string): JQuery {
        return this.prefix.child(Entities.Keys.runtimeInfo).get();
    }

    containerDiv(itemPrefix?: string) {
        var containerDivId = this.prefix.child(EntityBase.key_entity);
        var result = containerDivId.tryGet(); 
        if (result.length)
            return result;

        return SF.hiddenDiv(containerDivId, "").insertAfter(this.runtimeInfoHiddenElement()); 
    }

    getRuntimeInfo(): Entities.RuntimeInfo
    {
        return Entities.RuntimeInfo.getFromPrefix(this.options.prefix);
    }

    getEntityValue(): Entities.EntityValue {
        var ri = this.getRuntimeInfo();

        if (!ri)
            return null;

        return new Entities.EntityValue(ri, this.getToString());
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {

        var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

        if (runtimeInfo == null)
            return null;

        var div = this.containerDiv();

        var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo, this.getToString());

        result.html = div.children();

        div.html(null);

        return result;
    }

    getOrRequestEntityHtml() : Promise<Entities.EntityHtml>
    {
        var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

        if (runtimeInfo == null)
            return Promise.resolve(null);

        var div = this.containerDiv();

        var result = new Entities.EntityHtml(this.options.prefix, runtimeInfo, this.getToString());

        result.html = div.children();

        if (result.isLoaded())
            return Promise.resolve(result); 

        return Navigator.requestPartialView(result, this.defaultViewOptions(null));
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


        this.containerDiv().html(entityValue == null ? null : (<Entities.EntityHtml>entityValue).html);
        Entities.RuntimeInfo.setFromPrefix(this.options.prefix, entityValue == null ? null : entityValue.runtimeInfo);
        if (entityValue == null) {
            Validator.cleanHasError(this.element);
        }

        this.updateButtonsDisplay();
        this.notifyChanges(true);
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged(entityValue, itemPrefix);
        }
    }

    notifyChanges(setHasChanges: boolean) {
        if (setHasChanges)
            SF.setHasChanges(this.element);

        this.element.attr("changes", (parseInt(this.element.attr("changes")) || 0) + 1);
    }

    remove_click(event: MouseEvent): Promise<string> {
        return this.onRemove(this.options.prefix, event).then(result=> {
            if (result) {
                this.setEntity(null);
                return this.options.prefix;
            }

            return null;
        });
    }

    onRemove(prefix: string, event: MouseEvent): Promise<boolean> {
        if (this.removing != null)
            return this.removing(prefix, event);

        return Promise.resolve(true);
    }

    create_click(event: MouseEvent): Promise<string> {
        return this.onCreating(this.options.prefix, event).then(result => {
            if (result) {
                this.setEntity(result);
                return this.options.prefix;
            }

            this.notifyChanges(false);
            return null;
        });
    }

    typeChooser(filter: (type: Entities.TypeInfo) => boolean): Promise<Entities.TypeInfo> {
        return Navigator.typeChooser(this.options.prefix, this.options.types.filter(filter));
    }

    singleType(): string {
        if (this.options.types.length != 1)
            throw new Error("There are {0} types in {1}".format(this.options.types.length, this.options.prefix));

        return this.options.types[0].name;
    }

    onCreating(prefix: string, event: MouseEvent): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix, event);

        return this.typeChooser(ti => ti.creable).then<Entities.EntityValue> (type=> {
            if (!type)
                return null;

            return type.preConstruct().then(extra => {

                if (!extra)
                    return null;

                if (Navigator.isOpenNewWindow(event) || type.avoidPopup) {
                    if (this.options.navigate)
                        Navigator.navigate(new Entities.RuntimeInfo(type.name, null, true), extra, true);
                    return null;
                }

                var newEntity = this.options.template ? this.getEmbeddedTemplate(prefix) :
                    new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                return Navigator.viewPopup(newEntity, this.defaultViewOptions(extra));
            });
        });
    }

    getEmbeddedTemplate(itemPrefix?: string): Entities.EntityHtml {
        if (!this.options.template)
            throw new Error("no template in " + this.options.prefix);

        var result = new Entities.EntityHtml(this.options.prefix,
            new Entities.RuntimeInfo(this.singleType(), null, true), this.options.templateToString);

        result.loadHtml(this.options.template);

        return result;
    }

    view_click(event: MouseEvent): Promise<string> {
        event.preventDefault();
        event.stopPropagation();

        var entityHtml = this.extractEntityHtml();

        return this.onViewing(entityHtml, event).then(result=> {
            if (result) {
                this.setEntity(result);
                return this.options.prefix;
            }
            else {
                this.setEntity(entityHtml); //previous entity passed by reference
                return null;
            }
        });
    }

    onViewing(entityHtml: Entities.EntityHtml, event: MouseEvent): Promise<Entities.EntityValue> {
        if (this.viewing != null)
            return this.viewing(entityHtml, event);

        var type = this.options.types == null ? null :
            this.options.types.filter(t => t.name == entityHtml.runtimeInfo.type)[0];

        if (Navigator.isOpenNewWindow(event) || type && type.avoidPopup) {
            if (this.options.navigate && !entityHtml.runtimeInfo.isNew)
                Navigator.navigate(entityHtml.runtimeInfo, null, true);
            return null;
        }
        else
            return Navigator.viewPopup(entityHtml, this.defaultViewOptions(null));
    }

    find_click(event: MouseEvent): Promise<string> {
        return this.onFinding(this.options.prefix, event).then(result => {
            if (result) {
                this.setEntity(result);
                return this.options.prefix;
            }

            this.notifyChanges(false);
            return null;
        });
    }



    onFinding(prefix: string, event: MouseEvent): Promise<Entities.EntityValue> {
        if (this.finding != null)
            return this.finding(prefix, event);

        return this.typeChooser(ti => ti.findable).then(type=> {
            if (!type)
                return null;

            return Finder.find({
                webQueryName: type.name,
                prefix: prefix,
            });
        });
    }

    defaultViewOptions(extraJsonData: any): Navigator.ViewPopupOptions {
        return {
            readOnly: this.options.isReadonly,
            partialViewName: this.options.partialViewName,
            validationOptions: {
                rootType: this.options.rootType,
                propertyRoute: this.options.propertyRoute,
            },
            requestExtraJsonData : extraJsonData,
        };
    }

    updateButtonsDisplay() {

        var hasEntity = !!Entities.RuntimeInfo.getFromPrefix(this.options.prefix);

        this.visibleButton("btnCreate", !hasEntity);
        this.visibleButton("btnFind", !hasEntity);
        this.visibleButton("btnView", hasEntity);
        this.visibleButton("btnRemove", hasEntity);

        this.fixInputGroup();
    }

    fixInputGroup() {
        this.inputGroup.toggleClass("input-group", !!this.shownButton.children().length);
    }

    visibleButton(sufix: string, visible: boolean) {

        var element = this.prefix.child(sufix).tryGet();

        if (!element.length)
            return;

        (visible ? this.shownButton : this.hidden).append(element.detach());
    }

    setupAutocomplete($txt : JQuery) {

        var handler : number;
        var auto = $txt.typeahead({
            source: (query, response) => {
                if (handler)
                    clearTimeout(handler);

                handler = setTimeout(() => {
                    this.autoCompleter.getResults(query)
                        .then(entities=> response(entities));
                }, 300);
            },

            sorter: items => items,
            matcher: item => true,
            highlighter: (item: Entities.EntityValue) => $("<div>").append(
                $("<span>")
                    .attr("data-type", item.runtimeInfo.type)
                    .attr("data-id", item.runtimeInfo.id)
                    .text(item.toStr)).html(),

            updater : (val: Entities.EntityValue) => this.onAutocompleteSelected(val)
        });
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        throw new Error("onAutocompleteSelected is abstract");
    }

    getNiceName(typeName: string) : string {

        var t = this.options.types.filter(a=> a.name == typeName);

        return t.length ? t[0].niceName : typeName;
    }
}

export interface EntityAutocompleter {
    getResults(term: string): Promise<Entities.EntityValue[]>;
}

export interface AutocompleteResult {
    id: string;
    text: string;
    type: string;
    link: string;
}

export class AjaxEntityAutocompleter implements EntityAutocompleter {

    controllerUrl: string;

    getData: (term: string) => FormObject;

    constructor(controllerUrl: string, getData: (term: string) => FormObject) {
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
                success: (data: AutocompleteResult[]) => {
                    this.lastXhr = null;
                    var entities = data.map(item=> new Entities.EntityValue(new Entities.RuntimeInfo(item.type, item.id, false), item.text));
                    resolve(entities);
                }
            });
        });
    }

}

export class EntityLine extends EntityBase {

    _create() {
        var $txt = this.prefix.child(Entities.Keys.toStr).tryGet().filter(".sf-entity-autocomplete");
        if ($txt.length) {
            this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete,
                term => <any>({ types: this.options.types.map(t=> t.name).join(","), l: 5, q: term }));

            this.setupAutocomplete($txt);
        }
    }

    getLink(itemPrefix?: string): string {
        return this.prefix.child(Entities.Keys.link).get().attr("href");
    }

    getToString(itemPrefix?: string): string {
        return this.prefix.child(Entities.Keys.link).get().text();
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        var link = this.prefix.child(Entities.Keys.link).get();
        link.text(entityValue == null ? null : entityValue.toStr);
        this.prefix.child(Entities.Keys.toStr).get().val('');
        
        var linkParent = link.parent(".form-control-static");
        this.visible(linkParent.length ? linkParent : link, entityValue != null);
        this.visible(this.prefix.get().find("ul.typeahead.dropdown-menu"), entityValue == null);


        var toStr = this.prefix.child(Entities.Keys.toStr).tryGet();
        if (toStr != null && toStr.is(":focus")) {
            var tabables = toStr.closest("form").find("*[tabindex != '-1']:visible");
            var index = tabables.index(toStr);
            tabables.eq(index + 1).focus(); // Is there a better way? 
        }
        this.visible(toStr, entityValue == null);
    }

    visible(element : JQuery, visible: boolean) {
        if (!element.length)
            return;

        if (visible)
            this.shownButton.before(element.detach());
        else
            this.hidden.append(element.detach());
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        this.setEntity(entityValue);
    }
}

export class EntityCombo extends EntityBase {

    static key_combo = "sfCombo";
    static key_tostr = "sfToStr";

    combo() {
        return this.prefix.child(EntityCombo.key_combo).get();
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

        var toStr = this.prefix.child(EntityCombo.key_tostr).tryGet();

        if (toStr.length)
            return toStr.text();

        return this.combo().children("option[value='" + this.combo().val() + "']").text();
    }

    combo_selected() {
        var val = this.combo().val();

        var ri = Entities.RuntimeInfo.fromKey(val);

        this.setEntity(ri == null ? null : new Entities.EntityValue(ri, this.getToString()));
    }
}

export class EntityDetail extends EntityBase {

    options: EntityBaseOptions;

    constructor(element: JQuery, options: EntityBaseOptions) {
        super(element, options);
    }

    containerDiv(itemPrefix?: string) {
        return this.prefix.child("sfDetail").get();
    }

    fixInputGroup() {
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        if (entityValue == null)
            return;

        if (!entityValue.isLoaded())
            throw new Error("EntityDetail requires a loaded Entities.EntityHtml, consider calling Navigator.loadPartialView");
    }

    onCreating(prefix: string, event: MouseEvent): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix, event);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser(t => t.creable).then<Entities.EntityValue>(type=> {
            if (!type)
                return null;

            return type.preConstruct().then(args=> {
                if (!args)
                    return null;

                var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                return Navigator.requestPartialView(newEntity, this.defaultViewOptions(args));
            });
        });
    }

    find_click(event: MouseEvent): Promise<string> {
        return this.onFinding(this.options.prefix, event).then(result => {
            if (result == null) {
                this.notifyChanges(false);
                return null;
            }

            if (result.isLoaded())
                return Promise.resolve(<Entities.EntityHtml>result);

            return Navigator.requestPartialView(new Entities.EntityHtml(this.options.prefix, result.runtimeInfo), this.defaultViewOptions(null));
        }).then(result => {
                if (result) {
                    this.setEntity(result);
                    return this.options.prefix;
                }

                return null;
            });
    }
}



export interface EntityListBaseOptions extends EntityBaseOptions {
    maxElements?: number;
    remove?: boolean;
    reorder?: boolean;
}

export class EntityListBase extends EntityBase {
    static key_index = "sfIndex";
    static key_rowId = "sfRowId";

    options: EntityListBaseOptions;
    finding: (prefix: string, event: MouseEvent) => Promise<Entities.EntityValue>;  // DEPRECATED!
    findingMany: (prefix: string, event: MouseEvent) => Promise<Entities.EntityValue[]>;

    constructor(element: JQuery, options: EntityListBaseOptions) {
        super(element, options);
    }

    runtimeInfo(itemPrefix?: string): JQuery {
        return itemPrefix.child(Entities.Keys.runtimeInfo).get();
    }

    containerDiv(itemPrefix?: string): JQuery {
        var containerDivId = itemPrefix.child(EntityList.key_entity);

        var result = containerDivId.tryGet();

        if (result.length)
            return result;

        return SF.hiddenDiv(containerDivId, "").insertAfter(this.runtimeInfo(itemPrefix));
    }

    getEmbeddedTemplate(itemPrefix?: string) {
        if (!this.options.template)
            throw new Error("no template in " + this.options.prefix);

        var result = new Entities.EntityHtml(itemPrefix,
            new Entities.RuntimeInfo(this.singleType(), null, true), this.options.templateToString);

        var replaced = this.options.template.replace(new RegExp(this.options.prefix.child("0"), "gi"), itemPrefix)

        result.loadHtml(replaced);

        return result;
    }

    extractEntityHtml(itemPrefix?: string): Entities.EntityHtml {
        var runtimeInfo = Entities.RuntimeInfo.getFromPrefix(itemPrefix);

        var div = this.containerDiv(itemPrefix);

        var result = new Entities.EntityHtml(itemPrefix, runtimeInfo, this.getToString(itemPrefix));

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
            this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html);

        Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        this.notifyChanges(true);
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged(entityValue, itemPrefix);
        }
    }

    create_click(event: MouseEvent): Promise<string> {
        var itemPrefix = this.reserveNextPrefix();
        return this.onCreating(itemPrefix, event).then(entity => {
            if (entity) {
                this.addEntity(entity, itemPrefix);
                return itemPrefix;
            }

            this.notifyChanges(false);
            return null;
        }).then(
            prefix => { this.freeReservedPrefix(itemPrefix); return prefix; },
            (error): any => { this.freeReservedPrefix(itemPrefix); throw error; });
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
            this.containerDiv(itemPrefix).html((<Entities.EntityHtml>entityValue).html);
        Entities.RuntimeInfo.setFromPrefix(itemPrefix, entityValue.runtimeInfo);

        this.updateButtonsDisplay();
        this.notifyChanges(true);
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged(entityValue, itemPrefix);
        }
    }

    removeEntitySpecific(itemPrefix: string) {
        //virtual
    }

    removeEntity(itemPrefix: string) {
        this.removeEntitySpecific(itemPrefix);

        this.updateButtonsDisplay();
        this.notifyChanges(true);
        if (!SF.isEmpty(this.entityChanged)) {
            this.entityChanged(null, itemPrefix);
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

    reservedPrefixes: string[] = []; 

    reserveNextPrefix(): string {

        var currentPrefixes = this.getItems().toArray().map((e: HTMLElement) => e.id.before("_" + this.itemSuffix()));

        for (var i = 0; ; i++)
        {
            var newPrefix = this.options.prefix + "_" + i;

            if (this.reservedPrefixes.indexOf(newPrefix) == -1 &&
                currentPrefixes.indexOf(newPrefix) == -1) {

                this.reservedPrefixes.push(newPrefix);

                return newPrefix;
            }
        }
    }

    freeReservedPrefix(itemPrefix: string): void {
        var index = this.reservedPrefixes.indexOf(itemPrefix);
        if (index == -1)
            throw Error("itemPrefix not reserved: " + itemPrefix);

        this.reservedPrefixes.splice(index, 1);
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
        return (this.getLastPosIndex() + 1).toString();
    }

    canAddItems() {
        return SF.isEmpty(this.options.maxElements) || this.getItems().length < this.options.maxElements;
    }

    find_click(event: MouseEvent): Promise<string> {

        var prefixes = [];

        return this.onFindingMany(this.options.prefix, event).then(result => {
            if (result) {

                result.forEach(ev=> {
                    var pr = this.reserveNextPrefix();
                    prefixes.push(pr);
                    this.addEntity(ev, pr);
                });

                return prefixes.join(",");
            }

            this.notifyChanges(false);
            return null;
        }).then(
            prefix => { prefixes.forEach(this.freeReservedPrefix); return prefix; },
            (error): any => { prefixes.forEach(this.freeReservedPrefix); throw error; });
    }

    onFinding(prefix: string, event: MouseEvent): Promise<Entities.EntityValue> {
        throw new Error("onFinding is deprecated in EntityListBase");
    }

    onFindingMany(prefix: string, event: MouseEvent): Promise<Entities.EntityValue[]> {
        if (this.findingMany != null)
            return this.findingMany(prefix, event);

        return this.typeChooser(t => t.findable).then(type=> {
            if (type == null)
                return null;

            return Finder.findMany({
                webQueryName: type.name,
                prefix: prefix,
            });
        });
    }

    moveUp(itemPrefix: string, event: MouseEvent) {

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
    }

    moveDown(itemPrefix: string, event: MouseEvent) {

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
    }

    getPosIndex(itemPrefix: string) {
        return parseInt(itemPrefix.child(EntityListBase.key_index).get().val());
    }

    setPosIndex(itemPrefix: string, newIndex: number) {
        var $indexes = itemPrefix.child(EntityListBase.key_index).get().val(newIndex.toString());
    }
}

export class EntityList extends EntityListBase {

    static key_list = "sfList";

    _create() {
        var list = this.prefix.child(EntityList.key_list).get();

        list.change(() => this.selection_Changed());

        SF.onVisible(list).then(() => {
            if (list.height() < this.shownButton.height())
                list.css("min-height", this.shownButton.height());
        });

        this.selection_Changed();
    }

    selection_Changed() {
        this.updateButtonsDisplay();
    }

    itemSuffix() {
        return Entities.Keys.toStr;
    }

    updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
        itemPrefix.child(Entities.Keys.toStr).get().html(newToStr);
    }

    selectedItemPrefix(): string {
        var $items = this.getItems().filter(":selected");
        if ($items.length == 0) {
            return null;
        }

        var nameSelected = $items[0].id;
        return nameSelected.before("_" + this.itemSuffix());
    }

    getItems(): JQuery {
        return this.prefix.child(EntityList.key_list).get().children("option");
    }

    view_click(event: MouseEvent): Promise<string> {
        var selectedItemPrefix = this.selectedItemPrefix();

        var entityHtml = this.extractEntityHtml(selectedItemPrefix);

        return this.onViewing(entityHtml, event).then(result=> {
            if (result) {
                this.setEntity(result, selectedItemPrefix);
                return selectedItemPrefix;
            }
            else {
                this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
                return null;
            }
        });
    }



    updateButtonsDisplay() {
        var canAdd = this.canAddItems();
        this.visibleButton("btnCreate", canAdd);
        this.visibleButton("btnFind", canAdd);

        var hasSelected = this.selectedItemPrefix() != null;
        this.visibleButton("btnView", hasSelected);
        this.visibleButton("btnRemove", hasSelected);
        this.visibleButton("btnUp", hasSelected);
        this.visibleButton("btnDown", hasSelected);

        this.fixInputGroup();
    }

    getToString(itemPrefix?: string): string {
        return itemPrefix.child(Entities.Keys.toStr).get().text();
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        itemPrefix.child(Entities.Keys.toStr).get().text(entityValue.toStr);
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {

        this.inputGroup.before(SF.hiddenInput(itemPrefix.child(EntityList.key_index), this.getNextPosIndex()));
        this.inputGroup.before(SF.hiddenInput(itemPrefix.child(EntityList.key_rowId), ""));
        this.inputGroup.before(SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));
        this.inputGroup.before(SF.hiddenDiv(itemPrefix.child(EntityList.key_entity), ""));

        var select = this.prefix.child(EntityList.key_list).get();
        select.children('option').attr('selected', ''); //Fix for Firefox: Set selected after retrieving the html of the select

        var ri = entityValue.runtimeInfo;

        $("<option/>")
            .attr("id", itemPrefix.child(Entities.Keys.toStr))
            .attr("value", "")
            .attr('selected', 'selected')
            .text(entityValue.toStr)
            .attr('title', this.options.isEmbedded ? null : (this.getNiceName(ri.type) + (ri.id ? " " + ri.id : null)))
            .appendTo(select);
    }

    remove_click(event: MouseEvent): Promise<string> {
        var selectedItemPrefix = this.selectedItemPrefix();
        return this.onRemove(selectedItemPrefix, event).then(result=> {
            if (result) {
                var next = this.getItems().filter(":selected").next();
                if (next.length == 0)
                    next = this.getItems().filter(":selected").prev();

                this.removeEntity(selectedItemPrefix);

                next.attr("selected", "selected");
                this.selection_Changed();

                return selectedItemPrefix;
            }

            return null;
        });
    }

    removeEntitySpecific(itemPrefix: string) {
        itemPrefix.child(Entities.Keys.runtimeInfo).get().remove();
        itemPrefix.child(Entities.Keys.toStr).get().remove();
        itemPrefix.child(EntityList.key_entity).tryGet().remove();
        itemPrefix.child(EntityList.key_index).tryGet().remove();
        itemPrefix.child(EntityList.key_rowId).tryGet().remove();
    }

    moveUp_click(event: MouseEvent) {
        this.moveUp(this.selectedItemPrefix(), event);
    }

    moveDown_click(event: MouseEvent) {
        this.moveDown(this.selectedItemPrefix(), event);
    }
}

export interface EntityListDetailOptions extends EntityListBaseOptions {
    detailDiv: string;
}

export class EntityListDetail extends EntityList {

    options: EntityListDetailOptions;

    selection_Changed() {
        super.selection_Changed();
        this.stageCurrentSelected();
    }

    remove_click(event: MouseEvent): Promise<string>  {
        return super.remove_click(event).then(result => { this.stageCurrentSelected(); return result })
    }

    create_click(event: MouseEvent) {
        return super.create_click(event).then(result => { this.stageCurrentSelected(); return result; });
    }

    find_click(event: MouseEvent) {
        return super.find_click(event).then(result => { this.stageCurrentSelected(); return result; })
    }

    stageCurrentSelected() {
        var selPrefix = this.selectedItemPrefix();

        var detailDiv = $("#" + this.options.detailDiv)

        var currentChildren = detailDiv.children();
        var currentPrefix = currentChildren.length ? currentChildren[0].id.parent(EntityListDetail.key_entity) : null;
        if (currentPrefix == selPrefix) {
            return;
        }

        var hideCurrent = () => {

            if (currentPrefix) {
                currentChildren.hide();
                this.runtimeInfo(currentPrefix).after(currentChildren);
            }
        }; 

        if (selPrefix) {
            var selContainer = this.containerDiv(selPrefix);

            var promise = selContainer.children().length ? Promise.resolve<void>(null) :
                Navigator.requestPartialView(new Entities.EntityHtml(selPrefix, Entities.RuntimeInfo.getFromPrefix(selPrefix), null), this.defaultViewOptions(null))
                    .then<void>(e => { selContainer.html(e.html); });

            promise.then(() =>
            {
                detailDiv.append(selContainer);
                selContainer.show();

                if (currentPrefix) {
                    currentChildren.hide();
                    this.runtimeInfo(currentPrefix).after(currentChildren);
                }
            }); 

        } if (currentPrefix) {
            currentChildren.hide();
            this.runtimeInfo(currentPrefix).after(currentChildren);
        }
    }

    onCreating(prefix: string, event: MouseEvent): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix, event);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser(t => t.creable).then<Entities.EntityValue>(type=> {
            if (type == null)
                return null;

            return type.preConstruct().then(args=> {
                if (!args)
                    return null;

                var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                return Navigator.requestPartialView(newEntity, this.defaultViewOptions(args));
            }); 
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

    fixInputGroup() {
    }

    getItems() {
        return this.prefix.child(EntityRepeater.key_itemsContainer).get().children("." + EntityRepeater.key_repeaterItemClass);
    }

    removeEntitySpecific(itemPrefix: string) {
        itemPrefix.child(EntityRepeater.key_repeaterItem).get().remove();
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var fieldSet = $("<fieldset id='" + itemPrefix.child(EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" +
            "<legend><div class='item-group'>" +
            (this.options.remove ? ("<a id='" + itemPrefix.child("btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "', event);" + "\" class='sf-line-button sf-remove'><span class='glyphicon glyphicon-remove'></span></a>") : "") +
            (this.options.reorder ? ("<a id='" + itemPrefix.child("btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "', event);" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-up'></span></span></a>") : "") +
            (this.options.reorder ? ("<a id='" + itemPrefix.child("btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "', event);" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-down'></span></span></a>") : "") +
            "</div></legend>" +
            SF.hiddenInput(itemPrefix.child(EntityListBase.key_index), this.getNextPosIndex()) +
            SF.hiddenInput(itemPrefix.child(EntityListBase.key_rowId), "") +
            SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), null) +
            "<div id='" + itemPrefix.child(EntityRepeater.key_entity) + "' class='sf-line-entity'>" +
            "</div>" + //sfEntity
            "</fieldset>"
            );

        this.options.prefix.child(EntityRepeater.key_itemsContainer).get().append(fieldSet);
    }

    getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    remove_click(): Promise<string> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    removeItem_click(itemPrefix: string, event: MouseEvent): Promise<string> {
        return this.onRemove(itemPrefix, event).then(result=> {
            if (result) {
                this.removeEntity(itemPrefix);
                return itemPrefix;
            }
            return null;
        });
    }

    onCreating(prefix: string, event: MouseEvent): Promise<Entities.EntityValue> {
        if (this.creating != null)
            return this.creating(prefix, event);

        if (this.options.template)
            return Promise.resolve(this.getEmbeddedTemplate(prefix));

        return this.typeChooser(t => t.creable).then<Entities.EntityValue>(type=> {
            if (type == null)
                return null;

            return type.preConstruct().then(args=> {
                if (!args)
                    return null;

                var newEntity = new Entities.EntityHtml(prefix, new Entities.RuntimeInfo(type.name, null, true), lang.signum.newEntity);

                return Navigator.requestPartialView(newEntity, this.defaultViewOptions(args));
            }); 
        });
    }

    find_click(event: MouseEvent): Promise<string> {
        return this.onFindingMany(this.options.prefix, event)
            .then(result => {
                if (!result) {
                    this.notifyChanges(false);
                    return;
                }

                return Promise.all(result
                    .map(e => {
                        var itemPrefix = this.reserveNextPrefix();

                        var promise = e.isLoaded() ? Promise.resolve(<Entities.EntityHtml>e) :
                            Navigator.requestPartialView(new Entities.EntityHtml(itemPrefix, e.runtimeInfo), this.defaultViewOptions(null))

                        return promise.then(
                            ev=> { this.addEntity(ev, itemPrefix); this.freeReservedPrefix(itemPrefix); return itemPrefix; },
                            error => { this.freeReservedPrefix(itemPrefix); return null; });
                    }))
                    .then(result => result.join(","));
            });
    }

    updateButtonsDisplay() {
        var canAdd = this.canAddItems();

        this.prefix.child("btnCreate").tryGet().toggle(canAdd);
        this.prefix.child("btnFind").tryGet().toggle(canAdd);
    }
}

export class EntityTabRepeater extends EntityRepeater {
    static key_tabsContainer = "sfTabsContainer";

    _create() {
        super._create();
    }

    itemSuffix() {
        return EntityTabRepeater.key_repeaterItem;
    }


    getItems() {
        return this.prefix.child(EntityTabRepeater.key_itemsContainer).get().children("." + EntityTabRepeater.key_repeaterItemClass);
    }

    removeEntitySpecific(itemPrefix: string) {
        var li =  itemPrefix.child(EntityTabRepeater.key_repeaterItem).get(); 

        if (li.next().length)
            li.next().find("a").tab("show");
        else if (li.prev().length)
            li.prev().find("a").tab("show");

        li.remove();
        itemPrefix.child(EntityBase.key_entity).get().remove();
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var header = $("<li id='" + itemPrefix.child(EntityTabRepeater.key_repeaterItem) + "' class='" + EntityTabRepeater.key_repeaterItemClass + "'>" +
            "<a data-toggle='tab' href='#" + itemPrefix.child(EntityBase.key_entity) + "' >" +
            "<span>" + entityValue.toStr + "</span>" +
            SF.hiddenInput(itemPrefix.child(EntityListBase.key_index), this.getNextPosIndex()) +
            SF.hiddenInput(itemPrefix.child(EntityListBase.key_rowId), "") +
            SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), null) +
            (this.options.reorder ? ("<span id='" + itemPrefix.child("btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "', event);" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-left'></span></span>") : "") +
            (this.options.reorder ? ("<span id='" + itemPrefix.child("btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "', event);" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-right'></span></span>") : "") +
            (this.options.remove ? ("<span id='" + itemPrefix.child("btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "', event);" + "\" class='sf-line-button sf-remove' ><span class='glyphicon glyphicon-remove'></span></span>") : "") +
            "</a>" +
            "</li>"
            );

        this.prefix.child(EntityTabRepeater.key_itemsContainer).get().append(header);

        var entity = $("<div id='" + itemPrefix.child(EntityTabRepeater.key_entity) + "' class='tab-pane'>" +
            "</div>");

        this.prefix.child(EntityTabRepeater.key_tabsContainer).get().append(entity);

        header.find("a").tab("show");
    }

    getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }
}

export interface EntityStripOptions extends EntityBaseOptions {
    maxElements?: number;
    
    vertical?: boolean;
    reorder?: boolean;

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

    fixInputGroup() {
    }

    _create() {
        var $txt = this.prefix.child(Entities.Keys.toStr).tryGet().filter(".sf-entity-autocomplete");
        if ($txt.length) {
            this.autoCompleter = new AjaxEntityAutocompleter(this.options.autoCompleteUrl || SF.Urls.autocomplete,
                term => <any>({ types: this.options.types.map(t=> t.name).join(","), l: 5, q: term }));

            this.setupAutocomplete($txt);
        }
    }

    itemSuffix() {
        return EntityStrip.key_stripItem;
    }

    getItems() {
        return this.prefix.child(EntityStrip.key_itemsContainer).get().children("." + EntityStrip.key_stripItemClass);
    }

    setEntitySpecific(entityValue: Entities.EntityValue, itemPrefix?: string) {
        var link = itemPrefix.child(Entities.Keys.link).get();
        link.text(entityValue.toStr);
    }

    getLink(itemPrefix?: string): string {
        return itemPrefix.child(Entities.Keys.link).get().attr("hef");
    }

    getToString(itemPrefix?: string): string {
        return itemPrefix.child(Entities.Keys.link).get().text();
    }

    removeEntitySpecific(itemPrefix: string) {
        itemPrefix.child(EntityStrip.key_stripItem).get().remove();
    }

    addEntitySpecific(entityValue: Entities.EntityValue, itemPrefix: string) {
        var li = $("<li id='" + itemPrefix.child(EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + " input-group'>" +
            (this.options.navigate ?
            ("<a class='sf-entitStrip-link' id='" + itemPrefix.child(Entities.Keys.link) + "' onclick=\"" + this.getRepeaterCall() + ".viewItem_click('" + itemPrefix + "', event);" + "\" title='" + lang.signum.navigate + "'>" + entityValue.toStr + "</a>") :
            ("<span class='sf-entitStrip-link' id='" + itemPrefix.child(Entities.Keys.link) + "'>" + entityValue.toStr + "</span>")) +
            SF.hiddenInput(itemPrefix.child(EntityStrip.key_index), this.getNextPosIndex()) +
            SF.hiddenInput(itemPrefix.child(EntityStrip.key_rowId), "") +
            SF.hiddenInput(itemPrefix.child(Entities.Keys.runtimeInfo), null) +
            "<div id='" + itemPrefix.child(EntityStrip.key_entity) + "' style='display:none'></div>" +
            "<span>" + (
            (this.options.reorder ? ("<a id='" + itemPrefix.child("btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this.getRepeaterCall() + ".moveUp('" + itemPrefix + "');" + "\" class='sf-line-button move-up'><span class='glyphicon glyphicon-chevron-" + (this.options.vertical ? "up" : "left") + "'></span></a>") : "") +
            (this.options.reorder ? ("<a id='" + itemPrefix.child("btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this.getRepeaterCall() + ".moveDown('" + itemPrefix + "');" + "\" class='sf-line-button move-down'><span class='glyphicon glyphicon-chevron-" + (this.options.vertical ? "down" : "right") + "'></span></a>") : "") +
            (this.options.view ? ("<a id='" + itemPrefix.child("btnView") + "' title='" + lang.signum.view + "' onclick=\"" + this.getRepeaterCall() + ".viewItem_click('" + itemPrefix + "', event);" + "\" class='sf-line-button sf-view'><span class='glyphicon glyphicon-arrow-right'></span></a>") : "") +
            (this.options.remove ? ("<a id='" + itemPrefix.child("btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this.getRepeaterCall() + ".removeItem_click('" + itemPrefix + "', event);" + "\" class='sf-line-button sf-remove'><span class='glyphicon glyphicon-remove'></span></a>") : "")) +
            "</span>" +
            "</li>" 
            );

        this.prefix.child(EntityStrip.key_itemsContainer).get().children(" ." + EntityStrip.key_input).before(li);

    }

    private getRepeaterCall() {
        return "$('#" + this.options.prefix + "').data('SF-control')";
    }

    remove_click(event: MouseEvent): Promise<string> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    removeItem_click(itemPrefix: string, event: MouseEvent): Promise<string> {
        return this.onRemove(itemPrefix, event).then(result=> {
            if (result) {
                this.removeEntity(itemPrefix);
                return itemPrefix;
            }

            return null;
        });
    }

    view_click(event: MouseEvent): Promise<string> { throw new Error("remove_click is deprecated in EntityRepeater"); }

    viewItem_click(itemPrefix: string, event: MouseEvent): Promise<string> {
        event.preventDefault();
        event.stopPropagation();

        var entityHtml = this.extractEntityHtml(itemPrefix);

        return this.onViewing(entityHtml, event).then(result=> {
            if (result) {
                this.setEntity(result, itemPrefix);
                return itemPrefix;
            }
            else {
                this.setEntity(entityHtml, itemPrefix); //previous entity passed by reference
                return null;
            }
        });
    }

    updateButtonsDisplay() {
        var canAdd = this.canAddItems();

        this.prefix.child("btnCreate").tryGet().toggle(canAdd);
        this.prefix.child("btnFind").tryGet().toggle(canAdd);
        this.prefix.child("sfToStr").tryGet().toggle(canAdd);
    }

    onAutocompleteSelected(entityValue: Entities.EntityValue) {
        var prefix = this.reserveNextPrefix();
        this.addEntity(entityValue, prefix);
        this.prefix.child(Entities.Keys.toStr).get().val('');
        this.freeReservedPrefix(prefix);
    }
}

export class EntityListCheckbox extends EntityListBase {

}

