#Region "Importaciones"

Imports System.Collections.Generic

Imports Framework.AccesoDatos
Imports Framework.TiposYReflexion.DN

#End Region

Namespace AD
    Public Class ConstructorAdapterAD
        Implements IConstructorAD

#Region "Atributos"
        Protected _ConstructorMotor As ConstructorSQLSQLsAD
        Protected _MapInstClase As InfoTypeInstClaseDN
#End Region

#Region "Constructores"
        Public Sub New(ByVal pConstructorMotor As ConstructorSQLSQLSAD, ByVal pMapInstClase As InfoTypeInstClaseDN)
            _ConstructorMotor = pConstructorMotor
            _MapInstClase = pMapInstClase
        End Sub
#End Region

#Region "Metodos"
        Public Function ConstruirDatos(ByVal pDR As System.Data.IDataReader) As Object Implements IConstructorAD.ConstruirDatos
            Return _ConstructorMotor.ConstruirDatos(pDR)
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
            Return _ConstructorMotor.ConstSqlInsert(_MapInstClase, pParametros, pFechaModificacion, pSqlHistorica)
        End Function

        Public Function ConstSqlSelectID(ByVal pID As String) As String Implements IConstructorAD.ConstSqlSelectID
            Return _ConstructorMotor.ConstSqlSelectID(_MapInstClase, Nothing, pID)
        End Function
        Public Function ConstSqlSelect(ByVal pID As String) As String Implements IConstructorAD.ConstSqlSelect
            Return _ConstructorMotor.ConstSqlSelect(_MapInstClase, Nothing, pID)
        End Function
        Public Function ConstSqlUpdate(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String Implements IConstructorAD.ConstSqlUpdate
            Return _ConstructorMotor.ConstSqlUpdate(_MapInstClase, pParametros, pFechaModificacion, pSqlHistorica)
        End Function
 
        'Public Function ConstColHuellasRelInversa(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As Framework.DatosNegocio.ColIHuellaEntidadDN
        '    Return _ConstructorMotor.ConstColHuellasRelInversa(pObjeto, pInfoReferido, PropiedadDeInstanciaDN, pParametros, pID)
        'End Function
#End Region

    End Class
End Namespace
