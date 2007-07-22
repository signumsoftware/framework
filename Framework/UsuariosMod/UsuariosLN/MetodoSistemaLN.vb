#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.AD
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.AD
Imports Framework.AccesoDatos.MotorAD.DN
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.LN
Imports Framework.TiposYReflexion.DN

#End Region

Public Class MetodoSistemaLN
    Inherits BaseGenericLN

#Region "Constructores"

#End Region

#Region "Metodos"

    Public Function RecuperarMetodos() As IList(Of MetodoSistemaDN)
        Dim mMSad As MetodoSistemaAD

        Using tr As New Transaccion()

            mMSad = New MetodoSistemaAD()
            RecuperarMetodos = mMSad.RecuperarMetodos()

            tr.Confirmar()

        End Using

    End Function

    ''' <summary>
    ''' Método que recupera el MetodoSistemaDN de la base de datos a partir del MethodInfo y el tipo, o bien crea
    ''' uno nuevo si no existe
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function CrearMetodoSistema(ByVal metodoInfo As System.Reflection.MethodInfo) As MetodoSistemaDN
        Dim msAD As MetodoSistemaAD
        Dim metodoSistema As MetodoSistemaDN
        Dim vinculoM As VinculoMetodoDN

        Using tr As New Transaccion()
            msAD = New MetodoSistemaAD()

            metodoSistema = msAD.RecuperarMetodoSistema(metodoInfo)

            If metodoSistema Is Nothing Then
                'Si no existe el método, hay que comprobar si existe al menos el vínculo método
                Dim tyrLN As New TiposYReflexion.LN.TiposYReflexionLN()
                vinculoM = tyrLN.CrearVinculoMetodo(metodoInfo)
                metodoSistema = New MetodoSistemaDN(vinculoM)
            End If

            tr.Confirmar()

            Return metodoSistema

        End Using

    End Function

    'Public Function RecuperarMetodos() As List(Of MetodoSistemaDN)
    '    Dim motor As GestorInstanciacionLN
    '    Dim adMotor As AccesorMotorAD
    '    Dim tlProc As ITransaccionLogicaLN = Nothing
    '    Dim rctd As CTDLN
    '    Dim lista As ArrayList
    '    Dim listaMetodos As ArrayList
    '    Dim metodos As List(Of MetodoSistemaDN)
    '    Dim tipo As Type = Nothing
    '    Dim ensamblado As Assembly = Nothing
    '    Dim i As Integer

    '    rctd = New CTDLN
    '    Try
    '        rctd.IniciarTransaccion(mTL, tlProc)

    '        'Recuperacion de la col de ids
    '        adMotor = New AccesorMotorAD(tlProc, mRec, New ConstructorSQLSQLsAD)
    '        lista = adMotor.BuscarGenericoIDS("tlMetodoSistemaDN", New List(Of CondicionRelacionalDN))

    '        InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo("AutorizacionDN", "Framework.Autorizacion.DatosNegocio.MetodoSistemaDN", ensamblado, tipo)

    '        'Recuperamos la lista de permisos
    '        listaMetodos = New ArrayList
    '        metodos = New List(Of MetodoSistemaDN)
    '        If (lista.Count > 0) Then
    '            motor = New GestorInstanciacionLN(tlProc, Me.mRec)
    '            listaMetodos = motor.Recuperar(lista, tipo, Nothing)
    '        End If

    '        For i = 0 To listaMetodos.Count - 1
    '            metodos.Add(listaMetodos(i))
    '        Next

    '        tlProc.Confirmar()

    '        Return metodos
    '    Catch ex As Exception
    '        If (Not tlProc Is Nothing) Then
    '            tlProc.Cancelar()
    '        End If

    '        Throw ex
    '    End Try
    'End Function

    'Public Sub GuardarMetodos(ByVal metodos As List(Of MetodoSistemaDN))
    '    Dim motor As GestorInstanciacionLN
    '    Dim i As Integer

    '    Using tr As New Transaccion()
    '        'Guardamos la lista de permisos
    '        motor = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
    '        For i = 0 To metodos.Count - 1
    '            motor.Guardar(metodos(i))
    '        Next

    '        tr.Confirmar()

    '    End Using

    'End Sub

#End Region

End Class
