
import * as React from 'react'
import { ifError } from '@framework/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '@framework/Services';
import { SearchControl, SearchValueLine } from '@framework/Search'
import * as Finder from '@framework/Finder'
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import MessageModal from '@framework/Modals/MessageModal'
import { EntityData, EntityKind, symbolNiceName } from '@framework/Reflection'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { NormalControlMessage } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import { DynamicTypeEntity, DynamicMixinConnectionEntity, DynamicTypeOperation, DynamicSqlMigrationEntity, DynamicRenameEntity, DynamicTypeMessage, DynamicPanelPermission, DynamicApiEntity } from './Signum.Entities.Dynamic'
import DynamicTypeComponent from './Type/DynamicType' //typings only
import * as DynamicClientOptions from './DynamicClientOptions'
import * as AuthClient from '../Authorization/AuthClient'
import { Tab } from 'react-bootstrap';

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(DynamicTypeEntity, w => import('./Type/DynamicType')));
  Navigator.addSettings(new EntitySettings(DynamicMixinConnectionEntity, w => import('./Type/DynamicMixinConnection')));
  Navigator.addSettings(new EntitySettings(DynamicSqlMigrationEntity, w => import('./Type/DynamicSqlMigration')));

  Operations.addSettings(new EntityOperationSettings(DynamicTypeOperation.Clone, {
    contextual: { icon: "clone", iconColor: "black" },
  }));

  Operations.addSettings(new EntityOperationSettings(DynamicTypeOperation.Save, {
    onClick: eoc => {
      (eoc.frame.entityComponent as DynamicTypeComponent).beforeSave();

      return Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key)
        .then(pack => { eoc.frame.onReload(pack); Operations.notifySuccess(); })
        .then(() => {
          if (AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel)) {
            return MessageModal.show({
              title: DynamicTypeMessage.TypeSaved.niceToString(),
              message: DynamicTypeMessage.DynamicType0SucessfullySavedGoToDynamicPanelNow.niceToString(eoc.entity.typeName),
              buttons: "yes_no",
              style: "success",
              icon: "success"
            }).then(result => {
              if (result == "yes") 
                window.open(AppContext.toAbsoluteUrl("~/dynamic/panel"));
              return;
            });
          }
        })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
    },
    alternatives: eoc => [],
  }));

  QuickLinks.registerQuickLink(DynamicTypeEntity, ctx => new QuickLinks.QuickLinkLink("ViewDynamicPanel",
    () => symbolNiceName(DynamicPanelPermission.ViewDynamicPanel), "~/dynamic/panel", {
      isVisible: AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel),
      icon: "up-down-left-right",
      iconColor: "purple",
    }));

  DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicTypeEntity }} />);
  DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicMixinConnectionEntity }} />);
  DynamicClientOptions.Options.getDynaicMigrationsStep = () =>
    <>
      <h3>{DynamicSqlMigrationEntity.nicePluralName()}</h3>
      <SearchControl findOptions={{ queryName: DynamicSqlMigrationEntity }} />
      <h3>{DynamicRenameEntity.nicePluralName()}</h3>
      <SearchControl findOptions={{ queryName: DynamicRenameEntity }} />
    </>;

  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicTypeEntity, t => [
    { token: t.append(p => p.typeName), type: "Text" },
    { token: t.append(p => p.entity.typeDefinition), type: "JSon" },
  ]);

  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicMixinConnectionEntity, t => [
    { token: t.append(p => p.mixinName), type: "Text" },
    { token: t.append(p => p.entity.entityType.entity!.cleanName), type: "Text" },
  ]);

  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicSqlMigrationEntity, t => [
    { token: t.append(p => p.comment), type: "Text" },
    { token: t.append(p => p.entity.script), type: "Code" },
  ]);

  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicRenameEntity, t => [
    { token: t.append(p => p.oldName), type: "Text" },
    { token: t.append(p => p.newName), type: "Text" },
    { token: t.append(p => p.replacementKey), type: "Text" },
  ]);
}

export namespace API {

  export function getPropertyType(property: DynamicProperty): Promise<string> {
    return ajaxPost({ url: `~/api/dynamic/type/propertyType` }, property);
  }

  export function expressionNames(typeName: string): Promise<Array<string>> {
    return ajaxGet({ url: `~/api/dynamic/type/expressionNames/${typeName}` });
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
