Public Class BuscadorRelacionesAEntidadNavAD
    Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD



    Protected mTipo As System.Type


    Public Sub New(ByVal pTipo As System.Type)
        mTipo = pTipo
    End Sub

    Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As Framework.AccesoDatos.MotorAD.DN.FiltroDN, ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda

    End Function

    Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of Framework.AccesoDatos.MotorAD.DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda

    End Function

    Public Function ConstruirSQLBusqueda(ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda

        Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(mTipo)


        pParametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@NEO", vc.NombreEnsamblado))
        pParametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@NCO", vc.NombreClase))
        pParametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@NED", vc.NombreEnsamblado))
        pParametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@NCD", vc.NombreClase))

        Return "Select id from vwRelacionesxTipo where (NEO=@NEO and NCO=@NCO ) or (NED=@NED and NCD=@NCD )"
    End Function

    Public Function ConstruirSQLBusqueda1(ByVal pTypo As System.Type, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of Framework.AccesoDatos.MotorAD.DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements Framework.AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda
        Throw New NotImplementedException("Error: no implementado")
    End Function
End Class
