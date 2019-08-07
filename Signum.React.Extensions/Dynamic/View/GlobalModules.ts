import * as React from 'react'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Entities from '@framework/Signum.Entities'
import * as Operations from '@framework/Operations'
import * as Constructor from '@framework/Constructor'
import * as Globals from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Reflection from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import * as Components from '@framework/Components'
import * as AuthClient from '../../Authorization/AuthClient'
import * as Services from '@framework/Services'
import * as TreeClient from '../../Tree/TreeClient'
import * as AutoCompleteConfig from '@framework/Lines/AutoCompleteConfig'
import * as Hooks from '@framework/Hooks'
import * as SelectorModal from '@framework/SelectorModal'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export const globalModules: any = {
  numbro,
  moment,
  React,
  Components,
  Globals,
  Navigator,
  Finder,
  Reflection,
  Entities,
  AuthClient,
  Operations,
  Constructor,
  Services,
  TreeClient,
  AutoCompleteConfig,
  Hooks,
  SelectorModal,
  FontAwesomeIcon,
};
