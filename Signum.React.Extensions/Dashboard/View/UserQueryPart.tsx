
import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity } from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions, FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, Lite, is, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { SearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { UserQueryPartEntity } from '../Signum.Entities.Dashboard'


export interface UserQueryPartProps {
    part: UserQueryPartEntity
    entity?: Lite<Entity>;
}

export default class UserQueryPart extends React.Component<UserQueryPartProps, { fo?: FindOptions }> {

    constructor(props: any) {
        super(props);
        this.state = { fo: undefined };
    }


    componentWillMount() {
        this.loadFindOptions(this.props);
    }

    componentWillReceiveProps(newProps: UserQueryPartProps) {

        if (is(this.props.part.userQuery, newProps.part.userQuery) &&
            is(this.props.entity, newProps.entity))
            return;

        this.loadFindOptions(newProps);
    }

    loadFindOptions(props: UserQueryPartProps) {

        UserQueryClient.Converter.toFindOptions(props.part.userQuery!, props.entity)
            .then(fo => this.setState({ fo: fo }))
            .done();
    }

    render() {

        if (!this.state.fo)
            return <span>{JavascriptMessage.loading.niceToString()}</span>;

        return (
            <SearchControl
                findOptions={this.state.fo}
                showHeader={false}
                showFooter={false}
                allowSelection={this.props.part.renderMode == "SearchControl"} />
        );
    }
}



