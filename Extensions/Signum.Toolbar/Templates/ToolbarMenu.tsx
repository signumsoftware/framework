import * as React from 'react'
import { AutoLine, EntityRepeater, EntityLine, CheckboxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarMenuElementEmbedded, ToolbarMenuEntity, ToolbarMessage } from '../Signum.Toolbar'
import { ToolbarElementTable } from './Toolbar';
import { Tabs, Tab } from 'react-bootstrap';
import { getNiceTypeName } from '../../../Signum/React/Operations/MultiPropertySetter';
import { getTypeInfo } from '../../../Signum/React/Reflection';
import { getToString } from '../../../Signum/React/Signum.Entities';
import { useForceUpdate } from '../../../Signum/React/Hooks';

export default function ToolbarMenu(p : { ctx: TypeContext<ToolbarMenuEntity> }): React.JSX.Element {
  const ctx = p.ctx;

  const forceUpdate = useForceUpdate();
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(f => f.name)} />
      <EntityLine ctx={ctx.subCtx(f => f.owner)} />
      <EntityLine ctx={ctx.subCtx(f => f.entityType)} onChange={() => {
        forceUpdate();
      }} />
      {ctx.value.entityType ?
        <Tabs
          id="tabs"
          mountOnEnter
          unmountOnExit
          className="mt-2"
        >
          <Tab eventKey="noEntitySelected" title={ToolbarMessage.No0Selected.niceToString(getToString(ctx.value.entityType))} >
            <ToolbarElementTable ctx={ctx.subCtx(m => m.elements)}
              withEntity={false}
              extraColumns={[
                {
                  property: a => (a as ToolbarMenuElementEmbedded).autoSelect,
                },

              ]}
            />
          </Tab>
          <Tab eventKey="entitySelected" title={ToolbarMessage.If0Selected.niceToString(getToString(ctx.value.entityType))} >
            <ToolbarElementTable ctx={ctx.subCtx(m => m.elements)} 
              withEntity={true}
              extraColumns={[
                {
                  property: a => (a as ToolbarMenuElementEmbedded).autoSelect,
                },
               
              ]}
            />
          </Tab>

          <Tab eventKey="all" title={ToolbarMessage.ShowTogether.niceToString()}>
            <ToolbarElementTable ctx={ctx.subCtx(m => m.elements)} 
              extraColumns={[
                {
                  property: a => (a as ToolbarMenuElementEmbedded).autoSelect,
                },
                  {
                    property: a => (a as ToolbarMenuElementEmbedded).withEntity,
                  }
              ]}
            />
          </Tab>
        </Tabs>
        :
        <ToolbarElementTable ctx={ctx.subCtx(m => m.elements)}
          extraColumns={[
            {
              property: a => (a as ToolbarMenuElementEmbedded).autoSelect,
            },
           
          ]}
        />
      }
    </div>
  );
}
