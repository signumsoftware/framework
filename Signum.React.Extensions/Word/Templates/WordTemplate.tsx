import * as React from 'react'
import { Tab, Tabs }from 'react-bootstrap'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityDetail, EntityList, EntityRepeater, EntityTabRepeater} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SubTokensOptions, QueryToken, QueryTokenType, hasAnyOrAll }  from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { SearchControl }  from '../../../../Framework/Signum.React/Scripts/Search'
import { getToString, getMixin }  from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { WordTemplateEntity } from '../Signum.Entities.Word'
import { TemplateTokenMessage } from '../../Templating/Signum.Entities.Templating'
import FileLine from '../../Files/FileLine'
import QueryTokenEntityBuilder from '../../UserAssets/Templates/QueryTokenEntityBuilder'
import TemplateControls from '../../Templating/TemplateControls'


export default class WordTemplate extends React.Component<{ ctx: TypeContext<WordTemplateEntity> }, void> {

    handleOnInsert = (newCode: string) => {
        window.prompt("Copy to clipboard: Ctrl+C, Enter", newCode);
    }

    render() {

        const e = this.props.ctx;

        const ec = e.subCtx({ labelColumns: { sm: 4 } });
        const sc = ec.subCtx({ formGroupStyle: "Basic" });


        return (
            <div>
                <div className="row">
                    <div className="col-sm-8">
                        <ValueLine ctx={ec.subCtx(f => f.name) }  />
                        <EntityLine ctx={ec.subCtx(f => f.query) }  />
                        <EntityCombo ctx={ec.subCtx(f => f.systemWordTemplate) }  />
                        <EntityCombo ctx={ec.subCtx(f => f.culture) }  />
                        <EntityCombo ctx={ec.subCtx(f => f.wordTransformer) }  />
                        <EntityCombo ctx={ec.subCtx(f => f.wordConverter) }  />
                        <ValueLine ctx={ec.subCtx(f => f.fileName) }  />
                        <ValueLine ctx={ec.subCtx(f => f.disableAuthorization) }  />
                    </div>
                    {!ec.value.isNew &&
                        <div className="col-sm-4 form-vertical">
                            <fieldset style={ { marginTop: "-25px" }}>
                                <legend>Active</legend>
                                <ValueLine ctx={sc.subCtx(e => e.active) } inlineCheckbox={true} />
                                <ValueLine ctx={sc.subCtx(e => e.startDate) }  />
                                <ValueLine ctx={sc.subCtx(e => e.endDate) }  />
                            </fieldset>
                        </div>
                    }
                </div>

                { sc.value.query &&
                    <div className="form-vertical">
                        <div className="panel panel-default form-xs" style={{ marginTop: "10px", marginBottom: "10px" }}>
                            <div className="panel-heading" style={{ padding: "5px" }}>
                                <TemplateControls queryKey={sc.value.query.key} forHtml={false} onInsert={this.handleOnInsert}/>
                            </div>
                        </div>
                        <FileLine ctx={sc.subCtx(e => e.template) } />
                    </div>
                }
            </div>
        );
    }
}
