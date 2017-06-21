
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { ValueSearchControlLine } from '../../../Framework/Signum.React/Scripts/Search'
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { PropertyRouteEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as DynamicClient from './DynamicClient'
import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { DynamicValidationEntity, DynamicValidationOperation, DynamicValidationEval } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {

    Navigator.addSettings(new EntitySettings(DynamicValidationEntity, w => import('./Validation/DynamicValidation')));
    Constructor.registerConstructor(DynamicValidationEntity, () => DynamicValidationEntity.New({ eval: DynamicValidationEval.New() }));

    DynamicClient.Options.onGetDynamicLineForPanel.push(ctx => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicValidationEntity }} />);
    DynamicClient.Options.onGetDynamicLineForType.push((ctx, type) => <ValueSearchControlLine ctx={ctx} findOptions={{ queryName: DynamicValidationEntity, parentColumn: "EntityType.CleanName", parentValue: type }} />);
}

export namespace API {
    export function validationTest(request: DynamicValidationTestRequest): Promise<DynamicValidationTestResponse> {
        return ajaxPost<DynamicValidationTestResponse>({ url: `~/api/dynamic/validation/test` }, request);
    }

    export function parentType(request: PropertyRouteEntity): Promise<string> {
        return ajaxPost<string>({ url: `~/api/dynamic/validation/parentType` }, request);
    }
}


export interface DynamicValidationTestRequest {
    dynamicValidation: DynamicValidationEntity;
    exampleEntity: Entity;
}

export interface DynamicValidationTestResponse {
    compileError?: string;
    validationException?: string;
    validationResult?: string[];
}

