import { RouteObject } from 'react-router'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Lite, Entity } from '@framework/Signum.Entities'
import { ToolbarEntity, ToolbarMenuEntity, ToolbarElementEmbedded, ToolbarElementType, ToolbarLocation, ShowCount, ToolbarSwitcherEntity, ToolbarSwitcherOptionEmbedded, ToolbarMenuElementEmbedded } from './Signum.Toolbar'
import { Constructor } from '@framework/Constructor'
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'
import { Dic } from '@framework/Globals';
import QueryToolbarConfig from './QueryToolbarConfig';
import { ToolbarConfig } from './ToolbarConfig';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace ToolbarClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Toolbar", () => import("./Changelog"));
  
    Navigator.addSettings(new EntitySettings(ToolbarEntity, t => import('./Templates/Toolbar')));
    Navigator.addSettings(new EntitySettings(ToolbarMenuEntity, t => import('./Templates/ToolbarMenu')));
    Navigator.addSettings(new EntitySettings(ToolbarElementEmbedded, t => import('./Templates/ToolbarElement')));
    Navigator.addSettings(new EntitySettings(ToolbarMenuElementEmbedded, t => import('./Templates/ToolbarElement')));
    Navigator.addSettings(new EntitySettings(ToolbarSwitcherEntity, t => import('./Templates/ToolbarSwitcher')));
    Navigator.addSettings(new EntitySettings(ToolbarSwitcherOptionEmbedded, t => import('./Templates/ToolbarSwitcherOption')));
  
    registerConfig(new QueryToolbarConfig());
  
    Finder.addSettings({ queryName: ToolbarEntity, defaultOrders: [{ token: ToolbarEntity.token(a => a.priority), orderType: "Descending" }] });
  
    AppContext.clearSettingsActions.push(cleanConfigs);
  
    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(ToolbarEntity);
    UserAssetClient.registerExportAssertLink(ToolbarMenuEntity);
    UserAssetClient.registerExportAssertLink(ToolbarSwitcherEntity);
  }
  
  export function cleanConfigs(): void {
    Dic.clear(configs);
  }
  
  
  
  export const configs: { [type: string]: ToolbarConfig<any>[] } = {};
  
  export function registerConfig<T extends Entity>(config: ToolbarConfig<T>): void {
    (configs[config.type.typeName] ??= []).push(config);
  }
  
  export function getConfig(res: ToolbarResponse<any>): ToolbarConfig<any> | null {
    return configs[res.content!.EntityType]?.filter(c => c.isApplicableTo(res)).singleOrNull();
  }
  
  export namespace API {
    export function getCurrentToolbar(location: ToolbarLocation): Promise<ToolbarResponse<any> | null> {
      return ajaxGet({ url: `/api/toolbar/current/${location}` });
    }

    export function getToolbarMenu(menu: Lite<ToolbarMenuEntity>): Promise<ToolbarResponse<any> | null> {
      return ajaxGet({ url: `/api/toolbarMenu/${menu.id}` });
    }
  }
 
}

export interface ToolbarResponse<T extends Entity> {
  type: ToolbarElementType;
  label?: string;
  content?: Lite<T>;
  entityType?: string;
  url?: string;
  iconName?: string;
  iconColor?: string;
  showCount?: ShowCount;
  autoRefreshPeriod?: number;
  openInPopup?: boolean;
  autoSelect?: boolean;
  withEntity?: boolean;
  elements?: Array<ToolbarResponse<any>>;
  extraIcons?: Array<ToolbarResponse<any>>;
  queryKey?: string;
}
