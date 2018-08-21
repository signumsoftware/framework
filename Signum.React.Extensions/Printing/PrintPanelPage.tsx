import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import * as numbro from 'numbro'
import * as Finder from '@framework/Finder'
import EntityLink from '@framework/SearchControl/EntityLink'
import { ValueSearchControl, SearchControl, ValueSearchControlLine } from '@framework/Search'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { StyleContext } from '@framework/Lines'
import { getQueryNiceName, PropertyRoute, getTypeInfos } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { API, PrintStat } from './PrintClient'
import * as Operations from '@framework/Operations'
import * as Navigator from '@framework/Navigator'
import { PrintPackageEntity, PrintLineState, PrintLineEntity, PrintPackageProcess, } from './Signum.Entities.Printing'
import { FileTypeSymbol } from '../Files/Signum.Entities.Files'
import { ProcessEntity } from '../Processes/Signum.Entities.Processes'
import { Type } from '@framework/Reflection'

export interface PrintPanelPageState {
    stats: PrintStat[];
}

export default class PrintPanelPage extends React.Component<{}, PrintPanelPageState> {

    constructor(props: any) {
        super(props);

        this.state = { stats: [] };
    }

    componentWillMount() {
        API.getStats().then(stats => {
            this.setState({ stats });
        }).done();
    }

    render() {
        var ctx = new StyleContext(undefined, undefined);
        return (

            <div>
                <h2>PrintPanel</h2>

                <div>
                    <fieldset>
                        <legend>Ready To Print</legend>
                        {this.state.stats.map((s, i) =>
                            <ValueSearchControlLine ctx={ctx} key={i} initialValue={s.count}
                                labelText={s.fileType.toStr.after(".")}
                                extraButtons={vsc => this.renderStateButton(vsc, s.fileType)}
                                findOptions={{
                                    queryName: PrintLineEntity,
                                    filterOptions: [
                                        { token: "State", value: "ReadyToPrint" as PrintLineState },
                                        { token: "File.FileType", value: s.fileType },
                                    ]
                                }} />)
                        }
                    </fieldset>
                </div>

                <h3>{ProcessEntity.nicePluralName()}</h3>
                <SearchControl findOptions={{
                        queryName: ProcessEntity,
                        filterOptions: [{ token: "Entity.Data.(PrintPackage)", operation: "DistinctTo", value: undefined }],
                        pagination: { elementsPerPage: 10, mode: "Paginate", currentPage: 1 },
                    }}
                />
            </div>
        );
    }

    renderStateButton(vsc: ValueSearchControl, fileType: FileTypeSymbol) {
        if (vsc.state.value == undefined || vsc.state.value == 0)
            return undefined;

        return (
            <a href="#" className="sf-line-button" title="Print" onClick={e => this.handlePrintClick(e, fileType, vsc)}>
                <FontAwesomeIcon icon="print" />
            </a>
        );
    }

    handlePrintClick = (e: React.MouseEvent<any>, fileType: FileTypeSymbol, vsc: ValueSearchControl) => {
        e.preventDefault();
        API.createPrintProcess(fileType)
            .then(p => p && Navigator.navigate(p))
            .then(p => vsc.refreshValue())
            .done();
    }
}
