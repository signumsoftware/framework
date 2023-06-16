import * as React from 'react';
import { RemoteEmailFolderModel } from './Signum.Mailing.MicrosoftGraph.RemoteEmails';
import { Lite, getToString } from '@framework/Signum.Entities';
import { FormGroup, TypeContext } from '@framework/Lines';
import { UserEntity, UserLiteModel } from '../../Signum.Authorization/Signum.Authorization';
import { useAPI, useForceUpdate } from '@framework/Hooks';
import { classes } from '@framework/Globals';
import { API } from './RemoteEmailsClient';
import { defaultConstructFromEntity } from '@framework/Operations/EntityOperations';


export function FolderLine(p: { ctx: TypeContext<RemoteEmailFolderModel | null>; user: Lite<UserEntity> | undefined; label?: string; mandatory?: boolean; onChange: () => void }) {
  var oid = (p.user?.model as UserLiteModel)?.oID;
  var folders = useAPI(() => oid == null ? Promise.resolve([]) : API.getRemoteFolders(oid), [oid]);
  const forceUpdate = useForceUpdate();

  var allFolders = [
    null,
    ...folders?.some(f => f.folderId == p.ctx.value?.folderId) || p.ctx.value == null ? [] : [p.ctx.value],
    ...(folders ?? [])
  ];

  React.useEffect(() => {

    var mod = p.ctx.value;
    if (mod != null && folders != null) {
      var same = folders.firstOrNull(a => a.folderId == mod!.folderId); 
      if (same != null) {
        if (mod.folderId == mod.displayName) //Comming form url
        {
          mod.displayName = same.displayName;
          forceUpdate();
        }
      } else {
        p.ctx.value = null;
        p.onChange();
        forceUpdate();
      }
    }

  }, [folders]);

  function handleOnChange(event: React.ChangeEvent<HTMLSelectElement>) {
    const current = event.currentTarget as HTMLSelectElement;

    if (current.value != (p.ctx.value?.folderId)) {
      p.ctx.value = current.value == "" ? null : (folders??[]).single(a => a.folderId == current.value);
    }

    p.onChange();

    forceUpdate();
  }

  return (
    <FormGroup ctx={p.ctx} label={p.label}>
      {id => <select className={classes(p.ctx.formSelectClass, p.mandatory && "sf-mandatory")} onChange={handleOnChange} value={p.ctx.value?.folderId}
        title={getToString(p.ctx.value)}
        id={id}>
        {allFolders?.map((r, i) => <option key={i} value={r ? r.folderId : ""}>{r != null ? r.displayName : " - "}</option>)}
      </select>}
    </FormGroup>
  );
}
