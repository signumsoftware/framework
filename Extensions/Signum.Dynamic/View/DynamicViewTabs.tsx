import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic } from '@framework/Globals'
import { Finder } from '@framework/Finder'
import {PropertyRoute, Binding } from '@framework/Reflection'
import { Expression, DesignerNode } from './NodeUtils'
import { BaseNode } from './Nodes'
import { Tabs, Tab } from 'react-bootstrap';
import { TypeContext, EntityTable, AutoLine } from '@framework/Lines';
import { DynamicViewTree } from './DynamicViewTree';
import { DynamicViewInspector, PropsHelp } from './Designer';
import { ModulesHelp } from "./ModulesHelp";
import JavascriptCodeMirror from '../../Signum.CodeMirror/JavascriptCodeMirror';
import { DynamicViewEntity, DynamicViewPropEmbedded } from '../Signum.Dynamic.Views'

export function DynamicViewTabs({ ctx, rootNode }: { ctx: TypeContext<DynamicViewEntity>, rootNode: DesignerNode<BaseNode> }): React.JSX.Element {

  const typeName = rootNode.route!.typeReference().name;
  const handleChange = () => rootNode.context.refreshView();

  return (
    <Tabs id="dynamicView_dropdown" mountOnEnter={true}>
      <Tab eventKey="render" title="Render">
        <DynamicViewTree rootNode={rootNode} />
        <DynamicViewInspector selectedNode={rootNode.context.getSelectedNode()} />
      </Tab>
      <Tab eventKey="props" title="Props">
        <EntityTable ctx={ctx.subCtx(a => a.props)} onChange={handleChange}
          columns={[
            { property: a => a.name, template: sctx => <AutoLine ctx={sctx.subCtx(a => a.name)} onChange={handleChange} /> },
            { property: a => a.type, template: sctx => <AutoLine ctx={sctx.subCtx(a => a.type)} onChange={handleChange} /> },
          ]} />
      </Tab>
      <Tab eventKey="locals" title="Locals">
        <div className="code-container">
          <pre style={{ border: "0px", margin: "0px", overflow: "visible" }}>
            {"(ctx: TypeContext<" + typeName + "Entity>, "}
            <div style={{ display: "inline-flex" }}>
              <ModulesHelp cleanName={typeName} />{", "}<PropsHelp node={rootNode} />{") =>"}
            </div>
          </pre>
          <JavascriptCodeMirror code={ctx.value.locals ?? ""} onChange={newCode => { ctx.value.locals = newCode; ctx.value.modified = true; handleChange(); } } />
        </div>
      </Tab>
    </Tabs>
  );
}
