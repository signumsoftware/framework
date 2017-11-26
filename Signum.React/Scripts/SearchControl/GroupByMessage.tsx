
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import { classes, Dic } from '../Globals'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import { FindOptionsParsed, QueryToken, getTokenParents, QueryTokenType } from '../FindOptions'
import { SearchMessage, JavascriptMessage, ValidationMessage, Lite, Entity, External } from '../Signum.Entities'
import { Binding, IsByAll, getTypeInfos, TypeReference } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'


export default class GroupByMessage extends React.Component<{ findOptions: FindOptionsParsed, mainType : TypeReference }>{

    render() {
        const fo = this.props.findOptions;

        const tokensObj = fo.columnOptions.map(a=> a.token)
            .concat(fo.orderOptions.map(a => a.token))
            .filter(a => a != undefined && a.queryTokenType != "Aggregate")
            .toObjectDistinct(a=> a!.fullKey, a => a!);

        const tokens = Dic.getValues(tokensObj);

        const message = ValidationMessage.TheRowsAreBeingGroupedBy0.niceToString().formatWith(
            tokens.map(a=> a.niceName).joinComma(External.CollectionMessage.And.niceToString()))

        return (
            <div className="sf-td-multiply alert alert-info">
                { "Ʃ\u00A0" + message}
            </div>
        );
    }

}



