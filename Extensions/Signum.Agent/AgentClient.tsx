import * as React from 'react'
import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator';
import * as AppContext from '@framework/AppContext';
import { TypeContext } from '@framework/TypeContext';
import { SkillCustomizationEntity } from './Signum.Agent';

export namespace AgentClient {

  export function start(options: { routes: unknown[] }): void {
    Navigator.addSettings(new EntitySettings(SkillCustomizationEntity, e => import('./Templates/SkillCustomization')));
    AppContext.clearSettingsActions.push(() => propertyValueRegistry.clear());
  }

  export type PropertyValueFactory = (
    ctx: TypeContext<string | null>,
    meta: SkillPropertyMeta
  ) => React.ReactElement;

  const propertyValueRegistry = new Map<string, PropertyValueFactory>();

  export function registerPropertyValueControl(attributeName: string, factory: PropertyValueFactory): void {
    propertyValueRegistry.set(attributeName, factory);
  }

  export function getPropertyValueControl(attributeName: string): PropertyValueFactory | undefined {
    return propertyValueRegistry.get(attributeName);
  }

  export namespace API {
    export function getSkillCodeInfo(skillCode: string): Promise<SkillCodeInfo> {
      return ajaxGet({ url: `/api/agentSkill/skillCodeInfo/${encodeURIComponent(skillCode)}` });
    }
  }
}


export interface SkillPropertyMeta {
    propertyName: string;
    attributeName: string;
    valueHint: string | null;
    propertyType: string;
  }

  export interface SkillCodeInfo {
    defaultShortDescription: string;
    defaultInstructions: string;
    properties: SkillPropertyMeta[];
  }

