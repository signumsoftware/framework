/// <reference path="globals.ts" />

import {ajaxPost, ajaxGet} from 'Framework/Signum.React/Scripts/Services';

export class PropertyRoute {

    parent: PropertyRoute;

    add(property: (val: any) => any): PropertyRoute {
        return null;
    }
}

export function getEnumInfo(enumTypeName: string, enumId: number) {

    var ti = typeInfo(enumTypeName);

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
    members?: { [name: string]: MemberInfo };
    membersById?: { [name: string]: MemberInfo };
    mixins?: { [name: string]: string };
}

export interface MemberInfo {
    name: string,
    niceName: string;
    type: TypeReference;
    unit?: string;
    format?: string;
    id?: any; //symbols
}

export interface TypeReference {
    isCollection?: boolean;
    isLite?: boolean;
    isNullable?: boolean;
    name?: string;
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

export function typeInfo(type: PseudoType): TypeInfo {

    if ((type as TypeInfo).kind != null)
        return type as TypeInfo;

    if ((type as IType).typeName)
        return _types[((type as IType).typeName).toLowerCase()];

    if (typeof type == "string")
        return _types[(type as string).toLowerCase()];

    throw new Error("Unexpected type: " + type);
}


export const IsByAll = "[ALL]";
export function typeInfos(typeReference: TypeReference): TypeInfo[] {
    if (typeReference.name == IsByAll)
        return [];

    return typeReference.name.split(", ").map(typeInfo);

}

export function queryNiceName(queryName: any) {

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

export function queryKey(queryName: any): string {
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



export function lambdaBody(lambda: Function): string {
    return lambda.toString().after("return ").after(".").before(";");
}

export interface IType {
    typeName: string;
}

export class Type<T> implements IType {
    constructor(
        public typeName: string) { }

    typeInfo(): TypeInfo {
        return typeInfo(this.typeName);
    }

    propertyInfo(lambdaToProperty: (v: T) => any): MemberInfo {
        return this.typeInfo().members[lambdaBody(lambdaToProperty)];
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
        return typeInfo(this.type);
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
        return typeInfo(this.type).members[this.name]
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
        return typeInfo(this.type).members[this.name]
    }

    niceName(): string {
        return this.propertyInfo().niceName;
    }
}

interface ISymbol {
    key?: string;
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
