import * as React from 'react'
import { Lite, Entity } from '@framework/Signum.Entities'
import { OmniboxMessage } from './Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from './OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '@framework/FindOptions'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'



export default class EntityOmniboxProvider extends OmniboxProvider<EntityOmniboxResult>
{
    getProviderName() {
        return "EntityOmniboxResult";
    }

    icon() {
        return this.coloredIcon("arrow-circle-right", "#BCDEFF");
    }

    renderItem(result: EntityOmniboxResult): React.ReactChild[] {

        const array: React.ReactChild[] = [];

        array.push(this.icon());

        this.renderMatch(result.TypeMatch, array)
        array.push(<span> </span>);

        if (result.Id == undefined && result.ToStr == undefined) {
            throw Error("Invalid EntityOmniboxProvider result");
        } else {

            if (result.Id != undefined) {
                array.push(`${result.Id}: `);

                if (result.Lite == undefined) {
                    array.push(this.coloredSpan(OmniboxMessage.NotFound.niceToString(), "gray"));
                } else {
                    array.push(result.Lite.toStr!);
                }
            } else {
                if (result.Lite == undefined) {
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

    navigateTo(result: EntityOmniboxResult) {

        if (result.Lite == undefined)
            return undefined;

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
