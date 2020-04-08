import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { DynamicClientEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, TypeContext } from '@framework/Lines'
import CSharpCodeMirror from '../../Codemirror/CSharpCodeMirror'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import ValueLineModal from '@framework/ValueLineModal';
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror';
import { useForceUpdate } from '../../../../Framework/Signum.React/Scripts/Hooks';
import { ModulesHelp } from '../View/ModulesHelp';


export default function DynamicClientComponent(p: { ctx: TypeContext<DynamicClientEntity> }) {

  const forceUpdate = useForceUpdate();
  var ctx = p.ctx;

  function handleCodeChange(newCode: string) {
    ctx.value.modified = true;
    ctx.value.code = newCode;
    forceUpdate();
  }

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(d => d.name)} />
      <br />
      <div className="row">
        <div className="col-sm-7">

          <div className="code-container">
            <pre style={{ border: "0px", margin: "0px", overflow: "visible" }}>
              {"("}
              <div style={{ display: "inline-flex" }}>
                <ModulesHelp cleanName="YourType" clientCode />{") =>"}
              </div>
            </pre>
            <JavascriptCodeMirror code={ctx.value.code ?? ""} onChange={handleCodeChange} />
          </div>
        </div>
        <div className="col-sm-5">
          <TypeHelpComponent mode="TypeScript" />
        </div>
      </div>
    </div>
  );
}

