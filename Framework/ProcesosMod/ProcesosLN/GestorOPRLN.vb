#Region "importaciones"

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.DatosNegocio
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.AccesoDatos.MotorAD.AD
Imports Framework.Usuarios.DN

#End Region


Public Class GestorOPRLN


#Region "ARTEFACTOS DE GESTION DE PROCESOS ASÍNCRONOS EN BLOQUE"
    'TODO: luis - 777 - falta por hacer el gestor de procesos asíncronos en bloque
#End Region

    ''' <summary>
    ''' permite ejecutar una oepracion, este metodo devolvera un error si en el proceso de obtener las transiciones a tralizar aparecen más de una altarnativa posible
    ''' </summary>
    ''' <param name="objeto"></param>
    ''' <param name="actor"></param>
    ''' <param name="pOperacionRealizada"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function EjecutarOperacion(ByVal objeto As Object, ByVal pParametros As Object, ByVal actor As PrincipalDN, ByVal pOperacionRealizada As Framework.Procesos.ProcesosDN.OperacionRealizadaDN) As IEntidadBaseDN


        Using tr As New Transaccion()


            ' deducir la TRR a pasar
            Dim miTrr As Framework.Procesos.ProcesosDN.TransicionRealizadaDN
            ' Dim hdn As New HEDN(objeto, HuellaEntidadDNIntegridadRelacional.ninguna, Nothing)
            Dim hdn As HEDN
            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(objeto.GetType) Then
                hdn = objeto
            Else
                hdn = New HEDN(objeto, HuellaEntidadDNIntegridadRelacional.ninguna, Nothing)
            End If

            Dim opln As New OperacionesLN
            Dim colTRAutorizadas As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN
            colTRAutorizadas = opln.RecuperarTransicionesAutorizadasSobre(actor, hdn)

            miTrr = colTRAutorizadas.RecuperarxOperacionDestino(pOperacionRealizada.Operacion)

            EjecutarOperacion = EjecutarOperacion(objeto, pParametros, actor, miTrr)

            tr.Confirmar()

        End Using







    End Function

    Public Function VerificarAutorizacionActorDNyProceso(ByVal pMensaje As String, ByVal objeto As Object, ByVal actor As PrincipalDN, ByVal pTransicion As Framework.Procesos.ProcesosDN.TransicionDN, ByVal colTRAutorizadas As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN) As Boolean
        ' 1º  Verificar la autroizacion segun el oeprador y como esta la dn en el sistema

        Dim opln As New OperacionesLN
        Dim hdn As HEDN
        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(objeto.GetType) Then
            hdn = objeto
        Else
            hdn = New HEDN(objeto, HuellaEntidadDNIntegridadRelacional.ninguna, Nothing)
        End If

        Dim colOper As Framework.Procesos.ProcesosDN.ColOperacionDN


        colOper = actor.ColOperaciones
        If colOper Is Nothing OrElse Not colOper.Contiene(pTransicion.OperacionDestino, CoincidenciaBusquedaEntidadDN.Todos) Then

            pMensaje = "el rol no contiene esta operación autorizada"
            Return False
            '  Throw New ApplicationException("el rol no contiene esta operación autorizada")
        End If

        If colTRAutorizadas Is Nothing Then
            colTRAutorizadas = opln.RecuperarTransicionesAutorizadasSobre(actor, hdn)
        End If

        If Not colTRAutorizadas.ContieneTransicion(pTransicion) Then
            pMensaje = "La transición no está autorizada dado el estado de la DN en el flujo"
            Return False

            'Throw New Framework.LogicaNegocios.ApplicationExceptionLN("La transición no está autorizada dado el estado de la DN en el flujo")
        End If


        Return True
    End Function

    Public Function EjecutarOperacion(ByVal objeto As Object, ByVal pParametros As Object, ByVal actor As PrincipalDN, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN) As IEntidadBaseDN
        Return EjecutarOperacion(objeto, pParametros, actor, pTransicionRealizada, Nothing)
    End Function

    Private Function EjecutarOperacion(ByVal objeto As Object, ByVal pParametros As Object, ByVal actor As PrincipalDN, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal colTRAutorizadas As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN) As IEntidadBaseDN

        Using tr As New Transaccion()

            ' 1º  Verificar la autroizacion segun el oeprador y como esta la dn en el sistema
            ' objeto = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion

            If Not pTransicionRealizada.OperacionRealizadaOrigen Is Nothing Then
                objeto = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion
            End If


            Dim miMensaje As String
            If Not VerificarAutorizacionActorDNyProceso(miMensaje, objeto, actor, pTransicionRealizada.Transicion, colTRAutorizadas) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(miMensaje)
            End If



            '2ºcreacion de la opr 

            ' Dim tansi As New ProcesosDN.TransicionRealizadaDN

            Dim oprPadre As ProcesosDN.OperacionRealizadaDN
            Dim opln As New OperacionesLN

            If pTransicionRealizada.Transicion.TipoTransicion = ProcesosDN.TipoTransicionDN.Inicio OrElse pTransicionRealizada.Transicion.TipoTransicion = ProcesosDN.TipoTransicionDN.InicioDesde OrElse pTransicionRealizada.Transicion.TipoTransicion = ProcesosDN.TipoTransicionDN.InicioObjCreado Then

                Dim opln1 As New ProcesosLN.OperacionesLN
                Dim colTranSubordinadas1 As ProcesosDN.ColTransicionDN = opln1.RecuperarTransicionesSiguientesPosibles(pTransicionRealizada.Transicion.OperacionOrigen)
                colTranSubordinadas1 = colTranSubordinadas1.RecuperarTranscionesXTipo(ProcesosDN.TipoTransicionDN.Subordianda)


                Dim oprOrigen As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN(colTranSubordinadas1)
                oprOrigen.Operacion = pTransicionRealizada.Transicion.OperacionOrigen
                oprOrigen.ObjetoIndirectoOperacion = objeto
                oprOrigen.SujetoOperacion = actor
                pTransicionRealizada.OperacionRealizadaOrigen = oprOrigen
            End If


            If pTransicionRealizada.EsInicial Then

                oprPadre = pTransicionRealizada.OperacionRealizadaOrigen

            ElseIf pTransicionRealizada.EsFinalizacion Then

                ' no hay que establecer el padre
                Debug.Write("transición de finalización")
            Else

                oprPadre = pTransicionRealizada.OperacionRealizadaOrigen.OperacionPadre

            End If



            ' crear la opr destino
            ' si se trata de una operacion de finalizacion no hay que crear una nueva sino que es la operacion padre

            Dim opr As Framework.Procesos.ProcesosDN.OperacionRealizadaDN

            If pTransicionRealizada.EsFinalizacion Then
                ' se trata de una operacion de finalizacion
                opr = pTransicionRealizada.OperacionRealizadaOrigen.OperacionPadre

            Else

                Dim colTranSubordinadas As ProcesosDN.ColTransicionDN = opln.RecuperarTransicionesSiguientesPosibles(pTransicionRealizada.Transicion.OperacionDestino)
                colTranSubordinadas = colTranSubordinadas.RecuperarTranscionesXTipo(ProcesosDN.TipoTransicionDN.Subordianda)


                opr = New Framework.Procesos.ProcesosDN.OperacionRealizadaDN(colTranSubordinadas)
                opr.Operacion = pTransicionRealizada.Transicion.OperacionDestino

                'TODO: alesx revisar se hace porque parece que son la msima isntaica pero dos clones distientos
                If pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion.GUID = objeto.guid Then
                    opr.ObjetoIndirectoOperacion = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion
                Else
                    opr.ObjetoIndirectoOperacion = objeto
                End If
                'opr.ObjetoIndirectoOperacion = objeto
                opr.SujetoOperacion = actor
                opr.OperacionPadre = oprPadre


            End If




            pTransicionRealizada.FechaRealizada = Now
            pTransicionRealizada.OperacionRealizadaDestino = opr


            ' acualizazr las oepraciones en curso
            pTransicionRealizada.ActualizarEstadoColOPRFinalizadasoEnCursoOpPadreOrigen()


            ' 3º reuperar las transiciones desde la opActual QU E ESTA MAL EL METODO DE ABAJO

            Dim coltran As Framework.Procesos.ProcesosDN.ColTransicionDN = opln.RecuperarTransiciones(objeto.GetType, -1)
            ' Dim coltran As Framework.Procesos.ProcesosDN.ColTransicionDN = opln.RecuperarTransiciones(
            ' eliminar todas a quellas en las que yo no sea el origen
            coltran = coltran.RecuperarTranscionesDeOperacione(opr.Operacion, ProcesosDN.RecuperarColOperacionesTipos.origen)

            ControlOperacionesSubordinadas(pTransicionRealizada, coltran, pParametros)

            CerrarOpr(pTransicionRealizada, coltran, pParametros)

            'TODO: Revisar
            'Se recupera el objeto de la base de datos para evitar los problemas de crecimiento de la DN
            Dim objRespuesta As IEntidadBaseDN
            Dim gi As New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            If TypeOf objeto Is IHuellaEntidadDN Then
                Dim h As IHuellaEntidadDN = objeto
                gi.Recuperar(h)
                objRespuesta = h.EntidadReferida
            Else
                objRespuesta = gi.Recuperar(CType(objeto, IEntidadBaseDN).ID, objeto.GetType())

            End If


            tr.Confirmar()

            'Return opr
            Return objRespuesta

        End Using

    End Function


    Private Sub CerrarOpr(ByVal pTR As ProcesosDN.TransicionRealizadaDN, ByVal pColTran As Framework.Procesos.ProcesosDN.ColTransicionDN, ByVal pParametros As Object)
        Using tr As New Transaccion()


            Dim gi As GestorInstanciacionLN


            Dim opr As ProcesosDN.OperacionRealizadaDN = pTR.OperacionRealizadaDestino

            ' sitiene sub operaciones en espera no puede ejecutarse
            If opr.PreparadaParaEjecutatr Then

                ' como NO contine operaciones subordinadas pendientes de ser ejecutadas
                opr.TerminarOPR()



                ' 4º ejecutar el controlador
                Dim ir As Framework.Procesos.ProcesosDN.IGestorEjecutoresDeCliente = New ProcesosLN.GestorEjecutoresLN
                '  Dim mi As Reflection.MethodInfo = ir.RecuperarMethodInfoEjecutor("Servidor", opr.Operacion.VerboOperacion)

                'TODO: Se guarda una transición realizada en lugar de la operación
                '"nombreRolAplicacion" es el nombre del ROL de la máquina dentro del grafo de operaciones
                ' las oepraciones son realizadas por roles en concreto.
                ' lo normal es que esita un rol cliente y un rol servidor dentro del sistema
                ' pero es posible que hallan servidores especializados donde se deban realizar algunos pasos del grafo
                ' en este caso tendrán otro nombre
                ir.EjecutarMethodInfo(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String), pParametros, pTR, pTR.OperacionRealizadaOrigen.ObjetoIndirectoOperacion)

                ' ver si se trata de una operacion de iicio desde en ese caso aprece objeto directo el objeto original y objeto indirecto el objeto creado
                If pTR.Transicion.TipoTransicion = ProcesosDN.TipoTransicionDN.InicioDesde Then
                    ' intercambiar los objetos
                    Dim oi As Framework.DatosNegocio.IEntidadDN = pTR.OperacionRealizadaDestino.ObjetoDirectoOperacion
                    pTR.OperacionRealizadaDestino.ObjetoDirectoOperacion = pTR.OperacionRealizadaDestino.ObjetoIndirectoOperacion
                    pTR.OperacionRealizadaDestino.ObjetoIndirectoOperacion = oi

                    pTR.OperacionRealizadaOrigen.ObjetoDirectoOperacion = pTR.OperacionRealizadaDestino.ObjetoDirectoOperacion
                    pTR.OperacionRealizadaOrigen.ObjetoIndirectoOperacion = pTR.OperacionRealizadaDestino.ObjetoIndirectoOperacion

                End If


                ' guardar la transicion
                Me.GuardarLaTrtansicion(pTR, True)

                'Se traza la operación
                TrazarOperacion(opr)

                ' si tiene operaciones siguientes anutomaticas ejecutarlas

                If pColTran.ContieneTransionesAutomaticas Then

                    gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    opr = gi.Recuperar(Of ProcesosDN.OperacionRealizadaDN)(opr.ID)

                    ' si continen transiciones automaticas he de iniciarlas

                    For Each tranAutomatica As Framework.Procesos.ProcesosDN.TransicionDN In pColTran


                        If tranAutomatica.Automatica Then

                            Dim opln As New ProcesosLN.OperacionesLN
                            Dim colTranSubordinadas As ProcesosDN.ColTransicionDN = opln.RecuperarTransicionesSiguientesPosibles(tranAutomatica.OperacionDestino)
                            colTranSubordinadas = colTranSubordinadas.RecuperarTranscionesXTipo(ProcesosDN.TipoTransicionDN.Subordianda)

                            Dim oprauto As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN(colTranSubordinadas)
                            oprauto.Operacion = tranAutomatica.OperacionDestino
                            oprauto.ObjetoIndirectoOperacion = opr.ObjetoIndirectoOperacion
                            oprauto.SujetoOperacion = opr.SujetoOperacion

                            Dim trauto As New Framework.Procesos.ProcesosDN.TransicionRealizadaDN
                            trauto.Transicion = tranAutomatica
                            trauto.OperacionRealizadaOrigen = opr
                            trauto.OperacionRealizadaDestino = oprauto

                            If trauto.TransicionAutorizada Then

                                EjecutarOperacion(opr.ObjetoIndirectoOperacion, pParametros, opr.SujetoOperacion, trauto)

                            End If
                        End If

                    Next

                End If

            End If



            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' eliminar las oepraciones si la operración principal purde cerrar se
            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            'para encontrar ls operaciones subordinadas de una operacion de inicio lo podemos resolver por la ruta de GUID
            'para ello una operacion de inicio debe ser subbordinada de una inicial
            If pTR.OperacionRealizadaDestino.OperacionPadre Is Nothing AndAlso pTR.OperacionRealizadaDestino.EstadoIOperacionRealizada = ProcesosDN.EstadoIOperacionRealizadaDN.Terminada Then
                Me.EliminarTRyOPREnTablasActivas(pTR.OperacionRealizadaOrigen.OperacionPadre)
            End If



            tr.Confirmar()
        End Using
    End Sub


    Private Sub GuardarLaTrtansicion(ByVal ptr As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pCrearHistorico As Boolean)

        Dim gi As GestorInstanciacionLN

        ' guardar la transicion
        gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
        gi.Guardar(ptr)


        ''''''''''''''''''''''''''''''''
        ' crear la instancia heredada
        ''''''''''''''''''''''''''''''''

        'If pCrearHistorico Then
        '    Dim historicotr As New Framework.Procesos.ProcesosDN.HistTransicionRealizadaDN
        '    Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ClonSuperfEnClaseCompatible(ptr, historicotr, False)

        '    Dim oprhistOrigen, oprhistDestino As Framework.Procesos.ProcesosDN.HistOperacionRealizadaDN
        '    oprhistOrigen = New Framework.Procesos.ProcesosDN.HistOperacionRealizadaDN()
        '    Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ClonSuperfEnClaseCompatible(ptr.OperacionRealizadaOrigen, oprhistOrigen, False)

        '    oprhistDestino = New Framework.Procesos.ProcesosDN.HistOperacionRealizadaDN()
        '    Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ClonSuperfEnClaseCompatible(ptr.OperacionRealizadaOrigen, oprhistDestino, False)

        '    historicotr.OperacionRealizadaOrigen = oprhistOrigen
        '    historicotr.OperacionRealizadaDestino = oprhistDestino


        '    gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
        '    gi.Guardar(historicotr)
        'End If


    End Sub



    Private Sub TrazarOperacion(ByVal popr As ProcesosDN.OperacionRealizadaDN)



        If popr.Operacion.TrazarOperacion Then
            ' si la operacion fuera trazable se crearioa un objeto traza of t donde el objeto contenido se mapea con persistencia contenida NO serializada
            ' el proceso por el cual el motor obtiene el nombre de la tabla debe ser modificado
            ' actualmente se base en el nombre del tipo, pero si el tipo hereda de traza debe de ser el tipo del objeto que se encuentre referido tlTrazaNombredeltipo

            ' crear el ojeto de traza y guardarlo

            Dim fac As New FactoriaTrazasLN

            Dim colTrazas As Framework.Procesos.ProcesosDN.ColITrazaDN = fac.RecuperarTrazas(CType(popr.ObjetoIndirectoOperacion, Object).GetType)

            For Each traza As Framework.Procesos.ProcesosDN.ITrazaDN In colTrazas
                traza.TrazarEntidad(popr)

                Dim gi As GestorInstanciacionLN
                gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Guardar(traza)

            Next




        End If



    End Sub




    Private Sub EjecutarTransicionesAutomaticas(ByVal pTR As ProcesosDN.TransicionRealizadaDN, ByVal pColTran As Framework.Procesos.ProcesosDN.ColTransicionDN, ByVal pParametros As Object)



        Dim opr As ProcesosDN.OperacionRealizadaDN = pTR.OperacionRealizadaDestino

        For Each tran As ProcesosDN.TransicionDN In pColTran

            If tran.Automatica AndAlso opr.Operacion.GUID = tran.OperacionOrigen.GUID Then

                Dim tr As New ProcesosDN.TransicionRealizadaDN
                tr.Transicion = tran
                tr.OperacionRealizadaOrigen = opr

                Dim opln As New ProcesosLN.OperacionesLN
                Dim colTranSubordinadas As ProcesosDN.ColTransicionDN = opln.RecuperarTransicionesSiguientesPosibles(tran.OperacionDestino)
                colTranSubordinadas = colTranSubordinadas.RecuperarTranscionesXTipo(ProcesosDN.TipoTransicionDN.Subordianda)


                Dim oprDestino As New ProcesosDN.OperacionRealizadaDN(colTranSubordinadas)
                oprDestino.Operacion = tran.OperacionDestino
                oprDestino.SujetoOperacion = opr.SujetoOperacion
                oprDestino.ObjetoIndirectoOperacion = opr.ObjetoIndirectoOperacion
                ' determinar el padre de la operacion
                If tran.TipoTransicion = ProcesosDN.TipoTransicionDN.Normal Then
                    oprDestino.OperacionPadre = opr.OperacionPadre
                Else

                    oprDestino.OperacionPadre = opr

                End If

                tr.OperacionRealizadaDestino = oprDestino


                EjecutarOperacion(pTR.OperacionRealizadaDestino.ObjetoIndirectoOperacion, pParametros, pTR.OperacionRealizadaDestino.SujetoOperacion, tr)

            End If

        Next




    End Sub


    Private Sub ControlOperacionesSubordinadas(ByVal pTR As ProcesosDN.TransicionRealizadaDN, ByVal pColTran As Framework.Procesos.ProcesosDN.ColTransicionDN, ByVal pParametros As Object)

        Dim opr As ProcesosDN.OperacionRealizadaDN = pTR.OperacionRealizadaDestino


        If pColTran.ContieneTransionesDelTipo(Framework.Procesos.ProcesosDN.TipoTransicionDN.Subordianda) Then
            ' verificar si es la primera vez

            If opr.IniciadasTRSubordinadas = 0 Then
                ' dado que no se iniciaron hemos de ver si tine transiciones automaticas y de ser asi iniciar estas
                Dim colTSubor As ProcesosDN.ColTransicionDN = pColTran.RecuperarTranscionesXTipo(ProcesosDN.TipoTransicionDN.Subordianda)
                If colTSubor.ContieneTransionesAutomaticas Then
                    ' iniciar las transiciones automaticas
                    EjecutarTransicionesAutomaticas(pTR, colTSubor, pParametros)
                End If





            End If


        End If




    End Sub


    Private Sub EliminarTRyOPREnTablasActivas(ByVal OprPadreDelFlujo As ProcesosDN.OperacionRealizadaDN)

        Using tr As New Transaccion

            Dim TReliminados, OPReliminados As Int64

            Dim ad As New ProcesosAD.OperacionesAD

            ad.EliminarTRyOPREnTablasActivas(OprPadreDelFlujo.RutaSubordinada, TReliminados, OPReliminados)

            If TReliminados < 1 OrElse OPReliminados < 1 Then
                Throw New ApplicationExceptionDN("No se eliminó ninguna Transición u operación realizadas")
            End If

            tr.Confirmar()

        End Using

    End Sub






End Class
