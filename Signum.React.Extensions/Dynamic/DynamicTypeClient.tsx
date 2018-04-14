
import * as React from 'react'
import { Route } from 'react-router'
import { ifError } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '../../../Framework/Signum.React/Scripts/Services';
import { SearchControl, ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import MessageModal from '../../../Framework/Signum.React/Scripts/Modals/MessageModal'
import { EntityData, EntityKind, symbolNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as EntityOperations from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { Entity, NormalControlMessage, NormalWindowMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as QuickLink from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { StyleContext } from '../../../Framework/Signum.React/Scripts/TypeContext'


import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicTypeEntity, DynamicMixinConnectionEntity, DynamicTypeOperation, DynamicSqlMigrationEntity, DynamicRenameEntity, DynamicTypeMessage, DynamicPanelPermission } from './Signum.Entities.Dynamic'
import DynamicTypeComponent from './Type/DynamicType' //typings only
import * as DynamicClient from './DynamicClient'
import * as AuthClient from '../Authorization/AuthClient'
import { Tab } from '../../../Framework/Signum.React/Scripts/Components/Tabs';

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicTypeEntity, w => import('./Type/DynamicType')));
    Navigator.addSettings(new EntitySettings(DynamicMixinConnectionEntity, w => import('./Type/DynamicMixinConnection')));
    Navigator.addSettings(new EntitySettings(DynamicSqlMigrationEntity, w => import('./Type/DynamicSqlMigration')));

    Operations.addSettings(new EntityOperationSettings(DynamicTypeOperation.Clone, {
        contextual: { icon: "fa fa-clone", iconColor: "black" },
    }));

    Operations.addSettings(new EntityOperationSettings(DynamicTypeOperation.Save, {
        onClick: eoc => {
            (eoc.frame.entityComponent as DynamicTypeComponent).beforeSave();

            Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key)
                .then(pack => { eoc.frame.onReload(pack); EntityOperations.notifySuccess(); })
                .then(() => {
                    if (AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel)) {
                        MessageModal.show({
                            title: NormalControlMessage.Save.niceToString(),
                            message: DynamicTypeMessage.DynamicType0SucessfullySavedGoToDynamicPanelNow.niceToString(eoc.entity.typeName),
                            buttons: "yes_no",
                            style: "success",
                            icon: "success"
                        }).then(result => {
                            if (result == "yes")
                                window.open(Navigator.toAbsoluteUrl("~/dynamic/panel"));
                        }).done();
                    }
                })
                .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
                .done();
        }
    }));

    QuickLink.registerQuickLink(DynamicTypeEntity, ctx => new QuickLink.QuickLinkLink("ViewDynamicPanel",
        symbolNiceName(DynamicPanelPermission.ViewDynamicPanel), "~/dynamic/panel", {
            isVisible: AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel),
            icon: "fa fa-arrows-alt",
            iconColor: "purple",
        }));

    DynamicClient.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicTypeEntity }} />);
    DynamicClient.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicMixinConnectionEntity }} />);
    DynamicClient.Options.getDynaicMigrationsStep = () =>
        <Tab eventKey="migrations" title="Migrations" >
            <h3>{DynamicSqlMigrationEntity.nicePluralName()}</h3>
            <SearchControl findOptions={{ queryName: DynamicSqlMigrationEntity }} />
            <h3>{DynamicRenameEntity.nicePluralName()}</h3>
            <SearchControl findOptions={{ queryName: DynamicRenameEntity }} />
        </Tab>;
}

export namespace API {

    export function getPropertyType(property: DynamicProperty): Promise<string> {
        return ajaxPost<string>({ url: `~/api/dynamic/type/propertyType` }, property);
    }

    export function expressionNames(typeName: string): Promise<Array<string>> {
        return ajaxGet<Array<string>>({ url: `~/api/dynamic/type/expressionNames/${typeName}` });
    }
}

export interface DynamicTypeDefinition {
    primaryKey?: DynamicTypePrimaryKeyDefinition;
    ticks?: DynamicTypeTicksDefinition;
    tableName?: string;
    entityKind?: EntityKind;
    entityData?: EntityData;
    operationCreate?: OperationConstruct;
    operationSave?: OperationExecute;
    operationDelete?: OperationDelete;
    customInheritance?: DynamicTypeCustomCode;
    customEntityMembers?: DynamicTypeCustomCode;
    customStartCode?: DynamicTypeCustomCode;
    customLogicMembers?: DynamicTypeCustomCode;
    customTypes?: DynamicTypeCustomCode;
    customBeforeSchema?: DynamicTypeCustomCode;
    queryFields: string[];
    multiColumnUniqueIndex?: MultiColumnUniqueIndex;
    properties: DynamicProperty[];
    toStringExpression?: string;
}

export interface DynamicProperty {
    uid: string;
    name: string;
    columnName?: string;
    type: string;
    columnType?: string;
    isNullable: string;
    uniqueIndex: string;
    isLite?: boolean;
    isMList?: DynamicTypeBackMListDefinition;
    size?: number;
    scale?: number;
    _propertyType_?: string;
    validators?: Validators.DynamicValidator[];
    customAttributes?: string;
}

export interface DynamicTypePrimaryKeyDefinition {
    name?: string;
    type?: string;
    identity: boolean;
}

export interface DynamicTypeTicksDefinition {
    hasTicks: boolean;
    name?: string;
    type?: string;
}

export interface DynamicTypeBackMListDefinition {
    tableName?: string;
    preserveOrder: boolean;
    orderName?: string;
    backReferenceName?: string;
}

export interface MultiColumnUniqueIndex {
    fields: string[];
    where?: string;
}

export interface OperationConstruct {
    construct: string;
}

export interface OperationExecute {
    canExecute?: string;
    execute: string;
}

export interface OperationDelete {
    canDelete?: string;
    delete: string;
}

export interface DynamicTypeCustomCode {
    code?: string;
}


export namespace Validators {

    export interface DynamicValidator {
        type: string;
    }

    export interface StringLength extends DynamicValidator {
        type: 'StringLength';
        allowNulls: boolean;
        multiLine: boolean;
        min?: number;
        max?: number;
        allowLeadingSpaces?: boolean;
        allowTrailingSpaces?: boolean;
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
export const UniqueIndexValues = ["No", "Yes", "YesAllowNull"];
