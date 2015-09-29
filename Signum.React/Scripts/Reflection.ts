
export class PropertyRoute {

    parent: PropertyRoute;

    add(property: (val: any) => any): PropertyRoute {
        return null;
    } 
}

export interface TypeInfo
{
    name: string;
    namespace: string;
    baseType?: TypeInfoRef;
    niceName?: string;
    nicePluralName?: string;
    allowed?: Allowed;

    properties: { [name: string]: PropertyInfo }
}

export interface PropertyInfo {
    niceName: string;
    allowed?: Allowed
    isCollection?: boolean;
    isLite?: boolean;
    type?: TypeInfoRef;
}

export interface TypeInfoRef {
    namespace: string;
    name: string;
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
    Symbols, 
}
