Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Ficheros
Public Class HuellaFicheroIOLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Contructor"
    Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region


    ''' <summary>
    ''' guarda una huella de fichero que debe estar gargada en la base de datos
    ''' y el ficehro  en la carpeta de archivo que le corresponda
    ''' si durante el proceso de guarda del archivo se produce un errores relacioandos con la ruta de almacenamiento
    ''' error1: La ruta no existe --> se marca la ruta como incidentada y se solicita la siguiente activa y se repite la operacion
    ''' error2: no  hay espacio suficiente --> se cierra la ruta y se abre la sigiente y se repite la operacion
    ''' </summary>
    ''' <param name="pHuellaFichero"></param>
    ''' <remarks></remarks>
    Public Sub GuardarHuellayFichero(ByVal pHuellaFichero As FicherosDN.HuellaFicheroAlmacenadoIODN)

        Dim tlproc As ITransaccionLogicaLN = Nothing
        '  Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            If pHuellaFichero.Datos Is Nothing OrElse pHuellaFichero.Datos Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("La huella a fichero es nothing o no contine datos")
            End If


            ' se procede a crear la estructura de directorios para el destino del archivo y se guarda el archivo
            Try
                FicherosAD.FicherosAD.GuardarDocumentoDesdeArrayDeBites(pHuellaFichero)


            Catch ex As FicherosAD.EspacioInsuficienteAD

                ' se marca la ruta como cerrado y se repite la operacion para la siguiente ruta activa
                ' cerrar la ruta de almacenamiento y solicitar una nueva
                Dim raln As FicherosLN.RutaAlmacenamientoFicherosLN
                raln = New FicherosLN.RutaAlmacenamientoFicherosLN(tlproc, Me.mRec)
                pHuellaFichero.RutaAlmacenamiento = raln.CerraryAbrirSiguienteRaf(pHuellaFichero.RutaAlmacenamiento)
                Me.GuardarHuellayFichero(pHuellaFichero)



            Catch ex As FicherosAD.RutaNoEncontradaAD

                Dim raln As FicherosLN.RutaAlmacenamientoFicherosLN
                raln = New FicherosLN.RutaAlmacenamientoFicherosLN(tlproc, Me.mRec)
                pHuellaFichero.RutaAlmacenamiento = raln.IncidentaryAbrirSiguienteRaf(pHuellaFichero.RutaAlmacenamiento)
                Me.GuardarHuellayFichero(pHuellaFichero)



            Catch ex As System.UnauthorizedAccessException


                Dim raln As FicherosLN.RutaAlmacenamientoFicherosLN
                raln = New FicherosLN.RutaAlmacenamientoFicherosLN(tlproc, Me.mRec)
                pHuellaFichero.RutaAlmacenamiento = raln.IncidentaryAbrirSiguienteRaf(pHuellaFichero.RutaAlmacenamiento)
                Me.GuardarHuellayFichero(pHuellaFichero)

            Catch ex As System.IO.IOException
                Dim raln As FicherosLN.RutaAlmacenamientoFicherosLN
                raln = New FicherosLN.RutaAlmacenamientoFicherosLN(tlproc, Me.mRec)
                pHuellaFichero.RutaAlmacenamiento = raln.IncidentaryAbrirSiguienteRaf(pHuellaFichero.RutaAlmacenamiento)
                Me.GuardarHuellayFichero(pHuellaFichero)


            Catch ex As Exception
                Throw ex
            End Try


            ' descargamos la huella y la guardamos
            pHuellaFichero.Datos = Nothing
            Me.Guardar(pHuellaFichero)



            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try


    End Sub


    Private Sub modificarEstadoRutaALmacenamiento(ByVal tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN, ByVal ra As FicherosDN.RutaAlmacenamientoFicherosDN, ByVal pEstadoRaf As FicherosDN.RutaAlmacenamientoFicherosEstado)
        Dim raln As FicherosLN.RutaAlmacenamientoFicherosLN
        raln = New FicherosLN.RutaAlmacenamientoFicherosLN(tlproc, Me.mRec)
        ra.EstadoRAF = FicherosDN.RutaAlmacenamientoFicherosEstado.Incidentada
        raln.Guardar(ra)
    End Sub


    ''' <summary>
    ''' carga un fichero en la huella pasada
    ''' </summary>
    ''' <param name="pHuellaFichero"></param>
    ''' <remarks></remarks>
    Public Sub CargarFcicheroEnHuella(ByVal pHuellaFichero As FicherosDN.HuellaFicheroAlmacenadoIODN)





    End Sub

    Public Overloads Function Recuperar(ByVal id) As HuellaFicheroIOLN

        Return MyBase.Recuperar(Of HuellaFicheroIOLN)(id)

    End Function

    ''' <summary>
    ''' guarda una huella de fichero que debe estar DESCARGADA en la base de datos
    ''' y el ficehro  en la carpeta de archivo que le corresponda
    ''' </summary>
    ''' <remarks></remarks>
    Public Overloads Sub Guardar(ByVal pHf As FicherosDN.HuellaFicheroAlmacenadoIODN)
        MyBase.Guardar(Of FicherosDN.HuellaFicheroAlmacenadoIODN)(pHf)
    End Sub




End Class
