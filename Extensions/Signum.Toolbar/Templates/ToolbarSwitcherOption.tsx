import * as React from 'react'
import { AutoLine, EntityRepeater, EntityLine, EntityTable, FontAwesomeIcon } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarMenuEntity, ToolbarSwitcherEntity, ToolbarSwitcherOptionEmbedded } from '../Signum.Toolbar'
import { ToolbarElementTable } from './Toolbar';
import { fallbackIcon, IconTypeaheadLine, parseIcon } from '../../../Signum/React/Components/IconTypeahead';
import { useForceUpdate } from '@framework/Hooks';

export default function ToolbarSwitcherOption(p: { ctx: TypeContext<ToolbarSwitcherOptionEmbedded> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx;

  const ctx4 = ctx.subCtx({ labelColumns: 4 });

  const icon = parseIcon(ctx.value.iconName);
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(f => f.toolbarMenu)} />
      <div className="row">
        <div className="col-sm-6">
          <IconTypeaheadLine ctx={ctx4.subCtx(t => t.iconName)} onChange={() => forceUpdate()} extraIcons={["none"]} />
          <AutoLine ctx={ctx4.subCtx(t => t.iconColor)} onChange={() => forceUpdate()} />
        </div>
        </div>
    </div>
  );
}
