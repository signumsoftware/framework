
import * as React from 'react'
import { EntityLine, EntityTable, ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ColorPaletteEntity, SpecificColorEmbedded } from '../Signum.Entities.Chart';
import { colorSchemes } from './ColorUtils';
import { Dic } from '@framework/Globals';
import { useForceUpdate } from '@framework/Hooks';
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead';

export default function ColorPalette(p: { ctx: TypeContext<ColorPaletteEntity> }) {
  const ctx = p.ctx;
  const forceUpdate = useForceUpdate();
  const ctx4 = ctx.subCtx({ labelColumns: 2 });
  return (
    <div>
      <EntityLine ctx={ctx4.subCtx(n => n.type)} readOnly={!ctx.value.isNew || ctx.value.specificColors.length > 0} onChange={forceUpdate} />
      <ValueLine ctx={ctx4.subCtx(n => n.categoryName)}
        valueLineType="DropDownList"
        optionItems={Dic.getKeys(colorSchemes)}
      />
      <ValueLine ctx={ctx4.subCtx(n => n.seed)} />
      <EntityTable ctx={ctx.subCtx(p => p.specificColors)} columns={EntityTable.typedColumns<SpecificColorEmbedded>([
        {
          property: p => p.entity,
          template: (ectx) => <EntityLine ctx={ectx.subCtx(p => p.entity)} type={{ name: ctx4.value.type.cleanName }} />,
          headerHtmlAttributes: { style: { width: "40%" } },
        },
        {
          property: p => p.color,
          template: (ectx) => <ColorTypeaheadLine ctx={p.ctx.subCtx(t => t.categoryName, { formGroupStyle: "SrOnly" })} onChange={() => forceUpdate()} />,
          headerHtmlAttributes: { style: { width: "40%" } },
        },
      ])}
      />
    </div>
  );
}




