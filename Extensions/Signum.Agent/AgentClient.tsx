import * as React from 'react'
import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator';
import * as AppContext from '@framework/AppContext';
import { TypeContext } from '@framework/TypeContext';
import { LanguageModelClient } from './LanguageModelClient';
import { AgentSymbol, SkillActivation, SkillCustomizationEntity } from './Signum.Agent';

export namespace AgentClient {

  export function start(options: { routes: unknown[] }): void {

   
    Navigator.addSettings(new EntitySettings(SkillCustomizationEntity, e => import('./Templates/SkillCustomization')));
    Navigator.addSettings(new EntitySettings(AgentSymbol, e => import('./Templates/Agent')));

    LanguageModelClient.start(options);
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

    export function getDefaultAgentSkillCodeInfo(agentName: string): Promise<SkillCodeInfo> {
      return ajaxGet({ url: `/api/agentSkill/defaultAgentSkillCodeInfo/${encodeURIComponent(agentName)}` });
    }
  }
}


export interface SkillPropertyMeta {
  propertyName: string;
  attributeName: string;
  valueHint: string | null;
  propertyType: string;
  defaultValue: string | null;
}

export interface SkillCodeInfo {
  defaultShortDescription: string;
  defaultInstructions: string;
  properties: SkillPropertyMeta[];
  tools: ToolInfo[];
  subSkills: SubSkillInfo[];
}

export interface ToolInfo {
  mcpName: string;
  description: string | null;
  returnType: string;
  parameters: ToolParameter[];
}

export interface ToolParameter {
  name: string;
  type: string;
  isRequired: boolean;
  description: string | null;
}

export interface SubSkillInfo {
  className: string;
  activation: SkillActivation;
  info: SkillCodeInfo;
}

