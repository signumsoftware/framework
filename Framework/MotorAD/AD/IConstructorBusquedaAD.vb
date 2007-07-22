#Region "Importaciones"

Imports System.Collections.Generic
Imports Framework.AccesoDatos.MotorAD.DN

#End Region

Namespace AD
    Public Interface IConstructorBusquedaAD

#Region "Metodos"
        Function ConstruirSQLBusqueda(ByRef pParametros As List(Of IDataParameter)) As String
        Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As FiltroDN, ByRef pParametros As List(Of IDataParameter)) As String
        Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As List(Of CondicionRelacionalDN), ByRef pParametros As List(Of IDataParameter)) As String
        Function ConstruirSQLBusqueda(ByVal pTypo As Type, ByVal pNombreVistaFiltro As String, ByVal pFiltro As List(Of CondicionRelacionalDN), ByRef pParametros As List(Of IDataParameter)) As String

#End Region


    End Interface
End Namespace
