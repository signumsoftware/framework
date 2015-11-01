/// <reference path="globals.ts" />
import { baseUrl } from 'Framework/Signum.React/Scripts/Service'


export class PropertyRoute {

    parent: PropertyRoute;

    add(property: (val: any) => any): PropertyRoute {
        return null;
    } 
}


export interface TypeInfo
{
    kind: KindOfType;
    name: string;
    baseTypeName?: string;
    niceName?: string;
    nicePluralName?: string;
    allowed?: Allowed;

    properties: { [name: string]: PropertyInfo }
}

export interface PropertyInfo {
    name: string,
    niceName: string;
    allowed?: Allowed
    isCollection?: boolean;
    isLite?: boolean; 
    propertyTypeName?: string; //properties
    unit?: string; //properties
    format?: string; //properties
    id?: any; //symbols
}

export enum Allowed {
    None = 0,
    Read = 1,
    Modify = 2,
}

export enum KindOfType {
    Entity,
    Enum,
    Message,
    SymbolContainer, 
}

var _types: { [name: string]: TypeInfo };

export function typeInfo(name: string): TypeInfo {
    return _types[name];
}

export function setInitialTypes(types: TypeInfo[])
{
    _types = types.toObject(t=> t.name);

    symbols.forEach(s=> {
        s.id = _types[s.key.before(".")].properties[s.key.after(".")].id;
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


export class Type<T> {
    constructor(
        public type: string) { }

    typeInfo(): TypeInfo {
        return typeInfo(this.type);
    }

    propertyInfo(lambdaToProperty: (v: T) => any): PropertyInfo {
        return this.typeInfo().properties[lambdaBody(lambdaToProperty)];
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

        return this.typeInfo().properties[valueStr].niceName;
    }
}

export class MessageKey {

    constructor(
        public type: string,
        public name: string) { }

    propertyInfo(): PropertyInfo {
        return typeInfo(this.type).properties[this.name] 
    }

    niceName(): string {
        return this.propertyInfo().niceName;
    }
}

export class QueryKey {

    constructor(
        public type: string,
        public name: string) { }

    propertyInfo(): PropertyInfo {
        return typeInfo(this.type).properties[this.name] 
    }

    niceName(): string {
        return this.propertyInfo().niceName;
    }

    isAllowed() {
        return this.propertyInfo().allowed == Allowed.Modify;
    }
}

var symbols: { key?: string, id?: any }[] = [];

export function registerSymbol<T extends { key?: string, id?: any }>(symbol: T): T {
    symbols.push(symbol);
    return symbol;
} 
