import * as React from 'react';
import { Location } from 'react-router';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import * as AppContext from '@framework/AppContext';
import { Entity, Lite } from '@framework/Signum.Entities';
import { Type } from '@framework/Reflection';
import { fallbackIcon, parseIcon } from '@framework/Components/IconTypeahead';
import { ToolbarNavItem, renderExtraIcons, isActive } from './Renderers/ToolbarRenderer';
import { ToolbarClient, ToolbarResponse } from './ToolbarClient';


export abstract class ToolbarConfig<T extends Entity> {
  type: Type<T>;
  constructor(type: Type<T>) {
    this.type = type;
  }


  getIcon(element: ToolbarResponse<T>, entity: Lite<Entity> | null): React.ReactElement<any, string | React.JSXElementConstructor<any>> | null {
    const defaultIcon = this.getDefaultIcon();
    return (
      <>
        {ToolbarConfig.coloredIcon(parseIcon(element.iconName) ?? defaultIcon, element.iconColor)}
        {this.getCounter(element, entity)}
      </>
    );
  }

  async selectSubEntityForUrl(element: ToolbarResponse<T>, entity: Lite<Entity> | null): Promise<Lite<Entity> | null> {
    return null;
  }

  abstract getDefaultIcon(): IconProp;

  static coloredIcon(icon: IconProp | undefined, color: string | undefined): React.ReactElement | null {
    if (!icon)
      return null;

    return <FontAwesomeIcon icon={fallbackIcon(icon)} className={"icon"} color={color} />;
  }

  getCounter(element: ToolbarResponse<T>, entity: Lite<Entity> | null): React.ReactElement | undefined {
    return undefined;
  }

  abstract navigateTo(element: ToolbarResponse<T>, selectedEntity: Lite<Entity> | null): Promise<string | null>;
  abstract isCompatibleWithUrlPrio(element: ToolbarResponse<T>, location: Location, query: any, entityType?: string): { prio: number, inferredEntity ?: Lite<Entity> } | null;

  handleNavigateClick(e: React.MouseEvent<any> | undefined, res: ToolbarResponse<any>, selectedEntity: Lite<Entity> | null): void {
    e?.preventDefault();
    this.navigateTo(res, selectedEntity).then(url => {
      if (url)
        AppContext.pushOrOpenInTab(url, e);
    });
  }

  isApplicableTo(element: ToolbarResponse<T>) {
    return true;
  }

  getMenuItem(res: ToolbarResponse<T>, key: number | string, ctx: ToolbarContext, selectedEntity: Lite<Entity> | null): React.JSX.Element {
    return (
      <ToolbarNavItem key={key}
        title={res.label}
        onClick={(e: React.MouseEvent<any>) => {
          this.handleNavigateClick(e, res, selectedEntity);
          if (ctx.onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            ctx.onAutoClose();
        }}
        active={isActive(ctx.active, res, selectedEntity)}
        extraIcons={renderExtraIcons(res.extraIcons, ctx, selectedEntity)}
        icon={this.getIcon(res, selectedEntity)}
      />
    );
  }
}

export interface ToolbarContext {
  onAutoClose?: () => void;
  onRefresh: () => void;
  active: InferActiveResponse | null;
}

export interface InferActiveResponse {
  prio: number;
  response: ToolbarResponse<any>;
  inferredEntity?: Lite<Entity>;
  menuWithEntity?: { menu: ToolbarResponse<any>, entity: Lite<Entity> };
}


export interface IconColor {
  icon: IconProp;
  iconColor: string;
}
