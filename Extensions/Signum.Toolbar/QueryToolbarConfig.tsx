import * as React from 'react'
import { Location } from 'react-router'
import { IsByAll } from '@framework/Reflection'
import { getToString } from '@framework/Signum.Entities'
import { Finder } from '@framework/Finder'
import { QueryEntity } from '@framework/Signum.Basics'
import { ToolbarClient, ToolbarResponse } from './ToolbarClient'
import { ToolbarConfig, IconColor } from "./ToolbarConfig"
import { SearchValue, FindOptions } from '@framework/Search';
import { Navigator } from '@framework/Navigator';
import { useAPI, useInterval } from '@framework/Hooks'
import { classes } from '@framework/Globals'
import { ShowCount } from './Signum.Toolbar'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {
  constructor() {
    var type = QueryEntity;
    super(type);
  }

  getCounter(element: ToolbarResponse<QueryEntity>): React.ReactElement | undefined {
    if (element.showCount != null) {
      return (
        <SearchToolbarCount
          findOptions={{ queryName: getToString(element.content)! }}
          color={element.iconColor ?? "red"}
          autoRefreshPeriod={element.autoRefreshPeriod}
          showCount={element.showCount} />
      );
    }

    return undefined;
  }

  getDefaultIcon(): IconColor {
    return ({
      icon: "rectangle-list",
      iconColor: "dodgerblue",
    });
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<QueryEntity>): void {
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
    return location.pathname == Finder.findOptionsPath({ queryName: getToString(res.content)! }) ? 1 : 0;
  }
}

interface CountIconProps {
  color?: string;
  autoRefreshPeriod?: number;
  findOptions: FindOptions;
  moreThanZero?: boolean;
  showCount: ShowCount;
}

export function SearchToolbarCount(p: CountIconProps): React.JSX.Element {

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
      <ToolbarCount num={val} showCount={p.showCount} />}
  />;
}


export function ToolbarCount(p: { num: number | null | undefined, showCount: ShowCount }): React.JSX.Element | null {

  if (!p.num && p.showCount == "MoreThan0")
    return null;

  return (
    <div className="sf-toolbar-count-container">
      <div className={classes("badge badge-pill sf-toolbar-count", !p.num ? "text-bg-light" : "text-bg-danger")}>{p.num ?? "…"}</div>
    </div>
  );
}


