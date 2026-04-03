import * as React from 'react'
import { classes } from '../Globals'
import * as AppContext from '../AppContext'
import { Navigator } from '../Navigator'
import { ErrorBoundary } from "../Components/ErrorBoundary";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useForceUpdate, useUpdatedRef } from '../Hooks';
import { useLocation } from 'react-router';
import { ContainerToggleMessage } from '../Signum.Entities';
import { LinkButton } from '../Basics/LinkButton';

export default function ContainerToggleComponent(p: { children: React.ReactNode }): React.ReactElement {

  const [fluid, setFluid] = React.useState(false);
  const fluidRef = useUpdatedRef(fluid);

  React.useEffect(() => {
    AppContext.Expander.onGetExpanded = () => fluidRef.current;
    AppContext.Expander.onSetExpanded = (isExpanded: boolean) => setFluid(isExpanded);
  }, []);

  const forceUpdate = useForceUpdate();

  const location = useLocation();

  function handleExpandToggle(e: React.MouseEvent<any>){
    e.preventDefault();
    setFluid(!fluid);
    forceUpdate();
  }

  return (
    <div className={classes(fluid ? "container-fluid" : "container", "mt-3", "sf-page-container")}>
      <LinkButton className="expand-window d-none d-md-block" tabIndex={0} onClick={handleExpandToggle} title={(fluid ? ContainerToggleMessage.Compress : ContainerToggleMessage.Expand).niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon={fluid ? "compress" : "expand"} />
      </LinkButton>
      <ErrorBoundary deps={[location.pathname + location.search]}>
        {React.Children.map(p.children, c => c && React.cloneElement(c as React.ReactElement))}
      </ErrorBoundary>
    </div>
  );
}

