Imports System.Data
Imports Framework.AccesoDatos

Namespace AD

    ''' <summary>
    ''' Constructor genérico que permite realizar búsquedas de campos string
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ConstructorBusquedaCampoStringAD
        Implements IConstructorBusquedaAD


#Region "Atributos"

        Private mVista As String
        Private mCampo As String
        Private mValor As String

#End Region

#Region "Constructores"

        Public Sub New(ByVal vista As String, ByVal campo As String, ByVal valor As String)
            mVista = vista
            mCampo = campo
            mValor = valor
        End Sub

#End Region

#Region "IConstructorBusquedaAD Members"

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As Framework.AccesoDatos.MotorAD.DN.FiltroDN, ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New ApplicationExceptionAD("El método no está implementado")
        End Function

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of Framework.AccesoDatos.MotorAD.DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New ApplicationExceptionAD("El método no está implementado")
        End Function

        Public Function ConstruirSQLBusqueda(ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda
            Dim sql As String

            If pParametros Is Nothing Then
                pParametros = New System.Collections.Generic.List(Of System.Data.IDataParameter)()
            End If
            sql = "select ID from " & mVista
            If Not String.IsNullOrEmpty(mCampo) Then
                sql = sql & " where " & mCampo & " = @valor"
                pParametros.Add(ParametrosConstAD.ConstParametroString("@valor", mValor))
            End If

            Return sql
        End Function

#End Region


        Public Function ConstruirSQLBusqueda1(ByVal pTypo As System.Type, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New NotImplementedException("Error: no implementado")
        End Function
    End Class

End Namespace

