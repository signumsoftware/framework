import * as React from 'react'
import { ajaxPost } from '@framework/Services';
import { ValueSearchControlLine } from '@framework/Search'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Entity } from '@framework/Signum.Entities'
import { PropertyRouteEntity } from '@framework/Signum.Entities.Basics'
import * as Constructor from '@framework/Constructor'
import * as DynamicClientOptions from './DynamicClientOptions'
import { DynamicValidationEntity, DynamicValidationEval } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(DynamicValidationEntity, w => import('./Validation/DynamicValidation')));
  Constructor.registerConstructor(DynamicValidationEntity, () => DynamicValidationEntity.New({ eval: DynamicValidationEval.New() }));

  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: DynamicValidationEntity });
  DynamicClientOptions.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{
    queryName: DynamicValidationEntity,
    parentToken: DynamicValidationEntity.token(a => a.entityType!.cleanName),
    parentValue: type
  }} />);
}

export namespace API {
  export function validationTest(request: DynamicValidationTestRequest): Promise<DynamicValidationTestResponse> {
    return ajaxPost<DynamicValidationTestResponse>({ url: `~/api/dynamic/validation/test` }, request);
  }

  export function routeTypeName(request: PropertyRouteEntity): Promise<string> {
    return ajaxPost<string>({ url: `~/api/dynamic/validation/routeTypeName` }, request);
  }
}


export interface DynamicValidationTestRequest {
  dynamicValidation: DynamicValidationEntity;
  exampleEntity: Entity;
}

export interface DynamicValidationTestResponse {
  compileError?: string;
  validationException?: string;
  validationResult?: DynamicValidationResult[];
}

export interface DynamicValidationResult {
  propertyName: string;
  validationResult: string;
}
