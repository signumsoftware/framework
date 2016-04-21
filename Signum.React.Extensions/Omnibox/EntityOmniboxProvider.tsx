import * as React from 'react'
import { Lite, Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { OmniboxMessage } from './Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from './OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'



export default class EntityOmniboxProvider extends OmniboxProvider<EntityOmniboxResult>
{
    getProviderName() {
        return "EntityOmniboxResult";
    }

    icon() {
        return this.coloredGlyphicon("glyphicon-circle-arrow-right", "#BCDEFF");
    }

    renderItem(result: EntityOmniboxResult): React.ReactChild[] {

        var array: React.ReactChild[] = [];

        array.push(this.icon());

        this.renderMatch(result.TypeMatch, array)
        array.push(<span> </span>);

        if (result.Id == null && result.ToStr == null) {
            throw Error("Invalid EntityOmniboxProvider result");
        } else {

            if (result.Id != null) {
                array.push(`${result.Id}: `);

                if (result.Lite == null) {
                    array.push(this.coloredSpan(OmniboxMessage.NotFound.niceToString(), "gray"));
                } else {
                    array.push(result.Lite.toStr);
                }
            } else {
                if (result.Lite == null) {
                    array.push(`'${result.ToStr}': `);
                    array.push(this.coloredSpan(OmniboxMessage.NotFound.niceToString(), "gray"));
                } else {
                    array.push(`${result.Lite.id}: `);
                    this.renderMatch(result.ToStrMatch, array);
                }
            }
        }

        return array;

    }

    navigateTo(result: EntityOmniboxResult): Promise<string> {

        if (result.Lite == null)
            return null;

        return Promise.resolve(Navigator.navigateRoute(result.Lite));
    }

    toString(result: EntityOmniboxResult) {
        if (result.Id)
            return `${result.TypeMatch.Text} ${result.Id}`;

        if (result.ToStr)
            return `${result.TypeMatch.Text} "${result.ToStr}"`;

        return result.TypeMatch.Text;
    }
}

interface EntityOmniboxResult extends OmniboxResult {
    TypeMatch: OmniboxMatch;
    Id: any;
    ToStr: string;
    ToStrMatch: OmniboxMatch;

    Lite: Lite<Entity>
}
