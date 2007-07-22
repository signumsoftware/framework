Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.LogicaNegocios.Transacciones
Imports FN.Empresas.DN
Imports FN.Empresas.AD

Public Class EmpleadosLN
    Inherits Framework.LogicaNegocios.Transacciones.BaseGenericLN

#Region "Constructores"

#End Region

#Region "Métodos"

    'Public Function RecuperarListado(ByVal pIdEmpresa As String) As DataSet
    '    Dim objAD As EmpleadosAD

    '    Using tr As New Transaccion()

    '        'TODO: Provisional para probar
    '        If pIdEmpresa Is Nothing OrElse pIdEmpresa = String.Empty Then
    '            pIdEmpresa = 2
    '        End If

    '        objAD = New EmpleadosAD()
    '        RecuperarListado = objAD.RecuperarListado(pIdEmpresa)

    '        '4º Verificar las postCondiciones de negocio para el procedimiento
    '        tr.Confirmar()
    '    End Using

    'End Function

    Public Function RecuperarEmpleado(ByVal pId As String) As EmpleadoDN
        Dim gi As GestorInstanciacionLN

        Using tr As New Transaccion()

            gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            RecuperarEmpleado = gi.Recuperar(Of EmpleadoDN)(pId)

            tr.Confirmar()

        End Using

    End Function


    ''' <summary>
    ''' Método que guarda un EempleadoYPuestoR, dando de baja los objetos PuestoRealizadoDN que ya no
    ''' sean desempeñados por el empleado si el empleado ya tiene ID. 
    ''' </summary>
    ''' <param name="empPR"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GuardarEmpleadoYPuestoR(ByVal empPR As EmpleadoYPuestosRDN) As EmpleadoYPuestosRDN
        Dim empPRBD As EmpleadoYPuestosRDN = Nothing
        Dim colPR As ColPuestoRealizadoDN

        Using tr As New Transaccion()
            'Se recupera el EmpleadoYPuesto de la base de datos
            If Not String.IsNullOrEmpty(empPR.ID) Then
                empPRBD = MyBase.Recuperar(Of EmpleadoYPuestosRDN)(empPR.ID)

                If empPRBD IsNot Nothing AndAlso empPRBD.ColPuestoRealizado IsNot Nothing Then
                    colPR = empPRBD.ColPuestoRealizado.RecuperarColPRNoInterseccion(empPR.ColPuestoRealizado)
                    If colPR IsNot Nothing Then
                        For Each puestoR As PuestoRealizadoDN In colPR
                            MyBase.Baja(Of PuestoRealizadoDN)(puestoR.ID)
                        Next
                    End If
                End If

            End If

            MyBase.Guardar(Of EmpleadoYPuestosRDN)(empPR)

            tr.Confirmar()

            Return empPR

        End Using

    End Function

#End Region

End Class
