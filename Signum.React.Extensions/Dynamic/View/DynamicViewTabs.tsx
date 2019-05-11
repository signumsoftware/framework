import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import {PropertyRoute, Binding } from '@framework/Reflection'
import { Expression, DesignerNode } from './NodeUtils'
import { BaseNode } from './Nodes'
import { DynamicViewMessage, DynamicViewEntity, DynamicViewPropEmbedded } from '../Signum.Entities.Dynamic'
import { Modal, Typeahead, UncontrolledTabs, Tab } from '@framework/Components';
import { TypeContext, EntityTable, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines';
import { DynamicViewTree } from './DynamicViewTree';
import { DynamicViewInspector } from './Designer';

export function DynamicViewTabs({ ctx, rootNode }: { ctx: TypeContext<DynamicViewEntity>, rootNode: DesignerNode<BaseNode> }) {


  const handleChange = () => rootNode.context.refreshView();

  return (
    <UncontrolledTabs>
      <Tab eventKey="render" title="Render">
        <DynamicViewTree rootNode={rootNode} />
        <DynamicViewInspector selectedNode={rootNode.context.getSelectedNode()} />
      </Tab>
      <Tab eventKey="props" title="Props">
        <EntityTable ctx={ctx.subCtx(a => a.props)} onChange={handleChange}
          columns={EntityTable.typedColumns<DynamicViewPropEmbedded>([
            { property: a => a.name, template: sctx => <ValueLine ctx={sctx.subCtx(a => a.name)} onChange={handleChange} /> },
            { property: a => a.type, template: sctx => <ValueLine ctx={sctx.subCtx(a => a.type)} onChange={handleChange} /> },
          ])} />
      </Tab>
    </UncontrolledTabs>
  );
}
