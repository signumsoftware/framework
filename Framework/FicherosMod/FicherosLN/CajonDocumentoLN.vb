Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Ficheros


Public Class CajonDocumentoLN








    ''' <summary>
    ''' Recupera los cajones documentos y fichero que sean vincualbles, 
    ''' es decir que refiran al mismo identidad de documento y que el tipo de documento coincida entre 
    ''' el cajon y la identificacion y que no esten vinculados
    '''
    ''' </summary>
    ''' <remarks></remarks>
    Public Function VincularCajonDocumento(ByRef colcorectos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN, ByRef colIncorrectos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN) As DataSet




        Using tr As New Transaccion



            colcorectos = New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
            colIncorrectos = New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN

            Dim ad As Framework.Ficheros.FicherosAD.CajonDocumentoAD = New Framework.Ficheros.FicherosAD.CajonDocumentoAD(Transaccion.Actual, Recurso.Actual)

            Dim colpares As Framework.Ficheros.FicherosDN.ColParCDyHFVincualble = ad.RecuperarParesCDyHFVincualbles
            Dim mensaje As String

            If Not colpares.Verificar(mensaje) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            End If


            For Each par As Framework.Ficheros.FicherosDN.ParCDyHFVincualble In colpares

                par.CD.Documento = par.HF
                If Not par.CD.EstadoIntegridad(mensaje) = DatosNegocio.EstadoIntegridadDN.Consistente Then
                    colIncorrectos.Add(par.CD)
                End If


                Using tr1 As New Transaccion(True)

                    Try
                        Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                        gi.Guardar(par.CD)

                        tr1.Confirmar()
                        colcorectos.Add(par.CD)

                    Catch ex As Exception
                        colIncorrectos.Add(par.CD)
                        tr1.Cancelar()
                    End Try



                End Using

            Next


            ' crear el dts resumen de resultados
            Dim dts As New DataSet
            Dim dt As New DataTable
            dt.Columns.Add("idCD", GetType(String), Nothing)
            dt.Columns.Add("Resultado", GetType(String), Nothing)
            dt.Columns.Add("NombreFichero", GetType(String), Nothing)
            dt.Columns.Add("Entidades", GetType(String), Nothing)
            dt.Columns.Add("GuidCD", GetType(String), Nothing)
            dts.Tables.Add(dt)


            For Each cc As FicherosDN.CajonDocumentoDN In colcorectos
                Dim dtr As DataRow = dt.NewRow()
                dtr.Item("idCD") = cc.ID
                dtr.Item("GuidCD") = cc.GUID
                dtr.Item("Resultado") = "Correcto"
                dtr.Item("NombreFichero") = cc.Documento.NombreOriginalFichero
                dtr.Item("Entidades") = cc.HuellasEntidadesReferidas.ToString
                dt.Rows.Add(dtr)

            Next


            For Each cc As FicherosDN.CajonDocumentoDN In colIncorrectos
                Dim dtr As DataRow = dt.NewRow()
                dtr.Item("idCD") = cc.ID
                dtr.Item("GuidCD") = cc.GUID
                dtr.Item("Resultado") = "ERROR"
                dtr.Item("NombreFichero") = cc.Documento.NombreOriginalFichero
                dtr.Item("Entidades") = cc.HuellasEntidadesReferidas.ToString
                dt.Rows.Add(dtr)

            Next
      
            VincularCajonDocumento = dts
            tr.Confirmar()

        End Using




    End Function



    Public Function RecuperarColTipoFichero() As Framework.Ficheros.FicherosDN.ColTipoFicheroDN



        Using tr As New Transaccion
            Dim col As New Framework.Ficheros.FicherosDN.ColTipoFicheroDN

            Dim btln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            col.AddRangeObject(btln.RecuperarLista(GetType(Framework.Ficheros.FicherosDN.TipoFicheroDN)))
            RecuperarColTipoFichero = col

            tr.Confirmar()
        End Using



    End Function


    Public Function RecuperarColCajonDocumentoCoincidentes(ByVal colidDoc As Framework.Ficheros.FicherosDN.ColIdentificacionDocumentoDN) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN



        Using tr As New Transaccion


            Dim cdad As New Framework.Ficheros.FicherosAD.CajonDocumentoAD(Transaccion.Actual, Recurso.Actual)
            RecuperarColCajonDocumentoCoincidentes = cdad.RecuperarColCajonDocumentoCoincidentes(colidDoc)
            tr.Confirmar()
        End Using



    End Function


    Public Function GenerarCajonesParaObjetos(ByVal pColHeEntidadesReferidas As Framework.DatosNegocio.ColHEDN, ByVal pColGUIDEntidadesPuedenRequerirDoc As IList(Of String), ByRef pColTipoDocumentoRequerido As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN) As FicherosDN.ColCajonDocumentoDN
        Dim col As New FicherosDN.ColCajonDocumentoDN()





        Using tr As New Transaccion



            '1º recupear los documentos requeirods para las entidades que pueden requerirlo

            pColTipoDocumentoRequerido = New Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN

            For Each miguid As String In pColGUIDEntidadesPuedenRequerirDoc
                pColTipoDocumentoRequerido.AddRangeObjectUnico(Me.RecuperarTipoDocumentoRequerido(miguid))
            Next
            'TODO: alex44 este codigo mejora el rendimiento cuendo este implementado
            '  pColTipoDocumentoRequerido = RecuperarColTipoDocumentoRequerido(pColGUIDEntidadesPuedenRequerirDoc)

            ' 2º crear cajones documentos y relacioanrlos con las entidades


            For Each tdr As Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN In pColTipoDocumentoRequerido
                Dim cd As New Framework.Ficheros.FicherosDN.CajonDocumentoDN
                cd.TipoDocumento = tdr.TipoDoc
                For Each h As Framework.DatosNegocio.HEDN In pColHeEntidadesReferidas
                    cd.HuellasEntidadesReferidas.AddUnicoHuellaPara(h)
                Next
                cd.CrearAlerta(tdr.Prioridad, "Alerta de: " & tdr.TipoDoc.Nombre)
                cd.Alerta.FEjecProgramada = tdr.Plazo.IncrementarFecha(Now)

                col.Add(cd)
            Next

            tr.Confirmar()

        End Using





        Return col

    End Function



    Public Function RecuperarCajonesParaEntidadReferida(ByVal pGuid As String) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
        Dim col As New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN


        Dim al As IList


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


        Using tr As New Transaccion()
            Dim pi As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(GetType(Framework.Ficheros.FicherosDN.CajonDocumentoDN), "HuellasEntidadesReferidas", "", pGuid)
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            al = gi.RecuperarColHuellasRelInversa(pi)

            If al IsNot Nothing Then
                For Each huella As Framework.DatosNegocio.HEDN In al


                    Dim h As Framework.DatosNegocio.HEDN
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    h = gi.Recuperar(huella)

                    col.Add(h.EntidadReferida)
                Next
            End If



            tr.Confirmar()
        End Using

        Return col

    End Function


    Public Function RecuperarTipoDocumentoRequerido(ByVal pObjeto As Framework.DatosNegocio.IEntidadDN) As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
        Return RecuperarTipoDocumentoRequerido(pObjeto.GUID)
    End Function

    Public Function RecuperarTipoDocumentoRequerido(ByVal pGuid As String) As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
        Dim col As New Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN


        Dim al As IList


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


        Using tr As New Transaccion()
            Dim pi As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(GetType(Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN), "ColEntidadesRequeridoras", "", pGuid)
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            al = gi.RecuperarColHuellasRelInversa(pi)


            For Each huella As Framework.DatosNegocio.HEDN In al

                '                Dim td As Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
                Dim h As Framework.DatosNegocio.HEDN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                h = gi.Recuperar(huella)

                col.Add(h.EntidadReferida)
            Next


            tr.Confirmar()
        End Using

        Return col

    End Function





    Public Function RecuperarColTipoDocumentoRequerido(ByVal colGUIDs As IList(Of String)) As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN



        Using tr As New Transaccion


            Dim ad As New Framework.Ficheros.FicherosAD.CajonDocumentoAD(Transaccion.Actual, Recurso.Actual)
            RecuperarColTipoDocumentoRequerido = ad.RecuperarColTipoDocumentoRequerido(colGUIDs)
            tr.Confirmar()

        End Using




    End Function

End Class
