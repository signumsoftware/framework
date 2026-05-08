import * as React from 'react'
import { openModal, IModalProps } from '../Modals'
import { Lite, Entity, getToString, toLite, JavascriptMessage, CascadeDeleteMessage } from '../Signum.Entities'
import { ajaxPost, ServiceError } from '../Services'
import { Navigator } from '../Navigator'
import { Finder } from '../Finder'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Modal, Spinner } from 'react-bootstrap'
import { getTypeInfo, getQueryKey } from '../Reflection'
import { EntityOperations } from './EntityOperations'
import { Operations } from '../Operations'
import SearchValue from '../SearchControl/SearchValue'

export interface CascadeReferenceDto {
  typeName: string;
  propertyRoute: string;
  count: number;
}

interface DeleteErrorModalProps extends IModalProps<boolean> {
  lite: Lite<Entity>;
  executeDelete: () => Promise<void>;
  serviceError?: ServiceError;
}

export function DeleteErrorModal(p: DeleteErrorModalProps): React.ReactElement {
  const [show, setShow] = React.useState(true);
  const [references, setReferences] = React.useState<CascadeReferenceDto[] | undefined>(undefined);
  const [loading, setLoading] = React.useState(true);
  const [deleting, setDeleting] = React.useState(false);
  const [showDetails, setShowDetails] = React.useState(false);
  const [version, setVersion] = React.useState(0);

  React.useEffect(() => { loadReferences(); }, []);

  async function loadReferences(): Promise<void> {
    setLoading(true);
    try {
      const refs = await API.getReferences(p.lite);
      setReferences(refs);
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete(): Promise<void> {
    setDeleting(true);
    try {
      await p.executeDelete();
      p.onExited!(true);
    } catch {
      await loadReferences();
    } finally {
      setDeleting(false);
    }
  }

  function handleClose(): void {
    setShow(false);
  }

  function handleOnExited(): void {
    p.onExited!(false);
  }

  function handleExplored(): void {
    loadReferences();
    setVersion(v => v + 1);
  }

  const hasReferences = references != null && references.length > 0;
  const entityTypeName = getTypeInfo(p.lite.EntityType).niceName;

  return (
    <Modal show={show} onExited={handleOnExited} onHide={handleClose} size="lg">
      <Modal.Header closeButton>
        <Modal.Title>
          <FontAwesomeIcon icon="triangle-exclamation" className="text-warning me-2" />
          {CascadeDeleteMessage.ThisEntityIsStillReferenced.niceToString()}
        </Modal.Title>
      </Modal.Header>

      <Modal.Body>
        {loading ? (
          <div className="d-flex align-items-center gap-2">
            <Spinner animation="border" size="sm" />
            <span>{JavascriptMessage.loading.niceToString()}</span>
          </div>
        ) : hasReferences ? (
          <>
            <p className="mb-3">
              {CascadeDeleteMessage.TheFollowingEntitiesStillReference0RemoveThemBeforeDeleting
                .niceToString(getToString(p.lite) || entityTypeName)}
            </p>
            <div className="list-group">
              {references!.map(dto => (
                <ReferenceRow
                  key={dto.typeName + "|" + dto.propertyRoute}
                  dto={dto}
                  targetLite={p.lite}
                  onExplored={handleExplored}
                  version={version}
                />
              ))}
            </div>
          </>
        ) : (
          <div className="d-flex align-items-center gap-2 text-success">
            <FontAwesomeIcon icon="circle-check" />
            <span>{CascadeDeleteMessage.NoReferencesFoundYouCanNowDeleteThisEntity.niceToString()}</span>
          </div>
        )}

        {p.serviceError && (
          <div className="mt-3">
            <button
              className="btn btn-link btn-sm p-0 text-muted"
              onClick={() => setShowDetails(v => !v)}
            >
              <FontAwesomeIcon icon={showDetails ? "chevron-up" : "chevron-down"} className="me-1" />
              {CascadeDeleteMessage.ErrorDetails.niceToString()}
            </button>
            {showDetails && (
              <div className="mt-2 p-2 bg-light border rounded small font-monospace">
                <div className="text-danger fw-bold mb-1">{p.serviceError.httpError.exceptionType}</div>
                <div className="mb-2">{p.serviceError.httpError.exceptionMessage}</div>
                {p.serviceError.httpError.stackTrace && (
                  <pre className="mb-0 text-muted" style={{ whiteSpace: 'pre-wrap', fontSize: '0.75rem' }}>
                    {p.serviceError.httpError.stackTrace}
                  </pre>
                )}
              </div>
            )}
          </div>
        )}
      </Modal.Body>

      <Modal.Footer>
        <button className="btn btn-secondary" onClick={loadReferences} disabled={loading || deleting}>
          <FontAwesomeIcon icon="arrows-rotate" className="me-1" />
          {CascadeDeleteMessage.Refresh.niceToString()}
        </button>
        <button
          className="btn btn-danger"
          onClick={handleDelete}
          disabled={hasReferences || loading || deleting}
        >
          {deleting
            ? <><Spinner animation="border" size="sm" className="me-1" />{JavascriptMessage.loading.niceToString()}</>
            : <><FontAwesomeIcon icon="trash" className="me-1" />{CascadeDeleteMessage.Delete.niceToString()}</>
          }
        </button>
        <button className="btn btn-light" onClick={handleClose} disabled={deleting}>
          {JavascriptMessage.cancel.niceToString()}
        </button>
      </Modal.Footer>
    </Modal>
  );
}

interface ReferenceRowProps {
  dto: CascadeReferenceDto;
  targetLite: Lite<Entity>;
  onExplored: () => void;
  version: number;
}

function ReferenceRow({ dto, targetLite, onExplored, version }: ReferenceRowProps): React.ReactElement {
  const [visibleCount, setVisibleCount] = React.useState<number | undefined>(undefined);
  const isFindable = Finder.isFindable(dto.typeName, false);
  const typeInfo = getTypeInfo(dto.typeName);
  const token = propertyRouteToQueryToken(dto.propertyRoute);

  const findOptions = {
    queryName: dto.typeName,
    filterOptions: [{
      token: token,
      operation: "EqualTo" as const,
      value: targetLite,
    }],
  };

  const hidden = visibleCount != null ? Math.max(0, dto.count - visibleCount) : 0;

  return (
    <div className="list-group-item d-flex align-items-center gap-2 py-2">
      <div className="flex-grow-1">
        <span className="text-muted small me-1">{CascadeDeleteMessage.ReferencedVia.niceToString()}</span>
        <code className="small">{dto.propertyRoute}</code>
      </div>
      <div className="d-flex align-items-center gap-1">
        {isFindable ? (
          <>
            <SearchValue
              findOptions={findOptions}
              isLink={true}
              isBadge="MoreThanZero"
              onExplored={onExplored}
              onValueChange={v => setVisibleCount(v as number)}
              deps={[version]}
            />
            {hidden > 0 && (
              <span className="text-muted small">
                ({CascadeDeleteMessage._0MoreNotVisibleForYou.niceToString(hidden)})
              </span>
            )}
          </>
        ) : (
          <span className="badge bg-secondary">{dto.count} {typeInfo?.niceName ?? dto.typeName}</span>
        )}
      </div>
    </div>
  );
}

function propertyRouteToQueryToken(route: string): string {
  const rootMatch = route.match(/^\([^)]+\)/);
  if (!rootMatch) return 'Entity';

  const rest = route.slice(rootMatch[0].length);
  if (!rest) return 'Entity';

  let result = 'Entity';
  let i = 0;

  while (i < rest.length) {
    const ch = rest[i];
    if (ch === '/') {
      result += '.Any';
      i++;
    } else if (ch === '.') {
      i++;
    } else if (ch === '[') {
      const end = rest.indexOf(']', i);
      if (end === -1) break;
      const mixinName = rest.slice(i + 1, end);
      result += '.(' + mixinName + ')';
      i = end + 1;
    } else {
      const nextSep = rest.slice(i).search(/[./[]/);
      const fieldName = nextSep === -1 ? rest.slice(i) : rest.slice(i, i + nextSep);
      result += '.' + fieldName.charAt(0).toUpperCase() + fieldName.slice(1);
      i += fieldName.length;
    }
  }

  return result;
}

export namespace DeleteErrorModal {
  export function show(lite: Lite<Entity>, executeDelete: () => Promise<void>, serviceError?: ServiceError): Promise<boolean> {
    return openModal<boolean>(
      <DeleteErrorModal lite={lite} executeDelete={executeDelete} serviceError={serviceError} />
    );
  }

  export function register(): void {
    EntityOperations.onDeleteError = async (eoc, e) => {
      if (!e.httpError.exceptionType?.endsWith("ForeignKeyException"))
        return false;

      const lite = toLite(eoc.entity);
      const doDelete = (): Promise<void> => Operations.API.deleteLite(lite, eoc.operationInfo.key);
      const deleted = await DeleteErrorModal.show(lite, doDelete, e);
      if (deleted)
        (eoc.onDeleteSuccess ?? eoc.onDeleteSuccess_Default)?.();

      return true;
    };
  }
}

export namespace API {
  export function getReferences(lite: Lite<Entity>): Promise<CascadeReferenceDto[]> {
    return ajaxPost<CascadeReferenceDto[]>({ url: '/api/cascadeDelete/references' }, lite);
  }
}
