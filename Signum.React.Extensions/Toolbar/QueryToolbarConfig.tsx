import * as React from 'react'
import { Location } from 'history'
import { getQueryKey, getQueryNiceName, getTypeInfos, IsByAll } from '@framework/Reflection'
import { getToString } from '@framework/Signum.Entities'
import * as Finder from '@framework/Finder'
import * as AppContext from '@framework/AppContext'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { IconColor, ToolbarConfig, ToolbarResponse } from './ToolbarClient'
import { SearchValue, FindOptions } from '@framework/Search';
import * as Navigator from '@framework/Navigator';
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
        <>
          {super.getIcon(element)}
          <SearchToolbarCount findOptions={{ queryName: getToString(element.content)! }} color={element.iconColor ?? "red"} autoRefreshPeriod={element.autoRefreshPeriod} />
        </>
      );
    }

    return  super.getIcon(element);
  }

  getDefaultIcon(): IconColor {
    return ({
      icon: ["far", "rectangle-list"],
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

  var type = qd?.columns["Entity"].type.name;
  var types = type == null || type == IsByAll ? [] : type.split(",");

  Navigator.useEntityChanged(types, () => setInvalidation(a => a + 1), []);

  return <SearchValue deps={[deps, invalidations]}
    findOptions={p.findOptions}
    avoidNotifyPendingRequest={true}
    onRender={val => val == 0 && p.moreThanZero ? null :
        <ToolbarCount num={val}/>}
  />;
}


export function ToolbarCount(p: { num: number | null | undefined }) {
  return <div className="sf-toolbar-count-container"><div className={classes("badge badge-pill sf-toolbar-count", !p.num ? "bg-light text-secondary" : "bg-danger")}>{p.num ?? "…"}</div></div>
}


