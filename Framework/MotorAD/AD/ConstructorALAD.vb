#Region "Importaciones"

Imports System.Collections.Generic

Imports Framework.AccesoDatos

#End Region

Namespace AD
    Public Class ConstructorAL
        Implements IConstructorAD

        Implements IConstructorBusquedaAD

        Dim mTypo As System.Type


        Public Sub New(ByVal pTypo As System.Type)
            mTypo = pTypo
        End Sub

#Region "Metodos"
        'Este metodo devuelve un ArrayList que representa una colecion de ids
        Public Function ConstruirDatos(ByVal pDR As System.Data.IDataReader) As Object Implements IConstructorAD.ConstruirDatos
            Dim al As New ArrayList



            Dim literalID As String
            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(mTypo) Then
                literalID = "GUID"

            Else
                literalID = "ID"
            End If


            If (Not pDR Is Nothing) Then
                Do While pDR.Read
                    al.Add(pDR(literalID))
                Loop
            End If

            Return al
        End Function

        Public Overloads Function ConstruirEntidad(ByVal pHLDatos As System.Collections.Hashtable) As Object Implements IConstructorAD.ConstruirEntidad
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Overloads Function ConstruirEntidad1(ByVal pALDatos As System.Collections.IList) As Object Implements IConstructorAD.ConstruirEntidad
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Function ConstSqlBaja(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date) As String Implements IConstructorAD.ConstSqlBaja
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Function ConstSqlInsert(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String Implements IConstructorAD.ConstSqlInsert
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Function ConstSqlSelect(ByVal pID As String) As String Implements IConstructorAD.ConstSqlSelect
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Function ConstSqlUpdate(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String Implements IConstructorAD.ConstSqlUpdate
            Throw New NotImplementedException("Error: no implementado")
        End Function
#End Region

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As DN.FiltroDN, ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New NotImplementedException("Error: no implementado")

        End Function

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements IConstructorBusquedaAD.ConstruirSQLBusqueda
            '   Throw New NotImplementedException("Error: no implementado")
            Return "select id from " & pNombreVistaVisualizacion
        End Function

        Public Function ConstruirSQLBusqueda(ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New NotImplementedException("Error: no implementado")

        End Function

        Public Function ConstruirSQLBusqueda1(ByVal pTypo As System.Type, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements IConstructorBusquedaAD.ConstruirSQLBusqueda
            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTypo) Then

                Return "select GUID from tl" & pTypo.Name
            Else
                Return "select id from tl" & pTypo.Name
            End If


        End Function

        Public Function ConstSqlSelectID(ByVal pID As String) As String Implements IConstructorAD.ConstSqlSelectID

        End Function
    End Class
End Namespace
