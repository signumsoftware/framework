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
import { useAPI, useDocumentEvent, useInterval, useUpdatedRef } from '@framework/Hooks'
import { parseIcon } from '../Basics/Templates/IconTypeahead'
import a from 'bpmn-js/lib/features/search'
import { classes } from '../../Signum.React/Scripts/Globals'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {
  constructor() {
    var type = QueryEntity;
    super(type);
  }

  getIcon(element: ToolbarResponse<QueryEntity>) {

    if (element.showCount != null) {
      return (
        <div>
          {super.getIcon(element)}
          <SearchToolbarCount findOptions={{ queryName: getToString(element.content)! }} color={element.iconColor ?? "red"} autoRefreshPeriod={element.autoRefreshPeriod} />
        </div>
      );
    }

    return  super.getIcon(element);
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
      Finder.explore({ queryName: getToString(res.content)! })
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
  moreThanZero?: boolean;
}

export function SearchToolbarCount(p: CountIconProps) {

  const deps = useInterval(p.autoRefreshPeriod == null ? null : p.autoRefreshPeriod! * 1000, 0, a => a + 1);

  const [invalidations, setInvalidation] = React.useState<number>(0);

  var qd = useAPI(() => Finder.getQueryDescription(p.findOptions.queryName), [p.findOptions.queryName]);
  var qdRef = useUpdatedRef(qd);



  useDocumentEvent("count-user-query", (e: Event) => {
    if (e instanceof RefreshCounterEvent && qd != null) {
      var col = qd.columns["Entity"];

      if (e.queryKey == qd?.columns || Array.isArray(e.queryKey) && e.queryKey.contains(queryKey)) {
        setInvalidation(a => a + 1);
      } 
    }
  }, []);

  return <SearchValue deps={[deps, invalidations]}
    findOptions={p.findOptions}
    avoidNotifyPendingRequest={true}
    onRender={val => val == 0 && p.moreThanZero ? null :
        <ToolbarCount num={val}/>}
  />;
}


export function ToolbarCount(p: { num: number  | null | undefined }) {
  return <div className={classes("badge badge-pill sf-toolbar-count", !p.num ? "btn-light text-secondary" : "btn-danger")}>{p.num ?? "…"}</div>
}


