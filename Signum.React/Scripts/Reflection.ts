/// <reference path="globals.ts" />

import {ajaxPost, ajaxGet} from 'Framework/Signum.React/Scripts/Services';


export function getEnumInfo(enumTypeName: string, enumId: number) {

    var ti = getTypeInfo(enumTypeName);

    if (!ti || ti.kind != KindOfType.Enum)
        throw new Error(`${enumTypeName} is not an Enum`);

    if (!ti.membersById)
        ti.membersById = Dic.getValues(ti.members).toObject(a=> a.name);

    return ti.membersById[enumId];
}

export interface TypeInfo {
    kind: KindOfType;
    name: string;
    niceName?: string;
    nicePluralName?: string;
    gender?: string;
    entityKind?: EntityKind;
    entityData?: EntityData;
    isLowPopupation?: boolean;
    members?: { [name: string]: MemberInfo };
    membersById?: { [name: string]: MemberInfo };
    mixins?: { [name: string]: string; };
}

export interface MemberInfo {
    name: string,
    niceName: string;
    typeNiceName: string;
    type: TypeReference;
    isReadOnly?: boolean;
    isIgnored?: boolean;
    unit?: string;
    format?: string;
    id?: any; //symbols
}

//https://msdn.microsoft.com/en-us/library/az4se3k1%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
//http://momentjs.com/docs/#/displaying/format/
export function toMomentFormat(format: string): any {

    switch (format) {
        case "d": return "L"; // or "l"
        case "D": return "LL";
        case "f":
        case "F": return "LLLL";
        case "g": return "L LT";
        case "G": return "L LTS";
        case "M":
        case "m": return "D MMM";
        case "u":
        case "s": return moment.ISO_8601;
        case "t": return "LT";
        case "T": return "LTS";
        case "y": return "LTS";
        case "Y": return "MMMM YYY";
        default: format;
    }
}

export interface TypeReference {
    name?: string;
    isCollection?: boolean;
    isLite?: boolean;
    isNullable?: boolean;
    isEnum?: boolean;
    isEmbedded?: boolean;
}

export enum KindOfType {
    Entity = "Entity" as any,
    Enum = "Enum" as any,
    Message = "Message" as any,
    Query = "Query" as any,
    SymbolContainer = "SymbolContainer" as any,
}

export enum EntityKind {
    SystemString = "SystemString" as any,
    System = "System" as any,
    Relational = "Relational" as any,
    String = "String" as any,
    Shared = "Shared" as any,
    Main = "Main" as any,
    Part = "Part" as any,
    SharedPart = "SharedPart" as any,
}

export enum EntityData {
    Master = "Master" as any,
    Transactional = "Transactional" as any,
}

interface TypeInfoDictionary {
    [name: string]: TypeInfo
}

var _types: TypeInfoDictionary;


var _queryNames: {
    [queryKey: string]: {
        name: string, niceName: string
    }
};

export type PseudoType = IType | TypeInfo | string;

export function getTypeInfo(type: PseudoType): TypeInfo {

    if ((type as TypeInfo).kind != null)
        return type as TypeInfo;

    if ((type as IType).typeName)
        return _types[((type as IType).typeName).toLowerCase()];

    if (typeof type == "string")
        return _types[(type as string).toLowerCase()];

    throw new Error("Unexpected type: " + type);
}

export const IsByAll = "[ALL]";
export function getTypeInfos(typeReference: TypeReference): TypeInfo[] {
    if (typeReference.name == IsByAll)
        return [];

    return typeReference.name.split(", ").map(getTypeInfo);

}

export function getQueryNiceName(queryName: any) {

    if ((queryName as TypeInfo).kind != null)
        return (queryName as TypeInfo).nicePluralName;

    if (queryName instanceof Type)
        return (queryName as Type<any>).nicePluralName();

    if (queryName instanceof QueryKey)
        return (queryName as QueryKey).niceName();

    if (typeof queryName == "string") {
        var str = queryName as string;

        var type = _types[str.toLowerCase()];
        if (type)
            return type.nicePluralName;

        var qn = _queryNames[str.toLowerCase()];
        if (qn)
            return qn.niceName;

        return str;
    }

    throw new Error("unexpected queryName type");

}

export function getQueryKey(queryName: any): string {
    if ((queryName as TypeInfo).kind != null)
        return (queryName as TypeInfo).name;

    if (queryName instanceof Type)
        return (queryName as Type<any>).typeName;

    if (queryName instanceof QueryKey)
        return (queryName as QueryKey).name;

    if (typeof queryName == "string") {
        var str = queryName as string;

        var type = _types[str.toLowerCase()];
        if (type)
            return type.name;

        var qn = _queryNames[str.toLowerCase()];
        if (qn)
            return qn.name;

        return str;
    }

    throw new Error("unexpected queryName type");
}

export function loadTypes(): Promise<void> {

    return ajaxGet<TypeInfoDictionary>({ url: "/api/reflection/types" }).then((types) => {

        Dic.foreach(types, (k, t) => {
            t.name = k;
            if (t.members)
                Dic.foreach(t.members, (k2, t2) => t2.name = k2);
        });

        _types = Dic.getValues(types).toObject(a=> a.name.toLowerCase());

        _queryNames = Dic.getValues(types).filter(t=> t.kind == KindOfType.Query)
            .flatMap(a=> Dic.getValues(a.members))
            .toObject(m=> m.name.toLocaleLowerCase(), m=> ({ name: m.name, niceName: m.niceName }));

        earySymbols.forEach(s=> setSymbolId(s));

        earySymbols = null;
    });
}

export interface IBinding<T> {
    getValue(): T;
    setValue(val: T) :void;
}

export class Binding<T> implements IBinding<T> {

    constructor(
        public memberName: string,
        public parentValue: any) {
    }

    getValue() : T {       
        return this.parentValue[this.memberName];
    }
    setValue(val: T) {
        return this.parentValue[this.memberName] = val;
    }
}

export class ReadonlyBinding<T> implements IBinding<T> {
    constructor(
        public value: T) {
    }

    getValue() {
        return this.value;
    }
    setValue(val: T) {
        throw new Error("Readonly Binding");
    }
}


export function createBinding<T>(parentValue: any, lambda: (obj: any) => T): IBinding<T> {

    var lambdaMatch = functionRegex.exec((lambda as any).toString());

    if (lambdaMatch == null)
        throw Error("invalid function");

    var parameter = lambdaMatch[1];
    var body = lambdaMatch[2];

    if (parameter == body)
        return new ReadonlyBinding<T>(parentValue as T);

    var m = memberRegex.exec(body);

    if (m == null)
        return null;


    var realParentValue = m[1] == parameter ? parentValue :
        eval(`(function(${parameter}){ return ${m[1]};})`)(parentValue);

    return new Binding<T>(m[2], realParentValue);
}


var functionRegex = /^function\s*\(\s*([$a-zA-Z_][0-9a-zA-Z_$]*)\s*\)\s*{\s*return\s*(.*)\s*;\s*}$/;
var memberRegex = /^(.*)\.([$a-zA-Z_][0-9a-zA-Z_$]*)$/;
var mixinRegex = /^getMixin\((.*),\s*([$a-zA-Z_][0-9a-zA-Z_$]*_Type)\s*\)$/


export function getLambdaMembers(lambda: Function): LambdaMember[]{
    
    var lambdaMatch = functionRegex.exec((lambda as any).toString());

    if (lambdaMatch == null)
        throw Error("invalid function");

    var parameter = lambdaMatch[1];
    var body = lambdaMatch[2];
    var result: LambdaMember[] = [];

    while (body != parameter) {
        var m = memberRegex.exec(body);

        if (m != null) {
            result.push({ name: m[2], type: LambdaMemberType.Member });
            body = m[1];
        }

        m = mixinRegex.exec(body);

        if (m != null) {
            result.push({ name: m[2], type: LambdaMemberType.Mixin });
            body = m[1];
        }
    }

    return result.reverse();
}


interface LambdaMember {
    name: string;
    type: LambdaMemberType
}

enum LambdaMemberType {
    Member,
    Mixin,
}



export interface IType {
    typeName: string;
}

export class Type<T> implements IType {
    constructor(
        public typeName: string) { }

    typeInfo(): TypeInfo {
        return getTypeInfo(this.typeName);
    }

    propertyInfo(lambdaToProperty: (v: T) => any): MemberInfo {
        return PropertyRoute.root(this).add(lambdaToProperty).member;
    }

    propertyRoute(lambdaToProperty: (v: T) => any): PropertyRoute {
        return PropertyRoute.root(this).add(lambdaToProperty);
    }

    niceName() {
        return this.typeInfo().niceName;
    }

    nicePluralName() {
        return this.typeInfo().nicePluralName;
    }

    nicePropertyName(lambdaToProperty: (v: T) => any): string {
        return this.propertyInfo(lambdaToProperty).niceName;
    }
}


export class EnumType<T> {
    constructor(
        public type: string,
        public converter: { [value: number]: string }
    ) { }

    typeInfo(): TypeInfo {
        return getTypeInfo(this.type);
    }

    niceName(value?: T): string {

        if (value == null)
            return this.typeInfo().niceName;

        var valueStr = this.converter[<any>value];

        return this.typeInfo().members[valueStr].niceName;
    }
}

export class MessageKey {

    constructor(
        public type: string,
        public name: string) { }

    propertyInfo(): MemberInfo {
        return getTypeInfo(this.type).members[this.name]
    }

    niceToString(): string {
        return this.propertyInfo().niceName;
    }
}

export class QueryKey {

    constructor(
        public type: string,
        public name: string) { }

    propertyInfo(): MemberInfo {
        return getTypeInfo(this.type).members[this.name]
    }

    niceName(): string {
        return this.propertyInfo().niceName;
    }
}

interface ISymbol {
    Type: string; 
    key: string;
    id?: any;
}

var earySymbols: ISymbol[] = [];

function setSymbolId(s: ISymbol) {

    var type = _types[s.key.before(".").toLowerCase()];

    if (!type)
        return;

    var member = type.members[s.key.after(".")];

    if (!member)
        return

    s.id = member.id;
}


export function registerSymbol<T extends ISymbol>(symbol: T): T {

    if (_types)
        setSymbolId(symbol);
    else
        earySymbols.push(symbol);

    return symbol;
} 

export class PropertyRoute {
    
    propertyRouteType: PropertyRouteType;
    parent: PropertyRoute; //!Root
    type: TypeInfo; //Root
    member: MemberInfo; //Member
    mixinName: string; //Mixin
    
    constructor(parent: PropertyRoute, propertyRouteType: PropertyRouteType, type: TypeInfo, member: MemberInfo, mixinName: string) {

        this.propertyRouteType = propertyRouteType;
        this.parent = parent; //!Root
        this.type = type; //Root
        this.member = member; //Field or Property
        this.mixinName = mixinName; //Mixin
    }


    static root(type: PseudoType) {
        return new PropertyRoute(null, PropertyRouteType.Root, getTypeInfo(type), null, null);
    }

    add(property: (val: any) => any): PropertyRoute {
        var members = getLambdaMembers(property);

        var current: PropertyRoute = this;
        members.forEach(m=> current = current.addMember(m));

        return current;
    }

    get rootType() {
        return this.type || this.parent.rootType;
    }

    propertyPath() {
        switch (this.propertyRouteType) {
            case PropertyRouteType.Root: throw new Error("Root has no PropertyString");
            case PropertyRouteType.Field: return this.member.name;
            case PropertyRouteType.Mixin: return "[" + this.mixinName + "]";
            case PropertyRouteType.MListItems: return this.parent.propertyPath() + "/";
            case PropertyRouteType.LiteEntity: return this.parent.propertyPath() + ".entity";
        }
    }
   

    addMember(member: LambdaMember): PropertyRoute {

        if (member.type == LambdaMemberType.Member) {
            var memberName = this.propertyRouteType == PropertyRouteType.Root ? member.name :
                this.propertyRouteType == PropertyRouteType.MListItems ? this.propertyPath() + member.name :
                    this.propertyPath() + "." + member.name;

            var m = this.type.members[memberName.firstUpper()];
            if (!m)
                throw new Error(`member '${memberName}' not found`)

            return new PropertyRoute(this, PropertyRouteType.Field, null, m, null);
        }

        if (member.type == LambdaMemberType.Mixin) {
            if (this.propertyRouteType != PropertyRouteType.Root)
                throw new Error("invalid mixin at this stage");

            return new PropertyRoute(this, PropertyRouteType.Mixin, null, null, member.name);
        }

        throw new Error("not implemented");
    }
}

export enum PropertyRouteType {
    Root,
    Field,
    Mixin,
    LiteEntity,
    MListItems,
}
