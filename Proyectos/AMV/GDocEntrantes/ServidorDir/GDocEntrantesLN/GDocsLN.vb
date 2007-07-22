Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN

Imports Framework.Ficheros.FicherosDN
Imports Framework.Ficheros.FicherosLN

Imports AmvDocumentosDN
Imports GDocEntrantesAD

Public Class GDocsLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN



#Region "Constructor"

    Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region


#Region "CajonesDocumento"

    ''' <summary>
    ''' dado que debe tratarde de un cajon documento unido a una identificacion que es la misma que la de un ahuella fichero 
    ''' y este metodo indica que la vinculacion es incorrecta ha de ser porque la identificacion del fichero es incorrecta.
    ''' 
    ''' se marca la identificacion como incorrecta
    ''' </summary>
    ''' <param name="pCajonDocumentoDN"></param>
    ''' <remarks></remarks>
    Public Sub CajonDocumentoRechazarVinculacion(ByVal pCajonDocumentoDN As Framework.Ficheros.FicherosDN.CajonDocumentoDN)



        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Dim idd As Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN = pCajonDocumentoDN.IdentificacionDocumento
            Dim doc As HuellaFicheroAlmacenadoIODN = pCajonDocumentoDN.Documento
            pCajonDocumentoDN.Documento = Nothing
            doc.RechazarIndentificacion(idd)

            Me.Guardar(Of Framework.Ficheros.FicherosDN.CajonDocumentoDN)(pCajonDocumentoDN)
            Me.Guardar(Of Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)(doc)

            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try



    End Sub
    Public Function CajonDocumentoRechazarVinculacionyReVincular(ByVal pCajonDocumentoDN As Framework.Ficheros.FicherosDN.CajonDocumentoDN, ByVal pIdentificacionDocumentoEntrante As Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN) As HuellaFicheroAlmacenadoIODN



        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso


            Dim miIdentificacionDocumentoLN As New Framework.Ficheros.FicherosLN.IdentificacionDocumentoLN
            pIdentificacionDocumentoEntrante = miIdentificacionDocumentoLN.RecuperarOcrearIdentitific(pIdentificacionDocumentoEntrante.TipoFichero, pIdentificacionDocumentoEntrante.Identificacion)

            Dim iddocSaliente As Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN = pCajonDocumentoDN.IdentificacionDocumento


            Dim doc As HuellaFicheroAlmacenadoIODN = pCajonDocumentoDN.Documento
            pCajonDocumentoDN.Documento = Nothing
            doc.ReemplazarIndentificacion(pIdentificacionDocumentoEntrante, iddocSaliente)

            Me.Guardar(Of Framework.Ficheros.FicherosDN.CajonDocumentoDN)(pCajonDocumentoDN)
            Me.Guardar(Of Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)(doc)

            CajonDocumentoRechazarVinculacionyReVincular = doc
            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try



    End Function


#End Region





    Public Function RecuperarOperacionEnFicheroXid(ByVal id As String) As AmvDocumentosDN.RelacionENFicheroDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            RecuperarOperacionEnFicheroXid = gi.Recuperar(Of AmvDocumentosDN.RelacionENFicheroDN)(id)

            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try
    End Function

    ''' <summary>
    ''' este metodo permite que un cliente de sonda de entrada pueda dar de alta un nuevo fichero  en el sistema
    ''' recupera la ruta de almacemamiento vigente
    ''' guarda el fichero en la ruta de almacenamiento y la EntNegoioReferidoraDN a gaurdar 
    ''' 
    ''' el fichero puede crearse con referencia a una entidad de negocio o a un tipo de fichero(atraaves de una identificacion de fichero incompleta) a  hambas
    ''' </summary>
    ''' <param name="pDatosAltaFichero"></param>
    ''' <remarks></remarks>
    Public Sub AltaDocumento(ByVal pDatosAltaFichero As FicheroParaAlta)


        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Dim HuellaDoc As HuellaFicheroAlmacenadoIODN
            Dim RelacionENFichero As RelacionENFicheroDN
            'Dim EntidadNegocio As EntNegocioDN
            Dim TipoER As TipoEntNegoioDN
            Dim hfln As HuellaFicheroIOLN
            Dim raln As RutaAlmacenamientoFicherosLN

            HuellaDoc = New HuellaFicheroAlmacenadoIODN
            RelacionENFichero = New RelacionENFicheroDN
            'EntidadNegocio = Nothing

            raln = New RutaAlmacenamientoFicherosLN(tlproc, Me.mRec)

            ' cargar los valores de la huella de fichero
            HuellaDoc.Datos = pDatosAltaFichero.HuellaFichero.Datos
            HuellaDoc.RutaAlmacenamiento = raln.RecuperarRafActivo
            HuellaDoc.Nombre = pDatosAltaFichero.HuellaFichero.Nombre
            HuellaDoc.Extension = pDatosAltaFichero.HuellaFichero.Extension
            HuellaDoc.NombreOriginalFichero = pDatosAltaFichero.HuellaFichero.NombreOriginalFichero




            ' asicuar al canal correcto

            If Not pDatosAltaFichero.clanal.TipoCanal Is Nothing Then
                Dim tcad As New GDocEntrantesAD.TipoCanalAD(tlproc, Me.mRec)

                Dim tc As AmvDocumentosDN.TipoCanalDN = tcad.RecuperarXNombre(pDatosAltaFichero.clanal.TipoCanal.Nombre)
                If tc Is Nothing Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se pudo recuperar el canal adecuado")
                End If

                pDatosAltaFichero.clanal.TipoCanal = tc


            End If






            ' llenar los datos de la entidad de negocio (carpeta a la que pertenece)
            If Not pDatosAltaFichero.TipoEntidad Is Nothing AndAlso Not pDatosAltaFichero.TipoEntidad.ID Is Nothing AndAlso pDatosAltaFichero.TipoEntidad.ID <> "" Then
                ' recuperamos el tipo de entidad de negocio con los datos del servidor por si hubiera sido modificados que no de error
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
                TipoER = gi.Recuperar(Of TipoEntNegoioDN)(pDatosAltaFichero.TipoEntidad.ID)
                RelacionENFichero.TipoEntNegoio = TipoER
            End If


            ' cargar los datos de la relacion fichero entidad de negocio
            RelacionENFichero.HuellaFichero = HuellaDoc

            Dim operacion As OperacionEnRelacionENFicheroDN
            operacion = New OperacionEnRelacionENFicheroDN(Nothing, RelacionENFichero, New TipoOperacionREnFDN(TipoOperacionREnF.Crear))
            operacion.TipoCanal = pDatosAltaFichero.clanal.TipoCanal



            ' se crea una nueva huella de fichero en la nueva ubicacion del fichero 

            hfln = New HuellaFicheroIOLN(tlproc, Me.mRec)
            hfln.GuardarHuellayFichero(HuellaDoc)



            'Try
            operacion.EjecutarOperacionEndatos()
            Me.Guardar(Of OperacionEnRelacionENFicheroDN)(operacion)

            'Catch ex As Exception
            '    hfln = New FN.Ficheros.FicherosLN.HuellaFicheroIOLN(tlproc, Me.mRec)
            '    hfln.GuardarHuellayFichero(HuellaDoc)

            'End Try


            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try


    End Sub


    Public Sub RegistrarDocumentoAIR(ByVal pDatosDocumentoAIR As DatosDocumentoAIR)


        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Me.Guardar(Of DatosDocumentoAIR)(pDatosDocumentoAIR)
            ' mandar mail

            Dim mln As MailsLN
            mln = New MailsLN(tlproc, Me.mRec)
            mln.EnviarMailDocumentoRIA(pDatosDocumentoAIR)


            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try



    End Sub


    Public Sub RegistrarFicheroIncidentado(ByVal pDatosFicheroIncidentado As DatosFicheroIncidentado)




        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Me.Guardar(Of DatosFicheroIncidentado)(pDatosFicheroIncidentado)
            ' mandar mail

            Dim mln As MailsLN
            mln = New MailsLN(tlproc, Me.mRec)
            mln.EnviarMailFicheroIncidentados(pDatosFicheroIncidentado)


            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try



    End Sub

    ''' <summary>
    ''' seintenta gauadar el ficehro en la ruta de almacenamiento
    ''' </summary>
    ''' <param name="ptlproc"></param>
    ''' <param name="pHuellaDoc"></param>
    ''' <remarks></remarks>
    Private Sub GuardarFichero(ByVal ptlproc As ITransaccionLogicaLN, ByVal pHuellaDoc As HuellaFicheroAlmacenadoIODN)
        'se obtiene la ruta vigente de almacenamiento
        Dim rutaAlnmacen As RutaAlmacenamientoFicherosDN
        Dim raLN As RutaAlmacenamientoFicherosLN
        raLN = New RutaAlmacenamientoFicherosLN(ptlproc, Me.mRec)
        rutaAlnmacen = raLN.RecuperarRafActivo

        ' se copia le fichero de la huella en la ubicacion de almacenamiento vigente con su nuevo nombre


    End Sub

    Public Function RecupearNumDocPendientesClasificacionXTipoCanal(ByVal dts As DataSet) As DataSet




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ad As GDocEntrantesAD.OperacionEnRelacionENFicheroAD

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            ad = New GDocEntrantesAD.OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
            RecupearNumDocPendientesClasificacionXTipoCanal = ad.RecupearNumDocPendientesClasificacionXTipoCanal(dts)

            tlproc.Confirmar()


        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try







    End Function

    Public Function RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio(ByVal dts As DataSet) As DataSet




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ad As GDocEntrantesAD.OperacionEnRelacionENFicheroAD

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            ad = New GDocEntrantesAD.OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
            RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio = ad.RecupearNumDocPendientesClasificacionXTipoCanal(dts)

            tlproc.Confirmar()


        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try







    End Function

    Public Function RecuperarNumDocPendientesClasificaryPostClasificacion(ByVal dts As DataSet) As DataSet




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim ad As GDocEntrantesAD.OperacionEnRelacionENFicheroAD

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            ad = New GDocEntrantesAD.OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
            RecuperarNumDocPendientesClasificaryPostClasificacion = ad.RecuperarNumDocPendientesClasificaryPostClasificacion(dts)

            tlproc.Confirmar()


        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try







    End Function

    Public Function RecuperarOperacion(ByVal pActor As PrincipalDN, ByVal pIdOperacion As String) As OperacionEnRelacionENFicheroDN


        Dim pOperador As OperadorDN
        Dim tlproc As ITransaccionLogicaLN = Nothing
        ' Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Dim operacion As OperacionEnRelacionENFicheroDN

            ' Precondiciones

            '1º debe de exitir un operador
            pOperador = pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida

            If pOperador Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El operador no puede ser nothing")
            End If


            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            operacion = gi.Recuperar(Of AmvDocumentosDN.OperacionEnRelacionENFicheroDN)(pIdOperacion)


            RecuperarOperacion = operacion

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try





    End Function

    ''' <summary>
    '''  permite recuperar el documento que tine asignado un operador o el siguiente documento a procesar
    '''  este metodo requiere que se pase un operador
    ''' 1º intentara devolver la operacion que el operador tiene asignada
    ''' (si el operador dispone de tipos de documentos asociados se intentará suministrar uno de ese tipo)
    ''' 2º de no haber ninguna intentara buscar el primer documento de su tipo que no esta asignado a ninguna operacion abierta
    ''' 3º  de no haber ninguna intentara buscar el primer documento de CUALQUIER tipo que no esta asignado a ninguna operacion abierta
    '''     ''' el documento quedará vinculado a el operador hasta que el operador lo incidente o lo procese
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarOperacionAProcesar(ByVal pActor As PrincipalDN, ByVal pIdTipoCanal As String) As OperacionEnRelacionENFicheroDN
        Dim operador As OperadorDN
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim opad As OperacionEnRelacionENFicheroAD
        Dim relacion As RelacionENFicheroDN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            Dim operacion As OperacionEnRelacionENFicheroDN = Nothing

            ' Precondiciones

            '1º debe de exitir un operador
            operador = pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida

            If operador Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El operador no puede ser nothing")
            End If

            'Para evitar errores de concurrencia, forzamos que se guarde la relación
            Dim obj As Framework.DatosNegocio.IDatoPersistenteDN
            Dim objLN As GDocsLN

            For i As Integer = 0 To 3
                opad = New OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
                relacion = opad.RecuperarRelacionPendienteClasificacion(operador, pIdTipoCanal)

                If relacion IsNot Nothing Then
                    obj = relacion
                    obj.EstadoDatos = Framework.DatosNegocio.EstadoDatosDN.Modificado

                    Try
                        objLN = New GDocsLN(Nothing, mRec)
                        objLN.Guardar(Of AmvDocumentosDN.RelacionENFicheroDN)(relacion)
                        Exit For
                    Catch ex As Framework.DatosNegocio.NingunaFilaAfectadaException
                        Threading.Thread.Sleep(1000)
                    Catch ex As Exception
                        Throw
                    End Try
                Else
                    Exit For
                End If

                If i = 3 Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se ha podido recuperar un nuevo documento")
                End If
            Next

            '-------------------------------------------------------------------------

            If Not relacion Is Nothing Then
                operacion = New OperacionEnRelacionENFicheroDN(operador, relacion, New TipoOperacionREnFDN(TipoOperacionREnF.Clasificar))

                opad = New OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
                opad.Guardar(operacion)
            End If

            RecuperarOperacionAProcesar = operacion

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Function

    Public Function RecuperarOperacionAPostProcesar(ByVal pActor As PrincipalDN, ByVal pTipoEntNegoio As TipoEntNegoioDN, ByVal pIdentificadorentidadNegocio As String) As OperacionEnRelacionENFicheroDN
        Dim operador As OperadorDN
        Dim relacion As AmvDocumentosDN.RelacionENFicheroDN = Nothing
        Dim operacion As OperacionEnRelacionENFicheroDN = Nothing
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim opad As OperacionEnRelacionENFicheroAD

        Try
            tlproc = Me.ObtenerTransaccionDeProceso



            ' Precondiciones

            '1º debe de exitir un operador
            operador = pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida

            If operador Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El operador no puede ser nothing")
            End If

            'Para evitar errores de concurrencia, forzamos que se guarde la relación
            Dim obj As Framework.DatosNegocio.IDatoPersistenteDN
            Dim objLN As GDocsLN

            For i As Integer = 0 To 3
                opad = New OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
                If pTipoEntNegoio Is Nothing Then
                    relacion = opad.RecuperarRelacionPostClasificacion(operador, operador.ColTipoEntNegoio, pIdentificadorentidadNegocio)
                Else
                    Dim colentneg As New AmvDocumentosDN.ColTipoEntNegoioDN
                    ' colentneg.Add(pTipoEntNegoio)
                    colentneg.Add(operador.ColTipoEntNegoio.RecuperarxID(pTipoEntNegoio.ID))
                    relacion = opad.RecuperarRelacionPostClasificacion(operador, colentneg, pIdentificadorentidadNegocio)
                End If

                If relacion IsNot Nothing Then
                    obj = relacion
                    obj.EstadoDatos = Framework.DatosNegocio.EstadoDatosDN.Modificado

                    Try
                        objLN = New GDocsLN(Nothing, mRec)
                        objLN.Guardar(Of AmvDocumentosDN.RelacionENFicheroDN)(relacion)
                        Exit For

                    Catch ex As Framework.DatosNegocio.NingunaFilaAfectadaException
                        Threading.Thread.Sleep(1000)
                    Catch ex As Exception
                        Throw
                    End Try
                Else
                    Exit For
                End If

                If i = 3 Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se ha podido recuperar un nuevo documento")
                End If

            Next

            '-------------------------------------------------------------------------

            If Not relacion Is Nothing Then
                operacion = New OperacionEnRelacionENFicheroDN(operador, relacion, New TipoOperacionREnFDN(TipoOperacionREnF.Clasificar))
                opad = New OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
                opad.Guardar(operacion)

            End If

            RecuperarOperacionAPostProcesar = operacion

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try



    End Function

    Public Function RecuperarOperacionEnCursoPara(ByVal pActor As PrincipalDN) As OperacionEnRelacionENFicheroDN
        Dim pOperador As OperadorDN
        Dim tlproc As ITransaccionLogicaLN = Nothing
        ' Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            Dim operacion As OperacionEnRelacionENFicheroDN

            ' Precondiciones

            '1º debe de existir un operador
            pOperador = pActor.UsuarioDN.HuellaEntidadUserDN.EntidadReferida

            If pOperador Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El operador no puede ser nothing")
            End If

            'Dim relacion As AmvDocumentosDN.RelacionENFicheroDN

            Dim opad As OperacionEnRelacionENFicheroAD
            opad = New OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)

            Dim lanzarEx As Boolean = False
            operacion = opad.RecuperarOperacionEnCursoPara(pOperador, lanzarEx)

            If lanzarEx Then
                UnificarEstadoOperacionesFicheros()
                operacion = opad.RecuperarOperacionEnCursoPara(pOperador, lanzarEx)
            End If

            RecuperarOperacionEnCursoPara = operacion

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try


    End Function

    Private Function CrearNuevaOperacionApartirDeUnaRelacionEnCursoDeEstadoEspecificado(ByVal tlproc As ITransaccionLogicaLN, ByVal pOperador As OperadorDN, ByVal pTipoOperARecuperar As AmvDocumentosDN.TipoOperacionREnF, ByVal pTipoOperaCrear As AmvDocumentosDN.TipoOperacionREnF) As OperacionEnRelacionENFicheroDN

        ' si no tubiera  una operacion abierta se crearia una nueva para el tio
        Dim operacion As OperacionEnRelacionENFicheroDN
        Dim relad As RelacionENFicheroAD
        relad = New RelacionENFicheroAD(tlproc, Me.mRec)


        '1º recuperar la oepracion no cerrara y no abierta , en proceso


        'Dim opad As OperacionEnRelacionENFicheroAD
        'opad = New OperacionEnRelacionENFicheroAD(tlproc, Me.mRec)
        'operacion = opad.RecuperarOperacionCerrradaEnEstado(pOperador, pTipoOperARecuperar)


        ' recuperar una relacion en estado clasificando que nos este ligada  a una operaciona bierta

        Dim renf As GDocEntrantesAD.RelacionENFicheroAD
        Dim relEnf As AmvDocumentosDN.RelacionENFicheroDN
        renf = New GDocEntrantesAD.RelacionENFicheroAD(tlproc, Me.mRec)
        relEnf = renf.RecuperarPrimeraRelacionENFicheroEnEstado(pOperador.ColTipoEntNegoio, EstadosRelacionENFichero.Clasificando)
        relEnf = renf.RecuperarPrimeraRelacionENFicheroEnEstado(Nothing, EstadosRelacionENFichero.Clasificando)



        If relEnf Is Nothing Then
            ' en este caso no hay ficheros pendientes de procesarse en el servidor
            CrearNuevaOperacionApartirDeUnaRelacionEnCursoDeEstadoEspecificado = Nothing

        Else

            operacion = New OperacionEnRelacionENFicheroDN(pOperador, relEnf, New TipoOperacionREnFDN(pTipoOperaCrear))
            CrearNuevaOperacionApartirDeUnaRelacionEnCursoDeEstadoEspecificado = operacion

        End If
    End Function

    Private Function CrearNuevaOperacion(ByVal tlproc As ITransaccionLogicaLN, ByVal pOperador As OperadorDN, ByVal pTipoOper As TipoOperacionREnF) As OperacionEnRelacionENFicheroDN

        ' si no tubiera  una operacion abierta se crearia una nueva para el tio
        Dim operacion As OperacionEnRelacionENFicheroDN
        Dim relacEnF As AmvDocumentosDN.RelacionENFicheroDN
        Dim relad As RelacionENFicheroAD
        relad = New RelacionENFicheroAD(tlproc, Me.mRec)


        '1º recuperar la oepracion no cerrara y no abierta , en proceso




        ' recuperar una relacion recien subida que no haya sido todabia asignada a una operación
        relacEnF = relad.RecuperarPrimeraRelacionENFichero(pOperador.ColTipoEntNegoio)


        If relacEnF Is Nothing Then
            ' en este caso no hay ficheros pendientes de procesarse en el servidor
            CrearNuevaOperacion = Nothing

        Else
            ' como si hay una relacion pendiente de ser usada se crea una oepracion apra ella, se guarda y se delvuevle al operador
            ' recuperar la razon de creacion
            'Dim top As AmvDocumentosDN.TipoOperacionREnFDN
            'Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            'top = gi.Recuperar(Of AmvDocumentosDN.TipoOperacionREnFDN)(pTipoOper)

            operacion = New OperacionEnRelacionENFicheroDN(pOperador, relacEnF, New TipoOperacionREnFDN(pTipoOper))
            CrearNuevaOperacion = operacion

        End If
    End Function

    Public Function ClasificarOperacion(ByVal pOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN, ByVal colEntidaes As AmvDocumentosDN.ColEntNegocioDN) As AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN



        Dim col As AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN
        col = New AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN
        ClasificarOperacion = col

        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            If colEntidaes IsNot Nothing AndAlso colEntidaes.Count > 0 Then


                Dim entidad As AmvDocumentosDN.EntNegocioDN

                For Each entidad In colEntidaes

                    Dim miOperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
                    miOperacion = pOperacion.Clone
                    miOperacion.RelacionENFichero.TipoEntNegoio = entidad.TipoEntNegocioReferidora

                    ClasificarOperacion(miOperacion)
                    col.Add(miOperacion)
                Next

            Else
                ClasificarOperacion(pOperacion)
                ClasificarOperacion.Add(pOperacion)
            End If

            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try








    End Function

    ''' <summary>
    ''' permite guardar una operacion y la cierra
    ''' Precondiciones si la operacion implica el cierre de la entidad de negocio se verifica que que su estado pueda ser cerrado
    ''' fundamentalemtne exiten dos modos de clasificacion apra la modificacion
    ''' 
    ''' 1º el cambio de entidad de negocio relacioanda o la clasificación a una de ellas
    ''' 
    ''' 2º la identificacion
    ''' 
    ''' 3º la vinculacion con cajones documento
    ''' 
    ''' 
    ''' 
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub ClasificarOperacion(ByVal pOperacion As OperacionEnRelacionENFicheroDN)




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso
            ' si se queire cerrar la operacion verificar que alcanza el estado solicitado y poner alor a la FF

            If pOperacion.TipoOperacionREnF.Valor <> TipoOperacionREnF.Clasificar Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Esta metodo solo permite clasificar operaciones sin cerrarla")
            End If


            Dim hfichero As Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN = pOperacion.RelacionENFichero.HuellaFichero
            ' recuperar todos los identificadores pendientes de crear o recuperar de la base de dats
            Dim idln As New Framework.Ficheros.FicherosLN.IdentificacionDocumentoLN

            Dim colidentificables As Framework.Ficheros.FicherosDN.ColIdentificacionDocumentoDN = hfichero.Colidentificaciones.RecuperarIdentificables
            hfichero.Colidentificaciones.EliminarEntidadDN(colidentificables, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
            For Each ident As Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN In colidentificables
                hfichero.Colidentificaciones.Add(idln.RecuperarOcrearIdentitific(ident.TipoFichero, ident.Identificacion))
            Next




            pOperacion.EjecutarOperacionEndatos()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pOperacion)

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try


    End Sub

    Public Sub ClasificarYCerrarOperacion(ByVal pOperacion As OperacionEnRelacionENFicheroDN)




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso
            ' si se queire cerrar la operacion verificar que alcanza el estado solicitado y poner alor a la FF

            If pOperacion.TipoOperacionREnF.Valor <> TipoOperacionREnF.ClasificarYCerrar Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Esta metodo solo permite clasificar Y cerrar operaciones sin cerrarla")
            End If





            pOperacion.EjecutarOperacionEndatos()







            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pOperacion)

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try


    End Sub

    Public Sub AnularOperacion(ByVal pOperacion As OperacionEnRelacionENFicheroDN)




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        '    Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso
            ' si se queire cerrar la operacion verificar que alcanza el estado solicitado y poner alor a la FF

            If pOperacion.TipoOperacionREnF.Valor <> TipoOperacionREnF.Anular Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Esta metodo solo permite clasificar Y cerrar operaciones sin cerrarla")
            End If

            pOperacion.EjecutarOperacionEndatos()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pOperacion)


            ' mandar mail


            Dim datosMail As DatosDocumentoAIR
            datosMail = New DatosDocumentoAIR
            datosMail.Operacion = pOperacion
            datosMail.Fecha = DateTime.Now.ToString
            datosMail.Comentario = " se solicitó la anulación del fichero"


            Dim mln As MailsLN
            mln = New MailsLN(tlproc, Me.mRec)
            mln.EnviarMailDocumentoRIA(datosMail)


            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try


    End Sub

    Public Sub IncidentarOperacion(ByVal pOperacion As OperacionEnRelacionENFicheroDN)




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso
            ' si se queire cerrar la operacion verificar que alcanza el estado solicitado y poner alor a la FF

            If pOperacion.TipoOperacionREnF.Valor <> TipoOperacionREnF.Incidentar Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Este metodo solo permite operaciones Incidentar  ")
            End If

            pOperacion.EjecutarOperacionEndatos()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pOperacion)



            Dim datosMail As DatosDocumentoAIR
            datosMail = New DatosDocumentoAIR
            datosMail.Operacion = pOperacion
            datosMail.Fecha = DateTime.Now.ToString
            datosMail.Comentario = " se solicitó Incidentar el fichero"


            Dim mln As MailsLN
            mln = New MailsLN(tlproc, Me.mRec)
            mln.EnviarMailDocumentoRIA(datosMail)



            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try


    End Sub

    Public Sub RechazarOperacion(ByVal pOperacion As OperacionEnRelacionENFicheroDN)




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso
            ' si se queire cerrar la operacion verificar que alcanza el estado solicitado y poner alor a la FF

            If pOperacion.TipoOperacionREnF.Valor <> TipoOperacionREnF.Rechazar Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Este metodo solo permite operaciones Rechazar  ")
            End If

            'If pOperacion.RelacionENFichero.Estado <> Framework.DatosNegocio.EstadoDatosDN.SinModificar Then
            '    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Este metodo solo permite operaciones Rechazar y no puede estar modicado el objeto asociado  ")
            'End If



            ' garantizar que no se modifica
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            Dim relacionEnFichero As RelacionENFicheroDN = gi.Recuperar(pOperacion.RelacionENFichero.ID, pOperacion.RelacionENFichero.GetType)
            If Not relacionEnFichero.FechaModificacion = pOperacion.RelacionENFichero.FechaModificacion Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Error de concurrencia")
            End If
            pOperacion.RelacionENFichero = relacionEnFichero



            pOperacion.EjecutarOperacionEndatos()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pOperacion)




            Dim datosMail As DatosDocumentoAIR
            datosMail = New DatosDocumentoAIR
            datosMail.Operacion = pOperacion
            datosMail.Fecha = DateTime.Now.ToString
            datosMail.Comentario = " se solicitó Rechazar el fichero"


            Dim mln As MailsLN
            mln = New MailsLN(tlproc, Me.mRec)
            mln.EnviarMailDocumentoRIA(datosMail)



            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try


    End Sub


    Public Sub ProcesarColComandoOperacion(ByVal pOperador As OperadorDN, ByVal pCol As AmvDocumentosDN.ColComandoOperacionDN)


        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try

            tlproc = Me.ObtenerTransaccionDeProceso


            Dim op As AmvDocumentosDN.ComandoOperacionDN
            If pCol IsNot Nothing AndAlso pCol.Count > 0 Then
                For Each op In pCol
                    Try
                        Dim gdln As New GDocEntrantesLN.GDocsLN(Nothing, Me.mRec)
                        gdln.ProcesarComandoOperacion(pOperador, op)
                    Catch ex As Exception
                        System.Diagnostics.Debug.WriteLine(ex.Message)
                    End Try

                Next

                'Se llama al proceso unificador de estado de las operaciones para evitar que se quede alguna
                'operación no cerrada con su documento correspondiente cerrado
                UnificarEstadoOperacionesFicheros()

            End If

            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try




    End Sub


    Public Sub ProcesarComandoOperacion(ByVal pOperador As OperadorDN, ByVal pOp As AmvDocumentosDN.ComandoOperacionDN)



        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso


            Dim rel As AmvDocumentosDN.RelacionENFicheroDN
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            rel = gi.Recuperar(Of RelacionENFicheroDN)(pOp.IDRelacion)


            If rel Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("La relación no puede ser nothing")
            End If

            Dim op As OperacionEnRelacionENFicheroDN
            Dim gdocs As New GDocsLN(Nothing, Me.mRec)

            Try
                op = New OperacionEnRelacionENFicheroDN(pOperador, rel, New TipoOperacionREnFDN(TipoOperacionREnF.FijarEstado))
                op.Nombre = pOp.EstadoSolicitado

                op.FijarEstadoRelacion(pOp.EstadoSolicitado)
                gdocs.GuardarOperacion(op)
                pOp.Resultado = True
                pOp.Mensaje = ""

            Catch ex As Exception
                pOp.Resultado = False
                pOp.Mensaje = "Error: " & ex.Message


            End Try

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                pOp.Resultado = False
                pOp.Mensaje = "Error: " & e.Message


                tlproc.Cancelar()
            End If
            'Throw
        End Try


    End Sub

    Public Function RecuperarColCajonDocumentoCoincidentes(ByVal pHf As Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN


        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso
            ' si se queire cerrar la operacion verificar que alcanza el estado solicitado y poner valor a la FF



            Dim cdln As New Framework.Ficheros.FicherosLN.CajonDocumentoLN




            'pOperacion.EjecutarOperacionEndatos()

            'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            'gi.Guardar(pOperacion)

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try





    End Function



    Public Sub GuardarOperacion(ByVal pOperacion As OperacionEnRelacionENFicheroDN)




        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Dim mensaje As String

        Try

            tlproc = Me.ObtenerTransaccionDeProceso
            ' si se queire cerrar la operacion verificar que alcanza el estado solicitado y poner valor a la FF

            'If pOperacion.TipoOperacionREnF.Valor = TipoOperacionREnF.Anular orelse pOperacion.TipoOperacionREnF.Valor =TipoOperacionREnF.Rechazar orelse Then
            '    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Esta metodo solo permite clasificar operaciones sin cerrarla")
            'End If

            pOperacion.EjecutarOperacionEndatos()

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pOperacion)

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw
        End Try







    End Sub

    Public Function RecuperarOperacionxID(ByVal idOperacion As String) As OperacionEnRelacionENFicheroDN
        Return MyBase.Recuperar(Of OperacionEnRelacionENFicheroDN)(idOperacion)
    End Function


    ''' <summary>
    ''' Proceso que unifica el estado de las operaciones a cerrado si el documento relacionado está cerrado
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub UnificarEstadoOperacionesFicheros()
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim listaOp As IList(Of OperacionEnRelacionENFicheroDN)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = ObtenerTransaccionDeProceso()

            'Se recuperan las operaciones no cerradas cuyo estado del fichero sea cerrado
            listaOp = RecuperarListaCondicional(Of OperacionEnRelacionENFicheroDN)(New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("vwUnificacionEstadoOperacionesFichero", Nothing, Nothing))

            'Se cierran las operaciones
            For Each operacion As OperacionEnRelacionENFicheroDN In listaOp
                'operacion.CancelarOperacionRelacionCerrada()
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, mRec)
                operacion.Cancelada = True
                gi.Guardar(operacion)
            Next

            tlproc.Confirmar()

        Catch ex As Exception
            If tlproc IsNot Nothing Then
                tlproc.Cancelar()
            End If

            Throw
        End Try

    End Sub

End Class
