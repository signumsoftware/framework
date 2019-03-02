import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { DynamicApiEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, TypeContext } from '@framework/Lines'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'

interface DynamicApiProps {
  ctx: TypeContext<DynamicApiEntity>;
}

export default class DynamicApi extends React.Component<DynamicApiProps> {

  handleCodeChange = (newScript: string) => {
    const evalEntity = this.props.ctx.value.eval;
    evalEntity.modified = true;
    evalEntity.script = newScript;
    this.forceUpdate();
  }

  render() {
    var ctx = this.props.ctx;
    return (
      <div>
        <ValueLine ctx={ctx.subCtx(d => d.name)} />
        <br />
        <div className="row">
          <div className="col-sm-7">
            <div className="code-container">
              <pre style={{ border: "0px", margin: "0px" }} />
              <CSharpCodeMirror script={ctx.value.eval.script || ""} onChange={this.handleCodeChange} />
              <pre style={{ border: "0px", margin: "0px" }} />
            </div>
          </div>
          <div className="col-sm-5">
            <TypeHelpComponent mode="CSharp" />
          </div>
        </div>
      </div>
    );
  }
}
