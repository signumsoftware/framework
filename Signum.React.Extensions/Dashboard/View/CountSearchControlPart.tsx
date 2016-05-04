
import * as React from 'react'
import { Link } from 'react-router'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions, FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, Lite, is, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { CountSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import SelectorPopup from '../../../../Framework/Signum.React/Scripts/SelectorPopup'
import { TypeContext, FormGroupStyle, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { CountSearchControlPartEntity, CountUserQueryElementEntity } from '../Signum.Entities.Dashboard'

export default class CountSearchControlPart extends React.Component<{ part: CountSearchControlPartEntity; entity: Lite<Entity>}, void> {
    
    render() {

        const entity = this.props.part;
        const ctx = TypeContext.root(CountSearchControlPartEntity, entity, { formGroupStyle: FormGroupStyle.None });
        return (
            <div>
                {
                    mlistItemContext(ctx.subCtx(a => a.userQueries))
                        .map((ctx, i) =>                             
                            <div key={i} >    
                                <CountUserQueryElement ctx={ctx} entity={this.props.entity}/>    
                            </div>)
                }
            </div>
        );
    }
}

export interface CountUserQueryElementProps {
    ctx?: TypeContext<CountUserQueryElementEntity>
    entity?: Lite<Entity>; 
}

export class CountUserQueryElement extends React.Component<CountUserQueryElementProps, {fo?: FindOptions }> {
    
    state = { fo: null } as { fo?: FindOptions };

    componentWillMount(){
        this.loadFindOptions(this.props);
    }

    componentWillReceiveProps(newProps : CountUserQueryElementProps ){

        if(is(this.props.ctx.value.userQuery, newProps.ctx.value.userQuery))
            return;

        this.loadFindOptions(newProps);
    }

    loadFindOptions(props: CountUserQueryElementProps) {

        UserQueryClient.Converter.toFindOptions(props.ctx.value.userQuery,  this.props.entity)
            .then(fo=>this.setState({fo: fo }))
            .done();
    }

    render(){

        var ctx = this.props.ctx;

        if (!this.state.fo)
            return <span>{ JavascriptMessage.loading.niceToString() }</span>;

        return (
            <div>
                <span>{ctx.value.label || getQueryNiceName(this.state.fo.queryName) }</span>&nbsp;
                <CountSearchControl ctx ={ctx} findOptions={this.state.fo} style="Badge" />
            </div>             
        );
    }   
} 



