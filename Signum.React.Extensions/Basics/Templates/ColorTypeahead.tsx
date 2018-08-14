
import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes, Dic } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { Typeahead } from '@framework/Components'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { namedColors } from '../Color'


export class ColorTypeaheadLine extends React.Component<{ ctx: TypeContext<string | null | undefined>; onChange?: () => void }>{

    handleOnChange = (newColor: string | undefined | null) => {
        this.props.ctx.value = newColor;
        if (this.props.onChange)
            this.props.onChange();
        this.forceUpdate();

    }

    render() {
        var ctx = this.props.ctx;

        return (
            <FormGroup ctx={ctx} labelText={ctx.niceName()} >
                <ColorTypeahead color={ctx.value}
                    formControlClass={ctx.formControlClass}
                    onChange={this.handleOnChange} />
            </FormGroup>
        );
    }
}

interface ColorTypeaheadProps {
    color: string | null | undefined;
    onChange: (newColor: string | null | undefined) => void;
    formControlClass: string | undefined;
}

export class ColorTypeahead extends React.Component<ColorTypeaheadProps>{

    handleGetItems = (query: string) => {
        if (!query)
            return Promise.resolve([
                "black",
                "#00000"
            ]);

        const result = Dic.getKeys(namedColors)
            .filter(k => k.toLowerCase().contains(query.toLowerCase()))
            .orderBy(a => a.length)
            .filter((k, i) => i < 5);

        if (result.length == 0) {
            if (query.match(/^(#[0-9A-F]{3}|#[0-9A-F]{6}|#[0-9A-F]{8})$/i))
                result.push(query);
        }

        return Promise.resolve(result);
    }

    handleSelect = (item: unknown | string) => {
        this.props.onChange(item as string);
        this.forceUpdate();
        return item as string;
    }

    handleRenderItem = (item: unknown, query: string) => {

        return (
            <span>
                <FontAwesomeIcon icon="square" className="icon" color={item as string} />
                {Typeahead.highlightedText(item as string, query)}
            </span>
        );
    }

    render() {
        return (
            <div style={{ position: "relative" }}>
                <Typeahead
                    value={this.props.color || ""}
                    inputAttrs={{ className: classes(this.props.formControlClass, "sf-entity-autocomplete") }}
                    getItems={this.handleGetItems}
                    onSelect={this.handleSelect}
                    onChange={this.handleSelect}
                    renderItem={this.handleRenderItem}
                    minLength={0}
                    />
            </div>
        );
    }
}



