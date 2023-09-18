import * as React from 'react';
import { Location } from 'react-router';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import * as AppContext from '@framework/AppContext';
import { Entity } from '@framework/Signum.Entities';
import { Type } from '@framework/Reflection';
import { parseIcon } from '@framework/Components/IconTypeahead';
import { ToolbarNavItem } from './Renderers/ToolbarRenderer';
import { ToolbarResponse } from './ToolbarClient';


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
          extraIcons={res.extraIcons}
          icon={this.getIcon(res)}
        />
      );
    }
}

export interface IconColor {
  icon: IconProp;
  iconColor: string;
}
