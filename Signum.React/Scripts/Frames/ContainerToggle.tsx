import * as React from 'react'
import { classes } from '../Globals'
import * as AppContext from '../AppContext'
import * as Navigator from '../Navigator'
import { ErrorBoundary } from "../Components/ErrorBoundary";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useForceUpdate, useUpdatedRef } from '../Hooks';

export default function ContainerToggleComponent(p: { children: React.ReactNode }) {

  const [fluid, setFluid] = React.useState(false);
  const fluidRef = useUpdatedRef(fluid);

  React.useEffect(() => {
    AppContext.Expander.onGetExpanded = () => fluidRef.current;
    AppContext.Expander.onSetExpanded = (isExpanded: boolean) => setFluid(isExpanded);
  }, []);

  const forceUpdate = useForceUpdate();

  React.useEffect(() => {
    return AppContext.history.listen(forceUpdate);
  }, [])

  function handleExpandToggle(e: React.MouseEvent<any>){
    e.preventDefault();
    setFluid(!fluid);
    forceUpdate();
  }

  return (
    <div className={classes(fluid ? "container-fluid" : "container", "mt-3", "sf-page-container")}>
      <a className="expand-window d-none d-md-block" onClick={handleExpandToggle} href="#" >
        <FontAwesomeIcon icon={fluid ? "compress" : "expand"} />
      </a>
      <ErrorBoundary deps={[AppContext.history.location.pathname + AppContext.history.location.search]}>
        {React.Children.map(p.children, c => c && React.cloneElement(c as React.ReactElement))}
      </ErrorBoundary>
    </div>
  );
}

