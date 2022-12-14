import * as React from 'react'
import { Location } from 'history'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity } from '@framework/Signum.Entities'
import { Type } from '@framework/Reflection'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEmbedded, ToolbarElementType, ToolbarLocation, ShowCount } from './Signum.Entities.Toolbar'
import * as Constructor from '@framework/Constructor'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { parseIcon } from '../Basics/Templates/IconTypeahead';
import { Nav } from 'react-bootstrap';
import { SidebarMode } from './SidebarContainer';
import { Dic } from '../../Signum.React/Scripts/Globals';
import { ToolbarNavItem } from './Renderers/ToolbarRenderer';

export function start(options: { routes: JSX.Element[] }, ...configs: ToolbarConfig<any>[]) {
  Navigator.addSettings(new EntitySettings(ToolbarEntity, t => import('./Templates/Toolbar')));
  Navigator.addSettings(new EntitySettings(ToolbarMenuEntity, t => import('./Templates/ToolbarMenu')));
  Navigator.addSettings(new EntitySettings(ToolbarElementEmbedded, t => import('./Templates/ToolbarElement')));

  Finder.addSettings({ queryName: ToolbarEntity, defaultOrders: [{ token: ToolbarEntity.token(a => a.priority), orderType: "Descending" }] });

  Constructor.registerConstructor(ToolbarElementEmbedded, tn => ToolbarElementEmbedded.New({ type: "Item" }));

  AppContext.clearSettingsActions.push(cleanConfigs);

  configs.forEach(c => registerConfig(c));

  UserAssetClient.start({ routes: options.routes });
  UserAssetClient.registerExportAssertLink(ToolbarEntity);
}

export function cleanConfigs() {
  Dic.clear(configs);
}


export interface IconColor {
  icon: IconProp;
  iconColor: string;
}

export abstract class ToolbarConfig<T extends Entity> {
  type: Type<T>;
  constructor(type: Type<T>) {
    this.type = type;
  }


  getIcon(element: ToolbarResponse<T>) {
    const defaultIcon = this.getDefaultIcon();
    return ToolbarConfig.coloredIcon(parseIcon(element.iconName) ?? defaultIcon.icon, element.iconColor ?? defaultIcon.iconColor);
  }

  abstract getDefaultIcon(): IconColor;

  static coloredIcon(icon: IconProp | undefined, color: string | undefined): React.ReactChild | null {
    if (!icon)
      return null;

    return <FontAwesomeIcon icon={icon} className={"icon"} color={color} />;
  }

  abstract navigateTo(element: ToolbarResponse<T>): Promise<string | null>;
  abstract isCompatibleWithUrlPrio(element: ToolbarResponse<T>, location: Location, query: any): number;

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {
    e.preventDefault();
    e.persist();
    this.navigateTo(res).then(url => {
      if (url)
        AppContext.pushOrOpenInTab(url, e);
    });
  }

  isApplicableTo(element: ToolbarResponse<T>) {
    return true;
  }

  getMenuItem(res: ToolbarResponse<T>, isActive: boolean, key: number | string, onAutoClose?: () => void) {
    return (
      <ToolbarNavItem key={key}
        title={res.label}
        onClick={(e: React.MouseEvent<any>) => {
          this.handleNavigateClick(e, res);
          if (onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            onAutoClose();
        }}
        active={isActive}
        icon={this.getIcon(res)}/>
    );
  }
}


export const configs: { [type: string]: ToolbarConfig<any>[] } = {};

export function registerConfig<T extends Entity>(config: ToolbarConfig<T>) {
  (configs[config.type.typeName] ??= []).push(config);
}

export function getConfig(res: ToolbarResponse<any>) {
  return configs[res.content!.EntityType]?.filter(c => c.isApplicableTo(res)).singleOrNull();
}

export namespace API {
  export function getCurrentToolbar(location: ToolbarLocation): Promise<ToolbarResponse<any> | null> {
    return ajaxGet({ url: `~/api/toolbar/current/${location}` });
  }
}

export interface ToolbarResponse<T extends Entity> {
  type: ToolbarElementType;
  iconName?: string;
  iconColor?: string;
  showCount?: ShowCount;
  label?: string;
  content?: Lite<T>;
  url?: string;
  elements?: Array<ToolbarResponse<any>>;
  openInPopup?: boolean;
  autoRefreshPeriod?: number;
}
