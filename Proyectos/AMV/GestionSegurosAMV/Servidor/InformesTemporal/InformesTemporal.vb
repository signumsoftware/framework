Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections
Imports Framework.LogicaNegocios.Transacciones
Imports System.Xml
Imports System.Data

Public Class InformesTemporal

    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        If mRecurso Is Nothing Then
            connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
            htd.Add("connectionstring", connectionstring)
            mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        End If
        If Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos Is Nothing Then
            Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposNULOLN
        End If

    End Sub

    Public Sub CargarEsquemaXMLEnPlantillaDoc(ByVal pIDPresuspuesto As String)

        Dim tablasPrincipales As New List(Of String)
        tablasPrincipales.Add("DatosTarifa")

        Dim doc As XmlDocument = Framework.GestorInformes.AdaptadorDataSourceOXML.GenerarEsquemaXML(GenerarDataset(pIDPresuspuesto), tablasPrincipales)

        Dim sw As New System.IO.StreamWriter("EsquemaTarifa.XML", False, System.Text.Encoding.UTF8)

        sw.Write(doc.InnerXml)
        sw.Flush()
        sw.Close()
    End Sub


    Private Function GenerarDataset(ByVal pIDPresuspuesto As String) As DataSet
        ObtenerRecurso()

        If String.IsNullOrEmpty(pIDPresuspuesto) Then
            pIDPresuspuesto = "1"
        End If


        Dim dsfinal As New DataSet
        Dim ds As DataSet

        Using New CajonHiloLN(Me.mRecurso)
            Using tr As New Transaccion()
                Dim ej As New AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
                Dim misql As String = "SELECT * FROM  vwImpresionTarifa1 WHERE ID=" & pIDPresuspuesto
                ds = ej.EjecutarDataSet(misql, Nothing, False)
                tr.Confirmar()
            End Using

            dsfinal.Tables.Add(ds.Tables(0).Copy)
            dsfinal.Tables(0).TableName = "DatosTarifa"

            Using tr As New Transaccion()
                Dim ej As New AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
                Dim misql As String = "SELECT * FROM  vwImpresionTarifa2 WHERE ID=" & pIDPresuspuesto
                ds = ej.EjecutarDataSet(misql, Nothing, False)
                tr.Confirmar()
            End Using

            ds.Tables(0).TableName = "Productos"
            dsfinal.Tables.Add(ds.Tables(0).Copy)
        End Using

        Dim dr As New DataRelation("Productos", dsfinal.Tables("DatosTarifa").Columns("ID"), dsfinal.Tables("Productos").Columns("ID"), False)
        dsfinal.Relations.Add(dr)

        Return dsfinal

    End Function

    Public Function ImprimirPresupuesto(ByVal pIDPresuspuesto As String) As System.IO.FileInfo

        System.IO.File.Copy("PlantillaTarifa2.docx", "InformeTarifa.docx", True)

        Dim ds As DataSet = Me.GenerarDataset(pIDPresuspuesto)
        Dim tablasPrincipales As New List(Of String)
        tablasPrincipales.Add("DatosTarifa")

        'generamos el doc datasource cargado
        Dim doc As XmlDocument = Framework.GestorInformes.AdaptadorDataSourceOXML.GenerarContenidoDataSourceXML(ds, tablasPrincipales)

        Dim gestor As New Framework.GestorInformes.GestorWordOpenXML()
        gestor.ModificarCustomPart("InformeTarifa.docx", doc)

        Dim docMain As XmlDocument = gestor.ObtenerMainDocument("InformeTarifa.docx")
        Dim docDataSource As XmlDocument = gestor.ObtenerCustomPart("InformeTarifa.docx", New Uri("http://signumsoftware.com/2007/pruebas"), "item1.xml")
        Dim errores As Integer = gestor.AsociarYCombinarDocumento(docMain, docDataSource)
        gestor.GuardarMainDocument("InformeTarifa.docx", docMain)

        Return New System.IO.FileInfo("InformeTarifa.docx")

    End Function

End Class
