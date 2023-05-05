import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost } from '@framework/Services';
import { SearchValueLine } from '@framework/Search'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Entity } from '@framework/Signum.Entities'
import { PropertyRouteEntity } from '@framework/Signum.Basics'
import * as Constructor from '@framework/Constructor'
import * as EvalClient from '../Signum.Eval/EvalClient'
import { DynamicValidationEntity, DynamicValidationEval } from './Signum.Dynamic.Validations';

export function start(options: { routes: RouteObject[] }) {
  Navigator.addSettings(new EntitySettings(DynamicValidationEntity, w => import('./Validation/DynamicValidation')));
  Constructor.registerConstructor(DynamicValidationEntity, () => DynamicValidationEntity.New({ eval: DynamicValidationEval.New() }));

  EvalClient.Options.checkEvalFindOptions.push({ queryName: DynamicValidationEntity });
  EvalClient.Options.onGetDynamicLineForType.push((ctx, type) => <SearchValueLine ctx={ctx} findOptions={{
    queryName: DynamicValidationEntity,
    filterOptions: [{ token: DynamicValidationEntity.token(a => a.entityType!.cleanName), value: type}]
  }} />);
  EvalClient.Options.registerDynamicPanelSearch(DynamicValidationEntity, t => [
    { token: t.append(p => p.entity.entityType.cleanName), type: "Text" },
    { token: t.append(p => p.entity.name), type: "Text" },
    { token: t.append(p => p.entity.eval!.script), type: "Text" },
  ]);
}

export namespace API {
  export function validationTest(request: DynamicValidationTestRequest): Promise<DynamicValidationTestResponse> {
    return ajaxPost({ url: `/api/dynamic/validation/test` }, request);
  }

  export function routeTypeName(request: PropertyRouteEntity): Promise<string> {
    return ajaxPost({ url: `/api/dynamic/validation/routeTypeName` }, request);
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
