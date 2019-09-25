import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { DynamicCSSOverrideEntity } from './Signum.Entities.Dynamic'
import * as DynamicClientOptions from './DynamicClientOptions'

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(DynamicCSSOverrideEntity, w => import('./CSS/DynamicCSSOverride')));
  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicCSSOverrideEntity, t => [
    { token: t.append(p => p.name), type: "Text" },
    { token: t.append(p => p.script), type: "Code" },
  ]);
}
