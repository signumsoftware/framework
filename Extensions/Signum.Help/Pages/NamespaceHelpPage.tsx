import * as React from 'react'
import { useLocation, useParams, Link } from 'react-router-dom'
import * as Navigator from '@framework/Navigator'
import { API, Urls } from '../HelpClient'
import { useAPI, useForceUpdate, useAPIWithReload } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, NamespaceHelpOperation } from '../Signum.Help';
import { getTypeInfo, GraphExplorer, symbolNiceName, tryGetTypeInfo } from '@framework/Reflection';
import { JavascriptMessage, Entity } from '@framework/Signum.Entities';
import * as Operations from '@framework/Operations';
import { TypeContext } from '@framework/Lines';
import { EditableComponent } from './EditableText';
import { notifySuccess } from '@framework/Operations';
import { useTitle } from '@framework/AppContext';


export default function NamespaceHelpPage() {
  const params = useParams() as { namespace: string };

  var [count, setCount] = React.useState(0);
  var [namespace, reloadNamespace] = useAPIWithReload(() => API.namespace(params.namespace), [count]);
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

  var oi = Operations.tryGetOperationInfo(NamespaceHelpOperation.Save, NamespaceHelpEntity);

  if (!oi)
    return null;

  function onClick() {
    API.saveNamespace(ctx.value)
      .then((() => {
        onSuccess();
        notifySuccess();
      }));
  }

  return <button className="btn btn-primary" onClick={onClick}>{oi.niceName}</button>;
}
