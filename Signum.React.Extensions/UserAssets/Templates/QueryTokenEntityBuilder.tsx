import * as React from 'react'
import { ajaxGet, ajaxPost, ServiceError } from '../../../../Framework/Signum.React/Scripts/Services'
import { QueryTokenEntity, QueryTokenEntity_Type} from '../Signum.Entities.UserAssets'
import { FormGroup, FormControlStatic, EntityComponent, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { TypeContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { QueryDescription, QueryToken, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import QueryTokenBuilder from '../../../../Framework/Signum.React/Scripts/SearchControl/QueryTokenBuilder'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'


const CurrentEntityKey = "[CurrentEntity]";

interface QueryTokenEntityBuilderProps {
    ctx: TypeContext<QueryTokenEntity>;
    queryKey: string;
    subTokenOptions: SubTokensOptions;
    onTokenChanged?: (newToken: QueryToken) => void;
}

export default class QueryTokenEntityBuilder extends React.Component<QueryTokenEntityBuilderProps, { }> {

    handleTokenChanged = (newToken: QueryToken) => {
        if (newToken == null)
            this.props.ctx.value = null;
        else
            this.props.ctx.value = QueryTokenEntity_Type.New(t => {
                t.tokenString = newToken.fullKey;
                t.token = newToken
            });

        if (this.props.onTokenChanged)
            this.props.onTokenChanged(newToken);

        this.setState({ queryToken: newToken });
    }

    render() {


        const qte = this.props.ctx.value;

        const tokenBuilder = <QueryTokenBuilder queryToken={qte && qte.token}
            onTokenChange={this.handleTokenChanged} queryKey={this.props.queryKey} subTokenOptions={this.props.subTokenOptions}
            readOnly={this.props.ctx.readOnly}/>

        return (
            <FormGroup ctx={this.props.ctx}>
                {
                    !qte || !qte.parseException ? tokenBuilder :
                        <div>
                            {tokenBuilder}
                            <p className="alert alert-danger">{qte.parseException}</p>
                        </div>
                }
            </FormGroup>
        );
    }
}

