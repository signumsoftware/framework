Imports AmvDocumentosDN

Imports Framework.AccesoDatos.MotorAD.AD
Imports Framework.Mensajeria.GestorMails.DN
Imports Framework.LogicaNegocios.Transacciones

Imports FN.Ficheros.FicherosDN
Imports FN.Ficheros.FicherosLN

Imports GDocEntrantesAD

Public Class MailsLN

    Inherits Framework.ClaseBaseLN.BaseGenericLN


#Region "Constructor"

    Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region


    Public Sub EnviarMailFicheroIncidentados(ByVal pDatosFicheroIncidentado As DatosFicheroIncidentado)

        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso
            Dim colemail As New Framework.Mensajeria.GestorMails.DN.ColEmailDN
            Dim NombreRolResponsable As String

            NombreRolResponsable = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("ResponsableFichero")

            If NombreRolResponsable Is Nothing OrElse NombreRolResponsable = "" Then

            Else

                colemail.AddRange(Me.RecuperarListaEmailsAdmin(NombreRolResponsable))
                If colemail IsNot Nothing AndAlso colemail.Count > 0 Then
                    Dim email As Framework.Mensajeria.GestorMails.DN.EmailDN
                    For Each email In colemail
                        Me.EnviarMail(email, "Fichero Incidentado", "Fecha:" & pDatosFicheroIncidentado.Fecha & " -- " & pDatosFicheroIncidentado.Comentario)
                    Next
                End If
            End If




            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try


    End Sub

    Public Sub EnviarMailDocumentoRIA(ByVal pDatosDocumentoAIR As DatosDocumentoAIR)

        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso
            Dim colemail As New Framework.Mensajeria.GestorMails.DN.ColEmailDN
            colemail.AddRange(Me.RecuperarListaEmailsAdmin(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("ResponsableDocs")))


            If colemail IsNot Nothing AndAlso colemail.Count > 0 Then
                Dim email As Framework.Mensajeria.GestorMails.DN.EmailDN
                For Each email In colemail
                    Me.EnviarMail(email, "Documento " & pDatosDocumentoAIR.Operacion.TipoOperacionREnF.Valor.ToString, "Fecha:" & pDatosDocumentoAIR.Fecha & " -- (idOper, IdRel,IdeHuella)Fichero: (" & pDatosDocumentoAIR.Operacion.ID & "," & pDatosDocumentoAIR.Operacion.RelacionENFichero.ID & "," & pDatosDocumentoAIR.Operacion.RelacionENFichero.HuellaFichero.ID & ")" & pDatosDocumentoAIR.Operacion.RelacionENFichero.HuellaFichero.NombreOriginalFichero & " -- " & pDatosDocumentoAIR.Comentario)
                Next
            End If


            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try




    End Sub

    Public Function RecuperarListaEmailsAdmin(ByVal nombreRol As String) As IList(Of EmailDN)
        Return MyBase.RecuperarListaCondicional(Of EmailDN)(New ConstructorBusquedaCampoStringAD("vwListaMailsAdmin", "Rol", nombreRol))
    End Function
    Public Sub EnviarMail(ByVal pEmail As EmailDN, ByVal pAsunto As String, ByVal pBody As String)

        Dim miLNCorreo As Framework.Mensajeria.GestorMails.LN.CorreoLN
        Dim miMensaje As Framework.Mensajeria.GestorMails.DN.MensajeBasicoDN
        Dim miSobre As Framework.Mensajeria.GestorMails.DN.SobreDN

        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            miLNCorreo = New Framework.Mensajeria.GestorMails.LN.CorreoLN(tlproc, mRec)
            miMensaje = New Framework.Mensajeria.GestorMails.DN.MensajeBasicoDN(pBody, pAsunto, True)
            miSobre = New Framework.Mensajeria.GestorMails.DN.SobreDN(pEmail, miMensaje)

            miLNCorreo.Enviar(miSobre)
            tlproc.Confirmar()

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try

    End Sub
End Class
