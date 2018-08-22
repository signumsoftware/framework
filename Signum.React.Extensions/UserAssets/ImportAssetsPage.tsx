import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import * as numbro from 'numbro'
import * as moment from 'moment'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { TypeContext } from '@framework/TypeContext'
import { ValueSearchControl, SearchControl } from '@framework/Search'
import EntityLink from '@framework/SearchControl/EntityLink'
import { QueryDescription, SubTokensOptions } from '@framework/FindOptions'
import { getQueryNiceName, PropertyRoute, getTypeInfo } from '@framework/Reflection'
import { ModifiableEntity, EntityControlMessage, Entity, parseLite, getToString, JavascriptMessage } from '@framework/Signum.Entities'
import { API } from './UserAssetClient'
import { UserAssetMessage, UserAssetPreviewModel, UserAssetPreviewLineEmbedded, IUserAssetEntity, EntityAction } from './Signum.Entities.UserAssets'

interface ImportAssetsPageProps extends RouteComponentProps<{}> {

}

interface ImportAssetsPageState {
    file?: API.FileUpload;
    model?: UserAssetPreviewModel;
    success?: boolean;
    fileVer: number;
}


export default class ImportAssetsPage extends React.Component<ImportAssetsPageProps, ImportAssetsPageState> {

    constructor(props: ImportAssetsPageProps) {
        super(props);
        this.state = { fileVer: 0 };
    
        Navigator.setTitle("Import Assets Page");
    }

    componentWillUnmount() {
        Navigator.setTitle();
    }

    render() {
        return (
            <div>
                <h2>{UserAssetMessage.ImportUserAssets.niceToString() }</h2>
                <br />
                { this.state.success && this.renderSuccess() }
                  {  this.state.model ? this.renderModel() :
                        this.renderFileInput()
                }
            </div>
        );
    }

    handleInputChange = (e: React.FormEvent<any>) => {
        let f = (e.currentTarget as HTMLInputElement).files![0];
        let fileReader = new FileReader();
        fileReader.onerror = e => { setTimeout(() => { throw (e as any).error; }, 0); };
        fileReader.onload = e => {
            let content = ((e.target as any).result as string).after("base64,");
            let fileName = f.name;

            this.setState({
                file: { content, fileName },
                fileVer: this.state.fileVer + 1
            });

            API.importPreview(this.state.file!).then(model => this.setState({ model, success: false })).done();
        };
        fileReader.readAsDataURL(f);
    }

    renderFileInput() {
        return (
            <div>
                <div className="btn-toolbar">
                    <input key={this.state.fileVer} type="file" className="form-control" onChange={this.handleInputChange} style={{ display: "inline", float: "left", width: "inherit" }} />
                </div>
                <small>{UserAssetMessage.SelectTheXmlFileWithTheUserAssetsThatYouWantToImport.niceToString() }</small>
            </div>
        );
    }

    handleImport = () => {
        API.importAssets({
            file: this.state.file!,
            model: this.state.model!
        })
        .then(model => this.setState({ success: true, model: undefined, file: undefined }))
        .done();
    }

    renderModel() {
        const tc = TypeContext.root(this.state.model!, undefined);

        return (
            <div>
                <table className="table">
                    <thead>
                        <tr>
                            <th> { UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.action) } </th>
                            <th> { UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.overrideEntity) } </th>
                            <th> { UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.type) } </th>
                            <th> { UserAssetPreviewModel.nicePropertyName(a => a.lines![0].element.text) } </th>
                        </tr>
                    </thead>

                    <tbody>
                        {
                            tc.value.lines!.map(mle =>
                                <tr key={mle.element.type!.cleanName}>
                                    <td> {EntityAction.niceToString(mle.element.action!)} </td>
                                    <td>
                                        { mle.element.action == "Different" &&
                                            <input type="checkbox" checked={mle.element.overrideEntity} onChange={e => {
                                                mle.element.overrideEntity = (e.currentTarget as HTMLInputElement).checked;
                                                mle.element.modified = true;
                                                this.forceUpdate();
                                            } }></input>
                                        }
                                    </td>
                                    <td> { getTypeInfo(mle.element.type!.cleanName).niceName } </td>
                                    <td> {mle.element.text }</td>
                                </tr>
                            )
                        }
                    </tbody>
                </table>
                <button onClick={this.handleImport} className="btn btn-info"><FontAwesomeIcon icon="cloud-upload"/> Import</button>
            </div>
        );
    }

    renderSuccess() {
        return (
            <div className="alert alert-success" role="alert">{UserAssetMessage.SucessfullyImported.niceToString() }</div>
        );
    }

}



