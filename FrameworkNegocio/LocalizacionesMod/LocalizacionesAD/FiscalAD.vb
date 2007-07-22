Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Public Class FiscalAD


    Public Function RecuperarEntidadFiscalGenerica(ByVal pCifNif As String) As FN.Localizaciones.DN.EntidadFiscalGenericaDN





        Using tr As New Transaccion

            Dim parametros As List(Of System.Data.IDataParameter)
            Dim sql As String

            parametros = New List(Of System.Data.IDataParameter)

            parametros.Add(ParametrosConstAD.ConstParametroString("ValorCifNif", pCifNif))

            sql = "SELECT ID FROM tlEntidadFiscalGenericaDN WHERE ValorCifNif=@ValorCifNif"

            Dim ej As New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim ds As DataSet = ej.EjecutarDataSet(sql, parametros)

            Select Case ds.Tables(0).Rows.Count


                Case Is = 0

                    ' crear una empresa o una persona segun proceda
                    Dim mensaje As String
                    If FN.Localizaciones.DN.CifDN.ValidaCif(pCifNif, mensaje) Then
                        Dim empresa As New Empresas.DN.EmpresaFiscalDN
                        empresa.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN(pCifNif)
                        RecuperarEntidadFiscalGenerica = empresa.EntidadFiscalGenerica

                    ElseIf FN.Localizaciones.DN.NifDN.ValidaNif(pCifNif, mensaje) Then
                        Dim personaFisc As New FN.Personas.DN.PersonaFiscalDN
                        personaFisc.Persona = New FN.Personas.DN.PersonaDN
                        personaFisc.Persona.NIF = New FN.Localizaciones.DN.NifDN(pCifNif)
                        RecuperarEntidadFiscalGenerica = personaFisc.EntidadFiscalGenerica

                    Else
                        Throw New ApplicationException("la identificacion fiscal no es un CIF ni un NIF válido")

                    End If


                Case Is = 1
                    Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    RecuperarEntidadFiscalGenerica = gi.Recuperar(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(ds.Tables(0).Rows(0)(0))


                Case Is > 1
                    Throw New ApplicationException("error de integridad no debe haber varis entidades fiscales genericas para la misma identidad fiscal")

            End Select



            tr.Confirmar()



        End Using



    End Function

End Class
