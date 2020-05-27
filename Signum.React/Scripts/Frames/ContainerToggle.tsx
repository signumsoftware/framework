import * as React from 'react'
import { classes } from '../Globals'
import * as AppContext from '../AppContext'
import * as Navigator from '../Navigator'
import { ErrorBoundary } from "../Components/ErrorBoundary";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useForceUpdate } from '../Hooks';

export default function ContainerToggleComponent(p: { children: React.ReactNode }) {

  const [fluid, setFluid] = React.useState(false);

  React.useEffect(() => {
    AppContext.Expander.onGetExpanded = () => fluid;
    AppContext.Expander.onSetExpanded = (isExpanded: boolean) => setFluid(isExpanded);
  });

  const forceUpdate = useForceUpdate();

  React.useEffect(() => {
    return AppContext.history.listen(forceUpdate);
  }, [])

  function handleExpandToggle(e: React.MouseEvent<any>){
    e.preventDefault();
    setFluid(!fluid);
  }

  return (
    <div className={classes(fluid ? "container-fluid" : "container", "mt-3", "sf-page-container")}>
      <a className="expand-window d-none d-md-block" onClick={handleExpandToggle} href="#" >
        <FontAwesomeIcon icon={fluid ? "compress" : "expand"} />
      </a>
      <ErrorBoundary refreshKey={AppContext.history.location.pathname + AppContext.history.location.search}>
        {p.children}
      </ErrorBoundary>
    </div>
  );
}

