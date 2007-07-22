Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones

Imports FN.RiesgosVehiculos.DN
Imports FN.Seguros.Polizas.DN

Public Class RecNivelBonificacionLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    Implements IRecuperadorNivelBonificacion




    Public Tomador As Seguros.Polizas.DN.ITomador



    Public Function CalcularValorBonificacion(ByVal pTomador As IEntidadDN) As Double Implements DN.IRecuperadorNivelBonificacion.CalcularValorBonificacion
        Using tr As New Transaccion()
            Dim valor As Double

            pTomador = Tomador


            If pTomador IsNot Nothing Then
                If TypeOf pTomador Is FuturoTomadorDN Then
                    valor = CType(pTomador, FuturoTomadorDN).ValorBonificacion
                ElseIf TypeOf pTomador Is TomadorDN Then
                    valor = (CType(pTomador, FuturoTomadorDN).ValorBonificacion)
                Else
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El objeto tomador debe ser un tomador o futuro tomador")
                End If
            Else
                valor = 1
            End If

            If valor > 3.5 Then
                valor = 3.5
            ElseIf valor < 0.5 Then
                valor = 0.5
            End If

            tr.Confirmar()

            Return valor

        End Using

    End Function

End Class
