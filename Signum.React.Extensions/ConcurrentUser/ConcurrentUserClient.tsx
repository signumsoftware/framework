import * as React from 'react'
import { ConcurrentUserEntity } from './Signum.Entities.ConcurrentUser'
import { Entity, toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import * as Widgets from '@framework/Frames/Widgets';
import ConcurrentUser from './ConcurrentUser'
import { ajaxGet } from '@framework/Services'

export function start(options: { routes: JSX.Element[] }) {
  Widgets.onWidgets.push(ctx => {

    var entity = ctx.ctx.value;

    if (!entity.isNew)
      return <ConcurrentUser entity={entity as Entity} onReload={() =>
        Navigator.API.fetchEntityPack(toLite(entity as Entity))
          .then(pack => ctx.frame.onReload(pack))
          .done()} />;

    return undefined;
  });
}

export module API {
  export function getUsers(key: string): Promise<ConcurrentUserEntity[]> {
    return ajaxGet({ url: "~/api/concurrentUser/getUsers/" + key});
  }
}
