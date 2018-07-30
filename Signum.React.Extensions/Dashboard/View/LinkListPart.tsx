
import * as React from 'react'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater, RenderEntity} from '@framework/Lines'
import * as Finder from '@framework/Finder'
import { QueryDescription, SubTokensOptions, FindOptions } from '@framework/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, Lite, is } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import { ValueSearchControl } from '@framework/Search'
import { TypeContext, FormGroupStyle, mlistItemContext } from '@framework/TypeContext'
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
