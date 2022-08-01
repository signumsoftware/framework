import * as React from 'react'
import { Location } from 'history'
import { getQueryKey, getQueryNiceName } from '@framework/Reflection'
import { getToString } from '@framework/Signum.Entities'
import * as Finder from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { IconColor, RefreshCounterEvent, ToolbarConfig, ToolbarResponse } from './ToolbarClient'
import { SearchValue, FindOptions } from '@framework/Search';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import { useDocumentEvent, useInterval } from '@framework/Hooks'
import { parseIcon } from '../Basics/Templates/IconTypeahead'
import a from 'bpmn-js/lib/features/search'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {
  constructor() {
    var type = QueryEntity;
    super(type);
  }

  getIcon(element: ToolbarResponse<QueryEntity>) {

    if (element.iconName == "count")
      return <CountIcon findOptions={{ queryName: getToString(element.content)! }} color={element.iconColor ?? "red"} autoRefreshPeriod={element.autoRefreshPeriod} />;

    return super.getIcon(element);
  }

  getDefaultIcon(): IconColor {
    return ({
      icon: ["far", "list-alt"],
      iconColor: "dodgerblue",
    });
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {
    if (!res.openInPopup)
      super.handleNavigateClick(e, res);
    else {
      Finder.explore({ queryName: getToString(res.content)! }).done()
    }
  }

  navigateTo(res: ToolbarResponse<QueryEntity>): Promise<string> {
    return Promise.resolve(Finder.findOptionsPath({ queryName: getToString(res.content)! }));
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<QueryEntity>, location: Location, query: any): number {
    return location.pathname == AppContext.toAbsoluteUrl(Finder.findOptionsPath({ queryName: getToString(res.content)! })) ? 1 : 0;
  }
}

interface CountIconProps {
  color?: string;
  autoRefreshPeriod?: number;
  findOptions: FindOptions;
}

export function CountIcon(p: CountIconProps) {

  const deps = useInterval(p.autoRefreshPeriod == null ? null : p.autoRefreshPeriod! * 1000, 0, a => a + 1);

  const [invalidations, setInvalidation] = React.useState<number>(0);

  useDocumentEvent("count-user-query", (e: Event) => {
    const queryKey = getQueryKey(p.findOptions.queryName);
    if (e instanceof RefreshCounterEvent) {
      if (e.queryKey == queryKey || Array.isArray(e.queryKey) && e.queryKey.contains(queryKey)) {
        setInvalidation(a => a + 1);
      } 
    }
  }, [p.findOptions.queryName]);

  return <SearchValue deps={[deps, invalidations]}
    findOptions={p.findOptions}
    customClass="icon"
    customStyle={{ color: p.color }}
    avoidNotifyPendingRequest={true}
  />;
}


