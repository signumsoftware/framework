Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN
Imports MotorBusquedaBasicasDN

Public Class GestorBusquedaFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
        MyBase.New(tl, rec)
    End Sub

#End Region





    Public Function RecuperarEstructuraVista(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pParametroCargaEstructura As ParametroCargaEstructuraDN) As MotorBusquedaDN.EstructuraVistaDN
        'Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        'Try

        '    '1º guardar log de inicio
        '    mfh.EntradaMetodo(idSesion, pActor, mRec)

        '    '2º verificacion de permisos por rol de usuario
        '    ' pActor.Autorizado()

        '    '-----------------------------------------------------------------------------
        '    '3º creacion de la ln y ejecucion del metodo
        '    Dim mln As MotorBusquedaLN.GestorBusquedaLN

        '    mln = New MotorBusquedaLN.GestorBusquedaLN(mTL, mRec)
        '    RecuperarEstructuraVista = mln.RecuperarEstructuraVista(pParametroCargaEstructura)
        '    '-----------------------------------------------------------------------------

        '    '4º guardar log de fin de metodo , con salidas excepcionales incluidas
        '    mfh.SalidaMetodo(idSesion, pActor, mRec)

        'Catch ex As Exception

        '    mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
        '    Throw

        'End Try


        ''5º devolucion resultado
        ''Return Nothing





        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try

                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2º verificacion de permisos por rol de usuario
                ' pActor.Autorizado()

                '-----------------------------------------------------------------------------
                '3º creacion de la ln y ejecucion del metodo
                Dim mln As MotorBusquedaLN.GestorBusquedaLN

                mln = New MotorBusquedaLN.GestorBusquedaLN
                RecuperarEstructuraVista = mln.RecuperarEstructuraVista(pParametroCargaEstructura)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception

                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw

            End Try



        End Using





    End Function


    Public Function RecuperarDatos(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pFiltro As MotorBusquedaDN.FiltroDN) As DataSet
        'Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        'Try

        '    '1º guardar log de inicio
        '    mfh.EntradaMetodo(idSesion, pActor, mRec)

        '    '2º verificacion de permisos por rol de usuario
        '    ' pActor.Autorizado()

        '    '-----------------------------------------------------------------------------
        '    '3º creacion de la ln y ejecucion del metodo
        '    Dim mln As MotorBusquedaLN.GestorBusquedaLN

        '    mln = New MotorBusquedaLN.GestorBusquedaLN(mTL, mRec)
        '    RecuperarDatos = mln.RecuperarDatos(pFiltro)
        '    '-----------------------------------------------------------------------------

        '    '4º guardar log de fin de metodo , con salidas excepcionales incluidas
        '    mfh.SalidaMetodo(idSesion, pActor, mRec)

        'Catch ex As Exception

        '    mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
        '    Throw

        'End Try


        ''5º devolucion resultado
        ''Return Nothing




        Using New CajonHiloLN(mRec)

            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()


            Try

                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2º verificacion de permisos por rol de usuario
                ' pActor.Autorizado()

                '-----------------------------------------------------------------------------
                '3º creacion de la ln y ejecucion del metodo
                Dim mln As MotorBusquedaLN.GestorBusquedaLN

                mln = New MotorBusquedaLN.GestorBusquedaLN
                RecuperarDatos = mln.RecuperarDatos(pFiltro)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception

                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw

            End Try



        End Using






    End Function


    Public Function RecuperarTiposQueImplementan(ByVal idSesion As String, ByVal pActor As PrincipalDN, ByVal pNombreCompletoClase As String, ByVal nombrePropiedad As String) As Framework.TiposYReflexion.DN.ColVinculoClaseDN

        Using New CajonHiloLN(mRec)

            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()


            Try

                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2º verificacion de permisos por rol de usuario
                ' pActor.Autorizado()

                '-----------------------------------------------------------------------------
                '3º creacion de la ln y ejecucion del metodo
                Dim mln As MotorBusquedaLN.GestorBusquedaLN

                mln = New MotorBusquedaLN.GestorBusquedaLN
                RecuperarTiposQueImplementan = mln.RecuperarTiposQueImplementan(pNombreCompletoClase, nombrePropiedad)
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception

                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw

            End Try



        End Using

    End Function







End Class