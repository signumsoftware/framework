import * as React from 'react'
import { useLocation, useParams, Link } from 'react-router-dom'
import * as Navigator from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { API, Urls } from '../HelpClient'
import { useAPI, useForceUpdate, useAPIWithReload } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, NamespaceHelpOperation } from '../Signum.Help';
import { getTypeInfo, GraphExplorer, symbolNiceName, tryGetOperationInfo, tryGetTypeInfo } from '@framework/Reflection';
import { JavascriptMessage, Entity } from '@framework/Signum.Entities';
import * as Operations from '@framework/Operations';
import { TypeContext } from '@framework/Lines';
import { EditableHtmlComponent, EditableTextComponent } from './EditableText';
import { notifySuccess } from '@framework/Operations';
import { useTitle } from '@framework/AppContext';
import { classes } from '@framework/Globals';


export default function NamespaceHelpPage() {
  const params = useParams() as { namespace: string };

  var [count, setCount] = React.useState(0);
  var [namespace, reloadNamespace] = useAPIWithReload(() => API.namespace(params.namespace), [count]);
  useTitle(HelpMessage.Help.niceToString() + (namespace && (" > " + namespace.title)));
  var forceUpdate = useForceUpdate();
  if (namespace == null)
    return <div className="container"><h1 className="display-6">{JavascriptMessage.loading.niceToString()}</h1></div>;

  var ctx = TypeContext.root(namespace.entity, { readOnly: Navigator.isReadOnly(NamespaceHelpEntity) });

  return (
    <div className="container">
      <h1 className="display-6"><Link to={Urls.indexUrl()}>
        {HelpMessage.Help.niceToString()}</Link>
        {" > "}
        <EditableTextComponent ctx={ctx.subCtx(a => a.title, { formSize: "lg" })} defaultText={namespace.title} onChange={forceUpdate} />
      </h1>
      <EditableHtmlComponent ctx={ctx.subCtx(a => a.description)} onChange={forceUpdate} />
      <div className={classes("btn-toolbar", "sf-button-bar", "mt-2")}>
        {ctx.value.modified && <SaveButton ctx={ctx} onSuccess={() => reloadNamespace()} />}
      </div>
      <h2 className="display-7 mt-4">Types</h2>
      <ul className="mt-4">
        {namespace.allowedTypes.map(t => <li key={t.cleanName}><Link to={Urls.typeUrl(t.cleanName)} >{getTypeInfo(t.cleanName).niceName}</Link></li>)}
      </ul>
    </div>
  );
}

function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<NamespaceHelpEntity>, onSuccess: () => void }) {

  var oi = tryGetOperationInfo(NamespaceHelpOperation.Save, NamespaceHelpEntity);

  if (!oi)
    return null;

  function onClick() {
    API.saveNamespace(ctx.value)
      .then((() => {
        onSuccess();
        notifySuccess();
      }));
  }

  return <button className="btn btn-primary" onClick={onClick}><FontAwesomeIcon icon="save" /> {oi.niceName}</button>;
}
