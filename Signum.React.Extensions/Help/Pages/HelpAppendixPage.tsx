import * as React from 'react'
import { RouteComponentProps, Link } from 'react-router-dom'
import * as Navigator from '@framework/Navigator'
import { API, Urls } from '../HelpClient'
import * as Operations from '@framework/Operations';
import { useAPI, useTitle, useForceUpdate } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, NamespaceHelpOperation, AppendixHelpEntity, AppendixHelpOperation } from '../Signum.Entities.Help';
import { getTypeInfo, GraphExplorer, symbolNiceName } from '@framework/Reflection';
import { JavascriptMessage, Entity, toLite, OperationMessage, getToString } from '@framework/Signum.Entities';
import { TypeContext } from '@framework/Lines';
import { EditableComponent } from './EditableText';
import { notifySuccess, confirmInNecessary } from '@framework/Operations/EntityOperations';
import { getOperationInfo } from '../../../../Framework/Signum.React/Scripts/Operations';
import MessageModal from '../../../../Framework/Signum.React/Scripts/Modals/MessageModal';
import ButtonBar from '../../../../Framework/Signum.React/Scripts/Frames/ButtonBar';
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals';


export default function HelpAppendixHelp(p: RouteComponentProps<{ uniqueName: string }>) {

  
  var [count, setCount] = React.useState(0);
  var appendix = useAPI(undefined, [count], () => API.appendix(p.match.params.uniqueName));
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
        <EditableComponent ctx={ctx.subCtx(a => a.title)} inline onChange={forceUpdate} />
      </h1>
      <EditableComponent ctx={ctx.subCtx(a => a.description)} markdown onChange={forceUpdate} />
      <div className={classes("btn-toolbar", "sf-button-bar")}>
        {ctx.value.modified && <SaveButton ctx={ctx} onSuccess={() => setCount(count + 1)} />}
        <DeleteButton ctx={ctx} />
      </div>
    </div>
  );
}

function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<AppendixHelpEntity>, onSuccess: () => void }) {

  if (!Operations.isOperationAllowed(AppendixHelpOperation.Save, AppendixHelpEntity))
    return null;

  function onClick() {
    Operations.API.executeEntity(ctx.value, AppendixHelpOperation.Save.key)
      .then((() => {
        onSuccess();
        notifySuccess();
      }))
      .done();
  }

  return <button className="btn btn-primary" onClick={onClick}>{getOperationInfo(AppendixHelpOperation.Save, NamespaceHelpEntity).niceName}</button>;
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

  return <button className="btn btn-danger" onClick={onClick}>{getOperationInfo(AppendixHelpOperation.Delete, NamespaceHelpEntity).niceName}</button>;
}
