import * as React from 'react'
import { DynamicViewSelectorEntity } from '../Signum.Entities.Dynamic'
import { ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import JavascriptCodeMirror from './JavascriptCodeMirror'

export default class DynamicViewEntityComponent extends React.Component<{ ctx: TypeContext<DynamicViewSelectorEntity> }, void> {
    
    handleCodeChange = (newCode: string) => {
        var dvs = this.props.ctx.value;
        dvs.script = newCode;
        dvs.modified = true;
        this.forceUpdate();
    }

    render() {
        const ctx = this.props.ctx;
        return (
            <div>
                <EntityLine ctx={ctx.subCtx(a => a.entityType)} onChange={() => this.forceUpdate()} />

                {ctx.value.entityType && <div>
                    <pre style={{ border: "0", margin: "0" , color: "Green" }}>{"//Return the ViewName, \"NEW\",  \"STATIC\" or \"CHOOSE\""}</pre>
                    <pre style={{ border: "0", margin: "0" }}>{"(e: " + ctx.value.entityType.className + ", auth) =>"}</pre>
                    <JavascriptCodeMirror code={ctx.value.script || ""} onChange={this.handleCodeChange} />
                </div>}
            </div>
        );
    }
}

