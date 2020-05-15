import * as React from 'react'
import { classes } from '../Globals'
import * as AppContext from '../AppContext'
import * as Navigator from '../Navigator'
import { ErrorBoundary } from "../Components/ErrorBoundary";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export default function ContainerToggleComponent(p: { children: React.ReactNode }) {

  const [fluid, setFluid] = React.useState(false);

  React.useEffect(() => {
    AppContext.Expander.onGetExpanded = () => fluid;
    AppContext.Expander.onSetExpanded = (isExpanded: boolean) => setFluid(isExpanded);
  });

  function handleExpandToggle(e: React.MouseEvent<any>){
    e.preventDefault();
    setFluid(!fluid);
  }

  return (
    <div className={classes(fluid ? "container-fluid" : "container", "mt-3", "sf-page-container")}>
      <a className="expand-window d-none d-md-block" onClick={handleExpandToggle} href="#" >
        <FontAwesomeIcon icon={fluid ? "compress" : "expand"} />
      </a>
      <ErrorBoundary>
        {p.children}
      </ErrorBoundary>
    </div>
  );
}

