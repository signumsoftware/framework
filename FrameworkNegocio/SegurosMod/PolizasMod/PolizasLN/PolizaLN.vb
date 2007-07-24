Imports Framework.LogicaNegocios.Transacciones
Public Class PolizaLN


    'Public Function BajaPoliza(ByVal pPeriodoRenovacionPoliza As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pFechaBaja As Date)




    'End Function


    ''' <summary>
    '''  recupera o crea un tomador de no exixitir
    ''' </summary>
    ''' <param name="cifNif"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarCrearTomador(ByVal cifNif As String) As FN.Seguros.Polizas.DN.TomadorDN




        Using tr As New Transaccion
            Dim ad As New FN.Seguros.Polizas.AD.PolizasAD
            Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = ad.RecuperarTomador(cifNif)

            If tomador Is Nothing Then
                ' verificar si existe la entidad fiscal
                Dim locfiscalln As New FN.Localizaciones.LN.FiscalLN
                Dim efg As FN.Localizaciones.DN.EntidadFiscalGenericaDN = locfiscalln.RecuperarEntidadFiscalGenerica(cifNif)


                If efg Is Nothing Then

                    Throw New ApplicationException("No se recuperó ninguna entidad fiscal generica")
                Else
                    tomador = New FN.Seguros.Polizas.DN.TomadorDN
                    tomador.ValorBonificacion = 1
                    tomador.EntidadFiscalGenerica = efg
                    RecuperarCrearTomador = tomador
                End If

            Else
                RecuperarCrearTomador = tomador


            End If


            tr.Confirmar()

        End Using




    End Function

End Class
