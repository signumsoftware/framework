Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN

Public Class FactoriaTrazasLN
    ''' <summary>
    ''' recupera segun el mapeado de persistencia las trazas declaradas para una entidad dada
    ''' </summary>
    ''' <param name="ptipo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarTrazas(ByVal ptipo As System.Type) As Framework.Procesos.ProcesosDN.ColITrazaDN
        Dim col As New Framework.Procesos.ProcesosDN.ColITrazaDN


        Dim infoMapDatosInst As InfoDatosMapInstClaseDN
        Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor


        'Obtener el mapeado de comportamiento de la entidad
        infoMapDatosInst = gdmi.RecuperarMapPersistenciaCampos(ptipo)

        For Each tipo As System.Type In infoMapDatosInst.ColTiposTrazas
            col.Add(Activator.CreateInstance(tipo))
        Next




        Return col
    End Function
End Class
