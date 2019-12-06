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

export default function InlineCaseTags(p: InlineCaseTagsProps) {

  const [tags, setTags] = React.useState<CaseTagTypeEntity[]>(() => p.defaultTags ?? []);

  React.useEffect(() => {
    if (p.defaultTags) {
      setTags(p.defaultTags);
    } else {
      WorkflowClient.API.fetchCaseTags(p.case)
        .then(tags => setTags(tags))
        .done();
    }

  }, [p.case, ...p.defaultTags ?? []]);

  function handleTagsClick(e: React.MouseEvent<any>) {
    e.preventDefault();

    var model = CaseTagsModel.New({
      caseTags: tags.map(m => newMListElement(m)),
      oldCaseTags: tags.map(m => newMListElement(m)),
    });

    Navigator.view(model,
      { title: p.case.toStr ?? "" })
      .then(cm => {
        if (!cm)
          return;

        Operations.API.executeLite(p.case, CaseOperation.SetTags, cm)
          .then(() => WorkflowClient.API.fetchCaseTags(p.case))
          .then(tags => setTags(tags))
          .done()
      }).done();

  }

  return (
    <a href="#" onClick={handleTagsClick} className={classes("case-icon", tags.length == 0 && "case-icon-ghost")}>
      {
        tags.length == 0 ? <FontAwesomeIcon icon={"tags"} /> :
          tags.map((t, i) => <Tag key={i} tag={t} />)
      }
    </a>
  );
}
