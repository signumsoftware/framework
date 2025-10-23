import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic, classes } from '@framework/Globals'
import { Navigator } from '@framework/Navigator'
import { Operations } from '@framework/Operations'
import { getToString, Lite, newMListElement } from '@framework/Signum.Entities'
import { CaseTagTypeEntity, CaseEntity, CaseTagsModel, CaseOperation, CaseMessage } from '../Signum.Workflow'
import Tag from './Tag'
import { WorkflowClient } from '../WorkflowClient'

import "./Tag.css"
import { LinkButton } from '@framework/Basics/LinkButton'

export interface InlineCaseTagsProps {
  case: Lite<CaseEntity>;
  defaultTags?: CaseTagTypeEntity[];
  avoidHideIcon?: boolean;
  wrap?: boolean;
}

export default function InlineCaseTags(p: InlineCaseTagsProps): React.JSX.Element {

  const [tags, setTags] = React.useState<CaseTagTypeEntity[]>(() => p.defaultTags ?? []);

  React.useEffect(() => {
    if (p.defaultTags) {
      setTags(p.defaultTags);
    } else {
      WorkflowClient.API.fetchCaseTags(p.case)
        .then(tags => setTags(tags));
    }

  }, [p.case, ...p.defaultTags ?? []]);

  function handleTagsClick(e: React.MouseEvent<any>) {

    var model = CaseTagsModel.New({
      caseTags: tags.map(m => newMListElement(m)),
      oldCaseTags: tags.map(m => newMListElement(m)),
    });

    Navigator.view(model,
      { title: getToString(p.case) ?? "" })
      .then(cm => {
        if (!cm)
          return;

        Operations.API.executeLite(p.case, CaseOperation.SetTags, cm)
          .then(() => WorkflowClient.API.fetchCaseTags(p.case))
          .then(tags => setTags(tags))
      });

  }

  return (
    <LinkButton onClick={handleTagsClick} className={classes("case-icon", tags.length == 0 && !p.avoidHideIcon && "case-icon-ghost")} style={{ flexWrap: p.wrap ? "wrap" : undefined }}>
      {
        tags.length == 0 ? <FontAwesomeIcon icon={"tags"} title={CaseMessage.SetTags.niceToString()}/> :
          tags.map((t, i) => <Tag key={i} tag={t} />)
      }
    </LinkButton>
  );
}
