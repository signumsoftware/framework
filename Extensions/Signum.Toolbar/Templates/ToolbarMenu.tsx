import * as React from 'react'
import { AutoLine, EntityRepeater, EntityLine, CheckboxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import {
  ToolbarEntity,
  ToolbarMenuElementEmbedded, ToolbarMenuEntity, ToolbarMessage, 
  ToolbarSwitcherEntity } from '../Signum.Toolbar'
import { ToolbarElementTable } from './Toolbar';
import { Tabs, Tab } from 'react-bootstrap';
import { getNiceTypeName } from '../../../Signum/React/Operations/MultiPropertySetter';
import { getTypeInfo } from '../../../Signum/React/Reflection';
import { getToString } from '../../../Signum/React/Signum.Entities';
import { useForceUpdate } from '../../../Signum/React/Hooks';
import { UserAssetMessage } from '../../Signum.UserAssets/Signum.UserAssets';
import { SearchValueLine } from '@framework/Search';
import CollapsableCard from '@framework/Components/CollapsableCard';
import ToolbarSwitcher from './ToolbarSwitcher';

export default function ToolbarMenu(p: { ctx: TypeContext<ToolbarMenuEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx4 = ctx.subCtx({ labelColumns: 4 });

  const forceUpdate = useForceUpdate();
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(f => f.name)} />
      <EntityLine ctx={ctx.subCtx(f => f.owner)} />
      <EntityLine ctx={ctx.subCtx(f => f.entityType)} onChange={() => {
        forceUpdate();
      }} />
      {!ctx.value.isNew && <CollapsableCard header={UserAssetMessage.Advanced.niceToString()} size="xs">

        <div>
          <h2 className="mt-3 h5">{UserAssetMessage.UsedBy.niceToString()}</h2>
          <div className="row">
            <div className="col-sm-6">
              <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarMenuEntity, filterOptions: [{ token: ToolbarMenuEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
              <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarEntity, filterOptions: [{ token: ToolbarEntity.token(a => a.entity.elements).any().append(a => a.content), value: ctx.value }] }} />
            </div>
            <div className="col-sm-6">
              <SearchValueLine ctx={ctx4} findOptions={{ queryName: ToolbarSwitcherEntity, filterOptions: [{ token: ToolbarSwitcherEntity.token(a => a.entity.options).any().append(a => a.toolbarMenu), value: ctx.value }] }} />
            </div>
          </div>
        </div>
      </CollapsableCard>
      }

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
