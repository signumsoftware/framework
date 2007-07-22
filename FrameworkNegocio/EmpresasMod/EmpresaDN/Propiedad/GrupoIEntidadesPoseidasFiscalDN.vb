''' <summary>
''' igual que GrupoIEntidadesPoseidasDN, pero el possedor ha de ser una entidad fiscal
''' * RIESGOS:
''' la glase guarda su possedor en  mybase.mPoseedor del tipo IEntidadDN que debe ser restringdo para que sean IEntidadFiscalDN
'''      esta clase requiere un mapeado donde mPoseedor As Framework.DatosNegocio.IEntidadDN sea mapeado contra possedores que implemente ientidad fiscal
''' </summary>
''' <remarks></remarks>
Public Class GrupoIEntidadesPoseidasFiscalDN
    Inherits GrupoIEntidadesPoseidasDN
    ' esta clase requiere un mapeado donde mybase.mPoseedor As Framework.DatosNegocio.IEntidadDN sea mapeado contra possedores que implemente ientidad fiscal
End Class

