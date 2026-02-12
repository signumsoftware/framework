import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator';
import { ajaxGet } from '@framework/Services';
import { CssStepEmbedded, TourEntity, TourStepEntity } from './Signum.Tour'

export namespace TourClient {
  
  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(TourEntity, a => import('./Templates/Tour')));
    Navigator.addSettings(new EntitySettings(TourStepEntity, a => import('./Templates/TourStep')));
  }

  export namespace API {
    export function getTourByEntity(typeName: string): Promise<TourDTO | null> {
      return ajaxGet({ url: `/api/tour/byEntity/${typeName}` });
    }
  }
}

export interface TourDTO {
  guid: string;
  name: string;
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

