
import * as React from 'react'
import { Route } from 'react-router'
import { Tab } from 'react-bootstrap'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { CountSearchControl } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityData, EntityKind } from '../../../Framework/Signum.React/Scripts/Reflection'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as EntityOperations from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicTypeEntity, DynamicTypeOperation } from './Signum.Entities.Dynamic'
import DynamicTypeEntityComponent from './Type/DynamicTypeEntity'
import * as DynamicClient from './DynamicClient'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicTypeEntity, w => new ViewPromise(resolve => require(['./Type/DynamicTypeEntity'], resolve))));

    Operations.addSettings(new EntityOperationSettings(DynamicTypeOperation.Save, {
        onClick: ctx => {
            (ctx.frame.entityComponent as DynamicTypeEntityComponent).beforeSave();
            EntityOperations.defaultExecuteEntity(ctx);
        }
    }));

    DynamicClient.Options.onGetDynamicLine.push(ctx => <CountSearchControl ctx={ctx} findOptions={{ queryName: DynamicTypeEntity }} />);
    DynamicClient.Options.onGetDynamicTab.push(() => <Tab key="migrations" title="Migrations" >Not implemented</Tab>);
}

export namespace API {

    export function getPropertyType(property: DynamicProperty): Promise<string> {
        return ajaxPost<string>({ url: `~/api/dynamic/type/propertyType` }, property);
    }
}

export type DynamicBaseType = "Entity";

export interface DynamicTypeDefinition {
    baseType: DynamicBaseType;
    entityKind?: EntityKind;
    entityData?: EntityData;
    tableName?: string;
    properties: DynamicProperty[];
    toStringExpression?: string;
}

export interface DynamicProperty {
    name: string;
    columnName?: string;
    type: string;
    isNullable: string;
    isLite?: boolean;
    isMList?: boolean;
    preserveOrder?: boolean;
    size?: number;
    scale?: number;
    _propertyType_?: string;
    validators?: Validators.DynamicValidator[];
}

export namespace Validators {

    export interface DynamicValidator {
        type: string;
    }
    
    export interface StringLength extends DynamicValidator {
        type: 'StringLength';
        allowNulls: boolean;
        multiLine: boolean;
        min ?: number;
        max ?: number;
        allowLeadingSpaces ?: boolean;
        allowTrailingSpaces ?: boolean;
    }

    export interface Decimals extends DynamicValidator {
        type: 'Decimals';
        decimalPlaces: number;
    }

    export interface NumberIs extends DynamicValidator {
        type: 'NumberIs';
        comparisonType: string;
        number: number;
    }

    export interface CountIs extends DynamicValidator {
        type: 'CountIs';
        comparisonType: string;
        number: number;
    }

    export const ComparisonTypeValues = ["EqualTo", "DistinctTo", "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo"];

    export interface NumberBetween extends DynamicValidator {
        type: 'NumberBetween';
        min: number;
        max: number;
    }

    export interface DateTimePrecision extends DynamicValidator {
        type: 'DateTimePrecision';
        precision: string;
    }

    export interface TimeSpanPrecision extends DynamicValidator {
        type: 'TimeSpanPrecision';
        precision: string;
    }

    export const DateTimePrecisionTypeValues = ["Days", "Hours", "Minutes", "Seconds", "Milliseconds"];

    export interface StringCase extends DynamicValidator {
        type: 'StringCase';
        textCase: string;
    }

    export const StringCaseTypeValues = ["UpperCase", "LowerCase"];   

}

export const IsNullableValues = ["Yes", "OnlyInMemory", "No"];
