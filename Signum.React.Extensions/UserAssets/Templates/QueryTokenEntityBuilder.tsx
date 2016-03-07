import * as React from 'react'
import { ajaxGet, ajaxPost, ServiceError } from '../../../../Framework/Signum.React/Scripts/Services'
import { QueryTokenEntity} from '../Signum.Entities.UserAssets'
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
}



export default class QueryTokenEntityBuilder extends React.Component<QueryTokenEntityBuilderProps, { queryToken?: QueryToken; error?: string }> {

    constructor(props) {
        super(props);
        this.state = { queryToken: null };
    }

    componentWillMount() {
        this.props.ctx.value.tokenString;

        Finder.parseSingleToken(this.props.queryKey, this.props.ctx.value.tokenString, this.props.subTokenOptions)
            .then(qt => this.setState({ queryToken: qt, error: null }))
            .catch((error: ServiceError) => {
                if (error instanceof ServiceError) {
                    this.setState({
                        queryToken: null, error: error.httpError.ExceptionMessage
                    });
                }
            })
            .done();
    }

    handleTokenChanged = (newToken: QueryToken) => {
        this.props.ctx.value.tokenString = newToken ? newToken.fullKey : null;
        this.setState({ queryToken: newToken });
    }

    render() {

        var token = <QueryTokenBuilder queryToken={this.state.queryToken}
            onTokenChange={this.handleTokenChanged} queryKey={this.props.queryKey} subTokenOptions={this.props.subTokenOptions}
            readOnly={this.props.ctx.readOnly}/>

        return (
            <FormGroup ctx={this.props.ctx}>
                {
                    this.state.error == null ? token :
                        <div>
                            {token}
                            <p className="alert alert-danger">{this.state.error}</p>
                        </div>
                }
            </FormGroup>
        );
    }
}

