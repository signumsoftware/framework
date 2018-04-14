
import * as React from 'react'
import * as Finder from '../Finder'
import { classes } from '../Globals';
import { openModal, IModalProps } from '../Modals';
import { ColumnOptionParsed, QueryDescription, QueryToken, SubTokensOptions, FilterType } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, Entity } from '../Signum.Entities'
import { ValueLine, EntityLine, EntityCombo } from '../Lines'
import { Binding, IsByAll, getTypeInfos } from '../Reflection'
import { TypeContext, FormGroupStyle } from '../TypeContext'
import QueryTokenBuilder from './QueryTokenBuilder'


interface ColumnEditorProps extends React.Props<ColumnEditor> {
    columnOption: ColumnOptionParsed
    subTokensOptions: SubTokensOptions;
    queryDescription: QueryDescription;
    onChange: (token?: QueryToken) => void;
    close: () => void;
}

export default class ColumnEditor extends React.Component<ColumnEditorProps>{

    handleTokenChanged = (newToken: QueryToken | undefined) => {
        this.props.columnOption.token = newToken;
        this.props.columnOption.displayName = newToken && newToken.niceName;
        this.props.onChange(newToken);

    }

    handleOnChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        this.props.columnOption.displayName = event.currentTarget.value || undefined;
        this.props.onChange(undefined);
    }

    render() {
        const co = this.props.columnOption;

        const isCollection = co.token && co.token.type.isCollection;

        return (
            <div className={classes("sf-column-editor", isCollection ? "error" : undefined)}
                title={isCollection ? SearchMessage.CollectionsCanNotBeAddedAsColumns.niceToString() : undefined}>
                <button type="button" className="close" aria-label="Close" onClick={this.props.close} ><span aria-hidden="true">×</span></button>
                <div className="rw-widget-xs">
                    <QueryTokenBuilder
                        queryToken={co.token!}
                        onTokenChange={this.handleTokenChanged}
                        queryKey={this.props.queryDescription.queryKey}
                        subTokenOptions={this.props.subTokensOptions}
                        readOnly={false} />
                </div>
                <input className="form-control form-control-xs"
                    value={co.displayName || ""}
                    onChange={this.handleOnChange} />
            </div>
        );
    }

}



