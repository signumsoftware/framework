import * as React from 'react'
import { ConcurrentUserEntity } from './Signum.Entities.ConcurrentUser'
import { Entity, isEntity, Lite, toLite } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import * as Widgets from '@framework/Frames/Widgets';
import ConcurrentUser from './ConcurrentUser'
import { ajaxGet } from '@framework/Services'
import { UserEntity } from '../Authorization/Signum.Entities.Authorization';

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
  export function getUsers(key: string): Promise<ConcurrentUserResponse[]> {
    return ajaxGet({ url: "~/api/concurrentUser/getUsers/" + key});
  }
}

export interface ConcurrentUserResponse {
  user : Lite<UserEntity>;
  startTime : string /*DateTime*/;
  connectionID: string;
  isModified: boolean;
}
