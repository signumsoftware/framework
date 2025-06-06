import * as React from 'react';
import { Location } from 'react-router';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import * as AppContext from '@framework/AppContext';
import { Entity } from '@framework/Signum.Entities';
import { Type } from '@framework/Reflection';
import { fallbackIcon, parseIcon } from '@framework/Components/IconTypeahead';
import { ToolbarNavItem, renderExtraIcons } from './Renderers/ToolbarRenderer';
import { ToolbarClient, ToolbarResponse } from './ToolbarClient';


export abstract class ToolbarConfig<T extends Entity> {
  type: Type<T>;
  constructor(type: Type<T>) {
    this.type = type;
  }


  getIcon(element: ToolbarResponse<T>): React.ReactElement<any, string | React.JSXElementConstructor<any>> | null {
    const defaultIcon = this.getDefaultIcon();
    return (
      <>
        {ToolbarConfig.coloredIcon(parseIcon(element.iconName) ?? defaultIcon.icon, element.iconColor ?? defaultIcon.iconColor)}
        {this.getCounter(element)}
      </>
    );
  }

  abstract getDefaultIcon(): IconColor;

  static coloredIcon(icon: IconProp | undefined, color: string | undefined): React.ReactElement | null {
    if (!icon)
      return null;

    return <FontAwesomeIcon icon={fallbackIcon(icon)} className={"icon"} color={color} />;
  }

  getCounter(element: ToolbarResponse<T>): React.ReactElement | undefined {
    return undefined;
  }

  abstract navigateTo(element: ToolbarResponse<T>): Promise<string | null>;
  abstract isCompatibleWithUrlPrio(element: ToolbarResponse<T>, location: Location, query: any): number;

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>): void {
    e.preventDefault();
    this.navigateTo(res).then(url => {
      if (url)
        AppContext.pushOrOpenInTab(url, e);
    });
  }

  isApplicableTo(element: ToolbarResponse<T>) {
    return true;
  }

  getMenuItem(res: ToolbarResponse<T>, active: ToolbarResponse<any> | null, key: number | string, onAutoClose?: () => void): React.JSX.Element {
    return (
      <ToolbarNavItem key={key}
        title={res.label}
        onClick={(e: React.MouseEvent<any>) => {
          this.handleNavigateClick(e, res);
          if (onAutoClose && !(e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
            onAutoClose();
        }}
        active={res == active}
        extraIcons={renderExtraIcons(res.extraIcons, active, onAutoClose)}
        icon={this.getIcon(res)}
      />
    );
  }
}

export interface IconColor {
  icon: IconProp;
  iconColor: string;
}
