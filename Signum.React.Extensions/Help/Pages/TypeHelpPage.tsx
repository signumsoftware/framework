import * as React from 'react'
import { RouteComponentProps, Link } from 'react-router-dom'
import { Collapse } from 'react-bootstrap'
import * as numbro from 'numbro'
import * as Navigator from '@framework/Navigator'
import EntityLink from '@framework/SearchControl/EntityLink'
import { API, Urls } from '../HelpClient'
import { SearchControl } from '@framework/Search';
import { useAPI, useTitle, useForceUpdate, useAPIWithReload } from '@framework/Hooks';
import { HelpMessage, NamespaceHelpEntity, AppendixHelpEntity, TypeHelpEntity, TypeHelpOperation, PropertyRouteHelpEmbedded, OperationHelpEmbedded, QueryHelpEntity, QueryColumnHelpEmbedded } from '../Signum.Entities.Help';
import { getTypeInfo, GraphExplorer, getQueryNiceName } from '@framework/Reflection';
import { JavascriptMessage } from '@framework/Signum.Entities';
import { TypeContext, PropertyRoute } from '@framework/Lines';
import { EditableComponent, MarkdownText } from './EditableText';
import { classes } from '@framework/Globals';
import { notifySuccess } from '@framework/Operations';
import * as Operations from '@framework/Operations';
import * as HelpClient from '../HelpClient';
import { mlistItemContext } from '@framework/TypeContext';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

(window as any).myHistory = Navigator.history;

export default function TypeHelpPage(p: RouteComponentProps<{ cleanName: string }>) {

  var cleanName = p.match.params.cleanName;
  var [typeHelp, reloadTypeHelp] = useAPIWithReload(() => API.type(cleanName), [cleanName]);
  var namespaceHelp = useAPI(() => !typeHelp ? Promise.resolve(undefined) : API.namespace(typeHelp.type.namespace), [typeHelp]);
  var forceUpdate = useForceUpdate();

  useTitle(HelpMessage.Help.niceToString() +
    (namespaceHelp && (" > " + namespaceHelp.title)) +
    (" > " + getTypeInfo(cleanName).niceName));

  if (typeHelp == null || typeHelp.type.cleanName != cleanName)
    return <h1 className="display-6">{JavascriptMessage.loading.niceToString()}</h1>;

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

  return (
    <div>
      <h1 className="display-6">
        <Link to={Urls.indexUrl()}>{HelpMessage.Help.niceToString()}</Link>
        {" > "}
        {namespaceHelp && <Link to={Urls.namespaceUrl(namespaceHelp.namespace)}>{namespaceHelp.title}</Link>}
        {" > "}
        {getTypeInfo(typeHelp.type.cleanName).niceName}
      </h1>
      <div className="shortcut-container">
        <code className='shortcut'>[e:{cleanName}]</code>
        <MarkdownText className="sf-info" text={typeHelp.info} />
      </div>
      <EditableComponent ctx={ctx.subCtx(a => a.description)} markdown defaultEditable={typeHelp.isNew} onChange={forceUpdate} />

      <h2 className="display-7">{ctx.niceName(a => a.properties)}</h2>
      <dl className="row">
        {propertyTree.map(node => <PropertyLine key={node.value.pr.propertyPath()} node={node} cleanName={cleanName} onChange={forceUpdate} />)}
      </dl>

      {
        ctx.value.operations.length > 0 && (<>
          <h2 className="display-7">{ctx.niceName(a => a.operations)}</h2>
          <dl className="row">
            {mlistItemContext(ctx.subCtx(th => th.operations)).map(octx => <OperationLine key={octx.value.operation.key} ctx={octx} cleanName={cleanName} onChange={forceUpdate} />)}
          </dl>
        </>)
      }

      {
        ctx.value.queries.length > 0 && (<>
          <h2 className="display-7">{ctx.niceName(a => a.queries)}</h2>
          {mlistItemContext(ctx.subCtx(th => th.queries)).map(qctx => <QueryBlock key={qctx.value.query.key} ctx={qctx} cleanName={cleanName} onChange={forceUpdate} />)}
        </>)
      }

      <div className={classes("btn-toolbar", "sf-button-bar")}>
        <SaveButton ctx={ctx} onSuccess={() => reloadTypeHelp()} />
      </div>

    </div>
  );
}

function PropertyLine({ node, cleanName, onChange }: { node: TreeNode<{ ctx: TypeContext<PropertyRouteHelpEmbedded>, pr: PropertyRoute }>, cleanName: string, onChange: () => void }) {
  return (
    <>
      <dt className="col-sm-3 shortcut-container text-right" id={HelpClient.Urls.idProperty(node.value.pr)}>
        {node.value.pr.member!.niceName}<br />
        <code className='shortcut'>[p:{cleanName}.{node.value.pr.propertyPath()}]</code>
      </dt>
      <dd className="col-sm-9">
        <span className="info">
          <MarkdownText className="sf-info" text={node.value.ctx.value.info} />
          <EditableComponent ctx={node.value.ctx.subCtx(a => a.description)} markdown onChange={onChange} />
        </span>
      </dd>
      {node.children.length > 0 && <div className="col-sm-12">
        <SubPropertiesCollapse node={node} cleanName={cleanName} onChange={onChange} />
      </div>}
    </>
  );
}

function SubPropertiesCollapse({ node, cleanName, onChange }: { node: TreeNode<{ ctx: TypeContext<PropertyRouteHelpEmbedded>, pr: PropertyRoute }>, cleanName: string, onChange: () => void }) {
  var [open, setOpen] = React.useState(false);
  return (
    <>
      <div className="row ml-4 mb-2">
        <span className="col-sm-9 offset-sm-3 lead" style={{ cursor: "pointer" }} onClick={() => setOpen(!open)}>
          <FontAwesomeIcon icon={open ? "chevron-down" : "chevron-right"} /> {node.value.pr.member!.niceName}
        </span>
      </div>
      <Collapse in={open}>
        <dl className="row ml-4">
          {node.children.map(n => <PropertyLine key={node.value.pr.propertyPath()} node={node} cleanName={cleanName} onChange={onChange} />)}
        </dl>
      </Collapse>
    </>
  );
}

function OperationLine({ ctx, cleanName, onChange }: { ctx: TypeContext<OperationHelpEmbedded>, cleanName: string, onChange: () => void }) {
  return (
    <>
      <dt className="col-sm-3 shortcut-container text-right" id={HelpClient.Urls.idOperation(ctx.value.operation)}>
        {Operations.getOperationInfo(ctx.value.operation, cleanName).niceName}<br />
        <code className='shortcut'>[o:{cleanName}.{ctx.value.operation.key}]</code>
      </dt>
      <dd className="col-sm-9">
        <span className="info">
          <MarkdownText className="sf-info" text={ctx.value.info} />
          <EditableComponent ctx={ctx.subCtx(a => a.description)} markdown onChange={onChange} />
        </span>
      </dd>
    </>
  );
}

function QueryBlock({ ctx, cleanName, onChange }: { ctx: TypeContext<QueryHelpEntity>, cleanName: string, onChange: () => void }) {
  var [open, setOpen] = React.useState(!ctx.value.isNew);
  return (
    <>
      <div className="row mb-2 shortcut-container">
        <div className="col-sm-9 offset-sm-3">
          <span className="lead " style={{ cursor: "pointer" }} onClick={() => setOpen(!open)} id={HelpClient.Urls.idQuery(ctx.value.query.key)}>
            <FontAwesomeIcon icon={open ? "chevron-down" : "chevron-right"} /> {getQueryNiceName(ctx.value.query.key)} 
          </span>
          {" "}<code className='shortcut'>[q:{cleanName}.{ctx.value.query.key}]</code>
          <EditableComponent ctx={ctx.subCtx(a => a.description)} markdown onChange={onChange} />
        </div>
      </div>
      <Collapse in={open}>
        <dl className="row ml-4">
          {mlistItemContext(ctx.subCtx(th => th.columns)).map(qctx => <QueryColumnLine key={qctx.value.columnName} ctx={qctx} cleanName={cleanName} onChange={onChange} />)}
        </dl>
      </Collapse>
    </>
  );
}

function QueryColumnLine({ ctx, cleanName, onChange }: { ctx: TypeContext<QueryColumnHelpEmbedded>, cleanName: string, onChange: () => void }) {
  return (
    <>
      <dt className="col-sm-3 text-right">
        {ctx.value.niceName}<br />
      </dt>
      <dd className="col-sm-9">
        <span className="info">
          <MarkdownText className="sf-info" text={ctx.value.info} />
          <EditableComponent ctx={ctx.subCtx(a => a.description)} markdown onChange={onChange} />
        </span>
      </dd>
    </>
  );
}


function SaveButton({ ctx, onSuccess }: { ctx: TypeContext<TypeHelpEntity>, onSuccess: () => void }) {

  if (!Operations.isOperationAllowed(TypeHelpOperation.Save, TypeHelpEntity))
    return null;

  function onClick() {
    API.saveType(ctx.value)
      .then((() => {
        onSuccess();
        notifySuccess();
      }))
      .done();
  }

  return <button className="btn btn-primary" onClick={onClick}>{Operations.getOperationInfo(TypeHelpOperation.Save, TypeHelpEntity).niceName}</button>;
}
