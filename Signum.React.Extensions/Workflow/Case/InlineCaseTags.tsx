import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, classes } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import * as Operations from '@framework/Operations'
import { Lite, newMListElement } from '@framework/Signum.Entities'
import { CaseTagTypeEntity, CaseEntity, CaseTagsModel, CaseOperation } from '../Signum.Entities.Workflow'
import Tag from './Tag'
import * as WorkflowClient from '../WorkflowClient'

import "./Tag.css"

export interface InlineCaseTagsProps {
  case: Lite<CaseEntity>;
  defaultTags?: CaseTagTypeEntity[];
}

export interface InlineCaseTagsState {
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
          this.state.tags.length == 0 ? <FontAwesomeIcon icon={"tags"} /> :
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
