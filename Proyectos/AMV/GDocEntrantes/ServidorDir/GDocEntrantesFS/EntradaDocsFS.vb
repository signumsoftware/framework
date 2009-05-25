Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN



Imports GDocEntrantesFS
Imports AmvDocumentosDN



Public Class EntradaDocsFS
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region


#Region "Métodos"



    Public Function RecuperarRelacionEnFicheroXID(ByVal id As String, ByVal idSesion As String, ByVal pActor As PrincipalDN) As AmvDocumentosDN.RelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2 º --> todo el mundo puede verlo, no ponemos la comprobación por permisos

            '3º creación de la ln y ejecucion del método
            Dim miln As New GDocEntrantesLN.GDocsLN(mTL, mRec)

            RecuperarRelacionEnFicheroXID = miln.RecuperarOperacionEnFicheroXid(id)

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)


        Catch ex As Exception
            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex
        End Try
    End Function

    Public Function RecuperarColTipoCanal(ByVal idSesion As String, ByVal pActor As PrincipalDN) As AmvDocumentosDN.ColTipoCanalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            ' pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As GDocEntrantesLN.CanalLN
            miln = New GDocEntrantesLN.CanalLN(mTL, mRec)
            RecuperarColTipoCanal = miln.RecuperarColTipoCanal()
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try



    End Function

    Public Function RecupearNumDocPendientesClasificacionXTipoCanal(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal dts As DataSet) As DataSet

        'GDocsLN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As GDocEntrantesLN.GDocsLN
            miln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            RecupearNumDocPendientesClasificacionXTipoCanal = miln.RecupearNumDocPendientesClasificacionXTipoCanal(dts)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing


    End Function

    Public Function RecuperarNumDocPendientesClasificaryPostClasificacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal dts As DataSet) As DataSet




        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            'pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As GDocEntrantesLN.GDocsLN
            miln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            RecuperarNumDocPendientesClasificaryPostClasificacion = miln.RecuperarNumDocPendientesClasificaryPostClasificacion(dts)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try




    End Function

    Public Function RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal dts As DataSet) As DataSet




        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As GDocEntrantesLN.GDocsLN
            miln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio = miln.RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio(dts)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try




    End Function

    Public Sub AltaDocumento(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pFicheroParaAlta As FicheroParaAlta)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            If pActor Is Nothing Then
                Throw New ApplicationExceptionFL("no autorizado", Nothing)
            End If
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miln As GDocEntrantesLN.GDocsLN
            miln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            miln.AltaDocumento(pFicheroParaAlta)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Sub

    Public Function RecuperarArbolTiposEntNegocio(ByVal idSesion As String, ByVal pActor As PrincipalDN) As CabeceraNodoTipoEntNegoioDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            'pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.CabeceraNodoTipoEntNegoioLN
            mln = New GDocEntrantesLN.CabeceraNodoTipoEntNegoioLN(mTL, mRec)
            RecuperarArbolTiposEntNegocio = mln.RecuperarArbolTiposEntNegocio()
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function GuardarArbolTiposEntNegocio(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pCabeceraCabeceraNodoTipoEntNegoio As CabeceraNodoTipoEntNegoioDN) As CabeceraNodoTipoEntNegoioDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            'pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.CabeceraNodoTipoEntNegoioLN
            mln = New GDocEntrantesLN.CabeceraNodoTipoEntNegoioLN(mTL, mRec)
            GuardarArbolTiposEntNegocio = mln.GuardarArbolTiposEntNegocio(pCabeceraCabeceraNodoTipoEntNegoio)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function RecuperarOperacionAPostProcesar(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pTipoEntNegoio As TipoEntNegoioDN, ByVal pIdentificadorentidadNegocio As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()


        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)

            If pActor.UsuarioDN.HuellaEntidadUserDN Is Nothing OrElse pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida Is Nothing Then
                Throw New Framework.FachadaLogica.ApplicationExceptionFL("el usuario debe de ser  operador", Nothing)
            End If

            RecuperarOperacionAPostProcesar = mln.RecuperarOperacionAPostProcesar(pActor, pTipoEntNegoio, pIdentificadorentidadNegocio)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function RecuperarOperacionAProcesar(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pIdTipoCanal As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)

            If pActor.UsuarioDN.HuellaEntidadUserDN Is Nothing OrElse pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida Is Nothing Then
                Throw New Framework.FachadaLogica.ApplicationExceptionFL("el usuario debe de ser  operador", Nothing)
            End If

            RecuperarOperacionAProcesar = mln.RecuperarOperacionAProcesar(pActor, pIdTipoCanal)

            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function RecuperarOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal idOperacion As String) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)

            If pActor.UsuarioDN.HuellaEntidadUserDN Is Nothing OrElse pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida Is Nothing Then
                Throw New Framework.FachadaLogica.ApplicationExceptionFL("el usuario debe de ser  operador", Nothing)
            End If

            RecuperarOperacion = mln.RecuperarOperacion(pActor, idOperacion)

            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function RecuperarOperacionEnCursoPara(ByVal idSesion As String, ByVal pActor As PrincipalDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)

            If pActor.UsuarioDN.HuellaEntidadUserDN Is Nothing OrElse pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida Is Nothing Then
                Throw New Framework.FachadaLogica.ApplicationExceptionFL("el usuario debe de ser  operador", Nothing)
            End If

            RecuperarOperacionEnCursoPara = mln.RecuperarOperacionEnCursoPara(pActor)

            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try



    End Function

    Public Function GuardarOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            mln.GuardarOperacion(pOperacion)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

            Return pOperacion
        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function ClasificarOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN, ByVal colEntidaes As AmvDocumentosDN.ColEntNegocioDN) As AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN
        'Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        'Try

        '    '1º guardar log de inicio
        '    mfh.EntradaMetodo(idSesion, pActor, mRec)

        '    '2º verificacion de permisos por rol de usuario
        '    pActor.Autorizado()

        '    '-----------------------------------------------------------------------------
        '    '3º creacion de la ln y ejecucion del metodo
        '    Dim mln As GDocEntrantesLN.GDocsLN
        '    mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
        '    ClasificarOperacion = mln.ClasificarOperacion(pOperacion, colEntidaes)
        '    '-----------------------------------------------------------------------------

        '    '4º guardar log de fin de metodo , con salidas excepcionales incluidas
        '    mfh.SalidaMetodo(idSesion, pActor, mRec)

        'Catch ex As Exception

        '    mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
        '    Throw ex

        'End Try


        Using New CajonHiloLN(mRec)





            Using tr As New Transaccion

                Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
                Try
                    mfh.EntradaMetodo(idSesion, pActor, mRec)
                    Dim mln As New GDocEntrantesLN.GDocsLN(Transaccion.Actual, Recurso.Actual)
                    ClasificarOperacion = mln.ClasificarOperacion(pOperacion, colEntidaes)
                    tr.Confirmar()
                    mfh.SalidaMetodo(idSesion, pActor, mRec)
                Catch ex As Exception
                    mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                    Throw
                End Try



            End Using






        End Using



    End Function

    Public Function ClasificarYCerrarOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            mln.ClasificarYCerrarOperacion(pOperacion)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

            Return pOperacion
        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function AnularOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            mln.AnularOperacion(pOperacion)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

            Return pOperacion
        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function IncidentarOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            mln.IncidentarOperacion(pOperacion)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

            Return pOperacion
        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function RechazarOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN) As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            mln.RechazarOperacion(pOperacion)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

            Return pOperacion
        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try


        '5º devolucion resultado
        'Return Nothing

    End Function

    Public Function AutorizadoConfigurarClienteSonda(ByVal idSesion As String, ByVal pdi As DatosIdentidadDN) As Boolean
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
        Dim Actor As PrincipalDN


        Try

            mfh.EntradaMetodo(idSesion, Nothing, mRec)


            Try
                '0º recuperar el usuario
                ' crear el usuario
                Dim uln As UsuariosLN
                uln = New UsuariosLN(Nothing, mRec)
                Actor = uln.ObtenerPrincipal(pdi)


                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, Actor, mRec)

                '2º verificacion de permisos por rol de usuario
                If Actor Is Nothing Then
                    Throw New ApplicationExceptionFL("no autorizado", Nothing)
                End If
                Actor.Autorizado()

                mfh.SalidaMetodo(idSesion, Actor, mRec)
                Return True

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, Actor, ex, "", mRec)
                Return False
            End Try


            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, Actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, Actor, ex, "", mRec)
            Throw ex

        End Try


        Throw New NotImplementedException

    End Function

    Public Function RecuperarOperacionxID(ByVal idOperacion As String, ByVal pActor As PrincipalDN, ByVal idSesion As String) As OperacionEnRelacionENFicheroDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            RecuperarOperacionxID = mln.RecuperarOperacionxID(idOperacion)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try

        '5º devolucion resultado
        'Return Nothing
    End Function


    Public Sub RegistrarFicheroIncidentado(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pDatosFicheroIncidentado As DatosFicheroIncidentado)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            mln.RegistrarFicheroIncidentado(pDatosFicheroIncidentado)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try

    End Sub

    Public Sub ProcesarColComandoOperacion(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pCol As AmvDocumentosDN.ColComandoOperacionDN)

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.GDocsLN
            mln = New GDocEntrantesLN.GDocsLN(mTL, mRec)
            mln.ProcesarColComandoOperacion(pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida, pCol)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try
    End Sub

    Public Sub EnviarMailFicheroIncidentados(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pDatosFicheroIncidentado As DatosFicheroIncidentado)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, pActor, mRec)

            '2º verificacion de permisos por rol de usuario
            pActor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo
            Dim mln As GDocEntrantesLN.MailsLN
            mln = New GDocEntrantesLN.MailsLN(mTL, mRec)
            mln.EnviarMailFicheroIncidentados(pDatosFicheroIncidentado)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, pActor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
            Throw ex

        End Try
    End Sub


#End Region

End Class
