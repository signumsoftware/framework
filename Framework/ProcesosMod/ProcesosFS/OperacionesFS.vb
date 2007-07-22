Imports Framework.FachadaLogica
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN
Imports Framework.Procesos.ProcesosDN

Public Class OperacionesFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "Métodos"


    Public Function EjecutarOperacion(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal objeto As Object, ByVal pOperacionRealizada As Framework.Procesos.ProcesosDN.OperacionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
        Dim tipoAutorizacion As TipoAutorizacionClase
        Dim entidad As Framework.DatosNegocio.IEntidadBaseDN

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º verificacion de permisos por rol de usuario
                actor.Autorizado()

                entidad = objeto
                If String.IsNullOrEmpty(entidad.ID) Then
                    tipoAutorizacion = TipoAutorizacionClase.crear
                Else
                    tipoAutorizacion = TipoAutorizacionClase.modificar
                End If

                '3º verificacion de permisos por rol de usuario
                ' actor.Autorizado(tipoAutorizacion, objeto.GetType())

                '-----------------------------------------------------------------------------
                '4º creacion de la ln y ejecucion del metodo
                Dim miln As ProcesosLN.GestorOPRLN
                miln = New ProcesosLN.GestorOPRLN()
                EjecutarOperacion = miln.EjecutarOperacion(objeto, pParametros, actor, pOperacionRealizada)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function


    Public Function EjecutarOperacion(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
        Dim tipoAutorizacion As TipoAutorizacionClase
        Dim entidad As Framework.DatosNegocio.IEntidadBaseDN

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º verificacion de permisos por rol de usuario
                actor.Autorizado()

                If pTransicionRealizada.OperacionRealizadaOrigen Is Nothing Then
                    entidad = objeto
                Else
                    entidad = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion
                End If



                If String.IsNullOrEmpty(entidad.ID) Then
                    tipoAutorizacion = TipoAutorizacionClase.crear
                Else
                    tipoAutorizacion = TipoAutorizacionClase.modificar
                End If

                '3º verificacion de permisos por rol de usuario
                '  actor.Autorizado(tipoAutorizacion, objeto.GetType())

                '-----------------------------------------------------------------------------
                '4º creacion de la ln y ejecucion del metodo
                Dim miln As ProcesosLN.GestorOPRLN
                miln = New ProcesosLN.GestorOPRLN()
                EjecutarOperacion = miln.EjecutarOperacion(objeto, pParametros, actor, pTransicionRealizada)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

    Public Function RecuperarTransicionesAutorizadasSobre(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal pHuellaEntidad As Framework.DatosNegocio.HEDN) As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
        Dim tipoAutorizacion As TipoAutorizacionClase

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º verificacion de permisos por rol de usuario
                actor.Autorizado()

                If String.IsNullOrEmpty(pHuellaEntidad.IdEntidadReferida) OrElse pHuellaEntidad.IdEntidadReferida = "0" Then
                    tipoAutorizacion = TipoAutorizacionClase.crear
                Else
                    tipoAutorizacion = TipoAutorizacionClase.modificar
                End If

                '3º verificacion de permisos por rol de usuario
                ' actor.Autorizado(tipoAutorizacion, pHuellaEntidad.TipoEntidadReferida)

                '-----------------------------------------------------------------------------
                '4º creacion de la ln y ejecucion del metodo
                Dim miln As ProcesosLN.OperacionesLN
                miln = New ProcesosLN.OperacionesLN()
                RecuperarTransicionesAutorizadasSobre = miln.RecuperarTransicionesAutorizadasSobre(actor, pHuellaEntidad)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

    Public Function RecuperarEjecutorCliente(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal nombreCliente As String) As EjecutoresDeClienteDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º verificacion de permisos por rol de usuario
                actor.Autorizado()

                '-----------------------------------------------------------------------------
                '3º creacion de la ln y ejecucion del metodo
                Dim miln As ProcesosLN.OperacionesLN
                miln = New ProcesosLN.OperacionesLN()
                RecuperarEjecutorCliente = miln.RecuperarEjecutorCliente(nombreCliente)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

    Public Function RecuperarTransicionesDeInicio(ByVal idSesion As String, ByVal actor As PrincipalDN, ByVal pTipoDN As System.Type) As ColTransicionDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º verificacion de permisos por rol de usuario
                actor.Autorizado()

                '-----------------------------------------------------------------------------
                '3º creacion de la ln y ejecucion del metodo
                Dim miln As ProcesosLN.OperacionesLN
                miln = New ProcesosLN.OperacionesLN()
                RecuperarTransicionesDeInicio = miln.RecuperarTransicionesDeInicio(pTipoDN)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

#End Region

End Class
