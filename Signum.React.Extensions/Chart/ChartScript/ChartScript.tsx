import * as React from 'react'
import { ChartScriptEntity, ChartScriptColumnEmbedded, ChartScriptParameterEmbedded } from '../Signum.Entities.Chart'
import { ValueLine, EntityRepeater } from '@framework/Lines'
import * as Navigator from '@framework/Navigator'
import { TypeContext } from '@framework/TypeContext'
import FileLine from '../../Files/FileLine'
import ChartScriptCode from './ChartScriptCode'
import "../Chart.css"

export default class ChartScript extends React.Component<{ ctx: TypeContext<ChartScriptEntity> }> {

  componentWillMount() {
    this.loadIcon(this.props);
  }

  componentWillReceiveProps(newProps: { ctx: TypeContext<ChartScriptEntity> }) {
    this.loadIcon(newProps);
  }

  loadIcon(props: { ctx: TypeContext<ChartScriptEntity> }) {
    if (props.ctx.value.icon) {
      Navigator.API.fetchAndRemember(props.ctx.value.icon)
        .then(() => this.forceUpdate())
        .done();;
    }
  }


  render() {

    const ctx = this.props.ctx;
    const icon = ctx.value.icon;


    return (
      <div>

        <div className="row">
          <div className="col-sm-11">
            <ValueLine ctx={ctx.subCtx(c => c.name)} />
            <FileLine ctx={ctx.subCtx(c => c.icon)} onChange={() => this.forceUpdate()} />
            <ValueLine ctx={ctx.subCtx(c => c.groupBy)} />
          </div>
          <div className="col-sm-1">
            {icon && icon.entity && <img src={"data:image/png;base64," + icon.entity.binaryFile} />}
          </div>
        </div>

        <div className="sf-chartscript-columns">
          <EntityRepeater ctx={ctx.subCtx(c => c.columns)} getComponent={this.renderColumn} />
        </div>
        <EntityRepeater ctx={ctx.subCtx(c => c.parameters)} getComponent={this.renderParameter} />
        <ChartScriptCode ctx={ctx} />
      </div>
    );
  }

  renderColumn = (ctx: TypeContext<ChartScriptColumnEmbedded>) => {
    return (
      <div>
        <ValueLine ctx={ctx.subCtx(c => c.displayName)} valueColumns={{ sm: 8 }} />
        <div className="row">
          <div className="col-sm-6">
            <ValueLine ctx={ctx.subCtx(c => c.columnType)} labelColumns={{ sm: 4 }} />
          </div>
          <div className="col-sm-3">
            <ValueLine ctx={ctx.subCtx(c => c.isGroupKey)} inlineCheckbox={true} />
          </div>
          <div className="col-sm-3">
            <ValueLine ctx={ctx.subCtx(c => c.isOptional)} inlineCheckbox={true} />
          </div>
        </div>
      </div>
    );
  }

  renderParameter = (ctx: TypeContext<ChartScriptParameterEmbedded>) => {
    const cc = ctx.subCtx({ formGroupStyle: "Basic" });
    return (
      <div className="row">
        <div className="col-sm-2">
          <ValueLine ctx={cc.subCtx(c => c.name)} />
        </div>

        <div className="col-sm-2">
          <ValueLine ctx={cc.subCtx(c => c.type)} />
        </div>

        <div className="col-sm-6">
          <ValueLine ctx={cc.subCtx(c => c.valueDefinition)} />
        </div>
        <div className="col-sm-2">
          <ValueLine ctx={cc.subCtx(c => c.columnIndex)} />
        </div>
      </div>
    );
  }
}

