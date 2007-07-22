Imports System.Data
Imports Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.GestorInformes.ContenedorPlantilla.DN


Public Class AdaptadorInformesQueryBuildingLN
    Private Shared mRutaConfiguracion As String
    Private Shared mRutaPlantillas As String

    Public Sub New()
        If String.IsNullOrEmpty(mrutaconfiguracion) Then
            Dim ht As Dictionary(Of String, String) = Framework.Configuracion.LectorConfiguracionXML.LeerConfiguracion("AdaptadorInformesQueryBuildingLN.xml")
            If Not ht Is Nothing Then
                mRutaConfiguracion = ht("DirectorioTemporalInformes")
                mRutaPlantillas = ht("DirectorioPlantillas")
            End If
        End If
    End Sub

    Public Shared Function ObtenerRutaInformes() As String
        Return mRutaPlantillas
    End Function

    Public Shared Function ObtenerRutaTemporal() As String
        Return mRutaConfiguracion
    End Function

    Private Function GenerarDocumentoTemporal(ByVal pContenedorPlantilla As ContenedorPlantillaDN) As System.IO.FileInfo
        'generamos un nombre del archivo temporal
        Dim extension As String = pContenedorPlantilla.HuellaFichero.ExtensionFichero
        Dim nombreficherotemporal As String
        Dim rnd As New Random(CInt(Now.Ticks.ToString.Substring(0, 3)))
        nombreficherotemporal = System.IO.Path.Combine(mRutaConfiguracion, System.IO.Path.GetRandomFileName() & extension) ' & rnd.Next(99999999).ToString() & extension
        While System.IO.File.Exists(nombreficherotemporal)
            nombreficherotemporal = mRutaConfiguracion & "\" & rnd.Next(0).ToString() & extension
        End While

        'realizamos la copia al fichero temporal
        System.IO.File.Copy(pContenedorPlantilla.HuellaFichero.RutaFichero, nombreficherotemporal, True)

        Dim fi As New System.IO.FileInfo(nombreficherotemporal)
        Return fi
    End Function

    Private Sub GenerarDatasetYListaTablas(ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN, ByRef ds As DataSet, ByRef listaTablasPrincipales As List(Of String))
        ds = GenerarDataSet(AdaptadorIQB)

        listaTablasPrincipales = New List(Of String)()
        For Each tabla As ITabla In AdaptadorIQB.TablasPrincipales
            listaTablasPrincipales.Add(tabla.NombreTabla)
        Next
    End Sub

    Public Function GenerarEsquemaXML(ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As Xml.XmlDocument
        Dim ds As DataSet = Nothing
        Dim listaTablasPrincipales As List(Of String) = Nothing

        GenerarDatasetYListaTablas(AdaptadorIQB, ds, listaTablasPrincipales)

        Return GestorInformes.AdaptadorDataSourceOXML.GenerarEsquemaXML(ds, listaTablasPrincipales)
    End Function

    Public Function GenerarEsquemaXMLEnPlantilla(ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As System.IO.FileInfo
        Dim doc As Xml.XmlDocument = GenerarEsquemaXML(AdaptadorIQB)

        'creamos un archivo temporal
        Dim fi As System.IO.FileInfo = GenerarDocumentoTemporal(AdaptadorIQB.Plantilla)

        Dim gestor As New GestorWordOpenXML()
        gestor.ModificarCustomPart(fi.FullName, doc)

        Return fi
    End Function

    Public Function GenerarDataSet(ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As DataSet
        Dim ds As New DataSet

        For Each tabla As TablaPrincipalAIQB In AdaptadorIQB.TablasPrincipales
            Dim dstabla As DataSet = GenerarDataSetDesdeTabla(tabla)
            For Each t As DataTable In dstabla.Tables
                ds.Tables.Add(t.Copy)
            Next
            'añadimos los datarelations
            For Each dr As DataRelation In dstabla.Relations
                ds.Relations.Add(New DataRelation(dr.RelationName, ds.Tables(dr.ParentTable.TableName).Columns(dr.ParentColumns(0).ColumnName), ds.Tables(dr.ChildTable.TableName).Columns(dr.ChildColumns(0).ColumnName), False))
            Next
        Next

        Return ds
    End Function

    Private Function GenerarDataSetDesdeTabla(ByVal tabla As ITabla) As DataSet
        Dim dsTabla As DataSet
        'obtenemos el dataset a partir de la sql y la col de parametros que
        'nos da la tabla
        Using tr As New Transaccion()
            Dim colParametros As List(Of System.Data.IDataParameter) = Nothing
            Dim sql As String = String.Empty
            tabla.GenerarSQL(sql, colParametros)
            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dsTabla = ej.EjecutarDataSet(sql, colParametros, False)
            tr.Confirmar()
        End Using

        'ponemos el nombre de la tabla
        dsTabla.Tables(0).TableName = tabla.NombreTabla

        'añadimos los datatables hijos a partir de las tablas relacionadas
        If Not tabla.TablasRelacionadas Is Nothing Then
            For Each tablaHija As TablaRelacionadaAIQB In tabla.TablasRelacionadas
                Dim dsHijo As DataSet = GenerarDataSetDesdeTabla(tablaHija)
                dsTabla.Tables.Add(dsHijo.Tables(0).Copy)
                'añadimos un datarelation con el nombre que se le quiere dar
                dsTabla.Relations.Add(New DataRelation(tablaHija.NombreRelacion, dsTabla.Tables(tabla.NombreTabla).Columns(tablaHija.fkPadre), dsTabla.Tables(tablaHija.NombreTabla).Columns(tablaHija.fKPropio), False))
            Next
        End If

        Return dsTabla
    End Function

    Public Function GenerarInforme(ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As System.IO.FileInfo
        'obtenemos el dataset y la lista de tablas
        Dim ds As DataSet = Nothing
        Dim listaTablasPrincipales As List(Of String) = Nothing

        GenerarDatasetYListaTablas(AdaptadorIQB, ds, listaTablasPrincipales)

        Dim docDataSource As System.Xml.XmlDocument = AdaptadorDataSourceOXML.GenerarContenidoDataSourceXML(ds, listaTablasPrincipales)

        'generamos el archivo temporal
        Dim fi As System.IO.FileInfo = Me.GenerarDocumentoTemporal(AdaptadorIQB.Plantilla)

        Dim gestor As New GestorWordOpenXML()
        Dim docMain As Xml.XmlDocument = gestor.ObtenerMainDocument(fi.FullName)

        Dim errores As Integer = gestor.AsociarYCombinarDocumento(docMain, docDataSource)
        If errores <> 0 Then
            Throw New ApplicationException("Se han producido " & errores.ToString() & " errores en la generación del documento")
        End If

        'guardamos el main document y el custompart en el fichero temporal
        gestor.GuardarMainDocument(fi.FullName, docMain)
        gestor.ModificarCustomPart(fi.FullName, docDataSource)


        Return fi
    End Function

    Public Function GenerarInforme_Archivo(ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As Byte()
        Dim fi As System.IO.FileInfo = GenerarInforme(AdaptadorIQB)
        Return SerializarArchivo(fi)
    End Function

    Public Function GenerarEsquemaXMLEnPlantilla_Archivo(ByVal AdaptadorIQB As AdaptadorInformesQueryBuildingDN) As Byte()
        Dim fi As System.IO.FileInfo = Me.GenerarEsquemaXMLEnPlantilla(AdaptadorIQB)
        Return SerializarArchivo(fi)
    End Function

    Private Function SerializarArchivo(ByVal fi As System.IO.FileInfo) As Byte()
        Dim str As System.IO.FileStream = fi.OpenRead()
        Dim buffer(str.Length - 1) As Byte
        str.Read(buffer, 0, CInt(str.Length))
        str.Close()
        Return buffer
    End Function

End Class
