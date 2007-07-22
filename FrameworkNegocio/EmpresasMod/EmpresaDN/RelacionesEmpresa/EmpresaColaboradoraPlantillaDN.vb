Public Class EmpresaColaboradoraPlantillaDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"
    Protected mActividad As ActividadDN
#End Region

End Class

Public Class ColEmpresaColaboradoraPlantillaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of EmpresaColaboradoraPlantillaDN)

End Class