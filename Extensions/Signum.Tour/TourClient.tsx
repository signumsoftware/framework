import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator';
import { ajaxGet } from '@framework/Services';
import { CssStepEmbedded, TourEntity, TourStepEntity } from './Signum.Tour'
import { Entity, Lite, ModifiableEntity, EntityPack } from '@framework/Signum.Entities';
import { onWidgets } from '@framework/Frames/Widgets';
import { TourButton } from './TourComponent';
import { tryGetTypeInfo } from '@framework/Reflection';

export namespace TourClient {

  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(TourEntity, a => import('./Templates/Tour')));
    Navigator.addSettings(new EntitySettings(TourStepEntity, a => import('./Templates/TourStep')));

    onWidgets.push(wc => {
      if (!wc.frame.pack.hasTour)
        return undefined;

      return <TourButton trigger={wc.ctx.value.Type} />;
    });
  }

  export namespace API {
    export function getTourByEntity(typeName: string): Promise<TourDTO | null> {
      return ajaxGet({ url: `/api/tour/byEntity/${typeName}` });
    }

    export function getTourBySymbol(symbolKey: string): Promise<TourDTO | null> {
      return ajaxGet({ url: `/api/tour/bySymbol/${symbolKey}` });
    }
  }
}

export interface TourDTO {
  forEntity: Lite<Entity>;
  steps: TourStepDTO[];
  showProgress: boolean;
  animate: boolean;
  showCloseButton: boolean;
}

export interface TourStepDTO {
  cssSelector?: string;
  title?: string;
  description?: string;
  side?: string;
  align?: string;
}

declare module '@framework/Signum.Entities' {
  export interface EntityPack<T extends ModifiableEntity> {
    hasTour?: boolean;
  }
}

