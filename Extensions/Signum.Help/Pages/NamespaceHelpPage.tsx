import * as React from 'react'
import { useLocation, useParams, Link } from 'react-router-dom'
import { Navigator } from '@framework/Navigator'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { HelpClient } from '../HelpClient'
import { useAPI, useForceUpdate, useAPIWithReload } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, NamespaceHelpOperation } from '../Signum.Help';
import { getTypeInfo, GraphExplorer, symbolNiceName, tryGetOperationInfo, tryGetTypeInfo } from '@framework/Reflection';
import { JavascriptMessage, Entity } from '@framework/Signum.Entities';
import { Operations } from '@framework/Operations';
import { TypeContext } from '@framework/Lines';
import { EditableHtmlComponent, EditableTextComponent } from './EditableText';
import { useTitle } from '@framework/AppContext';
import { classes } from '@framework/Globals';
import { Shortcut } from './TypeHelpPage'


export default function NamespaceHelpPage(): React.JSX.Element {
  const params = useParams() as { namespace: string };

  var [count, setCount] = React.useState(0);
  var [namespace, reloadNamespace] = useAPIWithReload(() => HelpClient.API.namespace(params.namespace), [count]);
  useTitle(HelpMessage.Help.niceToString() + (namespace && (" > " + namespace.title)));
  var forceUpdate = useForceUpdate();
  if (namespace == null)
    return <div className="container"><h1 className="display-6">{JavascriptMessage.loading.niceToString()}</h1></div>;

  var ctx = TypeContext.root(namespace.entity, { readOnly: Navigator.isReadOnly(NamespaceHelpEntity) });

  return (
    <div className="container">
      <div className={classes("mb-2 shortcut-container")}>
        <h1 className="display-6"><Link to={HelpClient.Urls.indexUrl()}>
          {HelpMessage.Help.niceToString()}</Link>
          {" > "}
          <EditableTextComponent ctx={ctx.subCtx(a => a.title, { formSize: "lg" })} defaultText={namespace.title} onChange={forceUpdate} />
          <small className="ms-5 text-muted display-7">({ctx.value.culture.englishName})</small>
        </h1>
        <Shortcut text={`[n:${ctx.value.name}]`} />
      </div>

      <EditableHtmlComponent ctx={ctx.subCtx(a => a.description)} onChange={forceUpdate} />
      <div className={classes("btn-toolbar", "sf-button-bar", "mt-4")}>
        {ctx.value.modified && <SaveButton ctx={ctx} onSuccess={() => reloadNamespace()} />}
      </div>
      <h2 className="display-7 mt-4">Types</h2>
      <ul className="mt-4">
        {namespace.allowedTypes.map(t => <li key={t.cleanName}><Link to={HelpClient.Urls.typeUrl(t.cleanName)} >{getTypeInfo(t.cleanName).niceName}</Link></li>)}
      </ul>
    </div>
  );
}

function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<NamespaceHelpEntity>, onSuccess: () => void }) {

  var oi = tryGetOperationInfo(NamespaceHelpOperation.Save, NamespaceHelpEntity);

  if (!oi)
    return null;

  function onClick() {
    HelpClient.API.saveNamespace(ctx.value)
      .then((() => {
        onSuccess();
        Operations.notifySuccess();
      }));
  }

  return <button type="button" className="btn btn-primary" onClick={onClick}><FontAwesomeIcon aria-hidden={true} icon="save" /> {oi.niceName}</button>;
}
