/// <reference path="globals.ts"/>

export var Keys = {
    tabId: "sfTabId",
    antiForgeryToken: "__RequestVerificationToken",

    runtimeInfo: "sfRuntimeInfo",
    staticInfo: "sfStaticInfo",
    toStr: "sfToStr",
    link: "sfLink",
    loading: "loading",
    entityState: "sfEntityState",
    template: "sfTemplate",

    viewMode: "sfViewMode",
};


export interface TypeInfo {
    name: string;
    niceName: string;
    creable?: boolean;
    findable?: boolean;
    preConstruct?: (extraJsonArgs?: FormObject) => Promise<any>;
    avoidPopup?: boolean;
}

export class RuntimeInfo {
    type: string;
    id: string;
    isNew: boolean;
    ticks: string;

    constructor(entityType: string, id: string, isNew: boolean, ticks?: string) {
        if (SF.isEmpty(entityType))
            throw new Error("entityTyp is mandatory for RuntimeInfo");

        this.type = entityType;
        this.id = id;
        this.isNew = isNew;
        this.ticks = ticks;
    }

    public static parse(runtimeInfoString: string): RuntimeInfo {
        if (SF.isEmpty(runtimeInfoString))
            return null;

        var array = runtimeInfoString.split(';');
        return new RuntimeInfo(
            array[0],
            SF.isEmpty(array[1]) ? null : array[1],
            array[2] == "n",
            array[3]);
    }

    public toString() {
        return [this.type,
            this.id,
            this.isNew ? "n" : "o",
            this.ticks].join(";");
    }

    public static fromKey(key: string): RuntimeInfo {
        if (SF.isEmpty(key))
            return null;

        return new RuntimeInfo(
            key.before(";"),
            key.after(";"),
            false);
    }

    key(): string {
        if (this.id == null)
            throw Error("RuntimeInfo has no Id");

        return this.type + ";" + this.id;
    }

    static getFromPrefix(prefix: string, context?: JQuery): RuntimeInfo {
        return RuntimeInfo.parse(prefix.child(Keys.runtimeInfo).get().val());
    }

    static setFromPrefix(prefix: string, runtimeInfo: RuntimeInfo, context?: JQuery) {
        prefix.child(Keys.runtimeInfo).get().val(runtimeInfo == null? "": runtimeInfo.toString());
    }
}

export class EntityValue {
    constructor(runtimeInfo: RuntimeInfo, toString?: string) {
        if (runtimeInfo == null)
            throw new Error("runtimeInfo is mandatory for an EntityValue");

        this.runtimeInfo = runtimeInfo;
        this.toStr = toString;
    }

    runtimeInfo: RuntimeInfo;
    toStr: string;

    assertPrefixAndType(prefix: string, types: TypeInfo[]) {
        if (types == null) // All
            return;

        if (!types.some(ti=> ti.name == this.runtimeInfo.type))
            throw new Error("{0} not found in types {1}".format(this.runtimeInfo.type, types.join(", ")));
    }

    key(): string {
        return this.runtimeInfo.key() + ";" + this.toStr;
    }

    public static fromKey(key: string): EntityValue {

        var ri = RuntimeInfo.fromKey(key);

        if (!ri)
            return null;

        var firstIndex = key.indexOf(";");
        if (firstIndex == -1)
            throw Error("{0} not found".format(";"));

        var secondIndex = key.indexOf(";", firstIndex + 1);
        if (secondIndex == -1)
            return new EntityValue(RuntimeInfo.parse(key));

        return new EntityValue(RuntimeInfo.parse(key.substr(0, secondIndex)), key.substr(secondIndex + 1));
    }

    isLoaded() {
        return false;
    }
}

export class EntityHtml extends EntityValue {
    prefix: string;
    html: JQuery;

    hasErrors: boolean;

    constructor(prefix: string, runtimeInfo: RuntimeInfo, toString?: string) {
        super(runtimeInfo, toString);

        if (prefix == null)
            throw new Error("prefix is mandatory for EntityHtml");

        this.prefix = prefix;
    }

    assertPrefixAndType(prefix: string, types: TypeInfo[]) {

        super.assertPrefixAndType(prefix, types);

        if (this.prefix != null && this.prefix != prefix)
            throw Error("EntityHtml prefix should be {0} instead of  {1}".format(prefix, this.prefix));
    }

    isLoaded() {
        return this.html != null && this.html.length != 0;
    }

    loadHtml(htmlText: string) {
        this.html = $('<div/>').html(htmlText).contents();
    }

    getChild(pathPart: string) : JQuery {
        return this.prefix.child(pathPart).get(this.html);
    }

    tryGetChild(pathPart: string): JQuery {
        return this.prefix.child(pathPart).tryGet(this.html);
    }

    static fromHtml(prefix: string, htmlText: string): EntityHtml {
        var result = new EntityHtml(prefix, new RuntimeInfo("?", null, false));
        result.loadHtml(htmlText);
        return result;
    }

    static fromDiv(prefix: string, div: JQuery): EntityHtml {
        var result = new EntityHtml(prefix, new RuntimeInfo("?", null, false));
        result.html = div.clone();
        return result;
    }

    static withoutType(prefix: string): EntityHtml {
        var result = new EntityHtml(prefix, new RuntimeInfo("?", null, false));
        return result;
    }
}

