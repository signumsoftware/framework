import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator';
import { TourEntity, TourStepEmbedded } from './Signum.Tour'

export namespace TourClient {
  
  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(TourEntity, a => import('./Templates/Tour')));
    Navigator.addSettings(new EntitySettings(TourStepEmbedded, a => import('./Templates/TourStep')));
  }
}
