Imports Framework.LogicaNegocios.Transacciones


Public Class RecuperadorLiquidadoresConcretosLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    ''' <summary>
    ''' crea una isntacia de una clase liquidador concreto usando un origen de importe debido y un mapeado
    ''' </summary>
    ''' <param name="origenImportedebido"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarLiquidador(ByVal origenImportedebido As FN.GestionPagos.DN.IOrigenIImporteDebidoDN) As ILiquidadorConcretoLN

    End Function

    Public Function RecuperarLiquidador(ByVal pTypoOrigen As Type, ByVal pFecha As Date) As ILiquidadorConcretoLN

        ' recupera el el liquidador usandop el tipo del origen 

        Dim recuperador As FN.GestionPagos.AD.MapIorigenImpDevLiquidadoresAD = New FN.GestionPagos.AD.MapIorigenImpDevLiquidadoresAD()
        Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN = recuperador.Recuperar(pTypoOrigen, pFecha)

        If map Is Nothing Then
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se recupero ningun liquidador concreto")
        End If

        Return map.VCLiquidadorConcreto.CrearInstancia()



    End Function

End Class
