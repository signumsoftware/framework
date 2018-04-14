import * as React from 'react'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import { getMixin, Lite, newMListElement, is } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { ColorTypeaheadLine } from '../../Basics/Templates/ColorTypeahead'
import { CaseTagTypeEntity, CaseEntity, CaseTagsModel, CaseOperation } from '../Signum.Entities.Workflow'
import {
    ValueLine, EntityLine, RenderEntity, EntityCombo, EntityList, EntityDetail, EntityStrip,
    EntityRepeater, EntityCheckboxList, EntityTabRepeater, TypeContext, EntityTable
} from '../../../../Framework/Signum.React/Scripts/Lines'
import { SearchControl, ValueSearchControl } from '../../../../Framework/Signum.React/Scripts/Search'
import { Color } from '../../Basics/Color'
import Tag from './Tag'
import * as WorkflowClient from '../WorkflowClient'

import "./Tag.css"

export interface InlineCaseTagsProps{
    case: Lite<CaseEntity>;
    defaultTags?: CaseTagTypeEntity[];
}

export interface InlineCaseTagsState{
    tags: CaseTagTypeEntity[];
}


export default class InlineCaseTags extends React.Component<InlineCaseTagsProps, InlineCaseTagsState> {

    constructor(props: InlineCaseTagsProps) {
        super(props);

        this.state = { tags: props.defaultTags || [] };
    }

    componentWillMount() {
        this.reload(this.props);
    }

    componentWillReceiveProps(newProps: InlineCaseTagsProps) {
        if (!Dic.equals(this.props, newProps, true))
            this.reload(newProps);
    }

    reload(props: InlineCaseTagsProps) {

        if (props.defaultTags) {
            this.setState({ tags: props.defaultTags });
        } else {
            WorkflowClient.API.fetchCaseTags(props.case)
                .then(tags => this.setState({ tags }))
                .done();
        }
    }

    render() {
        

        return (
            <a href="#" onClick={this.handleTagsClick} className={classes("case-icon", this.state.tags.length == 0 && "case-icon-ghost")}>
                {
                    this.state.tags.length == 0 ? <span className={"fa fa-tags"} /> :
                        this.state.tags.map((t, i) => <Tag key={i} tag={t} />)
                }
            </a>
        );
    }

    handleTagsClick = (e: React.MouseEvent<any>) => {
        e.preventDefault();

        var model = CaseTagsModel.New({
            caseTags: this.state.tags.map(m => newMListElement(m)),
            oldCaseTags: this.state.tags.map(m => newMListElement(m)),
        });

        Navigator.view(model,
            { title: this.props.case.toStr || "" })
            .then(cm => {
                if (!cm)
                    return;

                Operations.API.executeLite(this.props.case, CaseOperation.SetTags, cm)
                    .then(() => WorkflowClient.API.fetchCaseTags(this.props.case))
                    .then(tags => this.setState({ tags }))
                    .done()
            }).done();

    }

}