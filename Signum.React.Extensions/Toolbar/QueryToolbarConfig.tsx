import * as React from 'react'
import { Location } from 'history'
import { getQueryNiceName } from '@framework/Reflection'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { ToolbarConfig, ToolbarResponse } from './ToolbarClient'
import { ValueSearchControl, FindOptions } from '@framework/Search';
import { parseIcon } from '../Dashboard/Admin/Dashboard';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import { useInterval } from '../../../Framework/Signum.React/Scripts/Hooks'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {
  constructor() {
    var type = QueryEntity;
    super(type);
  }

  getLabel(res: ToolbarResponse<QueryEntity>) {
    return res.label ?? getQueryNiceName(res.content!.toStr!);
  }

  getIcon(element: ToolbarResponse<QueryEntity>) {

    if (element.iconName == "count")
      return <CountIcon findOptions={{ queryName: element.content!.toStr! }} color={element.iconColor ?? "red"} autoRefreshPeriod={element.autoRefreshPeriod} />;

    return ToolbarConfig.coloredIcon(coalesceIcon(parseIcon(element.iconName) , ["far", "list-alt"]), element.iconColor || "dodgerblue");
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {
    if (!res.openInPopup)
      super.handleNavigateClick(e, res);
    else {
      Finder.explore({ queryName: res.content!.toStr! }).done()
    }
  }

  navigateTo(res: ToolbarResponse<QueryEntity>): Promise<string> {
    return Promise.resolve(Finder.findOptionsPath({ queryName: res.content!.toStr! }));
  }

  isCompatibleWithUrl(res: ToolbarResponse<QueryEntity>, location: Location, query: any): boolean {
    return location.pathname == Navigator.toAbsoluteUrl(Finder.findOptionsPath({ queryName: res.content!.toStr! }));
  }
}

interface CountIconProps {
  color?: string;
  autoRefreshPeriod?: number;
  findOptions: FindOptions;
}

export function CountIcon(p: CountIconProps) {

  const refreshKey = useInterval(p.autoRefreshPeriod == null ? null : p.autoRefreshPeriod! * 1000, 0, a => a + 1);

  return <ValueSearchControl refreshKey={refreshKey}
    findOptions={p.findOptions}
    customClass="icon"
    customStyle={{ color: p.color }}
    avoidNotifyPendingRequest={true}
  />;
}
