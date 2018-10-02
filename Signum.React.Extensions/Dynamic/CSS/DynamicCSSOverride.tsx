import * as React from 'react'
import { ValueLine, TypeContext } from '@framework/Lines'
import CSSCodeMirror from '../../Codemirror/CSSCodeMirror'
import { DynamicCSSOverrideEntity } from '../Signum.Entities.Dynamic'

export default class DynamicCSSOverrideComponent extends React.Component<{ ctx: TypeContext<DynamicCSSOverrideEntity> }> {

    handleCodeChange = (newScript: string) => {
        const entity = this.props.ctx.value;
        entity.script = newScript;
        entity.modified = true;
        this.forceUpdate();
    }

    render() {
        var ctx = this.props.ctx;

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(dt => dt.name)} />
                <br />
                <div className="code-container">
                    <CSSCodeMirror script={ctx.value.script || ""} onChange={this.handleCodeChange} />
                </div>
            </div>
        );
    }
}

