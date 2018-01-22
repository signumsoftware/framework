import * as React from 'react'
import { ajaxGet, ajaxPost, ServiceError } from '../../../../Framework/Signum.React/Scripts/Services'
import { QueryTokenEmbedded } from '../Signum.Entities.UserAssets'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { TypeContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { QueryDescription, QueryToken, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import QueryTokenBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/QueryTokenBuilder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'


const CurrentEntityKey = "[CurrentEntity]";

interface QueryTokenEntityBuilderProps {
    ctx: TypeContext<QueryTokenEmbedded | null | undefined>;
    queryKey: string;
    subTokenOptions: SubTokensOptions;
    onTokenChanged?: (newToken: QueryToken | undefined) => void;
    helpText?: React.ReactChild;
}

export default class QueryTokenEntityBuilder extends React.Component<QueryTokenEntityBuilderProps> {

    handleTokenChanged = (newToken: QueryToken | undefined) => {
        if (newToken == undefined)
            this.props.ctx.value = undefined;
        else
            this.props.ctx.value = QueryTokenEmbedded.New({
                tokenString : newToken.fullKey,
                token : newToken
            });

        if (this.props.onTokenChanged)
            this.props.onTokenChanged(newToken);

        this.setState({ queryToken: newToken });
    }

    render() {


        const qte = this.props.ctx.value;

        const tokenBuilder = (
            <div className={this.props.ctx.rwWidgetClass}>
                <QueryTokenBuilder queryToken={qte && qte.token}
                    onTokenChange={this.handleTokenChanged} queryKey={this.props.queryKey} subTokenOptions={this.props.subTokenOptions}
                    readOnly={this.props.ctx.readOnly} />
            </div>
        );

        return (
            <FormGroup ctx={this.props.ctx} helpText={this.props.helpText}>
                {
                    !qte || !qte.parseException ? tokenBuilder :
                        <div>
                            <code>{qte.tokenString}</code>
                            <br />
                            {tokenBuilder}
                            <br />
                            <p className="alert alert-danger">
                                {qte.parseException}
                            </p>
                        </div>
                }
            </FormGroup>
        );
    }
}

