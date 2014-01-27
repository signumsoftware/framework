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
    }

    export class EntityBase {
        options: EntityBaseOptions;
        element: JQuery;
        autoCompleter: EntityAutoCompleter;

        entityChanged: () => any;
        removing: (prefix: string) => Promise<boolean>;
        creating: (prefix: string) => Promise<EntityValue>;
        finding: (prefix: string) => Promise<EntityValue>; 
        viewing: (entityHtml: EntityHtml) => Promise<EntityValue>; 

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
            var $txt = $(this.pf(SF.Keys.toStr) + ".sf-entity-autocomplete");
            if ($txt.length > 0) {
                var data = $txt.data();

                this.autoCompleter = new AjaxEntityAutoCompleter(SF.Urls.autocomplete,
                    term => ({ types: this.staticInfo().types(), l: 5, q: term }));

                this.entityAutocomplete($txt);
            }
        }


        runtimeInfo(itemPrefix?: string) {
            return new SF.RuntimeInfoElement(this.options.prefix);
        }

        staticInfo() {
            return new SF.StaticInfo(this.options.prefix);
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

        extractEntityHtml(itemPrefix?: string): EntityHtml {
            
            var runtimeInfo = this.runtimeInfo().value();

            if (runtimeInfo == null)
                return null;

            var div = this.containerDiv(); 

            var result = new EntityHtml(this.options.prefix, runtimeInfo, null, null);

            result.html = div.children();

            div.html(null);

            return result;
        }


        setEntitySpecific(entityValue: EntityValue, itemPrefix?: string) {
            //virtual function
        }

        setEntity(entityValue: EntityValue, itemPrefix?: string) {


            this.setEntitySpecific(entityValue)

            if (entityValue) {
                entityValue.assertPrefixAndType(this.options.prefix, this.staticInfo());
            }

            SF.triggerNewContent(this.containerDiv().html(entityValue == null ? null : (<EntityHtml>entityValue).html));
            this.runtimeInfo().setValue(entityValue == null ? null : entityValue.runtimeInfo);

            if (entityValue == null) {
                Validation.cleanError($(this.pf(SF.Keys.toStr)).val(""));
                Validation.cleanError($(this.pf(SF.Keys.link)).val("").html(""));
            }
            
            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        }

        remove_click() {
            this.onRemove(this.options.prefix).then(result=> {
                if (result)
                    this.setEntity(null);
            });
        }

        onRemove(prefix : string) : Promise<boolean>{
            if (this.removing != null)
                return this.removing(prefix);

            return Promise.resolve(true);
        }

        create_click() {
            this.onCreating(this.options.prefix).then(result => {
                if (result)
                    this.setEntity(result);
            });
        }

        onCreating(prefix: string): Promise<EntityValue>
        {
            if (this.creating != null)
                return this.creating(prefix); 

            SF.ViewNavigator.typeChooser(this.staticInfo()).then(type=> {
                if (type == null)
                    return null;

                var newEntity = new EntityHtml(this.options.prefix, new RuntimeInfoValue(type, null));

                var template = this.getEmbeddedTemplate();
                if (!SF.isEmpty(template))
                    newEntity.html = $(template);

                return ViewNavigator.viewPopup(newEntity, this.defaultViewOptions());
            });
        }

        getEmbeddedTemplate(itemPrefix?: string) {
            return window[SF.compose(this.options.prefix, "sfTemplate")];
        }

        view_click() {
            var entityHtml = this.extractEntityHtml();

            this.onViewing(entityHtml).then(result=> {
                if (result)
                    this.setEntity(result);
                else
                    this.setEntity(entityHtml); //previous entity passed by reference
            }); 
        }

        onViewing(entityHtml: EntityHtml): Promise<EntityValue>
        {
            if (this.viewing != null)
                return this.viewing(entityHtml); 

            return ViewNavigator.viewPopup(entityHtml, this.defaultViewOptions());
        }

        find_click() {
            this.onFinding(this.options.prefix).then(result => {
                this.setEntity(result);
            }); 
        }

        onFinding(prefix: string): Promise<EntityValue> {
            if (this.finding != null)
                return this.finding(this.options.prefix);

            return SF.ViewNavigator.typeChooser(this.staticInfo()).then(type=> {
                if (type == null)
                    return null;

                return FindNavigator.find({
                    webQueryName: type,
                    prefix: prefix,
                });
            });
        }

        defaultViewOptions() : ViewNavigator.ViewPopupOptions {
            return {
                readOnly: this.staticInfo().isReadOnly(),
                partialViewName : this.options.partialViewName
            };
        }

        updateButtonsDisplay() {

            var hasEntity = !!this.runtimeInfo().value;

            $(this.pf("btnCreate")).toggle(!hasEntity);
            $(this.pf("btnFind")).toggle(!hasEntity);
            $(this.pf("btnRemove")).toggle(hasEntity);
            $(this.pf("btnView")).toggle(hasEntity);
            $(this.pf(SF.Keys.link)).toggle(hasEntity);
            $(this.pf(SF.Keys.toStr)).toggle(!hasEntity);
        }

        entityAutocomplete($txt) {
           
            var auto = $txt.autocomplete({
                delay: 200,
                source: (request, response) => {
                    this.autoCompleter.getResults(request.term).then(entities=>
                    {
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
                var val = <EntityValue>item.value 

                return $("<li>")
                    .attr("data-type", val.runtimeInfo.type)
                    .attr("data-id", val.runtimeInfo.id)
                    .append($("<a>").text(item.label))
                    .appendTo(ul);
            };
        }

        onAutocompleteSelected(entityValue: EntityValue) {
            throw new Error("onAutocompleteSelected is abstract");
        }
    }

    export interface EntityAutoCompleter
    {
        getResults(term: string): Promise<EntityValue[]>;
    }

    export class AjaxEntityAutoCompleter implements EntityAutoCompleter {

        controllerUrl: string;

        getData: (term: string) => any; 

        constructor(controllerUrl: string, getData: (term: string) => any) {
            this.controllerUrl = controllerUrl;
            this.getData = getData;
        }

        lastXhr : JQueryXHR; //To avoid previous requests results to be shown

        getResults(term: string): Promise<EntityValue[]>
        {
            if (this.lastXhr)
                this.lastXhr.abort();

            return new Promise<EntityValue[]>((resolve, failure) => {
                this.lastXhr = $.ajax({
                    url: this.controllerUrl,
                    data: this.getData(term),
                    success: function (data : any[]) {
                        this.lastXhr = null;
                        resolve(data.map(item=> new EntityValue(new RuntimeInfoValue(item.type, parseInt(item.id)), item.toStr, item.link)));
                    }
                });
            }); 
        }
 
    }

    once("SF-entityLine", () =>
        $.fn.entityLine = function (opt: EntityBaseOptions) {
            return new EntityLine(this, opt);
        });

    export class EntityLine extends EntityBase {

        setEntitySpecific(entityValue: EntityValue) {
            var link = $(this.pf(SF.Keys.link));
            link.text(entityValue == null? null: entityValue.toStr);
            if (link.filter('a').length !== 0)
                link.attr('href', entityValue == null ? null : entityValue.link);
            $(this.pf(SF.Keys.toStr)).val('');
        }

        onAutocompleteSelected(entityValue: EntityValue) {
            this.setEntity(entityValue);
        }
    }

    once("SF-entityCombo", () =>
        $.fn.entityCombo = function (opt: EntityBaseOptions) {
            return new EntityCombo(this, opt);
        });

    export class EntityCombo extends EntityBase {

        static key_combo = "sfCombo";

        combo() {
            return $(this.pf(EntityCombo.key_combo));
        }

        setEntitySpecific(entityValue: EntityValue) {
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

            var ri = RuntimeInfoValue.fromKey(val);

            this.setEntity(ri == null ? null : new EntityValue(ri));
        }
    }

    export interface EntityDetailOptions extends EntityBaseOptions {
        detailDiv: string;
    }

    once("SF-entityLineDetail", () =>
        $.fn.entityLineDetail = function (opt: EntityDetailOptions) {
            return new EntityLineDetail(this, opt);
        });

    export class EntityLineDetail extends EntityBase {

        options: EntityDetailOptions;

        constructor(element: JQuery, options: EntityDetailOptions) {
            super(element, options);
        }

        containerDiv(itemPrefix?: string) {
            return $("#" + this.options.detailDiv);
        }

        setEntitySpecific(entityValue: EntityValue) {
            if (entityValue == null)
                return;

            if (!entityValue.isLoaded())
                throw new Error("EntityLineDetail requires a loaded EntityHtml, consider calling ViewNavigator.loadPartialView"); 
        }

        onCreating(prefix: string): Promise<EntityValue> {
            if (this.creating != null)
                return this.creating(prefix);

            SF.ViewNavigator.typeChooser(this.staticInfo()).then(type=> {
                if (type == null)
                    return null;

                var newEntity = new EntityHtml(this.options.prefix, new RuntimeInfoValue(type, null));

                var template = this.getEmbeddedTemplate();
                if (!SF.isEmpty(template)) {
                    newEntity.html = $(template);
                    return Promise.resolve(newEntity);
                }

                return ViewNavigator.loadPartialView(newEntity, this.defaultViewOptions());
            });
        }

        onFinding(prefix: string): Promise<EntityValue> {
            return super.onFinding(prefix).then(entity => {
                if (entity == null)
                    return null;

                if ((<EntityHtml>entity).html != null)
                    return Promise.resolve(entity);

                return ViewNavigator.loadPartialView(new EntityHtml(this.options.prefix, entity.runtimeInfo), this.defaultViewOptions());
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
        finding: (prefix: string) => Promise<EntityValue>;  // DEPRECATED!
        findingMany: (prefix: string) => Promise<EntityValue[]>;

        constructor(element: JQuery, options: EntityListBaseOptions) {
            super(element, options);
        }

        runtimeInfo(itemPrefix?: string) {
            return new SF.RuntimeInfoElement(itemPrefix);
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

        extractEntityHtml(itemPrefix?: string): EntityHtml {
            var runtimeInfo = this.runtimeInfo(itemPrefix).value();

            var div = this.containerDiv(itemPrefix);

            var result = new EntityHtml(itemPrefix, runtimeInfo, null, null);

            result.html = div.children();

            div.html(null);

            return result;
        }

        setEntity(entityValue: EntityValue, itemPrefix?: string) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.setEntitySpecific(entityValue)

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html((<EntityHtml>entityValue).html));

            this.runtimeInfo(itemPrefix).setValue(entityValue.runtimeInfo);

            this.updateButtonsDisplay();
            if (!SF.isEmpty(this.entityChanged)) {
                this.entityChanged();
            }
        }



        addEntitySpecific(entityValue: EntityValue, itemPrefix: string) {
            //virtual
        }

        addEntity(entityValue: EntityValue, itemPrefix: string) {
            if (entityValue == null)
                throw new Error("entityValue is mandatory on setEntityItem");

            this.addEntitySpecific(entityValue, itemPrefix);

            if (entityValue)
                entityValue.assertPrefixAndType(itemPrefix, this.staticInfo());

            if (entityValue.isLoaded())
                SF.triggerNewContent(this.containerDiv(itemPrefix).html((<EntityHtml>entityValue).html));
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

        getNextPrefix() : string {
            var lastIndex = Math.max.apply(null, this.getItems().toArray()
                .map((e: HTMLElement) => parseInt(e.id.after(this.options.prefix + "_").before("_" + this.itemSuffix()))));

            return SF.compose(this.options.prefix, lastIndex + 1)
        }

        getLastPosIndex() : number {
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

        find_click() {
            this.onFindingMany(this.options.prefix).then(result => {
                if (result)
                    result.forEach(ev=> this.addEntity(ev, this.getNextPrefix()));
            });
        }

        onFinding(prefix: string): Promise<EntityValue> {
            throw new Error("onFinding is deprecated in EntityListBase");
        }

        onFindingMany(prefix: string): Promise<EntityValue[]> {
            if (this.findingMany != null)
                return this.findingMany(this.options.prefix);

            return SF.ViewNavigator.typeChooser(this.staticInfo()).then(type=> {
                if (type == null)
                    return null;

                return FindNavigator.findMany({
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

    once("SF-entityList", () =>
        $.fn.entityList = function (opt: EntityListBaseOptions) {
            return new EntityList(this, opt);
        });

    export class EntityList extends EntityListBase {
      
        static key_list = "sfList";

        itemSuffix() {
            return SF.Keys.toStr;
        }

        updateLinks(newToStr: string, newLink: string, itemPrefix?: string) {
            $('#' + SF.compose(itemPrefix, SF.Keys.toStr)).html(newToStr);
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

        view_click() {
            var selectedItemPrefix = this.selectedItemPrefix();

            var entityHtml = this.extractEntityHtml(selectedItemPrefix);

            this.onViewing(entityHtml).then(result=> {
                if (result)
                    this.setEntity(result, selectedItemPrefix);
                else
                    this.setEntity(entityHtml, selectedItemPrefix); //previous entity passed by reference
            });
        }

        create_click() {
            var prefix = this.getNextPrefix();
            this.onCreating(prefix).then(entity => {
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

        addEntitySpecific(entityValue: EntityValue, itemPrefix: string) {
            var $table = $("#" + this.options.prefix + "> .sf-field-list > .sf-field-list-table");

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, EntityList.key_indexes), this.getNextPosIndex()));

            $table.before(SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), entityValue.runtimeInfo.toString()));

            $table.before(SF.hiddenDiv(SF.compose(itemPrefix, EntityList.key_entity), ""));

            var select = $(this.pf(EntityList.key_list));
            select.append("\n<option id='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' name='" + SF.compose(itemPrefix, SF.Keys.toStr) + "' value='' class='sf-value-line'>" + entityValue.toStr + "</option>");
            select.children('option').attr('selected', false); //Fix for Firefox: Set selected after retrieving the html of the select
            select.children('option:last').attr('selected', true);
        }

        remove_click() {
            var selectedItemPrefix = this.selectedItemPrefix();
            this.onRemove(selectedItemPrefix).then(result=> {
                if (result)
                    this.removeEntity(selectedItemPrefix);
            });
        }

        removeEntitySpecific(prefix: string) {
            $("#" + SF.compose(prefix, SF.Keys.runtimeInfo)).remove();
            $("#" + SF.compose(prefix, SF.Keys.toStr)).remove();
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

    once("SF-entityListDetail", () =>
        $.fn.entityListDetail = function (opt: EntityDetailOptions) {
            return new EntityListDetail(this, opt);
        });

    export interface EntityListDetailOptions extends EntityListBaseOptions {
        detailDiv: string;
    }

    export class EntityListDetail extends EntityList {

        options: EntityListDetailOptions;

        constructor(element: JQuery, options: EntityListDetailOptions) {
            super(element, options);
        }

        //create_click() {
        //    var prefix = this.getNextPrefix();
        //    this.onCreating(prefix).then(entity => {
        //        if (entity) {
        //            this.addEntity(entity, prefix);
        //            this.stageCurrentSelected();
        //        }
        //    });
        //}

        //find_click() {
        //    this.onFindingMany(this.options.prefix).then(result => {
        //        if (result) {
        //            result.forEach(ev=> this.addEntity(ev, this.getNextPrefix()));
        //            this.stageCurrentSelected();
        //        }
        //    });
        //}

        //remove_click() {
        //    var selectedItemPrefix = this.selectedItemPrefix();
        //    this.onRemove(selectedItemPrefix).then(result=> {
        //        if (result) {
        //            this.removeEntity(selectedItemPrefix);
        //            this.stageCurrentSelected();
        //        }
        //    });
        //}

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
                var entity = new EntityHtml(selPrefix, this.runtimeInfo(selPrefix).value(), null, null);

                SF.ViewNavigator.loadPartialView(entity, this.defaultViewOptions()).then(e=> {
                    selContainer.html(e.html);
                    detailDiv.append(selContainer);
                });
            }
        }
    }

    once("SF-entityRepeater", () =>
        $.fn.entityRepeater = function (opt: EntityListBaseOptions) {
            return new EntityRepeater(this, opt);
        });

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

        addEntitySpecific(entityValue: EntityValue, itemPrefix: string) {
            var $div = $("<fieldset id='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' name='" + SF.compose(itemPrefix, EntityRepeater.key_repeaterItem) + "' class='" + EntityRepeater.key_repeaterItemClass + "'>" +
                "<legend>" +
                (this.options.remove ? ("<a id='" + SF.compose(itemPrefix, "btnRemove") + "' title='" + lang.signum.remove + "' onclick=\"" + this._getRemoving(itemPrefix) + "\" class='sf-line-button sf-remove' data-icon='ui-icon-circle-close' data-text='false'>" + lang.signum.remove + "</a>") : "") +
                (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnUp") + "' title='" + lang.signum.moveUp + "' onclick=\"" + this._getMovingUp(itemPrefix) + "\" class='sf-line-button sf-move-up' data-icon='ui-icon-triangle-1-n' data-text='false'>" + lang.signum.moveUp + "</span>") : "") +
                (this.options.reorder ? ("<span id='" + SF.compose(itemPrefix, "btnDown") + "' title='" + lang.signum.moveDown + "' onclick=\"" + this._getMovingDown(itemPrefix) + "\" class='sf-line-button sf-move-down' data-icon='ui-icon-triangle-1-s' data-text='false'>" + lang.signum.moveDown + "</span>") : "") +
                "</legend>" +
                SF.hiddenInput(SF.compose(itemPrefix, EntityListBase.key_indexes), this.getNextPosIndex()) +
                SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), null) +
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

        remove_click() { throw new Error("remove_click is deprecated in EntityRepeater"); }

        removeItem_click(itemPrefix: string) {
            this.onRemove(itemPrefix).then(result=> {
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

    once("SF-entityStrip", () =>
        $.fn.entityStrip = function (opt: EntityStripOptions) {
            return new EntityStrip(this, opt);
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

        setEntitySpecific(entityValue: EntityValue, itemPrefix?: string){
            $('#' + SF.compose(itemPrefix, SF.Keys.link)).html(entityValue.toStr);
        }

        addEntitySpecific(entityValue: EntityValue, itemPrefix: string) {
            var $li = $("<li id='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' name='" + SF.compose(itemPrefix, EntityStrip.key_stripItem) + "' class='" + EntityStrip.key_stripItemClass + "'>" +
                SF.hiddenInput(SF.compose(itemPrefix, EntityStrip.key_indexes), this.getNextPosIndex()) +
                SF.hiddenInput(SF.compose(itemPrefix, SF.Keys.runtimeInfo), null) +
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

        remove_click() { throw new Error("remove_click is deprecated in EntityRepeater"); }

        removeItem_click(itemPrefix: string) {
            this.onRemove(itemPrefix).then(result=> {
                if (result)
                    this.removeEntity(itemPrefix);
            });
        }

        view_click() { throw new Error("remove_click is deprecated in EntityRepeater"); }

        viewItem_click(itemPrefix: string) {
            var entityHtml = this.extractEntityHtml(itemPrefix);

            this.onViewing(entityHtml).then(result=> {
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

        onAutocompleteSelected(entityValue: EntityValue) {
            this.addEntity(entityValue, this.getNextPrefix());
        }
    }
}

