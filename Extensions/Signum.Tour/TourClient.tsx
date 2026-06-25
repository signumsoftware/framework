import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator';
import { ajaxGet } from '@framework/Services';
import { ClickTrigger, TourEntity, TourStepEntity } from './Signum.Tour'
import { Entity, Lite, ModifiableEntity, toLite, liteKey } from '@framework/Signum.Entities';
import { TypeEntity, TourTriggerSymbol } from '@framework/Signum.Basics';
import { onWidgets } from '@framework/Frames/Widgets';
import { TourButton } from './TourComponent';
import { TourButtonHolder } from '@framework/TourButton';
import { DashboardClient } from '../Signum.Dashboard/DashboardClient';
import '../Signum.UserQueries/UserQueryClient'; // augments SearchControlLoaded with getCurrentUserQuery
import { Finder } from '@framework/Finder';
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'
import SearchPage from '@framework/SearchControl/SearchPage';

// Tags of the full-page search controls that show the tour button in their title instead of the toolbar.
const titlePageTags = ["SearchPage", "UserQueryPage"];

export namespace TourClient {

  export function start(options: { routes: RouteObject[] }): void {

    // Implement the framework-level TourButton extension point. Modules render <TourButton> from
    // @framework without depending on this extension; tours only appear when Signum.Tour is started.
    TourButtonHolder.renderer = trigger => <TourButton trigger={trigger as any} />;

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

      // On full-page search controls the tour button is shown in the title (see onTitleElements below).
      if (titlePageTags.includes(ctx.searchControl.props.tag as string))
        return undefined;

      return {
        button: (
          <span className="d-inline-flex align-items-center mx-2">
            <TourButton trigger={uq} />
          </span>
        ),
      };
    });

    // On SearchPage / UserQueryPage, render the user query tour button in the page title.
    SearchPage.onTitleElements.push(scl => {
      const uq = scl.getCurrentUserQuery?.();
      return uq != null ? <TourButton trigger={uq} /> : null;
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

    export function getTriggerType(lite: Lite<TourTriggerSymbol>): Promise<Lite<TypeEntity> | null> {
      return ajaxGet({ url: `/api/tour/triggerType?liteKey=${encodeURIComponent(liteKey(lite))}` });
    }
  }
}

export interface TourDTO {
  tour: Lite<TourEntity>;
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

