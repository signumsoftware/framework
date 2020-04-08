import * as React from 'react'
import { Button } from 'react-bootstrap'
import { notifySuccess } from '@framework/Operations'
import { TypeContext, ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import { EntityLine, ValueLine } from '@framework/Lines'

import { useForceUpdate } from '@framework/Hooks'
import { ChartPaletteModel, ChartMessage, ChartColorEntity } from '../Signum.Entities.Chart'
import * as ChartPaletteClient from './ChartPaletteClient'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import { AuthAdminMessage } from '../../Authorization/Signum.Entities.Authorization'
import { EntityLink } from '../../../../Framework/Signum.React/Scripts/Search'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { Color } from '../../Basics/Color'

export default React.forwardRef(function ChartPaletteControl(p: { ctx: TypeContext<ChartPaletteModel> }, ref: React.Ref<IRenderButtons>) {


  function reload(bc: ButtonsContext) {
    const pal = (bc.pack.entity as ChartPaletteModel);
    ChartPaletteClient.API.fetchColorPalette(pal.type.cleanName, true)
      .then(newPack => {
        ChartPaletteClient.setColorPalette(newPack!);
        bc.frame.onReload({ entity: newPack!, canExecute: {} });
      })
      .done();
  }

  function handleSaveClick(bc: ButtonsContext) {
    const pal = (bc.pack.entity as ChartPaletteModel);
    ChartPaletteClient.API.saveColorPalette(pal)
      .then(newPack => {
        notifySuccess();
        reload(bc);
      })
      .done();
  }

  function handleDeleteClick(bc: ButtonsContext) {
    const pal = (bc.pack.entity as ChartPaletteModel);
    ChartPaletteClient.API.deleteColorPalette(pal.type.cleanName)
      .then(newPack => {
        notifySuccess();
        reload(bc);
      })
      .done();
  }

  function handleNewPaletteClick(bc: ButtonsContext) {
    const pal = (bc.pack.entity as ChartPaletteModel);
    SelectorModal.chooseElement(["Category10", "Category20", "Category20b", "Category20c"], { title: ChartMessage.ChooseABasePalette.niceToString() })
      .then(palette => palette && ChartPaletteClient.API.newColorPalette(pal.type.cleanName, palette)
        .then(newPack => {
          notifySuccess();
          reload(bc);
        }).done()
      ).done();
  }

  function renderButtons(bc: ButtonsContext) {
    return [
      { button: <Button variant="primary" onClick={() => handleSaveClick(bc)}>{ChartMessage.SavePalette.niceToString()}</Button> },
      { button: <Button variant="warning" onClick={() => handleNewPaletteClick(bc)}>{ChartMessage.NewPalette.niceToString()}</Button> },
      { button: <Button variant="danger" onClick={() => handleDeleteClick(bc)}>{ChartMessage.DeletePalette.niceToString()}</Button> },
    ];
  }

  React.useImperativeHandle(ref, () => ({ renderButtons }), [p.ctx.value])

  let ctx = p.ctx;

  return (
    <div>
      <div className="form-compact">
        <EntityLine ctx={ctx.subCtx(f => f.type)} readOnly />
      </div>
      <table className="table table-sm sf-auth-rules">
        <thead>
          <tr>
            <th>
              {ChartColorEntity.nicePropertyName(a => a.related)}
            </th>
            <th>
              {ChartColorEntity.nicePropertyName(a => a.color)}
            </th>
          </tr>
        </thead>
        <tbody>
          {ctx.mlistItemCtxs(a => a.colors).map((c, i) => <ChartColorRow key={i} ctx={c}/>)}
        </tbody>
      </table>

    </div>
  );
});


export function ChartColorRow(p: { ctx: TypeContext<ChartColorEntity> }) {

  const forceUpdate = useForceUpdate();

  return (
    <tr >
      <td style={{ background: p.ctx.value.color, color: Color.tryParse(p.ctx.value.color)?.opositePole().toString() }}>
        <EntityLink lite={p.ctx.value.related} />
      </td>
      <td style={{ textAlign: "center" }}>
        <ColorTypeaheadLine ctx={p.ctx.subCtx(t => t.color, { formGroupStyle: "SrOnly" })} onChange={() => forceUpdate()} />
      </td>
    </tr>
  );
}

