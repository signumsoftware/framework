import * as React from 'react'
import { RouteComponentProps, Link } from 'react-router-dom'
import * as Navigator from '@framework/Navigator'
import { API, Urls } from '../HelpClient'
import { useAPI, useTitle, useForceUpdate, useAPIWithReload } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, NamespaceHelpOperation } from '../Signum.Entities.Help';
import { getTypeInfo, GraphExplorer, symbolNiceName } from '@framework/Reflection';
import { JavascriptMessage, Entity } from '@framework/Signum.Entities';
import * as Operations from '@framework/Operations';
import { TypeContext } from '@framework/Lines';
import { EditableComponent } from './EditableText';
import { notifySuccess } from '@framework/Operations';
import { getOperationInfo } from '../../../../Framework/Signum.React/Scripts/Operations';


export default function NamespaceHelpPage(p: RouteComponentProps<{ namespace: string }>) {

  var [count, setCount] = React.useState(0);
  var [namespace, reloadNamespace] = useAPIWithReload(() => API.namespace(p.match.params.namespace), [count]);
  useTitle(HelpMessage.Help.niceToString() + (namespace && (" > " + namespace.title)));
  var forceUpdate = useForceUpdate();
  if (namespace == null)
    return <h1 className="display-6">{JavascriptMessage.loading.niceToString()}</h1>;

  var ctx = TypeContext.root(namespace.entity, { readOnly: Navigator.isReadOnly(NamespaceHelpEntity) });

  return (
    <div>
      <h1 className="display-6"><Link to={Urls.indexUrl()}>
        {HelpMessage.Help.niceToString()}</Link>
        {" > "}
        <EditableComponent ctx={ctx.subCtx(a => a.title)} defaultText={namespace.title} inline onChange={forceUpdate} />
      </h1>
      <EditableComponent ctx={ctx.subCtx(a => a.description)} markdown onChange={forceUpdate} />
      {ctx.value.modified && <SaveButton ctx={ctx} onSuccess={() => reloadNamespace()} />}
      <h2 className="display-7 mt-4">Types</h2>
      <ul className="mt-4">
        {namespace.allowedTypes.map(t => <li key={t.cleanName}><Link to={Urls.typeUrl(t.cleanName)} >{getTypeInfo(t.cleanName).niceName}</Link></li>)}
      </ul>
    </div>
  );
}

function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<NamespaceHelpEntity>, onSuccess: () => void }) {

  if (!Operations.isOperationAllowed(NamespaceHelpOperation.Save, NamespaceHelpEntity))
    return null;

  function onClick() {
    API.saveNamespace(ctx.value)
      .then((() => {
        onSuccess();
        notifySuccess();
      }))
      .done();
  }

  return <button className="btn btn-primary" onClick={onClick}>{getOperationInfo(NamespaceHelpOperation.Save, NamespaceHelpEntity).niceName}</button>;
}
