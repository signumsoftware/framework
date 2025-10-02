
import * as React from 'react'
import { RouteObject } from 'react-router'
import { ifError } from '@framework/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '@framework/Services';
import { SearchControl, SearchValueLine } from '@framework/Search'
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import MessageModal from '@framework/Modals/MessageModal'
import { EntityData, EntityKind, symbolNiceName } from '@framework/Reflection'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import { QuickLinkClient, QuickLinkLink } from '@framework/QuickLinkClient'
import DynamicTypeComponent from './Type/DynamicType' //typings only
import { EvalClient } from '../Signum.Eval/EvalClient'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { DynamicTypeEntity, DynamicTypeMessage, DynamicTypeOperation } from './Signum.Dynamic.Types';
import { DynamicMixinConnectionEntity } from './Signum.Dynamic.Mixins';
import { DynamicRenameEntity, DynamicSqlMigrationEntity } from './Signum.Dynamic.SqlMigrations';
import { EvalPanelPermission } from '../Signum.Eval/Signum.Eval';

export namespace DynamicTypeClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(DynamicTypeEntity, w => import('./Type/DynamicType')));
    Navigator.addSettings(new EntitySettings(DynamicMixinConnectionEntity, w => import('./Type/DynamicMixinConnection')));
    Navigator.addSettings(new EntitySettings(DynamicSqlMigrationEntity, w => import('./Type/DynamicSqlMigration')));
  
    Operations.addSettings(new EntityOperationSettings(DynamicTypeOperation.Clone, {
      contextual: { icon: "clone", iconColor: "var(--bs-body-color)" },
    }));
  
    Operations.addSettings(new EntityOperationSettings(DynamicTypeOperation.Save, {
      onClick: eoc => {
        (eoc.frame.entityComponent as DynamicTypeComponent).beforeSave();
  
        return Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key)
          .then(pack => { eoc.frame.onReload(pack); Operations.notifySuccess(); })
          .then(() => {
            if (AppContext.isPermissionAuthorized(EvalPanelPermission.ViewDynamicPanel)) {
              return MessageModal.show({
                title: DynamicTypeMessage.TypeSaved.niceToString(),
                message: DynamicTypeMessage.DynamicType0SucessfullySavedGoToDynamicPanelNow.niceToString(eoc.entity.typeName),
                buttons: "yes_no",
                style: "success",
                icon: "success"
              }).then(result => {
                if (result == "yes") 
                  window.open(AppContext.toAbsoluteUrl("/dynamic/panel"));
                return;
              });
            }
          })
          .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
      },
      alternatives: eoc => [],
    }));
  
    QuickLinkClient.registerQuickLink(DynamicTypeEntity, new QuickLinkLink("ViewDynamicPanel", () => symbolNiceName(EvalPanelPermission.ViewDynamicPanel), () => "/dynamic/panel",
      {
        isVisible: AppContext.isPermissionAuthorized(EvalPanelPermission.ViewDynamicPanel),
        icon: "up-down-left-right",
        iconColor: "purple"
      }
    ));
  
    EvalClient.Options.onGetDynamicLineForPanel.push(ctx => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicTypeEntity }} />);
    EvalClient.Options.onGetDynamicLineForPanel.push(ctx => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicMixinConnectionEntity }} />);
    EvalClient.Options.getDynaicMigrationsStep = () =>
      <>
        <h3>{DynamicSqlMigrationEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: DynamicSqlMigrationEntity }} />
        <h3>{DynamicRenameEntity.nicePluralName()}</h3>
        <SearchControl findOptions={{ queryName: DynamicRenameEntity }} />
      </>;
  
    EvalClient.Options.registerDynamicPanelSearch(DynamicTypeEntity, t => [
      { token: t.append(p => p.typeName), type: "Text" },
      { token: t.append(p => p.entity.typeDefinition), type: "JSon" },
    ]);
  
    EvalClient.Options.registerDynamicPanelSearch(DynamicMixinConnectionEntity, t => [
      { token: t.append(p => p.mixinName), type: "Text" },
      { token: t.append(p => p.entity.entityType.entity!.cleanName), type: "Text" },
    ]);
  
    EvalClient.Options.registerDynamicPanelSearch(DynamicSqlMigrationEntity, t => [
      { token: t.append(p => p.comment), type: "Text" },
      { token: t.append(p => p.entity.script), type: "Code" },
    ]);
  
    EvalClient.Options.registerDynamicPanelSearch(DynamicRenameEntity, t => [
      { token: t.append(p => p.oldName), type: "Text" },
      { token: t.append(p => p.newName), type: "Text" },
      { token: t.append(p => p.replacementKey), type: "Text" },
    ]);
  }
  
  export namespace API {
  
    export function getPropertyType(property: DynamicProperty): Promise<string> {
      return ajaxPost({ url: `/api/dynamic/type/propertyType` }, property);
    }
  
    export function expressionNames(typeName: string): Promise<Array<string>> {
      return ajaxGet({ url: `/api/dynamic/type/expressionNames/${typeName}` });
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
    operationClone?: OperationConstructFrom;
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
    unit?: string;
    format?: string;
    notifyChanges?: boolean;
    _propertyType_?: string;
    validators?: Validators.DynamicValidator[];
    customFieldAttributes?: string;
    customPropertyAttributes?: string;
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
  
  export interface OperationConstructFrom {
    canConstruct?: string;
    construct: string;
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
  
    export interface NotNull extends DynamicValidator {
      type: 'NotNull';
      disabled?: number;
    }
  
    export interface StringLength extends DynamicValidator {
      type: 'StringLength';
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
  
    export const ComparisonTypeValues: string[] = ["EqualTo", "DistinctTo", "GreaterThan", "GreaterThanOrEqualTo", "LessThan", "LessThanOrEqualTo"];
  
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
  
    export const DateTimePrecisionTypeValues : string[] = ["Days", "Hours", "Minutes", "Seconds", "Milliseconds"];
  
    export interface StringCase extends DynamicValidator {
      type: 'StringCase';
      textCase: string;
    }
  
    export const StringCaseTypeValues: string[] = ["UpperCase", "LowerCase"];
  
  }
  
  export const IsNullableValues : string[] = ["Yes", "OnlyInMemory", "No"];
  export const UniqueIndexValues: string[] = ["No", "Yes", "YesAllowNull"];
}
