
import * as React from 'react'
import { Dic } from '../Globals'
import { FindOptionsParsed, QueryToken, getTokenParents, isFilterGroupOptionParsed } from '../FindOptions'
import { ValidationMessage, External } from '../Signum.Entities'
import { getTypeInfos, TypeReference } from '../Reflection'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { FilterOptionParsed } from '../Search';


export default class MultipliedMessage extends React.Component<{ findOptions: FindOptionsParsed, mainType : TypeReference }>{

    render() {
        const fops = this.props.findOptions;

        function getFilterTokens(fop: FilterOptionParsed): (QueryToken | undefined)[] {
            if (isFilterGroupOptionParsed(fop))
                return [fop.token, ...fop.filters.flatMap(f => getFilterTokens(f))];
            else
                return [fop.operation == undefined ? undefined : fop.token]
        }

        const tokensObj = fops.columnOptions.map(a => a.token)
            .concat(fops.filterOptions.flatMap(fo => getFilterTokens(fo)))
            .concat(fops.orderOptions.map(a=> a.token))
            .filter(a=> a != undefined)
            .flatMap(a=> getTokenParents(a))
            .filter(a=> a.queryTokenType == "Element")
            .toObjectDistinct(a=> a.fullKey);

        const tokens = Dic.getValues(tokensObj);

        if (tokens.length == 0)
            return null;

        const message = ValidationMessage.TheNumberOf0IsBeingMultipliedBy1.niceToString().formatHtml(
            getTypeInfos(this.props.mainType).map(a => a.nicePluralName).joinComma(External.CollectionMessage.And.niceToString()),
            tokens.map(a => <strong>{a.parent!.niceName}</strong>).joinCommaHtml(External.CollectionMessage.And.niceToString()))

        return (
            <div className="sf-search-message alert alert-warning">
                <FontAwesomeIcon icon="exclamation-triangle" />&nbsp;{message}
            </div>
        );
    }

}



