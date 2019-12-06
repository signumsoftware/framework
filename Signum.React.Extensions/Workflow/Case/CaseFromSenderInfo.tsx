import * as React from 'react'
import * as moment from 'moment'
import * as Navigator from '@framework/Navigator'
import { JavascriptMessage, is } from '@framework/Signum.Entities'
import { CaseActivityEntity, CaseActivityMessage } from '../Signum.Entities.Workflow'
import { useFetchInState } from '../../../../Framework/Signum.React/Scripts/Hooks'

interface CaseFromSenderInfoProps {
  current: CaseActivityEntity;
}

interface CaseFromSenderInfoState {
  prev?: CaseActivityEntity;
}

export default function CaseFromSenderInfo(p: CaseFromSenderInfoProps) {

  const prev = useFetchInState(p.current.previous);

  const c = p.current;

  return (
    <div>
      {
        c.previous == null ? null :
          <div className="alert alert-info case-alert">
            {prev == null ? JavascriptMessage.loading.niceToString() :
              CaseActivityMessage.From0On1.niceToString().formatHtml(
                <strong>{prev.doneBy && prev.doneBy.toStr}</strong>,
                prev.doneDate && <strong>{moment(prev.doneDate).format("L LT")} ({moment(prev.doneDate).fromNow()})</strong>)
            }
          </div>
      }
      {
        prev?.note && <div className="alert alert-warning case-alert">
          <strong>{CaseActivityEntity.nicePropertyName(a => a.note)}:</strong>
          {prev.note.contains("\n") ? "\n" : null}
          {prev.note}
        </div>
      }
    </div>
  );
}
