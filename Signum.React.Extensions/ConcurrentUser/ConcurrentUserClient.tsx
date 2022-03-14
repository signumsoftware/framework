import * as React from 'react'
import { ConcurrentUserEntity } from './Signum.Entities.ConcurrentUser'
import { Entity, isEntity, toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import * as Widgets from '@framework/Frames/Widgets';
import ConcurrentUser from './ConcurrentUser'
import { ajaxGet } from '@framework/Services'

export function start(options: { routes: JSX.Element[] }) {
  Widgets.onWidgets.push(ctx => {

    var me = ctx.ctx.value;

    if (isEntity(me) && !me.isNew) {
      const entity = me;
      return <ConcurrentUser entity={entity} onReload={() =>
        Navigator.API.fetchEntityPack(toLite(entity))
          .then(pack => ctx.frame.onReload(pack))
          .done()} />;
    }

    return undefined;
  });
}

export module API {
  export function getUsers(key: string): Promise<ConcurrentUserEntity[]> {
    return ajaxGet({ url: "~/api/concurrentUser/getUsers/" + key});
  }
}
