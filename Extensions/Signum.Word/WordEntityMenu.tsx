import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Lite, toLite, Entity, EntityPack, getToString } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import { WordTemplateEntity, WordTemplateMessage } from './Signum.Word'
import { WordClient } from './WordClient'
import { saveFile } from "@framework/Services";
import { DropdownButton, Dropdown } from 'react-bootstrap';

export interface WordEntityMenuProps {
  entityPack: EntityPack<Entity>;
}

export default function WordEntityMenu(p : WordEntityMenuProps): React.JSX.Element {
  function handleOnClick(wt: Lite<WordTemplateEntity>) {
    Navigator.API.fetch(wt)
      .then<string | undefined>(wordTemplate => wordTemplate.model ? WordClient.API.getConstructorType(wordTemplate.model) : undefined)
      .then(ct => {

        if (!ct || ct == p.entityPack.entity.Type)
          return WordClient.API.createAndDownloadReport({ template: wt, lite: toLite(p.entityPack.entity) });

        var s = WordClient.settings[ct];
        if (!s)
          throw new Error("No 'WordModelSettings' defined for '" + ct + "'");

        if (!s.createFromEntities)
          throw new Error("No 'createFromEntities' defined in the WordModelSettings of '" + ct + "'");

        return s.createFromEntities(wt, [toLite(p.entityPack.entity)])
          .then<Response | undefined>(m => m && WordClient.API.createAndDownloadReport({ template: wt, entity: m }));
      })
      .then(response => response && saveFile(response));
  }

  const label = <span><FontAwesomeIcon aria-hidden={true} icon={"file-word"} />&nbsp;{WordTemplateMessage.WordReport.niceToString()}</span>;

  return (
    <DropdownButton id="wordMenu" className="sf-word-dropdown" variant="outline-info" title={label}> 
      {
          p.entityPack.wordTemplates == "error" ? <Dropdown.Item className="text-danger">Error</Dropdown.Item> : 
          p.entityPack.wordTemplates!.map((wt, i) =>
            <Dropdown.Item key={i}
              onClick={() => handleOnClick(wt)}>
              {getToString(wt)}
            </Dropdown.Item>)
        }
    </DropdownButton>
  )
}



