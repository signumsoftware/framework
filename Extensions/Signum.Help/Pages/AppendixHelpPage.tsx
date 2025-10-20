import * as React from 'react'
import { useLocation, useParams, Link } from 'react-router-dom'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { HelpClient } from '../HelpClient'
import { Operations } from '@framework/Operations';
import { useForceUpdate, useAPIWithReload } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, NamespaceHelpOperation, AppendixHelpEntity, AppendixHelpOperation } from '../Signum.Help';
import { getOperationInfo, getTypeInfo, GraphExplorer, symbolNiceName, tryGetOperationInfo } from '@framework/Reflection';
import { JavascriptMessage, Entity, toLite, OperationMessage, getToString } from '@framework/Signum.Entities';
import { TypeContext } from '@framework/Lines';
import { EditableTextComponent, EditableHtmlComponent } from './EditableText';
import MessageModal from '@framework/Modals/MessageModal';
import { classes } from '@framework/Globals';
import { useTitle } from '@framework/AppContext'
import { Shortcut } from './TypeHelpPage'


export default function AppendixHelpHelp(): React.JSX.Element {
  const params = useParams() as { uniqueName: string | undefined };

  var [appendix, reloadAppendix] = useAPIWithReload(() => HelpClient.API.appendix(params.uniqueName), []);
  useTitle(HelpMessage.Help.niceToString() + (appendix && (" > " + appendix.title)));
  var forceUpdate = useForceUpdate();
  if (appendix == null)
    return <div className="container"><h1 className="display-6">{JavascriptMessage.loading.niceToString()}</h1></div>;

  var ctx = TypeContext.root(appendix, { readOnly: Navigator.isReadOnly(AppendixHelpEntity) });

  return (
    <div className="container">
      <h1 className="display-6"><Link to={HelpClient.Urls.indexUrl()}>
        {HelpMessage.Help.niceToString()}</Link>
        {" > "}
        <EditableTextComponent ctx={ctx.subCtx(a => a.title, { formSize: "lg" })} onChange={() => { ctx.value.isNew && (ctx.value.uniqueName = ctx.value.title.replace(/[^a-zA-Z0-9]/g, "")); forceUpdate(); }} defaultEditable={appendix.isNew} />
        <small className="ms-5 text-muted display-7">({ctx.value.culture.englishName})</small>
      </h1>

      <div className={classes("mb-2 shortcut-container")}>
        <div>
          <strong className="me-2">{ctx.niceName(a => a.uniqueName)}</strong>
          <EditableTextComponent ctx={ctx.subCtx(a => a.uniqueName)} onChange={forceUpdate} defaultEditable={appendix.isNew} />
        </div>
        <Shortcut text={`[a:${ctx.value.uniqueName}]`} />
      </div>

      <EditableHtmlComponent ctx={ctx.subCtx(a => a.description)} onChange={forceUpdate} defaultEditable={appendix.isNew} />
      <div className={classes("btn-toolbar", "sf-button-bar", "mt-4")}>
        <SaveButton ctx={ctx} onSuccess={() => ctx.value.isNew ? AppContext.navigate(HelpClient.Urls.appendixUrl(ctx.value.uniqueName)) : reloadAppendix()} />
        <DeleteButton ctx={ctx} />
      </div>
    </div>
  );
}

function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<AppendixHelpEntity>, onSuccess: () => void }) {

  const oi = tryGetOperationInfo(AppendixHelpOperation.Save, AppendixHelpEntity)

  if (!oi)
    return null;

  function onClick() {
    HelpClient.API.saveAppendix(ctx.value)
      .then(() => {
        onSuccess();
        Operations.notifySuccess();
      });
  }

  return <button className="btn btn-primary" onClick={onClick}><FontAwesomeIcon aria-hidden={true} icon="save" /> {oi.niceName}</button>;
}

function DeleteButton({ ctx }: { ctx: TypeContext<AppendixHelpEntity> }) {

  if (!tryGetOperationInfo(AppendixHelpOperation.Delete, AppendixHelpEntity))
    return null;

  function onClick() {
    MessageModal.show({
      title: OperationMessage.Confirm.niceToString(),
      message: OperationMessage.PleaseConfirmYouWouldLikeToDelete0FromTheSystem.niceToString().formatHtml(<strong>{getToString(ctx.value)}</strong>),
      buttons: "yes_no",
      icon: "warning",
      style: "warning",
    }).then(result => {
      if (result == "yes") {

        Operations.API.deleteLite(toLite(ctx.value), AppendixHelpOperation.Delete.key)
          .then((() => {
            AppContext.navigate(HelpClient.Urls.indexUrl());
            Operations.notifySuccess();
          }));
      }
    });
  }

  return <button className="btn btn-danger ms-4" onClick={onClick}><FontAwesomeIcon aria-hidden={true} icon="trash" /> {getOperationInfo(AppendixHelpOperation.Delete, AppendixHelpEntity).niceName}</button>;
}
