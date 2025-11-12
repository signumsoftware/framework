import * as React from 'react'
import { useParams, Link, useLocation } from 'react-router-dom'
import { Collapse } from 'react-bootstrap'
import { Navigator } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import * as AppContext from '@framework/AppContext'
import { HelpClient } from '../HelpClient'
import { Overlay, Tooltip } from "react-bootstrap";
import { useAPI, useForceUpdate, useAPIWithReload, useInterval } from '@framework/Hooks';
import { HelpMessage, AppendixHelpEntity, TypeHelpEntity, TypeHelpOperation, PropertyRouteHelpEmbedded, OperationHelpEmbedded, QueryHelpEntity, QueryColumnHelpEmbedded } from '../Signum.Help';
import { getTypeInfo, getQueryNiceName, getOperationInfo, tryGetOperationInfo } from '@framework/Reflection';
import { FrameMessage, JavascriptMessage } from '@framework/Signum.Entities';
import { TypeContext, PropertyRoute } from '@framework/Lines';
import { EditableHtml, HtmlViewer } from '../Editor/EditableHtml';
import { classes } from '@framework/Globals';
import { mlistItemContext } from '@framework/TypeContext';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useTitle } from '@framework/AppContext'
import { getNiceTypeName } from '@framework/Operations/MultiPropertySetter'

export default function TypeHelpPage(): React.JSX.Element {
  const params = useParams() as { cleanName: string };

  var hash = useHash();

  var cleanName = params.cleanName;
  var [typeHelp, reloadTypeHelp] = useAPIWithReload(() => HelpClient.API.type(cleanName), [cleanName]);
  var namespaceHelp = useAPI(() => !typeHelp ? Promise.resolve(undefined) : HelpClient.API.namespace(typeHelp.type.namespace), [typeHelp]);
  var forceUpdate = useForceUpdate();

  React.useEffect(() => {
    var elem = hash && document.getElementById(hash);

    if (elem)
      elem.scrollIntoView({ block: "center" });

  }, [hash, typeHelp]);


  useTitle(HelpMessage.Help.niceToString() +
    (namespaceHelp && (" > " + namespaceHelp.title)) +
    (" > " + getTypeInfo(cleanName).niceName));

  if (typeHelp == null || typeHelp.type.cleanName != cleanName)
    return <div className="container"><h1 className="display-6">{JavascriptMessage.loading.niceToString()}</h1></div>;

  var ctx = TypeContext.root(typeHelp, { readOnly: Navigator.isReadOnly(AppendixHelpEntity) });

  var propertyTree = mlistItemContext(ctx.subCtx(th => th.properties))
    .map(phCtx => ({ ctx: phCtx, pr: PropertyRoute.parse(cleanName, phCtx.value.property.path) }))
    .toTree(t => t.pr.propertyPath(), t => {
      var parent = t.pr.parent!;
      if (parent.propertyRouteType == "Root" || parent.propertyRouteType == "Mixin")
        return null;

      if (parent.propertyRouteType == "MListItem")
        return parent.parent!.propertyPath();

      return parent.propertyPath();
    });

  var filteredOperations = mlistItemContext(ctx.subCtx(th => th.operations)).filter(ectx => {
    var os = Operations.getSettings(ectx.value.operation);

    if (os instanceof EntityOperationSettings && os.isVisibleOnlyType && !os.isVisibleOnlyType(ctx.value.type.cleanName))
      return false;

    return true;
  });

  return (
    <div className="container">
      <h1 className="display-6">
        <Link to={HelpClient.Urls.indexUrl()}>{HelpMessage.Help.niceToString()}</Link>
        {" > "}
        {namespaceHelp && <Link to={HelpClient.Urls.namespaceUrl(namespaceHelp.namespace)}>{namespaceHelp.title}</Link>}
        {" > "}
        {getTypeInfo(typeHelp.type.cleanName).niceName}
        <small className="ms-5 text-muted display-7">({ctx.value.culture.englishName})</small>
      </h1>
      <div className="shortcut-container">
        <Shortcut text={`[t:${cleanName}]`} />
        <HtmlViewer htmlAttributes={{ className: "sf-info" }} text={typeHelp.info} />
      </div>
      <EditableHtml key={"__type_help_main_editor__"} ctx={ctx.subCtx(a => a.description)} defaultEditable={typeHelp.isNew} onChange={forceUpdate} />

      <h2 className="display-6">{ctx.niceName(a => a.properties)}</h2>
      <dl className="row">
        {propertyTree.map(node => <PropertyLine key={node.value.pr.propertyPath()} node={node} cleanName={cleanName} onChange={forceUpdate} hash={hash} />)}
      </dl>

      {
        filteredOperations.length > 0 && (<>
          <h2 className="display-6">{ctx.niceName(a => a.operations)}</h2>
          <dl className="row">
            {filteredOperations.map(octx => <OperationLine key={octx.value.operation.key} ctx={octx} cleanName={cleanName} onChange={forceUpdate} hash={hash} />)}
          </dl>
        </>)
      }

      {
        ctx.value.queries.length > 0 && (<>
          <h2 className="display-6">{ctx.niceName(a => a.queries)}</h2>
          {mlistItemContext(ctx.subCtx(th => th.queries)).map(qctx => <QueryBlock key={qctx.value.query.key} ctx={qctx} cleanName={cleanName} onChange={forceUpdate} hash={hash} />)}
        </>)
      }

      <div className={classes("btn-toolbar", "sf-button-bar")}>
        <SaveButton ctx={ctx} onSuccess={() => reloadTypeHelp()} />
      </div>

    </div>
  );
}

function PropertyLine({ node, cleanName, onChange, hash }: { node: TreeNode<{ ctx: TypeContext<PropertyRouteHelpEmbedded>, pr: PropertyRoute }>, cleanName: string, onChange: () => void, hash: string | undefined }) {

  var id = HelpClient.Urls.idProperty(node.value.pr);
  return (
    <>
      <dt className={classes("col-sm-3 shortcut-container text-end", hash == id && "sf-target")} id={id}>
        {node.value.pr.member!.niceName}<br />
        <Shortcut text={`[p:${cleanName}.${node.value.pr.propertyPath()}]`}/>
      </dt>
      <dd className="col-sm-9">
        <span className="info">
          <HtmlViewer htmlAttributes={{ className: "sf-info" }} text={node.value.ctx.value.info} />
          <EditableHtml ctx={node.value.ctx.subCtx(a => a.description)} onChange={onChange} />
        </span>
      </dd>
      {node.children.length > 0 && <div className="col-sm-12">
        <SubPropertiesCollapse node={node} cleanName={cleanName} onChange={onChange} hash={hash} />
      </div>}
    </>
  );
}

function SubPropertiesCollapse({ node, cleanName, onChange, hash }: { node: TreeNode<{ ctx: TypeContext<PropertyRouteHelpEmbedded>, pr: PropertyRoute }>, hash: string | undefined, cleanName: string, onChange: () => void }) {
  var [open, setOpen] = React.useState(false);
  const pr = node.value.pr;
  return (
    <>
      <div className="row mb-2">
        <button
          type="button"
          className="col-sm-9 offset-sm-3 lead border-0 bg-transparent text-start"
          onClick={() => setOpen(!open)}
          aria-expanded={open}>
          <FontAwesomeIcon aria-hidden="true" icon={open ? "chevron-down" : "chevron-right"}/> {pr.member!.niceName} ({getNiceTypeName(pr.member!.type)})
        </button>
      </div>

      <Collapse in={open}>
        <dl className="row ms-4">
          {open && node.children.map(n => <PropertyLine key={n.value.pr.propertyPath()} node={n} cleanName={cleanName} onChange={onChange} hash={hash} />)}
        </dl>
      </Collapse>
    </>
  );
}

function OperationLine({ ctx, cleanName, onChange, hash }: { ctx: TypeContext<OperationHelpEmbedded>, cleanName: string, onChange: () => void, hash: string | undefined }) {

  const id = HelpClient.Urls.idOperation(ctx.value.operation);

  return (
    <>
      <dt className={classes("col-sm-3 shortcut-container text-end", id == hash && "sf-target")} id={id}>
        {getOperationInfo(ctx.value.operation, cleanName).niceName}<br />
        <Shortcut text={`[o:${ctx.value.operation.key}]`} />
      </dt>
      <dd className="col-sm-9">
        <span className="info">
          <HtmlViewer htmlAttributes={{ className: "sf-info" }} text={ctx.value.info} />
          <EditableHtml ctx={ctx.subCtx(a => a.description)} onChange={onChange} />
        </span>
      </dd>
    </>
  );
}

function QueryBlock({ ctx, cleanName, onChange, hash }: { ctx: TypeContext<QueryHelpEntity>, cleanName: string, onChange: () => void, hash: string | undefined }) {
  var [open, setOpen] = React.useState(!ctx.value.isNew);
  const id = HelpClient.Urls.idQuery(ctx.value.query.key);
  return (
    <>
      <div className={classes("row mb-2 shortcut-container", id == hash && "sf-target")} id={id}>
        <div className="col-sm-9 offset-sm-3">
          <button
            type="button"
            className="lead border-0 bg-transparent"
            onClick={() => setOpen(!open)}
            aria-expanded={open}>
            <FontAwesomeIcon aria-hidden="true" icon={open ? "chevron-down" : "chevron-right"} /> {getQueryNiceName(ctx.value.query.key)}
          </button>
          {" "}
          {Finder.isFindable(ctx.value.query.key, true) && <a href={AppContext.toAbsoluteUrl(Finder.findOptionsPath({ queryName: ctx.value.query.key }))} target="_blank"><FontAwesomeIcon aria-hidden={true} icon="arrow-up-right-from-square" /></a>}
          {" "}
          <Shortcut text={`[q:${ctx.value.query.key}]`} />
          <EditableHtml ctx={ctx.subCtx(a => a.description)} onChange={onChange} />
        </div>
      </div>
      <Collapse in={open}>
        <dl className="row ms-4">
          {mlistItemContext(ctx.subCtx(th => th.columns)).map(qctx => <QueryColumnLine key={qctx.value.columnName} ctx={qctx} cleanName={cleanName} onChange={onChange} />)}
        </dl>
      </Collapse>
    </>
  );
}

function QueryColumnLine({ ctx, cleanName, onChange }: { ctx: TypeContext<QueryColumnHelpEmbedded>, cleanName: string, onChange: () => void }) {
  return (
    <>
      <dt className="col-sm-3 text-end">
        {ctx.value.niceName}<br />
      </dt>
      <dd className="col-sm-9">
        <span className="info">
          <HtmlViewer htmlAttributes={{ className: "sf-info" }} text={ctx.value.info} />
          <EditableHtml ctx={ctx.subCtx(a => a.description)} onChange={onChange} />
        </span>
      </dd>
    </>
  );
}


function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<TypeHelpEntity>, onSuccess: () => void }) {

  if (!tryGetOperationInfo(TypeHelpOperation.Save, TypeHelpEntity))
    return null;

  function onClick() {
    HelpClient.API.saveType(ctx.value)
      .then(() => {
        onSuccess();
        Operations.notifySuccess();
      });
  }

  return <button type="button" className="btn btn-primary" onClick={onClick}><FontAwesomeIcon aria-hidden={true} icon="save" /> {getOperationInfo(TypeHelpOperation.Save, TypeHelpEntity).niceName}</button>;
}

export function Shortcut(p: { text: string; }): React.JSX.Element {

  const supportsClipboard = (navigator.clipboard && window.isSecureContext);
  if (!supportsClipboard)
    return <code className='shortcut'>{p.text}</code>;

  const link = React.useRef<HTMLAnchorElement>(null);
  const [showTooltip, setShowTooltip] = React.useState<boolean>(false);
  const elapsed = useInterval(showTooltip ? 1000 : null, 0, d => d + 1);

  React.useEffect(() => {
    setShowTooltip(false);
  }, [elapsed]);

  return (
    <span>
      <code className='shortcut' ref={link} onClick={handleCopyLiteButton} title={FrameMessage.CopyToClipboard.niceToString()} >
        {p.text}
      </code>
      <Overlay target={link.current} show={showTooltip} placement="bottom">
        <Tooltip>
          {FrameMessage.Copied.niceToString()}
        </Tooltip>
      </Overlay>
    </span>
  );

  function handleCopyLiteButton(e: React.MouseEvent<any>) {
    e.preventDefault();
    navigator.clipboard.writeText(p.text)
      .then(() => setShowTooltip(true));
  }
}

function useHash(): string | undefined {
  const forceUpdate = useForceUpdate();

  React.useEffect(() => {
    window.addEventListener('hashchange', forceUpdate);
    return () => {
      window.removeEventListener('hashchange', forceUpdate);
    };
  }, []);

  return window.location.hash.tryAfter("#");
};

