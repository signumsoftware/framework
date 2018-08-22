import * as React from 'react'
import { classes } from '@framework/Globals'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater } from '@framework/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll } from '@framework/FindOptions'
import { SearchControl } from '@framework/Search'
import { getToString, getMixin } from '@framework/Signum.Entities'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import { WordTemplateEntity } from '../Signum.Entities.Word'
import { TemplateTokenMessage, TemplateApplicableEval } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'
import TemplateApplicable from '../../Templating/Templates/TemplateApplicable'
import ValueLineModal from '@framework/ValueLineModal'

export default class WordTemplate extends React.Component<{ ctx: TypeContext<WordTemplateEntity> }> {

    handleOnInsert = (newCode: string) => {
        ValueLineModal.show({
            type: { name: "string" },
            initialValue: newCode,
            title: "Template",
            message: "Copy to clipboard: Ctrl+C, ESC",
            initiallyFocused: true,
        }).done();
    }

    render() {

        const ctx = this.props.ctx;
        const sc = ctx.subCtx({ formGroupStyle: "Basic" });

        return (
            <div>
                <ValueLine ctx={ctx.subCtx(f => f.name)} />
                <EntityLine ctx={ctx.subCtx(f => f.query)} />
                <EntityCombo ctx={ctx.subCtx(f => f.systemWordTemplate)} />
                <EntityCombo ctx={ctx.subCtx(f => f.culture)} />
                <EntityCombo ctx={ctx.subCtx(f => f.wordTransformer)} />
                <EntityCombo ctx={ctx.subCtx(f => f.wordConverter)} />
                <ValueLine ctx={ctx.subCtx(f => f.fileName)} />
                <ValueLine ctx={ctx.subCtx(f => f.disableAuthorization)} />

                {sc.value.query &&
                    <div>
                        <div>
                            <div className="card form-xs" style={{ marginTop: "10px", marginBottom: "10px" }}>
                                <div className="card-header" style={{ padding: "5px" }}>
                                    <TemplateControls queryKey={sc.value.query.key} forHtml={false} onInsert={this.handleOnInsert} />
                                </div>
                            </div>
                            <FileLine ctx={ctx.subCtx(e => e.template)} />
                            <EntityDetail ctx={ctx.subCtx(e => e.applicable)}
                                getComponent={(ctx2: TypeContext<TemplateApplicableEval>) => <TemplateApplicable ctx={ctx2} query={sc.value.query!} />} />
                        </div>
                    </div>
                }
            </div>
        );
    }
}
