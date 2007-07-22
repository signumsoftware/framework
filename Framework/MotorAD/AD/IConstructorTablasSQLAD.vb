#Region "Importaciones"

Imports Framework.TiposYReflexion.DN

#End Region

Namespace AD
    Public Interface IConstructorTablasSQLAD



#Region "Metodos"

        Function ConstSqlCreateTable(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pHistoricas As Boolean) As String
        Function ConstSqlCreateRelations(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal pInfoCampo As InfoDatosMapInstCampoDN, ByVal pHistoricas As Boolean) As RelacionSQLsDN
#End Region

    End Interface
End Namespace
