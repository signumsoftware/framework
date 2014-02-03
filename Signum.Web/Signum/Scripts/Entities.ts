/// <reference path="globals.ts"/>

export class StaticInfo {
    static _types = 0;
    static _typeNiceNames = 1;
    static _isEmbedded = 2;
    static _isReadOnly = 3;
    static _rootType = 4;
    static _propertyRoute = 5;

    prefix: string;
    $elem: JQuery;

    constructor(prefix: string) {
        this.prefix = prefix;
    }

    public find() {
        if (!this.$elem) {
            this.$elem = $('#' + SF.compose(this.prefix, Keys.staticInfo));
        }
        return this.$elem;
    }

    public value(): string {
        return this.find().val();
    }

    public toArray() {
            return this.value().split(";")
        }

    public toValue(array) {
        return array.join(";");
    }

    public getValue(key) {
        var array = this.toArray();
        return array[key];
    }

    public singleType() {
        var typeArray = this.types();
        if (typeArray.length !== 1) {
            throw "types should have only one element for element {0}".format(this.prefix);
        }
        return typeArray[0];
    }

    public types(): string[] {
        return this.getValue(StaticInfo._types).split(',');
    }

    public typeNiceNames(): string[] {
        return this.getValue(StaticInfo._typeNiceNames).split(',');
    }

    public isEmbedded(): boolean {
        return this.getValue(StaticInfo._isEmbedded) == "e";
    }

    public isReadOnly(): boolean {
        return this.getValue(StaticInfo._isReadOnly) == "r";
    }

    public rootType(): string {
        return this.getValue(StaticInfo._rootType);
    }

    public propertyRoute(): string {
        return this.getValue(StaticInfo._propertyRoute);
    }

    public static getFor(prefix: string) : StaticInfo {
        if (!prefix)
            throw new Error("prefix not provided"); 

        var staticInfo = new StaticInfo(prefix);
        if (staticInfo.find().length > 0)
            return staticInfo;

        return new StaticInfo(prefix.beforeLast("_"));  //If List => use parent
    }
}

export class EntityValue {
    constructor(runtimeInfo: RuntimeInfoValue, toString?: string, link?: string) {
        if (runtimeInfo == null)
            throw new Error("runtimeInfo is mandatory for an EntityValue");

        this.runtimeInfo = runtimeInfo;
        this.toStr = toString;
        this.link = link;
    }

    runtimeInfo: RuntimeInfoValue;
    toStr: string;
    link: string;

    assertPrefixAndType(prefix: string, staticInfo: StaticInfo) {
        var types = staticInfo.types();

        if (types.length == 0 && types[0] == "[All]")
            return;

        if (types.indexOf(this.runtimeInfo.type) == -1)
            throw new Error("{0} not found in types {1}".format(this.runtimeInfo.type, types.join(", ")));
    }

    isLoaded() {
        return false;
    }
}

export class EntityHtml extends EntityValue {
    prefix: string;
    html: JQuery;

    hasErrors: boolean;

    constructor(prefix: string, runtimeInfo: RuntimeInfoValue, toString?: string, link?: string) {
        super(runtimeInfo, toString, link);

        if (this.prefix == null)
            throw new Error("prefix is mandatory for EntityHtml");
    }

    assertPrefixAndType(prefix: string, staticInfo: StaticInfo) {

        super.assertPrefixAndType(prefix, staticInfo);

        if (this.prefix != null && this.prefix != prefix)
            throw Error("EntityHtml prefix should be {0} instead of  {1}".format(prefix, this.prefix));
    }

    isLoaded() {
        return this.html != null && this.html.length != 0;
    }

    static fromHtml(prefix: string, html: string): EntityHtml {
        var result = new EntityHtml(prefix, new RuntimeInfoValue("?", null));
        result.html = $(html);
        return result;
    }

    static withoutType(prefix: string): EntityHtml {
        var result = new EntityHtml(prefix, new RuntimeInfoValue("?", null));
        return result;
    }
}



export class RuntimeInfoValue {
    type: string;
    id: number;
    isNew: boolean;
    ticks: number;

    constructor(entityType: string, id: number, isNew?: boolean, ticks?: number) {
        if (SF.isEmpty(entityType))
            throw new Error("entityTyp is mandatory for RuntimeInfoValue");

        this.type = entityType;
        this.id = id;
        this.isNew = isNew;
        this.ticks = ticks;
    }

    public static parse(runtimeInfoString: string): RuntimeInfoValue {
        if (SF.isEmpty(runtimeInfoString))
            return null;

        var array = runtimeInfoString.split(';');
        return new RuntimeInfoValue(
            array[0],
            SF.isEmpty(array[1]) ? null : parseInt(array[1]),
            array[2] == "n",
            SF.isEmpty(array[3]) ? null : parseInt(array[3]));
    }

    public toString() {
        return [this.type,
            this.id,
            this.isNew ? "n" : "o",
            this.ticks].join(";");
    }

    public static fromKey(key: string): RuntimeInfoValue {
        if (SF.isEmpty(key))
            return null;

        var array = key.split(',');
        return new RuntimeInfoValue(
            array[0],
            parseInt(array[1]),
            false, null);
    }

    key(): string {
        if (this.id == null)
            throw Error("RuntimeInfoValue has no Id");

        return this.type + ";" + this.id;
    }
}

export class RuntimeInfoElement {

    prefix: string;
    $elem: JQuery;

    constructor(prefix: string) {
        this.prefix = prefix;
    }

    public getElem() {
        if (!this.$elem) {
            this.$elem = $('#' + SF.compose(this.prefix, Keys.runtimeInfo));
        }
        return this.$elem;
    }

    value(): RuntimeInfoValue {
        return RuntimeInfoValue.parse(this.getElem().val());
    }

    setValue(runtimeInfo: RuntimeInfoValue) {
        this.getElem().val(runtimeInfo == null ? null : runtimeInfo.toString());
    }
}

export var Keys = {
    tabId: "sfTabId",
    antiForgeryToken: "__RequestVerificationToken",

    entityTypeNames: "sfEntityTypeNames",

    runtimeInfo: "sfRuntimeInfo",
    staticInfo: "sfStaticInfo",
    toStr: "sfToStr",
    link: "sfLink",
    loading: "loading",
    entityState: "sfEntityState",
};


