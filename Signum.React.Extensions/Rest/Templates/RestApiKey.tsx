import * as React from 'react'
import * as moment from 'moment'
import { RestApiKeyEntity } from '../Signum.Entities.Rest'
import { TypeContext, ValueLine, ValueLineType, EntityLine, EntityRepeater } from "../../../../Framework/Signum.React/Scripts/Lines";
import { classes } from "../../../../Framework/Signum.React/Scripts/Globals";
import { API } from "../RestClient";

export default class RestApiKeyComponent extends React.Component<{ ctx: TypeContext<RestApiKeyEntity> }> {

    apiKey?: ValueLine | null;

    render() {
        const ctx = this.props.ctx;
        return (
            <div>
                <EntityLine ctx={ctx.subCtx(e => e.user)} />
                <ValueLine ctx={ctx.subCtx(e => e.apiKey)}
                    ref={apiKey => this.apiKey = apiKey}
                    extraButtons={vl =>
                        <a href="#" className={classes("sf-line-button", "sf-view", "btn btn-light")}
                            onClick={this.generateApiKey}>
                            <span className="fa fa-key" />
                        </a>} />
            </div>
        );
    }

    generateApiKey = (e: React.MouseEvent<any>) => {
        e.preventDefault();
        API.generateRestApiKey()
            .then(key => this.apiKey!.setValue(key))
            .done();
    }
}

