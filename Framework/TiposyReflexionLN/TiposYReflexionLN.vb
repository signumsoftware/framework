Imports Framework.TiposyReflexion.DN
Imports Framework.TiposyReflexion.AD
Imports Framework.LogicaNegocios.Transacciones

Public Class TiposYReflexionLN
    Inherits BaseGenericLN

#Region "Métodos"

    ''' <summary>
    ''' Método que recupera el VinculoMetodo de la base de datos a partir del MethodInfo, o bien crea
    ''' uno nuevo si no existe
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function CrearVinculoMetodo(ByVal metodoInfo As System.Reflection.MethodInfo) As VinculoMetodoDN
        Dim adTipos As TiposYReflexionAD
        Dim vinculoM As VinculoMetodoDN
        Dim vinculoC As VinculoClaseDN

        Using tr As New Transaccion()
            adTipos = New TiposYReflexionAD()

            vinculoM = adTipos.RecuperarVinculoMetodo(metodoInfo)

            If vinculoM Is Nothing Then
                'Si no existe el método, hay que comprobar si existe al menos la clase, sino se genera
                vinculoC = Me.CrearVinculoClase(metodoInfo.ReflectedType)
                vinculoM = New VinculoMetodoDN(metodoInfo, vinculoC)
            End If

            tr.Confirmar()

            Return vinculoM

        End Using

    End Function

    ''' <summary>
    ''' Método que recupera el VinculoClase de la base de datos a partir del type, o bien crea
    ''' uno nuevo si no existe
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function CrearVinculoClase(ByVal tipo As System.Type) As VinculoClaseDN
        Dim adTipos As TiposYReflexionAD
        Dim vinculoC As VinculoClaseDN

        Using tr As New Transaccion()
            adTipos = New TiposYReflexionAD()

            vinculoC = adTipos.RecuperarVinculoClase(tipo)

            If vinculoC Is Nothing Then
                vinculoC = New VinculoClaseDN(tipo)
            End If

            tr.Confirmar()

            Return vinculoC

        End Using

    End Function

#End Region

End Class
