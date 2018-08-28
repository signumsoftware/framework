import * as moment from 'moment';
import * as numbro from 'numbro';
import { Dic } from './Globals';
import { ModifiableEntity, Entity, Lite, MListElement, ModelState, MixinEntity } from './Signum.Entities'; //ONLY TYPES!
import {ajaxPost, ajaxGet} from './Services';
import { MList } from "./Signum.Entities";


export function getEnumInfo(enumTypeName: string, enumId: number) {

    const ti = getTypeInfo(enumTypeName);

    if (!ti || ti.kind != "Enum")
        throw new Error(`${enumTypeName} is not an Enum`);

    return ti.membersById![enumId];
}


export interface TypeInfo {
    kind: KindOfType;
    name: string;
    fullName: string;
    niceName?: string;
    nicePluralName?: string;
    gender?: string;
    entityKind?: EntityKind;
    entityData?: EntityData;
    toStringFunction?: string;
    isLowPopulation?: boolean;
    isSystemVersioned?: boolean;
    requiresSaveOperation?: boolean;
    queryDefined?: boolean;
    requiresEntityPack?: boolean;
    members: { [name: string]: MemberInfo };
    membersById?: { [name: string]: MemberInfo };

    operations?: { [name: string]: OperationInfo };
}

export interface MemberInfo {
    name: string,
    niceName: string;
    typeNiceName: string;
    type: TypeReference;
    isReadOnly?: boolean;
    isIgnoredEnum?: boolean;
    unit?: string;
    format?: string;
    maxLength?: number;
    isMultiline?: boolean;
    preserveOrder?: boolean;
    notVisible?: boolean;
    id?: any; //symbols
}

export interface OperationInfo {
    key: string,
    niceName: string;
    operationType: OperationType;
    canBeNew: boolean;
    canBeModified: boolean;
    hasCanExecute: boolean;
    hasStates: boolean;
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
export function toMomentFormat(format: string | undefined): string | undefined {

    if (!format)
        return undefined;

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
        case "s":
        case "o": return undefined;
        case "t": return "LT";
        case "T": return "LTS";
        case "y": return "LTS";
        case "Y": return "L";
        default: return format
            .replaceAll("y", "Y")
            .replaceAll("f", "S")
            .replaceAll("tt", "A")
            .replaceAll("t", "a")
            .replaceAll("dddd", "ßßßß") 
            .replaceAll("ddd", "ßßß")
            .replaceAll("d", "D") //replace only d -> D and dd -> DD
            .replaceAll("ßßß", "ddd")
            .replaceAll("ßßßß", "dddd");
    }
}


//https://msdn.microsoft.com/en-us/library/ee372286(v=vs.110).aspx
//https://github.com/jsmreese/moment-duration-format
export function toMomentDurationFormat(format: string | undefined): string | undefined{

    if (format == undefined)
        return undefined;

    return format.replace("\:", ":");
}

export function toNumbroFormat(format: string | undefined) {

    if (format == undefined)
        return undefined;

    const f = format.toUpperCase();

    if (f.startsWith("C"))
        return "0." + "0".repeat(parseInt(f.after("C") || "2"));

    if (f.startsWith("N"))
        return "0,0." + "0".repeat(parseInt(f.after("N") || "2"));

    if (f.startsWith("D"))
        return "0".repeat(parseInt(f.after("D") || "1"));

    if (f.startsWith("F"))
        return "0." + "0".repeat(parseInt(f.after("F") || "2"));

    if (f.startsWith("E"))
        return "0." + "0".repeat(parseInt(f.after("E") || "2"));

    if (f.startsWith("P"))
        return "0." + "0".repeat(parseInt(f.after("P") || "2")) + "%";

    if (f.contains("#"))
        format = format
            .replaceAll(".#", "[.]0")
            .replaceAll(",#", "[,]0")
            .replaceAll("#", "0");

    return format;
}

export function valToString(val: any) {
    if (val == null)
        return "";

    return val.toString();
}

export function numberToString(val: any, format?: string) {
    if (val == null)
        return "";

    return numbro(val).format(toNumbroFormat(format));
}

export function dateToString(val: any, format?: string) {
    if (val == null)
        return "";

    var m = moment(val, moment.ISO_8601);
    return m.format(toMomentFormat(format));
}

export function durationToString(val: any, format?: string) {
    if (val == null)
        return "";

    var dur = moment.duration(val);
    return dur.format(toMomentDurationFormat(format));
}


export interface TypeReference {
    name: string;
    typeNiceName?: string;
    isCollection?: boolean;
    isLite?: boolean;
    isNotNullable?: boolean;
    isEmbedded?: boolean;
}

export type KindOfType = "Entity" | "Enum" | "Message" | "Query" | "SymbolContainer";

export type EntityKind = "SystemString" | "System" | "Relational" | "String" | "Shared" | "Main" | "Part" | "SharedPart";
export const EntityKindValues: EntityKind[] = ["SystemString", "System", "Relational", "String", "Shared", "Main", "Part", "SharedPart"];

export type EntityData = "Master" | "Transactional";
export const EntityDataValues: EntityData[] = ["Master", "Transactional"];

export function getAllTypes(): TypeInfo[] {
    return Dic.getValues(_types);
}

export interface TypeInfoDictionary {
    [name: string]: TypeInfo
}

let _types: TypeInfoDictionary = {};


let _queryNames: {
    [queryKey: string]: MemberInfo
};

export type PseudoType = IType | TypeInfo | string;

export function getTypeName(pseudoType: IType | TypeInfo | string | Lite<Entity> | ModifiableEntity): string {
    if ((pseudoType as Lite<Entity>).EntityType)
        return (pseudoType as Lite<Entity>).EntityType;

    if ((pseudoType as ModifiableEntity).Type)
        return (pseudoType as ModifiableEntity).Type;
    
    if ((pseudoType as IType).typeName)
        return (pseudoType as IType).typeName;

    if ((pseudoType as TypeInfo).name)
        return (pseudoType as TypeInfo).name;

    if (typeof pseudoType == "string")
        return pseudoType as string;

    throw new Error("Unexpected pseudoType " + pseudoType);
}

export function isTypeEntity(type: PseudoType): boolean {
    const ti = getTypeInfo(type);
    return ti && ti.kind == "Entity" && !!ti.members["Id"];
}

export function isTypeEnum(type: PseudoType): boolean {
    const ti = getTypeInfo(type);
    return ti && ti.kind == "Enum";
}

export function isTypeModel(type: PseudoType): boolean {
    const ti = getTypeInfo(type);
    return ti && ti.kind == "Entity" && !ti.members["Id"];
}

export function isTypeEmbeddedOrValue(type: PseudoType): boolean {
    const ti = getTypeInfo(type);
    return !ti;
}

export function isTypeModifiableEntity(type: TypeReference): boolean {
    return type.isEmbedded == true || getTypeInfos(type).every(ti => ti != undefined && (isTypeEntity(ti) || isTypeModel(ti)));
}


export function getTypeInfo(type: PseudoType): TypeInfo {

    if ((type as TypeInfo).kind != undefined)
        return type as TypeInfo;

    if ((type as IType).typeName)
        return _types[((type as IType).typeName).toLowerCase()];

    if (typeof type == "string")
        return _types[(type as string).toLowerCase()];

    throw new Error("Unexpected type: " + type);
}

export function isLowPopulationSymbol(type: PseudoType) {

    var ti = getTypeInfo(type);

    return ti != null && ti.kind == "Entity" && ti.fullName.endsWith("Symbol") && ti.isLowPopulation;
}

export function parseId(ti: TypeInfo, id: string): string | number {
    return ti.members["Id"].type.name == "number" ? parseInt(id) : id;
}

export const IsByAll = "[ALL]";
export function getTypeInfos(typeReference: TypeReference | string): TypeInfo[] {

    const name = typeof typeReference == "string" ? typeReference : typeReference.name;

    if (name == IsByAll || name == "")
        return [];

    return name.split(", ").map(getTypeInfo);

}

export function getQueryNiceName(queryName: PseudoType | QueryKey): string {

    if ((queryName as TypeInfo).kind != undefined)
        return (queryName as TypeInfo).nicePluralName!;

    if (queryName instanceof Type)
        return (queryName as Type<any>).nicePluralName();

    if (queryName instanceof QueryKey)
        return (queryName as QueryKey).niceName();

    if (typeof queryName == "string") {
        const str = queryName as string;

        const type = _types[str.toLowerCase()];
        if (type)
            return type.nicePluralName!;

        const qn = _queryNames[str.toLowerCase()];
        if (qn)
            return qn.niceName;

        return str;
    }

    throw new Error("unexpected queryName type");

}

export function getQueryInfo(queryName: PseudoType | QueryKey): MemberInfo | TypeInfo {
    if (queryName instanceof QueryKey) {
        return queryName.memberInfo();
    }
    else {
        const ti = getTypeInfo(queryName);
        if (ti)
            return ti;

        const mi = _queryNames[(queryName as string).toLowerCase()];

        if (mi)
            return mi;

        throw Error("Unexpected query type");
    }
}

export function getQueryKey(queryName: PseudoType | QueryKey): string {
    if ((queryName as TypeInfo).kind != undefined)
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

    throw Error("Unexpected query type");
}

export function isQueryDefined(queryName: PseudoType | QueryKey): boolean {
    if ((queryName as TypeInfo).kind != undefined)
        return (queryName as TypeInfo).queryDefined || false;

    if (queryName instanceof Type)
        return getTypeInfo(queryName).queryDefined || false;

    if (queryName instanceof QueryKey)
        return !!_queryNames[queryName.name.toLowerCase()];

    if (typeof queryName == "string") {
        const str = queryName as string;

        const type = _types[str.toLowerCase()];
        if (type) {
            return type.queryDefined || false;
        }

        const qn = _queryNames[str.toLowerCase()];

        return !!qn;
    }

    return false;
}

export function reloadTypes(): Promise<void> {
    return ajaxGet<TypeInfoDictionary>({ url: "~/api/reflection/types" })
        .then(types => setTypes(types));
}

export function setTypes(types: TypeInfoDictionary) {

    Dic.foreach(types, (k, t) => {
        t.name = k;
        if (t.members) {
            Dic.foreach(t.members, (k2, t2) => t2.name = k2);
            Object.freeze(t.members);

            if (t.kind == "Enum") {
                t.membersById = Dic.getValues(t.members).toObject(a => a.name);
                Object.freeze(t.membersById);
            }
        }

        if (t.requiresSaveOperation == undefined && t.entityKind)
            t.requiresSaveOperation = calculateRequiresSaveOperation(t.entityKind);

        Object.freeze(t);
    });

    _types = Dic.getValues(types).toObject(a => a.name.toLowerCase());
    Object.freeze(_types);

    Dic.foreach(types, (k, t) => {
        if (t.operations) {
            Dic.foreach(t.operations, (k2, t2) => {
                t2.key = k2;
                const typeName = k2.before(".").toLowerCase();
                const memberName = k2.after(".");

                const ti = _types[typeName];
                if (!ti)
                    console.error(`Type ${typeName} not found (looking for operatons of ${t.name}). Consider synchronizing of calling ReflectionServer.RegisterLike.`);
                else {
                    const member = ti.members[k2.after(".")];
                    if (!member)
                        console.error(`Member ${memberName} not found in ${ti.name} (looking for operatons of ${t.name}). Consider synchronizing of calling ReflectionServer.RegisterLike.`);
                    else
                        t2.niceName = member.niceName;
                }
            });

            Object.freeze(t.operations);
        }
    });

    _queryNames = Dic.getValues(types).filter(t => t.kind == "Query")
        .flatMap(a => Dic.getValues(a.members))
        .toObject(m => m.name.toLocaleLowerCase(), m => m);

    Object.freeze(_queryNames);

    missingSymbols = missingSymbols.filter(s => {
        const m = getMember(s.key);
        if (m)
            s.id = m.id;
    });
}

function calculateRequiresSaveOperation(entityKind: EntityKind): boolean 
{
    switch (entityKind) {
        case "SystemString": return false;
        case "System": return false;
        case "Relational": return false;
        case "String": return true;
        case "Shared": return true;
        case "Main": return true;
        case "Part": return false;
        case "SharedPart": return false;
        default: throw new Error("Unexpeced entityKind");
    }
}

export interface IBinding<T> {
    getValue(): T;
    setValue(val: T): void;
    suffix: string;
    getError(): string | undefined;
    setError(value: string | undefined): void;
}

export class Binding<T> implements IBinding<T> {

    initialValue: T; // For deep compare

    constructor(
        public parentObject: any,
        public member: string | number) {
        this.initialValue = this.parentObject[member];
    }

    static create<F, T>(parentValue: F, fieldAccessor: (from: F) => T) {

        const memberName = Binding.getSingleMember(fieldAccessor);

        return new Binding<T>(parentValue, memberName);
    }

    static getSingleMember(fieldAccessor: (from: any) => any) {
        const members = getLambdaMembers(fieldAccessor);

        if (members.length != 1 || members[0].type != "Member")
            throw Error("invalid function 'fieldAccessor'");

        return members[0].name;
    }

    get suffix() {
        return this.member == null ? "--unknown--" : this.member.toString();
    }

    getValue(): T {

        if (!this.parentObject)
            throw new Error(`Impossible to get '${this.member}' from '${this.parentObject}'`);

        return this.parentObject[this.member];
    }
    setValue(val: T) {

        if (!this.parentObject)
            throw new Error(`Impossible to set '${this.member}' from '${this.parentObject}'`);

        const oldVal = this.parentObject[this.member];
        this.parentObject[this.member] = val;

        if ((this.parentObject as ModifiableEntity).Type) {
            if (oldVal !== val || Array.isArray(oldVal))
                (this.parentObject as ModifiableEntity).modified = true;
        }
    }

    deleteValue() {
        if (!this.parentObject)
            throw new Error(`Impossible to delete '${this.member}' from '${this.parentObject}'`);

        delete this.parentObject[this.member];
    }

    getError(): string | undefined {
        const parentErrors = (this.parentObject as ModifiableEntity).error;
        return parentErrors && parentErrors[this.member];
    }

    setError(value: string | undefined) {
        const parent = this.parentObject as ModifiableEntity;

        if (!value) {
            if (parent.error)
                delete parent.error[this.member];
        } else {
            if (!parent.Type)
                return;

            if (!parent.error)
                parent.error = {};

            parent.error[this.member] = value;
        }
    }
}

export class ReadonlyBinding<T> implements IBinding<T> {
    constructor(
        public value: T,
        public suffix: string) {
    }

    getValue() {
        return this.value;
    }
    setValue(val: T) {
        throw new Error("Readonly Binding");
    }

    getError(): string | undefined {
        return undefined;
    }

    setError(name: string | undefined): void {        
    }
}

export class MListElementBinding<T> implements IBinding<T>{

    suffix: string;
    constructor(
        public mListBinding: IBinding<MList<T>>,
        public index: number) {
        this.suffix = this.index.toString();
    }

    getValue() {
        var mlist = this.mListBinding.getValue();

        if (mlist.length <= this.index) //Some animations?
            return undefined as any as T;

        return mlist[this.index].element;
    }
    setValue(val: T) {
        var mlist = this.mListBinding.getValue()
        const mle = mlist[this.index];
        mle.rowId = null;
        mle.element = val;
        this.mListBinding.setValue(mlist);
    }

    getError(): string | undefined {
        return undefined;
    }

    setError(name: string | undefined): void {
    }
}

export function createBinding(parentValue: any, lambdaMembers: LambdaMember[]): IBinding<any> {

    if (lambdaMembers.length == 0)
        return new ReadonlyBinding<any>(parentValue, "");

    let val = parentValue;
    for (let i = 0; i < lambdaMembers.length  - 1; i++)
    {
        const member = lambdaMembers[i];
        switch (member.type) {

            case "Member": val = val[member.name]; break;
            case "Mixin": val = val.mixins[member.name]; break;
            default: throw new Error("Unexpected " + member.type);

        }
    }

    const lastMember = lambdaMembers[lambdaMembers.length - 1];
    switch (lastMember.type) {

        case "Member": return new Binding(val, lastMember.name); 
        case "Mixin": return new ReadonlyBinding(val.mixins[lastMember.name], "");
        default: throw new Error("Unexpected " + lastMember.type);
    }
}


const functionRegex = /^function\s*\(\s*([$a-zA-Z_][0-9a-zA-Z_$]*)\s*\)\s*{\s*(\"use strict\"\;)?\s*return\s*([^;]*)\s*;?\s*}$/;
const lambdaRegex = /^\s*\(?\s*([$a-zA-Z_][0-9a-zA-Z_$]*)\s*\)?\s*=>\s*{?\s*(return\s+)?([^;]*)\s*;?\s*}?$/;
const memberRegex = /^(.*)\.([$a-zA-Z_][0-9a-zA-Z_$]*)$/;
const memberIndexerRegex = /^(.*)\["([$a-zA-Z_][0-9a-zA-Z_$]*)"\]$/; 
const mixinMemberRegex = /^(.*)\.mixins\["([$a-zA-Z_][0-9a-zA-Z_$]*)"\]$/; //Necessary for some crazy minimizers
const indexRegex = /^(.*)\[(\d+)\]$/;

export function getLambdaMembers(lambda: Function): LambdaMember[]{

    var lambdaStr = (lambda as any).toString();

    const lambdaMatch = functionRegex.exec(lambdaStr) || lambdaRegex.exec(lambdaStr);

    if (lambdaMatch == undefined)
        throw Error("invalid function");

    const parameter = lambdaMatch[1];
    let body = lambdaMatch[3];
    let result: LambdaMember[] = [];

    while (body != parameter) {
        let m: RegExpExecArray | null;
        if (m = mixinMemberRegex.exec(body)) {
            result.push({ name: m[2], type: "Mixin" });
            body = m[1];
        }
        else if (m = memberRegex.exec(body) || memberIndexerRegex.exec(body)) {
            result.push({ name: m[2], type: "Member" });
            body = m[1];
        }
        else if (m = indexRegex.exec(body)) {
            result.push({ name: m[2], type: "Indexer" });
            body = m[1];
        }
        else {
            throw new Error(`Impossible to extract the properties from: ${body}` +
                (body.contains("Mixin") ? "\r\n Consider using subCtx(MyMixin) directly." : null));
        }
    }

    result = result.reverse();

    result = result.filter((m, i) => !(m.type == "Member" && m.name == "element" && i > 0 && result[i - 1].type == "Indexer"));

    return result;
}

export function getFieldMembers(field: string): LambdaMember[]{
    if (field.contains(".")) {
        var mixinType = field.before(".").trimStart("[").trimEnd("]");
        var fieldName = field.after(".");

        return [
            { type: "Mixin", name: mixinType },
            { type: "Member", name: fieldName.firstLower() }
        ];

    } else {
        return [
            { type: "Member", name: field.firstLower() }
        ];
    }
}




export interface LambdaMember {
    name: string;
    type: MemberType
}

export type MemberType = "Member" | "Mixin" | "Indexer";

export function New(type: PseudoType, props?: any): ModifiableEntity {

    const ti = getTypeInfo(type);

    const result = { Type: getTypeName(type), isNew: true, modified: true } as any as ModifiableEntity;

    if (ti) {

        var e = result as Entity;

        const mixins = Dic.getKeys(ti.members)
            .filter(a => a.startsWith("["))
            .groupBy(a => a.after("[").before("]"))
            .forEach(gr => {

                var m = ({ Type: gr.key, isNew: true, modified: true, }) as MixinEntity;

                if (!e.mixins)
                    e.mixins = {};

                e.mixins[gr.key] = m;
            });

        Dic.getValues(ti.members)
            .filter(a => a.type.isCollection && !a.name.contains("."))
            .forEach(m => (result as any)[m.name.firstLower()] = []);

        //TODO: Collections in Embeddeds...
    }

    if (props)
        Dic.assign(result, props);

    return result;
}

export interface IType {
    typeName: string;
}

export function isType(obj: any): obj is IType {
    return (obj as IType).typeName != undefined;
}

export function newLite<T extends Entity>(type: Type<T>, id: number | string | undefined): Lite<T>;
export function newLite(typeName: PseudoType, id: number | string | undefined): Lite<Entity>;
export function newLite(type: PseudoType, id: number | string | undefined): Lite<Entity>{
    return {
        EntityType: getTypeName(type),
        id: id
    };
}

export class Type<T extends ModifiableEntity> implements IType {

    New(props?: Partial<T>): T {
        return New(this.typeName, props) as T;
    }
    

    constructor(
        public typeName: string) { }

    tryTypeInfo(): TypeInfo {
        return getTypeInfo(this.typeName);
    }

    typeInfo(): TypeInfo {

        const result = this.tryTypeInfo();

        if (!result)
            throw new Error(`Type ${this.typeName} has no TypeInfo. If is an embedded? Then start from some main entity type containing it to get metadata for the embedded properties (i.e. MyEntity.propertyRoute(m => m.myEmbedded.someProperty)`);

        return result;
    }

    memberInfo(lambdaToProperty: (v: T) => any): MemberInfo {
        var pr = this.propertyRoute(lambdaToProperty);

        if (!pr.member)
            throw new Error(`${pr.propertyPath()} has no member`);

        return pr.member;
    }

    hasMixin(mixinType: Type<MixinEntity>): boolean {
        return Dic.getKeys(this.typeInfo().members).some(k => k.startsWith("[" + mixinType.typeName + "]"));
    }

    mixinMemberInfo<M extends MixinEntity>(mixinType: Type<M>, lambdaToProperty: (v: M) => any): MemberInfo {
        var pr = this.mixinPropertyRoute(mixinType, lambdaToProperty);

        if (!pr.member)
            throw new Error(`${pr.propertyPath()} has no member`);

        return pr.member;
    }

    propertyRoute(lambdaToProperty: (v: T) => any): PropertyRoute {
        return PropertyRoute.root(this.typeInfo()).addLambda(lambdaToProperty);
    }

    mixinPropertyRoute<M extends MixinEntity>(mixinType: Type<M>, lambdaToProperty: (v: M) => any): PropertyRoute {
        return PropertyRoute.root(this.typeInfo()).addMember("Mixin", mixinType.typeName).addLambda(lambdaToProperty);
    }

    niceName(): string {
        const ti = this.typeInfo();

        if (!ti.niceName)
            throw new Error(`no niceName found for ${ti.name}`);

        return ti.niceName;
    }

    nicePluralName(): string {
        const ti = this.typeInfo();

        if (!ti.nicePluralName)
            throw new Error(`no nicePluralName found for ${ti.name}`);

        return ti.nicePluralName;
    }

    niceCount(count: number): string {
        return count + " " + (count == 1 ? this.niceName() : this.nicePluralName());
    }

    nicePropertyName(lambdaToProperty: (v: T) => any): string  {
        const member = this.memberInfo(lambdaToProperty);

        if (!member.niceName)
            throw new Error(`no nicePropertyName found for ${member.name}`);

        return member.niceName;
    }

    isInstance(obj: any): obj is T {
        return obj && (obj as ModifiableEntity).Type == this.typeName;
    }

    isLite(obj: any): obj is Lite<T & Entity> {
        return obj && (obj as Lite<Entity>).EntityType == this.typeName;
    }
}

export class EnumType<T extends string> {
    constructor(public type: string) { }

    typeInfo(): TypeInfo {
        return getTypeInfo(this.type);
    }

    values(): T[] {
        return Dic.getKeys(this.typeInfo().members) as T[];
    }

    isDefined(val: any): val is T {
        return typeof val == "string" && this.typeInfo().members[val] != null
    }

    assertDefined(val: any): T {
        if (this.isDefined(val))
            return val;

        throw new Error(`'${val}' is not a valid ${this.type}`)
    }

    value(val: T): T {
        return val;
    }

    niceName(): string | undefined {
        return this.typeInfo().niceName;
    }

    niceToString(value: T): string {
        return this.typeInfo().members[value as string].niceName;
    }
}

export class MessageKey {

    constructor(
        public type: string,
        public name: string) { }

    propertyInfo(): MemberInfo {
        return getTypeInfo(this.type).members[this.name]
    }

    niceToString(...args: any[]): string {
        const msg = this.propertyInfo().niceName;

        return args.length ? msg.formatWith(...args) : msg;
    }
}

export class QueryKey {

    constructor(
        public type: string,
        public name: string) { }

    memberInfo(): MemberInfo {
        return getTypeInfo(this.type).members[this.name]
    }

    niceName(): string {
        return this.memberInfo().niceName;
    }
}

export interface ISymbol {
    Type: string; 
    key: string;
    id?: any;
}

let missingSymbols: ISymbol[] = [];

function getMember(key: string): MemberInfo | undefined {

    const type = _types[key.before(".").toLowerCase()];

    if (!type)
        return undefined;

    const member: MemberInfo | undefined = type.members[key.after(".")];

    return member;
}

export function symbolNiceName(symbol: Entity & ISymbol | Lite<Entity & ISymbol>) {
    if ((symbol as Entity).Type != null) //Don't use isEntity to avoid cycle
        return getMember((symbol as Entity & ISymbol).key)!.niceName;
    else
        return getMember(symbol.toStr!)!.niceName;
}

export function getSymbol<T extends Entity & ISymbol>(type: Type<T>, key: string) { //Unsafe Type!

    const mi = getMember(key);
    if (mi == null)
        throw new Error(`No Symbol with key '${key}' found`);

    var symbol = {
        Type: type.typeName,
        id: mi.id,
        key: key
    } as T;
    return symbol as T
}

export function registerSymbol(type: string, key: string): any /*ISymbol*/ {

    const mi = getMember(key);

    var symbol = {
        Type: type,
        id: mi && mi.id || null,
        key: key
    } as ISymbol;

    if (symbol.id == null)
        missingSymbols.push(symbol);

    return symbol as any;
}




export class PropertyRoute {
    
    propertyRouteType: PropertyRouteType;
    parent?: PropertyRoute; //!Root
    rootType?: TypeInfo; //Root
    member?: MemberInfo; //Member
    mixinName?: string; //Mixin

    static root(type: PseudoType) {
        const typeInfo = getTypeInfo(type);
        if (!typeInfo) {
            throw Error(`No TypeInfo for "${getTypeName(type)}" found. Consider calling ReflectionServer.RegisterLike on the server side.`);
        }
        return new PropertyRoute(undefined, "Root", typeInfo, undefined, undefined);
    }

    static member(parent: PropertyRoute, member: MemberInfo) {
        return new PropertyRoute(parent, "Field", undefined, member, undefined);
    }

    static mixin(parent: PropertyRoute, mixinName: string) {
        return new PropertyRoute(parent, "Mixin", undefined, undefined, mixinName);
    }

    static mlistItem(parent: PropertyRoute) {
        return new PropertyRoute(parent, "MListItem", undefined, undefined, undefined);
    }

    static liteEntity(parent: PropertyRoute) {
        return new PropertyRoute(parent, "LiteEntity", undefined, undefined, undefined);
    }

    

    static parse(rootType: PseudoType, propertyString: string): PropertyRoute {
        let result = PropertyRoute.root(rootType);
        const parts = propertyString.replaceAll("/", ".&&.").trimEnd(".").split(".").map((p): { type: MemberType, name: string } =>
        {
            if (p == "&&")
                return { type: "Indexer", name: "0" };


            if (p.startsWith("[") && p.endsWith("]"))
                return { type: "Mixin", name: p.trimStart("[").trimEnd("]") };

            return { type: "Member", name: p };
        });

        parts.forEach(m => result = result.addMember(m.type, m.name));

        return result;
    }

    constructor(
        parent: PropertyRoute | undefined,
        propertyRouteType: PropertyRouteType,
        rootType: TypeInfo | undefined,
        member: MemberInfo | undefined,
        mixinName: string | undefined) {

        this.propertyRouteType = propertyRouteType;
        this.parent = parent;
        this.rootType = rootType;
        this.member = member;
        this.mixinName = mixinName;
    }

    addLambda(property: ((val: any) => any) | string): PropertyRoute {
        const lambdaMembers = typeof property == "function" ?
            getLambdaMembers(property) :
            getFieldMembers(property);

        let result: PropertyRoute = lambdaMembers.reduce<PropertyRoute>((pr, m) => pr.addLambdaMember(m), this)

        return result;
    }

    typeReference(): TypeReference {
        switch (this.propertyRouteType) {
            case "Root": return { name: this.rootType!.name };
            case "Field": return this.member!.type;
            case "Mixin": throw new Error("mixins can not be used alone");
            case "MListItem": return { ...this.parent!.typeReference(), isCollection: false };
            case "LiteEntity": return { ...this.parent!.typeReference(), isLite: false };
            default: throw new Error("Unexpected propertyRouteType");
        }
    }

    typeReferenceInfo(): TypeInfo {
        return getTypeInfo(this.typeReference().name);
    }

    findRootType(): TypeInfo {
        switch (this.propertyRouteType) {
            case "Root": return this.rootType!;
            case "Field": return this.parent!.findRootType();
            case "Mixin": return this.parent!.findRootType();
            case "MListItem": return this.parent!.findRootType();
            case "LiteEntity": return this.parent!.findRootType();
            default: throw new Error("Unexpected propertyRouteType");
        }
    }

    propertyPath(): string {
        switch (this.propertyRouteType) {
            case "Root": throw new Error("Root has no PropertyString");
            case "Field": return this.member!.name;
            case "Mixin": return "[" + this.mixinName + "]";
            case "MListItem": return this.parent!.propertyPath() + "/";
            case "LiteEntity": return this.parent!.propertyPath() + ".entity";
            default: throw new Error("Unexpected propertyRouteType");
        }
    }

    tryAddMember(memberType: MemberType, memberName: string): PropertyRoute | undefined {
        try {
            return this.addMember(memberType, memberName);
        } catch (e) {
            return undefined;
        }
    }


    addLambdaMember(lm: LambdaMember): PropertyRoute {
        return this.addMember(lm.type, lm.type == "Member" ? toCSharp(lm.name) : lm.name)
    }


    addMember(memberType: MemberType, memberName: string): PropertyRoute {
        
        if (memberType == "Member") {

            if (this.propertyRouteType == "Field"  ||
                this.propertyRouteType == "MListItem" ||
                this.propertyRouteType == "LiteEntity") {
                const ref = this.typeReference();

                if (ref.isLite) {
                    if (memberName != "Entity")
                        throw new Error("Entity expected");

                    return PropertyRoute.liteEntity(this);
                }

                const ti = getTypeInfos(ref).single("Ambiguity due to multiple Implementations"); //[undefined]
                if (ti) {
                    
                    const m = ti.members[memberName];
                    if (!m)
                        throw new Error(`member '${memberName}' not found`);

                    return PropertyRoute.member(PropertyRoute.root(ti), m);
                } else if (this.propertyRouteType == "LiteEntity") {
                    throw Error("Unexpected lite case");
                }
            }

            const fullMemberName = this.propertyRouteType == "Root" ? memberName :
                this.propertyRouteType == "MListItem" ? this.propertyPath() + memberName :
                    this.propertyPath() + "." + memberName;

            const m = this.findRootType().members[fullMemberName];
            if (!m)
                throw new Error(`member '${fullMemberName}' not found`)

            return PropertyRoute.member(this, m);
        }

        if (memberType == "Mixin") {
            if (this.propertyRouteType != "Root")
                throw new Error("invalid mixin at this stage");

            return PropertyRoute.mixin(this, memberName);
        }

        if (memberType == "Indexer") {
            if (this.propertyRouteType != "Field")
                throw new Error("invalid indexer at this stage");

            const tr = this.typeReference();
            if (!tr.isCollection)
                throw new Error(`${this.propertyPath()} is not a collection`);

            return PropertyRoute.mlistItem(this);
        }

        throw new Error("not implemented");
    }

    static generateAll(type: PseudoType): PropertyRoute[] {
        var ti = getTypeInfo(type);
        var mixins: string[] = [];
        return Dic.getValues(ti.members).flatMap(mi => {
            const pr = PropertyRoute.parse(ti, mi.name);
            if (pr.typeReference().isCollection)
                return [pr, PropertyRoute.mlistItem(pr)];
            return [pr];
        }).flatMap(pr => {
            if (pr.parent && pr.parent.propertyRouteType == "Mixin" && !mixins.contains(pr.parent.propertyPath())) {
                mixins.push(pr.parent.propertyPath());
                return [pr.parent, pr];
            } else
                return [pr];
            });
    }

    subMembers(): { [subMemberName: string]: MemberInfo } {

        function simpleMembersAfter(type: TypeInfo, path: string) {
            return Dic.getValues(type.members)
                .filter(m => {
                    if (m.name == path || !m.name.startsWith(path))
                        return false;

                    var name = m.name.substring(path.length);
                    if (name.contains(".") || name.contains("/"))
                        return false;

                    return true;
                })
                .toObject(m => m.name.substring(path.length))
        }

        function mixinMembers(type: TypeInfo) {
            var mixins = Dic.getValues(type.members).filter(a => a.name.startsWith("[")).groupBy(a => a.name.after("[").before("]")).map(a => a.key);

            return mixins.flatMap(mn => Dic.getValues(simpleMembersAfter(type, `[${mn}].`))).toObject(a => a.name);
        }
        

        switch (this.propertyRouteType) {
            case "Root": return {
                ...simpleMembersAfter(this.findRootType(), ""),
                ...mixinMembers(this.findRootType())
            };
            case "Mixin": return simpleMembersAfter(this.findRootType(), this.propertyPath() + ".");                
            case "LiteEntity": return simpleMembersAfter(this.typeReferenceInfo(), "");
            case "Field":
            case "MListItem": 
                {
                    const ti = getTypeInfos(this.typeReference()).single("Ambiguity due to multiple Implementations"); //[undefined]
                    if (ti && isTypeEntity(ti))
                        return simpleMembersAfter(ti, "");
                    else
                        return simpleMembersAfter(this.findRootType(), this.propertyPath() + (this.propertyRouteType == "Field" ? "." : ""));
                }
            default: throw new Error("Unexpected propertyRouteType");

        }
    }

    toString() {
        if (this.propertyRouteType == "Root")
            return `(${this.findRootType().name})`;

        return `(${this.findRootType().name}).${this.propertyPath()}`;
    }
}

function toCSharp(name: string) {
    var result = name.firstUpper();

    if (result == name)
        throw new Error(`Name '${name}' should start by lowercase`);

    return result;
}

export type PropertyRouteType = "Root" | "Field" | "Mixin" | "LiteEntity" | "MListItem";

export type GraphExplorerMode = "collect" | "set" | "clean";


export class GraphExplorer {

    static propagateAll(...args: any[]) {
        const ge = new GraphExplorer("clean", {});
        args.forEach(o => ge.isModified(o, ""));
    }

    static setModelState(e: ModifiableEntity, modelState: ModelState | undefined, initialPrefix: string) {
        const ge = new GraphExplorer("set", modelState == undefined ? {} : { ...modelState });
        ge.isModifiableObject(e, initialPrefix);
        if (Dic.getValues(ge.modelState).length) //Assign remaining
        {
            if (e.error == undefined)
                e.error = {};

            for (const key in ge.modelState) {
                e.error[key] = ge.modelState[key].join("\n");
                delete ge.modelState[key];
            }
        }
    }

    static collectModelState(e: ModifiableEntity, initialPrefix: string): ModelState {
        const ge = new GraphExplorer("collect", {});
        ge.isModifiableObject(e, initialPrefix);
        return ge.modelState;
    }

    //cycle detection
    private modified : any[] = [];
    private notModified: any[] = [];

    constructor(mode: GraphExplorerMode, modelState: ModelState) {
        this.modelState = modelState;
        this.mode = mode;
    }

    private mode: GraphExplorerMode;

    private modelState: ModelState;


    isModified(obj: any, modelStatePrefix: string): boolean {

        if (obj == undefined)
            return false;

        const t = typeof obj;
        if (t != "object")
            return false;

        if (this.modified.contains(obj))
            return true;

        if (this.notModified.contains(obj))
            return false;

        const result = this.isModifiableObject(obj, modelStatePrefix);

        if (result) {
            this.modified.push(obj);
            return true;
        } else {
            this.notModified.push(obj);
            return false;
        }
    }

    private static specialProperties = ["Type", "id", "isNew", "ticks", "toStr", "modified"];

    //The redundant return true / return false are there for debugging
    private isModifiableObject(obj: any, modelStatePrefix: string) {

        if (obj instanceof Date)
            return false;

        if (obj instanceof Array) {
            let result = false;
            for (let i = 0; i < obj.length; i++) {
                if (this.isModified(obj[i], modelStatePrefix + "[" + i + "]"))
                    result = true;
            }
            return result;
        }

        const mle = obj as MListElement<any>;
        if (mle.hasOwnProperty && mle.hasOwnProperty("rowId")) {
            if (this.isModified(mle.element, dot(modelStatePrefix, "element")))
                return true;

            if (mle.rowId == undefined)
                return true;

            return false;
        };

        const lite = obj as Lite<Entity>
        if (lite.EntityType) {
            if (lite.entity != undefined && this.isModified(lite.entity, dot(modelStatePrefix, "entity")))
                return true;

            return false;
        }

        const mod = obj as ModifiableEntity;
        if (mod.Type == undefined) { //Other object
            let result = false;
            for (const p in obj) {
                if (obj.hasOwnProperty == null || obj.hasOwnProperty(p)) {
                    const propertyPrefix = dot(modelStatePrefix, p);
                    if (this.isModified(obj[p], propertyPrefix))
                        result = true;
                }
            }

            return result;
        }

        if (this.mode == "collect") {
            if (mod.error != undefined) {
                for (const p in mod.error) {
                    const propertyPrefix = dot(modelStatePrefix, p);

                    if (mod.error[p])
                        this.modelState[dot(modelStatePrefix, p)] = [mod.error[p]];
                }
            }
        }
        else if (this.mode == "set") {

            mod.error = undefined;

            const prefix = dot(modelStatePrefix, "");
            for (const key in this.modelState) {
                const propName = key.tryAfter(prefix)
                if (propName && !propName.contains(".")) {
                    if (mod.error == undefined)
                        mod.error = {};

                    mod.error[propName] = this.modelState[key].join("\n");

                    delete this.modelState[key];
                }
            }

            if (mod.error == undefined)
                delete mod.error;
        }
        else if (this.mode == "clean") {
            if (mod.error)
                delete mod.error
        }

        
        for (const p in obj) {
            if (obj.hasOwnProperty(p) && !GraphExplorer.specialProperties.contains(p)) {
                const propertyPrefix = dot(modelStatePrefix, p);
                if (this.isModified(obj[p], propertyPrefix))
                    mod.modified = true;
            }
        }

        if ((mod as Entity).isNew) {
            mod.modified = true; //Just in case

            if (GraphExplorer.TypesLazilyCreated.push((mod as Entity).Type))
                return false;
        }
      
        return mod.modified;
    }

    static TypesLazilyCreated: string[] = [];
}

function dot(prev: string, property: string) {
    if (prev == undefined || prev == "")
        return property;

    return prev + "." + property
}

