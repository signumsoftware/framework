/// <reference path="globals.ts" />

import { Dic } from './Globals';
import {ajaxPost, ajaxGet} from './Services';


export function getEnumInfo(enumTypeName: string, enumId: number) {

    const ti = getTypeInfo(enumTypeName);

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
    toStringFunction?: string;
    isLowPopupation?: boolean;
    members?: { [name: string]: MemberInfo };
    membersById?: { [name: string]: MemberInfo };
    mixins?: { [name: string]: string; };

    operations?: { [name: string]: OperationInfo };
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

export interface OperationInfo {
    key: string,
    niceName: string;
    operationType: OperationType;
    allowNew: boolean;
    lite: boolean;
    hasCanExecute: boolean;
}

export enum OperationType {
    Execute = "Execute" as any,
    Delete = "Delete" as any,
    Constructor = "Constructor" as any,
    ConstructorFrom = "ConstructorFrom" as any,
    ConstructorFromMany = "ConstructorFromMany" as any
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
    name: string;
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

export interface TypeInfoDictionary {
    [name: string]: TypeInfo
}

let _types: TypeInfoDictionary = {};


let _queryNames: {
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
        const str = queryName as string;

        const type = _types[str.toLowerCase()];
        if (type)
            return type.nicePluralName;

        const qn = _queryNames[str.toLowerCase()];
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
        const str = queryName as string;

        const type = _types[str.toLowerCase()];
        if (type)
            return type.name;

        const qn = _queryNames[str.toLowerCase()];
        if (qn)
            return qn.name;

        return str;
    }

    throw new Error("unexpected queryName type");
}

export function requestTypes(): Promise<TypeInfoDictionary> {
    return ajaxGet<TypeInfoDictionary>({ url: "/api/reflection/types" });
}

export function setTypes(types: TypeInfoDictionary) {

    Dic.foreach(types, (k, t) => {
        t.name = k;
        if (t.members)
            Dic.foreach(t.members, (k2, t2) => t2.name = k2);
    });

    _types = Dic.getValues(types).toObject(a => a.name.toLowerCase());

    Dic.foreach(types, (k, t) => {
        
        if (t.operations)
            Dic.foreach(t.operations, (k2, t2) => {
                t2.key = k2;
                t2.niceName = _types[k2.before(".").toLowerCase()].members[k2.after(".")].niceName;
            });
    });

    _queryNames = Dic.getValues(types).filter(t => t.kind == KindOfType.Query)
        .flatMap(a => Dic.getValues(a.members))
        .toObject(m => m.name.toLocaleLowerCase(), m => ({ name: m.name, niceName: m.niceName }));

    missingSymbols = missingSymbols.filter(s => !setSymbolId(s));
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

    const lambdaMatch = functionRegex.exec((lambda as any).toString());

    if (lambdaMatch == null)
        throw Error("invalid function");

    const parameter = lambdaMatch[1];
    const body = lambdaMatch[2];

    if (parameter == body)
        return new ReadonlyBinding<T>(parentValue as T);

    const m = memberRegex.exec(body);

    if (m == null)
        return null;


    const realParentValue = m[1] == parameter ? parentValue :
        eval(`(function(${parameter}){ return ${m[1]};})`)(parentValue);

    return new Binding<T>(m[2], realParentValue);
}


const functionRegex = /^function\s*\(\s*([$a-zA-Z_][0-9a-zA-Z_$]*)\s*\)\s*{\s*return\s*(.*)\s*;\s*}$/;
const memberRegex = /^(.*)\.([$a-zA-Z_][0-9a-zA-Z_$]*)$/;
const indexRegex = /^(.*)\[(\d+)\]$/;
const mixinRegex = /^getMixin\((.*),\s*([$a-zA-Z_][0-9a-zA-Z_$]*_Type)\s*\)$/


export function getLambdaMembers(lambda: Function): LambdaMember[]{
    
    const lambdaMatch = functionRegex.exec((lambda as any).toString());

    if (lambdaMatch == null)
        throw Error("invalid function");

    const parameter = lambdaMatch[1];
    let body = lambdaMatch[2];
    const result: LambdaMember[] = [];

    while (body != parameter) {
        let m = memberRegex.exec(body);

        if (m != null) {
            result.push({ name: m[2], type: LambdaMemberType.Member });
            body = m[1];
        }

        m = indexRegex.exec(body);

        if (m != null) {
            result.push({ name: m[2], type: LambdaMemberType.Indexer });
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
    Indexer,
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

    memberInfo(lambdaToProperty: (v: T) => any): MemberInfo {
        return PropertyRoute.root(this.typeInfo()).add(lambdaToProperty).member;
    }

    propertyRoute(lambdaToProperty: (v: T) => any): PropertyRoute {
        return PropertyRoute.root(this.typeInfo()).add(lambdaToProperty);
    }

    niceName() {
        return this.typeInfo().niceName;
    }

    nicePluralName() {
        return this.typeInfo().nicePluralName;
    }

    nicePropertyName(lambdaToProperty: (v: T) => any): string {
        return this.memberInfo(lambdaToProperty).niceName;
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

        const valueStr = this.converter[<any>value];

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

let missingSymbols: ISymbol[] = [];

function setSymbolId(s: ISymbol): boolean {

    const type = _types[s.key.before(".").toLowerCase()];

    if (!type)
        return false;

    const member = type.members[s.key.after(".")];

    if (!member)
        return false;

    s.id = member.id;

    return true;
}


export function registerSymbol<T extends ISymbol>(symbol: T): T {

    if (!setSymbolId(symbol))
        missingSymbols.push(symbol);

    return symbol;
}

export class PropertyRoute {
    
    propertyRouteType: PropertyRouteType;
    parent: PropertyRoute; //!Root
    rootType: TypeInfo; //Root
    member: MemberInfo; //Member
    mixinName: string; //Mixin
    

    static root(typeInfo: TypeInfo) {
        return new PropertyRoute(null, PropertyRouteType.Root, typeInfo, null, null);
    }

    static member(parent: PropertyRoute, member: MemberInfo) {
        return new PropertyRoute(parent, PropertyRouteType.Field, null, member, null);
    }

    static mixin(parent: PropertyRoute, mixinName: string) {
        return new PropertyRoute(parent, PropertyRouteType.Mixin, null, null, mixinName);
    }

    static mlistItem(parent: PropertyRoute) {
        return new PropertyRoute(parent, PropertyRouteType.MListItem, null, null, null);
    }

    static liteEntity(parent: PropertyRoute) {
        return new PropertyRoute(parent, PropertyRouteType.LiteEntity, null, null, null);
    }

    constructor(parent: PropertyRoute, propertyRouteType: PropertyRouteType, rootType: TypeInfo, member: MemberInfo, mixinName: string) {

        this.propertyRouteType = propertyRouteType;
        this.parent = parent;
        this.rootType = rootType;
        this.member = member;
        this.mixinName = mixinName;
    }

    add(property: (val: any) => any): PropertyRoute {
        const members = getLambdaMembers(property);

        let current: PropertyRoute = this;
        members.forEach(m=> current = current.addMember(m));

        return current;
    }

    findRootType() {
        return this.rootType || this.parent.findRootType();
    }

    typeReference(): TypeReference {
        switch (this.propertyRouteType) {
            case PropertyRouteType.Root: return { name: this.rootType.name };
            case PropertyRouteType.Field: return this.member.type;
            case PropertyRouteType.Mixin: throw new Error("mixins can not be used alone");
            case PropertyRouteType.MListItem: return Dic.extend({}, this.parent.typeReference(), { isCollection: false });
            case PropertyRouteType.LiteEntity: return Dic.extend({}, this.parent.typeReference(), { isLite: false });
        }
    }

    closestTypeInfo(): TypeInfo {
        switch (this.propertyRouteType) {
            case PropertyRouteType.Root: return this.rootType;

            case PropertyRouteType.Field: return this.parent.closestTypeInfo();
            case PropertyRouteType.Mixin: throw this.parent.closestTypeInfo();
            case PropertyRouteType.MListItem: return this.parent.closestTypeInfo();
            case PropertyRouteType.LiteEntity: return this.parent.closestTypeInfo();
        }
    }

    propertyPath() {
        switch (this.propertyRouteType) {
            case PropertyRouteType.Root: throw new Error("Root has no PropertyString");
            case PropertyRouteType.Field: return this.member.name;
            case PropertyRouteType.Mixin: return "[" + this.mixinName + "]";
            case PropertyRouteType.MListItem: return this.parent.propertyPath() + "/";
            case PropertyRouteType.LiteEntity: return this.parent.propertyPath() + ".entity";
        }
    }

    
   

    addMember(member: LambdaMember): PropertyRoute {

        if (member.type == LambdaMemberType.Member) {

            const ref = this.typeReference();
            if (ref.isLite) {
                if (member.name != "entity")
                    throw new Error("Entity expected");

                return PropertyRoute.liteEntity(this);    
            }

            if (this.propertyRouteType != PropertyRouteType.Root) {
                const ti = getTypeInfos(ref).single("Ambiguity due to multiple Implementations");
                if (ti) {
                    const m = ti.members[member.name];
                    if (!m)
                        throw new Error(`member '${member.name}' not found`);

                    return PropertyRoute.member(PropertyRoute.root(ti), m);
                }
            }

            const memberName = this.propertyRouteType == PropertyRouteType.Root ? member.name :
                this.propertyRouteType == PropertyRouteType.MListItem ? this.propertyPath() + member.name :
                    this.propertyPath() + "." + member.name;

            const m = this.closestTypeInfo().members[memberName.firstUpper()];
            if (!m)
                throw new Error(`member '${memberName}' not found`)

            return PropertyRoute.member(this, m);
        }

        if (member.type == LambdaMemberType.Mixin) {
            if (this.propertyRouteType != PropertyRouteType.Root)
                throw new Error("invalid mixin at this stage");

            return PropertyRoute.mixin(this, member.name);
        }


        if (member.type == LambdaMemberType.Indexer) {
            if (this.propertyRouteType != PropertyRouteType.Field)
                throw new Error("invalid mixin at this stage");

            return PropertyRoute.mlistItem(this);
        }

        throw new Error("not implemented");
    }

    toString() {
        return `(${this.findRootType().name}).${this.propertyPath()}`;
    }
}

export enum PropertyRouteType {
    Root = "Root" as any,
    Field = "Field" as any,
    Mixin = "Mixin" as any,
    LiteEntity = "LiteEnity" as any,
    MListItem = "MListItem" as any,
}
