Namespace AD
    Public Class ContructorValoresHashAD
        Implements AD.IConstructorBusquedaAD


        Private mTipo As System.Type
        Private mValorHash As String


        Public Sub New(ByVal pTipo As System.Type, ByVal pValorHash As String)
            mTipo = pTipo
            mValorHash = pValorHash

        End Sub

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As AccesoDatos.MotorAD.DN.FiltroDN, ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New NotImplementedException
        End Function

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of AccesoDatos.MotorAD.DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New NotImplementedException
        End Function

        Public Function ConstruirSQLBusqueda(ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements AccesoDatos.MotorAD.AD.IConstructorBusquedaAD.ConstruirSQLBusqueda

            Dim miInfoTypeInstClaseDN As Framework.TiposYReflexion.DN.InfoTypeInstClaseDN
            miInfoTypeInstClaseDN = New Framework.TiposYReflexion.DN.InfoTypeInstClaseDN(mTipo)
            pParametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@HashValores", mValorHash))
            Return "select id from " & miInfoTypeInstClaseDN.TablaNombre & " where HashValores=@HashValores"
        End Function

        Public Function ConstruirSQLBusqueda1(ByVal pTypo As System.Type, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements IConstructorBusquedaAD.ConstruirSQLBusqueda
            Throw New NotImplementedException("Error: no implementado")
        End Function
    End Class
End Namespace
