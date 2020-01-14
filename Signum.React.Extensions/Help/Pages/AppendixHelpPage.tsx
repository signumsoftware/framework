import * as React from 'react'
import { RouteComponentProps, Link } from 'react-router-dom'
import * as Navigator from '@framework/Navigator'
import { API, Urls } from '../HelpClient'
import * as Operations from '@framework/Operations';
import { useAPI, useTitle, useForceUpdate, useAPIWithReload } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, NamespaceHelpOperation, AppendixHelpEntity, AppendixHelpOperation } from '../Signum.Entities.Help';
import { getTypeInfo, GraphExplorer, symbolNiceName } from '@framework/Reflection';
import { JavascriptMessage, Entity, toLite, OperationMessage, getToString } from '@framework/Signum.Entities';
import { TypeContext } from '@framework/Lines';
import { EditableComponent } from './EditableText';
import { notifySuccess } from '@framework/Operations';
import { getOperationInfo } from '@framework/Operations';
import MessageModal from '@framework/Modals/MessageModal';
import { classes } from '@framework/Globals';


export default function AppendixHelpHelp(p: RouteComponentProps<{ uniqueName: string | undefined }>) {

  var [appendix, reloadAppendix] = useAPIWithReload(() => API.appendix(p.match.params.uniqueName), []);
  useTitle(HelpMessage.Help.niceToString() + (appendix && (" > " + appendix.title)));
  var forceUpdate = useForceUpdate();
  if (appendix == null)
    return <h1 className="display-6">{JavascriptMessage.loading.niceToString()}</h1>;

  var ctx = TypeContext.root(appendix, { readOnly: Navigator.isReadOnly(AppendixHelpEntity) });

  return (
    <div>
      <h1 className="display-6"><Link to={Urls.indexUrl()}>
        {HelpMessage.Help.niceToString()}</Link>
        {" > "}
        <EditableComponent ctx={ctx.subCtx(a => a.title)} inline onChange={forceUpdate} defaultEditable={appendix.isNew} />
      </h1>
      <EditableComponent ctx={ctx.subCtx(a => a.uniqueName)} onChange={forceUpdate} defaultEditable={appendix.isNew} />
      <EditableComponent ctx={ctx.subCtx(a => a.description)} markdown onChange={forceUpdate} defaultEditable={appendix.isNew} />
      <div className={classes("btn-toolbar", "sf-button-bar")}>
        {ctx.value.modified && <SaveButton ctx={ctx} onSuccess={a => ctx.value.isNew ? Navigator.history.push(Urls.appendixUrl(a.uniqueName)) : reloadAppendix()} />}
        <DeleteButton ctx={ctx} />
      </div>
    </div>
  );
}

function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<AppendixHelpEntity>, onSuccess: (a: AppendixHelpEntity) => void }) {

  if (!Operations.isOperationAllowed(AppendixHelpOperation.Save, AppendixHelpEntity))
    return null;

  function onClick() {
    Operations.API.executeEntity(ctx.value, AppendixHelpOperation.Save)
      .then(p => {
        onSuccess(p.entity);
        notifySuccess();
      })
      .done();
  }

  return <button className="btn btn-primary" onClick={onClick}>{getOperationInfo(AppendixHelpOperation.Save, AppendixHelpEntity).niceName}</button>;
}

function DeleteButton({ ctx }: { ctx: TypeContext<AppendixHelpEntity> }) {

  if (!Operations.isOperationAllowed(AppendixHelpOperation.Delete, AppendixHelpEntity))
    return null;

  function onClick() {
    MessageModal.show({
      title: OperationMessage.Confirm.niceToString(),
      message: OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem.niceToString(getToString(ctx.value)),
      buttons: "yes_no",
      icon: "warning",
      style: "warning",
    }).then(result => {
      if (result == "yes") {

        Operations.API.deleteLite(toLite(ctx.value), AppendixHelpOperation.Delete.key)
          .then((() => {
            Navigator.history.push(Urls.indexUrl());
            notifySuccess();
          }))
          .done();
      }
    }).done();
  }

  return <button className="btn btn-danger" onClick={onClick}>{getOperationInfo(AppendixHelpOperation.Delete, AppendixHelpEntity).niceName}</button>;
}
