import * as React from 'react'
import { Location } from 'history'
import { OutputParams } from 'query-string'
import { getQueryNiceName } from '@framework/Reflection'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { ToolbarConfig, ToolbarResponse } from './ToolbarClient'
import { ValueSearchControl, FindOptions } from '@framework/Search';
import { parseIcon } from '../Dashboard/Admin/Dashboard';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {
  constructor() {
    var type = QueryEntity;
    super(type);
  }

  getLabel(res: ToolbarResponse<QueryEntity>) {
    return res.label || getQueryNiceName(res.content!.toStr!);
  }

  countIcon?: CountIcon | null;
  getIcon(element: ToolbarResponse<QueryEntity>) {

    if (element.iconName == "count")
      return <CountIcon ref={ci => this.countIcon = ci} findOptions={{ queryName: element.content!.toStr! }} color={element.iconColor || "red"} autoRefreshPeriod={element.autoRefreshPeriod} />;

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

  isCompatibleWithUrl(res: ToolbarResponse<QueryEntity>, location: Location, query: OutputParams): boolean {
    return location.pathname == Navigator.toAbsoluteUrl(Finder.findOptionsPath({ queryName: res.content!.toStr! }));
  }
}

interface CountIconProps {
  color?: string;
  autoRefreshPeriod?: number;
  findOptions: FindOptions;
}

export class CountIcon extends React.Component<CountIconProps>{

  componentWillUnmount() {
    if (this.handler)
      clearTimeout(this.handler);

    this._isMounted = false;
  }

  _isMounted = true;
  handler: number | undefined;
  handleValueChanged = () => {
    if (this.props.autoRefreshPeriod && this._isMounted) {

      if (this.handler)
        clearTimeout(this.handler);

      this.handler = setTimeout(() => {
        this.refreshValue();
      }, this.props.autoRefreshPeriod * 1000);
    }
  }

  refreshValue() {
    this.valueSearchControl && this.valueSearchControl.refreshValue();
  }

  valueSearchControl?: ValueSearchControl | null;

  render() {
    return <ValueSearchControl ref={vsc => this.valueSearchControl = vsc}
      findOptions={this.props.findOptions}
      customClass="icon"
      customStyle={{ color: this.props.color }}
      onValueChange={this.handleValueChanged}
      avoidNotifyPendingRequest={true}
    />;
  }
}
