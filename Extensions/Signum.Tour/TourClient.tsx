import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator';
import { ajaxGet } from '@framework/Services';
import { ClickTrigger, TourEntity, TourStepEntity } from './Signum.Tour'
import { Entity, Lite, ModifiableEntity, toLite, liteKey } from '@framework/Signum.Entities';
import { onWidgets } from '@framework/Frames/Widgets';
import { TourButton } from './TourComponent';
import { DashboardClient } from '../Signum.Dashboard/DashboardClient';
import '../Signum.UserQueries/UserQueryClient'; // augments SearchControlLoaded with getCurrentUserQuery
import { Finder } from '@framework/Finder';
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'

export namespace TourClient {

  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(TourEntity, a => import('./Templates/Tour')));
    Navigator.addSettings(new EntitySettings(TourStepEntity, a => import('./Templates/TourStep')));

    onWidgets.push(wc => {
      if (!wc.frame.pack.hasTour)
        return undefined;

      return <TourButton trigger={wc.ctx.value.Type} />;
    });

    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(TourEntity);

    DashboardClient.onDashboardPageActions.push(dashboard =>
      dashboard.id != null ? <TourButton trigger={toLite(dashboard)} /> : undefined);

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
      const uq = ctx.searchControl.getCurrentUserQuery?.();
      if (uq == null)
        return undefined;
      return {
        button: (
          <span className="d-inline-flex align-items-center mx-2">
            <TourButton trigger={uq} />
          </span>
        ),
      };
    });
  }

  export namespace API {
    export function getTourByEntity(typeName: string): Promise<TourDTO | null> {
      return ajaxGet({ url: `/api/tour/byEntity/${typeName}` });
    }

    export function getTourBySymbol(symbolKey: string): Promise<TourDTO | null> {
      return ajaxGet({ url: `/api/tour/bySymbol/${symbolKey}` });
    }

    export function getTourByLite(lite: Lite<Entity>): Promise<TourDTO | null> {
      return ajaxGet({ url: `/api/tour/byLite?liteKey=${encodeURIComponent(liteKey(lite))}` });
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
  click?: ClickTrigger;
}

declare module '@framework/Signum.Entities' {
  export interface EntityPack<T extends ModifiableEntity> {
    hasTour?: boolean;
  }
}

