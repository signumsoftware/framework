import * as React from 'react'
import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator';
import * as AppContext from '@framework/AppContext';
import { TypeContext } from '@framework/TypeContext';
import { AgentSkillEntity, SkillPropertyMeta, SkillCodeInfo } from './Signum.Agent';

export namespace AgentSkillClient {

  export function start(options: { routes: unknown[] }): void {
    Navigator.addSettings(new EntitySettings(AgentSkillEntity, e => import('./Templates/AgentSkill')));
    AppContext.clearSettingsActions.push(() => propertyValueRegistry.clear());
  }

  // ─── Property value control registry ─────────────────────────────────────

  export type PropertyValueFactory = (
    ctx: TypeContext<string | null>,
    meta: SkillPropertyMeta
  ) => React.ReactElement;

  const propertyValueRegistry = new Map<string, PropertyValueFactory>();

  /**
   * Register a custom control for editing AgentSkillPropertyOverride.value,
   * keyed by the C# attribute name without "Attribute"
   * (e.g. "AgentSkillProperty_QueryList", "AgentSkillProperty").
   */
  export function registerPropertyValueControl(
    attributeName: string,
    factory: PropertyValueFactory
  ): void {
    propertyValueRegistry.set(attributeName, factory);
  }

  export function getPropertyValueControl(attributeName: string): PropertyValueFactory | undefined {
    return propertyValueRegistry.get(attributeName);
  }

  // ─── API ──────────────────────────────────────────────────────────────────

  export namespace API {
    export function getSkillCodeInfo(skillCode: string): Promise<SkillCodeInfo> {
      return ajaxGet({ url: `/api/agentSkill/skillCodeInfo/${encodeURIComponent(skillCode)}` });
    }
  }
}
