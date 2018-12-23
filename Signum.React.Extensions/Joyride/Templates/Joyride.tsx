import * as React from 'react'
import { JoyrideEntity, JoyrideStepEntity } from '../Signum.Entities.Joyride'
import { ValueLine, EntityLine, EntityStrip, TypeContext } from '@framework/Lines'

export default class JoyrideStep extends React.Component<{ ctx: TypeContext<JoyrideEntity> }> {

  render() {
    const ctx = this.props.ctx;
    return (
      <div>
        <ValueLine ctx={ctx.subCtx(a => a.name)} />
        <EntityLine ctx={ctx.subCtx(a => a.culture)} onChange={() => this.forceUpdate()} />
        <ValueLine ctx={ctx.subCtx(a => a.type)} />
        <EntityStrip ctx={ctx.subCtx(a => a.steps)}
          findOptions={{
            queryName: JoyrideStepEntity,
            parentToken: JoyrideStepEntity.token(e => e.culture),
            parentValue: ctx.value.culture
          }}
        />
        <div className="row">
          <div className="col-xs-5 col-xs-offset-2">
            <ValueLine ctx={ctx.subCtx(a => a.showSkipButton)} inlineCheckbox="block" />
          </div>
          <div className="col-xs-5">
            <ValueLine ctx={ctx.subCtx(a => a.showStepsProgress)} inlineCheckbox="block" />
          </div>
        </div>
        <div className="row">
          <div className="col-xs-5 col-xs-offset-2">
            <ValueLine ctx={ctx.subCtx(a => a.keyboardNavigation)} inlineCheckbox="block" />
          </div>
          <div className="col-xs-5">
            <ValueLine ctx={ctx.subCtx(a => a.debug)} inlineCheckbox="block" />
          </div>
        </div>
      </div>
    );
  }
}
