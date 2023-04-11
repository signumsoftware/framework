import * as React from 'react'
import { RouteObject } from 'react-router'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { SearchControl, SearchValueLine } from '@framework/Search'
import * as Finder from '@framework/Finder'
import * as Constructor from '@framework/Constructor'
import * as DynamicClientOptions from './DynamicClientOptions'
import { DynamicApiEntity, DynamicApiEval } from './Signum.Dynamic.Controllers'

export function start(options: { routes: RouteObject[] }) {
  Navigator.addSettings(new EntitySettings(DynamicApiEntity, w => import('./Api/DynamicApi')));
  Constructor.registerConstructor(DynamicApiEntity, () => DynamicApiEntity.New({ eval: DynamicApiEval.New() }));
  DynamicClientOptions.Options.onGetDynamicLineForPanel.push(ctx => <SearchValueLine ctx={ctx} findOptions={{ queryName: DynamicApiEntity }} />);
  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicApiEntity, t => [
    { token: t.append(p => p.name), type: "Text" },
    { token: t.append(p => p.entity.eval!.script), type: "Code" },
  ]);
    
}
