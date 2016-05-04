
import * as React from 'react'
import { Link } from 'react-router'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions, FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, Lite, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { CountSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import SelectorPopup from '../../../../Framework/Signum.React/Scripts/SelectorPopup'
import { TypeContext, FormGroupStyle, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { LinkListPartEntity, LinkElementEntity} from '../Signum.Entities.Dashboard'

export default class LinkListPart extends React.Component<{ part: LinkListPartEntity }, void> {
    
    render() {

        var entity = this.props.part;
        
        return (
        
            <ul className="sf-cp-link-list">
                {
                    entity.links.map(mle => mle.element)
                        .map((link, i) =>                             
                            <li key={i} >    
                                <a href={Navigator.currentHistory.createHref(link.link)} 
                                    title={link.label}>
                                    {link.label}
                                    </a>    
                            </li>)
                }
            </ul>
        );
    }
}
