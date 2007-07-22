#Region "Importaciones"

Imports Framework.LogicaNegocios
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.AccesoDatos.MotorAD.AD
Imports Framework.Usuarios.DN
Imports Framework.Usuarios.AD

#End Region

Public Class UsuariosLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Constructores"
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region

#Region "Métodos"


    Public Function CrearAdministradorTotal(ByVal pNick As String, ByVal pClave As String) As PrincipalDN






        Using tr As New Transaccion


            ' crear un rol con todos los metos de sistema y todas las operaciones 
            Dim rolln As New RolLN(Transacciones.Transaccion.Actual, Transacciones.Recurso.Actual)
            Dim colRoles As New ColRolDN

            colRoles.Add(rolln.GeneraRolAutorizacionTotal("Administrador Total"))


            Dim di As New DatosIdentidadDN(pNick, pClave)
            Dim user As New UsuarioDN(pNick, True)
            Dim prin As New PrincipalDN(pNick, user, colRoles)



            CrearAdministradorTotal = Me.AltaPrincipal(prin, di)


            tr.Confirmar()

        End Using








    End Function

    Public Function ObtenerPrincipal(ByVal di As DatosIdentidadDN) As PrincipalDN

        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            Dim principal As PrincipalDN
            Dim uad As UsuariosAD
            uad = New UsuariosAD(tlproc, mRec)

            principal = uad.ObtenerPrincipal(di)

            Me.CargarEntidadReferidaPrincipal(principal)

            tlproc.Confirmar()
            Return principal

        Catch ex As Exception
            tlproc.Cancelar()
            Throw ex
        End Try

    End Function

    Public Function ObtenerPrincipal(ByVal pId As String) As PrincipalDN

        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim gi As GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            gi = New GestorInstanciacionLN(tlproc, mRec)
            ObtenerPrincipal = gi.Recuperar(Of PrincipalDN)(pId)
            tlproc.Confirmar()

        Catch ex As Exception
            tlproc.Cancelar()
            Throw ex
        End Try

    End Function

    Public Sub CargarEntidadReferidaPrincipal(ByVal pPrincipal As PrincipalDN)

        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim gi As GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            If Not pPrincipal Is Nothing AndAlso Not pPrincipal.UsuarioDN.HuellaEntidadUserDN Is Nothing Then
                gi = New GestorInstanciacionLN(tlproc, mRec)
                gi.Recuperar(pPrincipal.UsuarioDN.HuellaEntidadUserDN)
            End If

            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN = pPrincipal
            idp.EstadoDatos = DatosNegocio.EstadoDatosDN.SinModificar


            tlproc.Confirmar()

        Catch ex As Exception
            tlproc.Cancelar()
            Throw ex
        End Try

    End Sub

    Public Function RecuperarListado() As DataSet


        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            '1º obtener la transaccion de procedimiento
            tlproc = Me.ObtenerTransaccionDeProceso()

            Dim ad As UsuariosAD
            ad = New UsuariosAD(tlproc, Me.mRec)

            RecuperarListado = ad.RecuperarListado()

            '4º Verificar las postCondiciones de negocio para el procedimiento
            tlproc.Confirmar() '5º confirmar transaccion si tudo fue bien 

        Catch ex As Exception
            tlproc.Cancelar() ' *º si se dio una excepcion en mi o en alguna de mis dependencias cancelar la transaccion y propagar la excepcion
            Throw ex
        End Try

    End Function



    Public Function GuardarPrincipal(ByVal pPrincipal As PrincipalDN) As PrincipalDN
        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            '1º obtener la transaccion de procedimiento
            tlproc = ObtenerTransaccionDeProceso()

            '2º Verificar las precondiciones de negocio para el procedimiento
            ' si alguna de las condiciones no es cierta suele tener que generarse una excepción

            '3 Realiar las operaciones propieas del procedimiento
            ' pueden implicar codigo propio ollamadas a otros LN,  AD, AS 

            'Se unifican los datos del usuario y datos de identidad
            pPrincipal.Nombre = pPrincipal.UsuarioDN.Nombre

            Dim gi As GestorInstanciacionLN
            gi = New GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pPrincipal)

            GuardarPrincipal = pPrincipal

            '4º Verificar las postCondiciones de negocio para el procedimiento
            tlproc.Confirmar() '5º confirmar transaccion si tudo fue bien 

        Catch ex As Exception
            tlproc.Cancelar() ' *º si se dio una excepcion en mi o en alguna de mis dependencias cancelar la transaccion y propagar la excepcion
            Throw ex
        End Try
    End Function

    ''' <summary>
    ''' Función que guarda el objeto principal y el objeto Datos de identidad. Para ello se borra previamente
    ''' cualquier objeto datos de identidad que tuviera asociado el nick del principal y se guarda el nuevo
    ''' datos de identidad. Así mismo se comprueba que no exista ya un datos de identidad con el mismo nick.
    ''' </summary>
    ''' <param name="pDI"></param>
    ''' <param name="pPrincipal"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GuardarPrincipal(ByVal pPrincipal As PrincipalDN, ByVal pDI As DatosIdentidadDN) As PrincipalDN
        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            '1º obtener la transaccion de procedimiento
            tlproc = ObtenerTransaccionDeProceso()

            '2º Verificar las precondiciones de negocio para el procedimiento
            ' si alguna de las condiciones no es cierta suele tener que generarse una excepción
            If pPrincipal Is Nothing OrElse pPrincipal.UsuarioDN Is Nothing Then
                Throw New ApplicationExceptionLN("No puede guardarse un principal nulo, o con su usuario nulo")
            End If

            '3 Realizar las operaciones propias del procedimiento
            ' pueden implicar código propio o llamadas a otros LN, AD, AS 

            'Se borra el objeto datos de identidad antiguo
            Dim nickOld As String
            nickOld = pPrincipal.UsuarioDN.Nombre

            If Not String.IsNullOrEmpty(nickOld) Then
                BorrarDatosIdentidad(nickOld)
            End If

            'Se comprueba si existe un objeto datos de identidad con el mismo nick del nuevo
            If Me.ExisteNickDatosIdentidad(pDI.Nick) Then
                Throw New ApplicationExceptionLN("Ya existe un usuario con el mismo nick")
            End If

            GuardarDatosIdentidad(pDI)

            'Se unifican los datos del usuario y datos de identidad
            pPrincipal.UsuarioDN.Nombre = pDI.Nick

            'Se guarda el objeto principal
            GuardarPrincipal = GuardarPrincipal(pPrincipal)

            '4º Verificar las postCondiciones de negocio para el procedimiento
            tlproc.Confirmar() '5º confirmar transaccion si tudo fue bien 

        Catch ex As Exception
            tlproc.Cancelar() ' *º si se dio una excepcion en mi o en alguna de mis dependencias cancelar la transaccion y propagar la excepcion
            Throw ex
        End Try
    End Function

    Private Sub BorrarDatosIdentidad(ByVal pNick As String)
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ad As UsuariosAD

        Try
            'obtener la transaccion de procedimiento
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            ad = New UsuariosAD(ProcTl, Me.mRec)

            ad.BorrarDatosIdentidad(pNick)

            'Verificar las postCondiciones de negocio para el procedimiento
            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Sub

    Private Function ExisteNickDatosIdentidad(ByVal pNick As String) As Boolean
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ad As UsuariosAD

        Try
            'obtener la transaccion de procedimiento
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            ad = New UsuariosAD(ProcTl, Me.mRec)

            ExisteNickDatosIdentidad = ad.ExisteNickDatosIdentidad(pNick)

            'Verificar las postCondiciones de negoio para el procedimiento
            ProcTl.Confirmar()
        Catch ex As Exception
            ProcTl.Cancelar() ' *º si se dio una excepcion en mi o en alguna de mis dependencias cancelar la transaccion y propagar la excepcion
            Throw ex
        End Try
    End Function

    Private Function GuardarDatosIdentidad(ByVal pDI As DatosIdentidadDN) As DatosIdentidadDN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN

        Try
            '1º obtener la transaccion de procedimiento
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            '2º Verificar las precondiciones de negocio para el procedimiento
            ' si alguna de las condiciones no es cierta suele tener que generarse una excepción


            '3 Realizar las operaciones propias del procedimiento, pueden implicar codigo propio o llamadas a otros LN,  AD, AS 
            Dim gi As GestorInstanciacionLN
            gi = New GestorInstanciacionLN(ProcTl, Me.mRec)
            gi.Guardar(pDI)

            GuardarDatosIdentidad = pDI

            '4º Verificar las postCondiciones de negocio para el procedimiento
            ProcTl.Confirmar() '5º confirmar transaccion si tudo fue bien 
        Catch ex As Exception
            ProcTl.Cancelar() ' *º si se dio una excepcion en mi o en alguna de mis dependencias cancelar la transaccion y propagar la excepcion
            Throw ex
        End Try
    End Function

    Public Function BajaPrincipal(ByVal pPrincipal As PrincipalDN) As PrincipalDN
        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim gi As GestorInstanciacionLN
        Dim eb As Framework.DatosNegocio.IDatoPersistenteDN

        Try
            tlproc = ObtenerTransaccionDeProceso()

            'Se establece el estado de baja del principal, y PrincipalN se encarga de poner en baja al UsuarioDN
            eb = pPrincipal
            eb.Baja = True

            Dim nickOld As String
            nickOld = pPrincipal.UsuarioDN.Nombre

            pPrincipal.UsuarioDN.Nombre = ""

            gi = New GestorInstanciacionLN(tlproc, mRec)
            gi.Baja(pPrincipal)

            'Además se elimina el objeto DatosIdentidadDN asociado
            Me.BorrarDatosIdentidad(nickOld)

            tlproc.Confirmar()

            Return pPrincipal

        Catch ex As Exception
            tlproc.Cancelar()
            Throw ex
        End Try
    End Function


    Public Function AltaPrincipal(ByVal pPrincipal As PrincipalDN, ByVal pDatosIdentidad As DatosIdentidadDN) As PrincipalDN
        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        ' Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim eb As Framework.DatosNegocio.IDatoPersistenteDN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            'Se comprueba si existe un objeto datos de identidad con el mismo nick del nuevo
            If Me.ExisteNickDatosIdentidad(pPrincipal.UsuarioDN.Nombre) Then
                Throw New ApplicationExceptionLN("Ya existe un usuario con el mismo nick")
            End If


            'Se establece el estado de alta del principal, y PrincipalN se encarga de poner en alta al UsuarioDN
            eb = pPrincipal
            eb.Baja = False

            GuardarPrincipal(pPrincipal)

            'Además hay que crear el objeto DatosIdentidadDN asociado
            Me.GuardarDatosIdentidad(pDatosIdentidad)

            tlproc.Confirmar()

            Return pPrincipal

        Catch ex As Exception
            tlproc.Cancelar()
            Throw ex
        End Try
    End Function





    Public Function RecuperarPrincipalxNick(ByVal pNick As String) As PrincipalDN
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim uAD As UsuariosAD

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            uAD = New UsuariosAD(tlproc, Me.mRec)
            RecuperarPrincipalxNick = uAD.RecuperarPrincipalxNick(pNick)

            tlproc.Confirmar()

        Catch ex As Exception
            If tlproc IsNot Nothing Then
                tlproc.Cancelar()
            End If
            Throw ex
        End Try
    End Function

    Public Sub BajaxIdEntidadUser(ByVal idEU As String)
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim miColPrincipal As IList(Of PrincipalDN)

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            miColPrincipal = MyBase.RecuperarListaCondicional(Of PrincipalDN)(New ConstructorBusquedaCampoStringAD("vwPrincipalxEntidadUser", "idEntidadUser", idEU))
            For Each principal As PrincipalDN In miColPrincipal
                BajaPrincipal(principal)
            Next

            tlproc.Confirmar()

        Catch ex As Exception
            If tlproc IsNot Nothing Then
                tlproc.Cancelar()
            End If
            Throw ex
        End Try


    End Sub


    Public Function RecuperarPrincipalxEntidadUser(ByVal tipoEnt As System.Type, ByVal idEntidad As String) As PrincipalDN
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim uAD As UsuariosAD

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            uAD = New UsuariosAD(tlproc, Me.mRec)
            RecuperarPrincipalxEntidadUser = uAD.RecuperarPrincipalxEntidadUser(tipoEnt, idEntidad)

            tlproc.Confirmar()

        Catch ex As Exception
            If tlproc IsNot Nothing Then
                tlproc.Cancelar()
            End If
            Throw ex
        End Try
    End Function

#End Region

End Class
