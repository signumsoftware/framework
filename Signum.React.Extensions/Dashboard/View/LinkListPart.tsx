
import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '../../../../Framework/Signum.React/Scripts/Lines'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { QueryDescription, SubTokensOptions, FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, Lite, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import { ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { TypeContext, FormGroupStyle, mlistItemContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { LinkListPartEntity, LinkElementEmbedded } from '../Signum.Entities.Dashboard'

export default class LinkListPart extends React.Component<{ part: LinkListPartEntity }> {

    render() {

        const entity = this.props.part;

        return (

            <ul className="sf-cp-link-list">
                {
                    entity.links!.map(mle => mle.element)
                        .map((le, i) =>
                            <li key={i} >
                                <a href={Navigator.toAbsoluteUrl(le.link!) }
                                    onClick={le.link!.startsWith("~") ? (e => { e.preventDefault(); Navigator.history.push(le.link!) }) : undefined}
                                    title={le.label!}>
                                    {le.label}
                                </a>
                            </li>)
                }
            </ul>
        );
    }
}
