/// <reference path="globals.ts" />

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
        ti.membersById = Dic.getValues(ti.members).toObject(a=> a.id);

    return ti.membersById[enumId];
}

export interface TypeInfo
{
    kind: KindOfType;
    name: string;
    niceName?: string;
    nicePluralName?: string;
    entityKind?: EntityKind;
    entityData?: EntityData;
    members?: { [name: string]: MemberInfo };
    membersById?: { [name: string]: MemberInfo };
    mixins?: { [name: string]: string };
}

export interface MemberInfo {
    name: string,
    niceName: string;
    isCollection?: boolean;
    isLite?: boolean; 
    isNullable?: boolean; 
    type?: string; 
    unit?: string; 
    format?: string; 
    id?: any; //symbols
}

export enum KindOfType {
    Entity,
    Enum,
    Message,
    Query,
    SymbolContainer, 
}

export enum EntityKind {
    /// <summary>
    /// Doesn't make sense to view it from other entity, since there's not to much to see. Not editable. 
    /// Not SaveProtected
    /// ie: PermissionSymbol
    /// </summary>
    SystemString,

    /// <summary>
    /// Not editable.
    /// Not SaveProtected
    /// ie: ExceptionEntity
    /// </summary>
    System,

    /// <summary>
    /// An entity that connects two entitities to implement a N to N relationship in a symetric way (no MLists)
    /// Not SaveProtected, not vieable, not creable (override on SearchControl) 
    /// ie: DiscountProductEntity
    /// </summary>
    Relational,


    /// <summary>
    /// Doesn't make sense to view it from other entity, since there's not to much to see. 
    /// SaveProtected
    /// ie: CountryEntity
    /// </summary>
    String,

    /// <summary>
    /// Used and shared by other entities, can be created from other entity. 
    /// SaveProtected
    /// ie: CustomerEntity (can create new while creating the order)
    /// </summary>
    Shared,

    /// <summary>
    /// Used and shared by other entities, but too big to create it from other entity.
    /// SaveProtected
    /// ie: OrderEntity
    /// </summary>
    Main,

    /// <summary>
    /// Entity that belongs to just one entity and should be saved together, but that can not be implemented as EmbeddedEntity (usually to enable polymorphisim)
    /// Not SaveProtected
    /// ie :ProductExtensionEntity
    /// </summary>
    Part,

    /// <summary>
    /// Entity that can be created on the fly and saved with the parent entity, but could also be shared with other entities to save space. 
    /// Not SaveProtected
    /// ie: AddressEntity
    /// </summary>
    SharedPart,
}

export enum EntityData {
    /// <summary>
    /// Entity created for business definition
    /// By default ordered by id Ascending
    /// ie: ProductEntity, OperationEntity, PermissionEntity, CountryEntity...  
    /// </summary>
    Master,

    /// <summary>
    /// Entity created while the business is running
    /// By default is ordered by id Descending
    /// ie: OrderEntity, ExceptionEntity, OperationLogEntity...
    /// </summary>
    Transactional
}

var _types: { [name: string]: TypeInfo };

export function typeInfo(name: string): TypeInfo {
    return _types[name];
}

export function setInitialTypes(types: TypeInfo[])
{
    _types = types.toObject(t=> t.name);

    symbols.forEach(s=> {
        s.id = _types[s.key.before(".")].members[s.key.after(".")].id;
    });
}

export function lambdaBody(lambda: Function): string
{
    return lambda.toString().after("return ").after(".").before(";");
}


//Type -> niceName nicePluralName
//Message -> niceToString
//Operation -> niceToString()
//Enum -> niceName

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

var symbols: { key?: string, id?: any }[] = [];

export function registerSymbol<T extends { key?: string, id?: any }>(symbol: T): T {
    symbols.push(symbol);
    return symbol;
} 
