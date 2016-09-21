﻿
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar } from 'react-bootstrap'
import { classes, Dic } from '../Globals'
import * as Finder from '../Finder'
import { openModal, IModalProps } from '../Modals';
import { FindOptionsParsed, QueryToken, getTokenParents, QueryTokenType } from '../FindOptions'
import { SearchMessage, JavascriptMessage, ValidationMessage, Lite, Entity, External } from '../Signum.Entities'
import { Binding, IsByAll, getTypeInfos, TypeReference } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'


export default class MultipliedMessage extends React.Component<{ findOptions: FindOptionsParsed, mainType : TypeReference }, {}>  {

    render() {
        const fo = this.props.findOptions;

        const tokensObj = fo.columnOptions.map(a=> a.token)
            .concat(fo.filterOptions.filter(a=> a.operation != undefined).map(a=> a.token))
            .concat(fo.orderOptions.map(a=> a.token))
            .filter(a=> a != undefined)
            .flatMap(a=> getTokenParents(a))
            .filter(a=> a.queryTokenType == QueryTokenType.Element)
            .toObjectDistinct(a=> a.fullKey);

        const tokens = Dic.getValues(tokensObj);

        if (tokens.length == 0)
            return null;

        const message = ValidationMessage.TheNumberOf0IsBeingMultipliedBy1.niceToString().formatWith(
            getTypeInfos(this.props.mainType).map(a=> a.nicePluralName).joinComma(External.CollectionMessage.And.niceToString()),
            tokens.map(a=> a.parent!.niceName).joinComma(External.CollectionMessage.And.niceToString()))

        return (
            <div className="sf-td-multiply alert alert-warning">
                <span className="glyphicon glyphicon-exclamation-sign" />{ "\u00A0" + message}
            </div>
        );
    }

}



