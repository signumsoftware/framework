Imports Framework.Procesos.ProcesosDN
Imports Framework.TiposYReflexion.DN
Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN

Public Class OperacionesLN
    Inherits BaseGenericLN

#Region "Métodos"


    Public Function RecuperarTodasOperaciones() As ColOperacionDN



        Using tr As New Transaccion

            Dim opad As New Framework.Procesos.ProcesosAD.OperacionesAD

            RecuperarTodasOperaciones = opad.RecuperarTodasOperaciones()


            tr.Confirmar()

        End Using




    End Function


    ''' <summary>
    ''' recuperar las dn que seon referidas por OPR que refieren a la operacion pasada o a cualqueira de ser nothing,
    ''' 
    ''' </summary>
    ''' <param name="pColOperacion"> sub conjuento de las operaciones autorizadas al rol, para el cual se desean obtener sus dn pendientes de procesar
    ''' , si es nothing se considerara todas las oepraciones del rol
    ''' ' si no tiene elementos no se devolvera nada
    ''' </param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarReferenciasDNsTrabajoPendiente(ByVal Actor As PrincipalDN, ByVal pColOperacion As ColOperacionDN) As DataSet


        If Not pColOperacion Is Nothing AndAlso pColOperacion.Count = 0 Then
            Return Nothing
        End If

        Dim ejecutorOperaciones As IEjecutorOperacionDN = Actor

        Dim ColOperObjetivo As ColOperacionDN
        If pColOperacion Is Nothing Then
            ColOperObjetivo = ejecutorOperaciones.ColOperaciones
        Else
            ColOperObjetivo = ejecutorOperaciones.ColOperaciones.Interseccion(pColOperacion, CoincidenciaBusquedaEntidadDN.Todos)
        End If



        Dim miTransicionLN As New TransicionLN
        Dim colTransiciones As ColTransicionDN = miTransicionLN.RecuperarCol(ColOperObjetivo, PosicionEnTransicion.Destino, -1)

        Dim ColOpreferidorasdeMisOperacionesFiltradas As ColOperacionDN = colTransiciones.RecuperarColOperaciones(RecuperarColOperacionesTipos.origen)



        ' recuperar el dts de estructura tipo ide y tostring , de las dn referidas
        Return RecuperarColDNReferidasPorOPRsDEOperacion(ColOpreferidorasdeMisOperacionesFiltradas)


    End Function

    ''' <summary>
    ''' recupera las OPs 
    ''' </summary>
    ''' <param name="pOperacionesFiltro"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarColDNReferidasPorOPRsDEOperacion(ByVal pOperacionesFiltro As ColOperacionDN) As DataSet

        Throw New NotImplementedException

    End Function

    ''' <summary>
    ''' dado un principal, una huella del dato que queire modificar 
    ''' devulve el conjuento de operaciones que un principal esta autorizado a realizar envase a
    ''' los permisos del principal y el estado de la dn en el grafo de operaciones
    ''' </summary>
    ''' <param name="Actor"></param>
    ''' <param name="pHuellaEntidadDatos"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarTransicionesAutorizadasSobre(ByVal Actor As PrincipalDN, ByVal pHuellaEntidadDatos As HEDN) As ColTransicionRealizadaDN

        ' ver si la entidad ya exite o se trata de un alta

        If String.IsNullOrEmpty(pHuellaEntidadDatos.IdEntidadReferida) OrElse pHuellaEntidadDatos.IdEntidadReferida = "0" Then
            ' se trata de una operacion de alta

            Return RecuperarTransicionesAutorizadasSobreDeINICIO(Actor, pHuellaEntidadDatos)

        Else

            Dim col As New ColTransicionRealizadaDN


            col.AddRangeObjectUnico(RecuperarTransicionesAutorizadasSobreNOINICIO(Actor, pHuellaEntidadDatos))
            col.AddRangeObjectUnico(RecuperarTransicionesAutorizadasSobre(Actor, pHuellaEntidadDatos, Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioDesde))

            'Las operaciones de tipo InicioObjDesde solo deben aparecer si el objeto no está en nigún proceso iniciado
            'en primer lugar recuperamos posibles operaciones de tipo InicioObjDesde, si no existen no hago nada
            'Si existen operaciones de tipo InicioObjDesde, compruebo si la entidad está incluida en un proceso activo,
            'en cuyo caso no se incluirían estas transiciones
            Dim colObjC As New ColTransicionRealizadaDN()
            colObjC = RecuperarTransicionesAutorizadasSobre(Actor, pHuellaEntidadDatos, Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado)

            If colObjC.Count > 0 Then
                Dim colOPR As ColOperacionRealizadaDN
                colOPR = RecuperarColProcesosActivos(pHuellaEntidadDatos)
                If colOPR Is Nothing OrElse colOPR.Count = 0 Then
                    col.AddRangeObjectUnico(RecuperarTransicionesAutorizadasSobre(Actor, pHuellaEntidadDatos, Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado))
                End If
            End If

            Return col

        End If

    End Function

    Public Function RecuperarTransicionesAutorizadasSobreNOINICIO(ByVal Actor As PrincipalDN, ByVal pHuellaEntidadDatos As HEDN) As ColTransicionRealizadaDN
        Dim ColTransicionesSiguientesTotales As ColTransicionDN
        ' si la entidad ya existe
        '1º recuperar la lsita de las ultimas operaciones relaizadas en el grafo

        Dim colOPR As ColOperacionRealizadaDN
        colOPR = RecuperarColProcesosActivos(pHuellaEntidadDatos)

        Dim colOprUltimas As ColOperacionRealizadaDN
        If colOPR IsNot Nothing Then
            colOprUltimas = colOPR.RecuperarColRecursivaUltimasOperacionRealizadaNoIniciadas()
        Else
            colOprUltimas = New ColOperacionRealizadaDN()
        End If



        '2º recuperar la coleccion de operaciones destino dada una operacion en un grafo
        ColTransicionesSiguientesTotales = RecuperarTransicionesSiguientesPosibles(colOprUltimas)

        '3º descontar las operaciones que el principal no tenga asignadas
        Dim ejecutorOperaciones As IEjecutorOperacionDN = Actor
        Dim coltransicionesAutorizadas As ColTransicionDN = ColTransicionesSiguientesTotales.RecuperarInterseccionTranscionesDeOperaciones(ejecutorOperaciones.ColOperaciones, RecuperarColOperacionesTipos.destino)


        Dim coltr As New ColTransicionRealizadaDN

        'colOprUltimas

        '   For Each opr As OperacionRealizadaDN In colOPR
        For Each opr As OperacionRealizadaDN In colOprUltimas

            Dim contransis As ColTransicionDN = coltransicionesAutorizadas.RecuperarTranscionesDeOperacione(opr.Operacion, RecuperarColOperacionesTipos.origen)

            If Not contransis Is Nothing Then
                For Each tran As TransicionDN In contransis

                    Dim tranR As New TransicionRealizadaDN
                    tranR.Transicion = tran
                    tranR.OperacionRealizadaOrigen = opr
                    coltr.Add(tranR)

                Next
            End If


        Next



        Return coltr



    End Function


    Public Function RecuperarTransicionesAutorizadasSobreDeINICIO(ByVal Actor As PrincipalDN, ByVal pHuellaEntidadDatos As HEDN) As ColTransicionRealizadaDN


        ' recuperar transiciones de inicio sobre el tipo
        Dim ColTransicionesSiguientesTotales As ColTransicionDN
        ColTransicionesSiguientesTotales = Me.RecuperarTransicionesAltaPosibles(pHuellaEntidadDatos.TipoEntidadReferida)

        ' eliminar las que utilizan operaciones no autorizadas para el principal
        Dim ejecutorOperaciones As IEjecutorOperacionDN = Actor
        Dim colTranAutorizadas As ColTransicionDN = ColTransicionesSiguientesTotales.RecuperarInterseccionTranscionesDeOperaciones(ejecutorOperaciones.ColOperaciones, RecuperarColOperacionesTipos.destino)

        Dim coltr As New ColTransicionRealizadaDN



        For Each tran As TransicionDN In colTranAutorizadas

            Dim tranR As New TransicionRealizadaDN
            tranR.Transicion = tran
            '        tranR.OperacionRealizadaDestino = New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()


            coltr.Add(tranR)

        Next

        Return coltr

    End Function


    Public Function RecuperarTransicionesAutorizadasSobre(ByVal Actor As PrincipalDN, ByVal pHuellaEntidadDatos As HEDN, ByVal pTipoTransicion As Framework.Procesos.ProcesosDN.TipoTransicionDN) As ColTransicionRealizadaDN


        ' recuperar transiciones de inicio sobre el tipo
        Dim ColTransicionesSiguientesTotales As ColTransicionDN
        ColTransicionesSiguientesTotales = Me.RecuperarTransiciones(pHuellaEntidadDatos.TipoEntidadReferida, pTipoTransicion)

        ' eliminar las que utilizan operaciones no autorizadas para el principal
        Dim ejecutorOperaciones As IEjecutorOperacionDN = Actor
        Dim colTranAutorizadas As ColTransicionDN = ColTransicionesSiguientesTotales.RecuperarInterseccionTranscionesDeOperaciones(ejecutorOperaciones.ColOperaciones, RecuperarColOperacionesTipos.destino)

        Dim coltr As New ColTransicionRealizadaDN



        For Each tran As TransicionDN In colTranAutorizadas

            Dim tranR As New TransicionRealizadaDN
            tranR.Transicion = tran
            '        tranR.OperacionRealizadaDestino = New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()


            coltr.Add(tranR)

        Next

        Return coltr

    End Function



    Public Function RecuperarTransiciones(ByVal pTipoDN As System.Type, ByVal pTipoTransicion As TipoTransicionDN) As ColTransicionDN
        Dim operacionesAD As Framework.Procesos.ProcesosAD.OperacionesAD
        Dim colTrans As ColTransicionDN

        Using tr As New Transaccion()

            operacionesAD = New Framework.Procesos.ProcesosAD.OperacionesAD()

            colTrans = operacionesAD.RecuperarTransiciones(pTipoDN, pTipoTransicion)

            tr.Confirmar()

            Return colTrans

        End Using

    End Function

    ''' <summary>
    ''' recupera el conjuento de trancisiones iniciales para un tipo
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarTransicionesDeInicio(ByVal pTipoDN As System.Type) As ColTransicionDN
        Return RecuperarTransiciones(pTipoDN, TipoTransicionDN.Inicio)
    End Function

    'Public Function RecuperarOperacionesAltaPosibles(ByVal pTipoDN As System.Type) As ColOperacionDN
    '    Return RecuperarTransicionesDeInicio(pTipoDN).RecuperarColOperaciones(RecuperarColOperacionesTipos.destino)
    'End Function
    Public Function RecuperarTransicionesAltaPosibles(ByVal pTipoDN As System.Type) As ColTransicionDN
        Return RecuperarTransicionesDeInicio(pTipoDN)
    End Function

    '''' <summary>
    '''' permite recuperar la operación asociada al metodo pasado y de no existir la crea nueva
    '''' la intención de este metodo es poblar operaciones en tiempo de administracion
    '''' </summary>
    '''' <param name="pVinculoMetodo"></param>
    '''' <returns></returns>
    '''' <remarks></remarks>
    'Public Function RecuperarOAltaOperacion(ByVal pVinculoMetodo As VinculoMetodoDN) As OperacionDN


    '    Using New Transaccion()



    '        Dim operacionAsignada As OperacionDN

    '        ' llamar al ad y recuperar la operacion para el metodo pasado





    '        ' retotrnar la oepracion si fue encontrada
    '        If Not operacionAsignada Is Nothing Then
    '            Return operacionAsignada
    '        End If


    '        ' si la operacion es nothin se debera crear la operacion nuega guardarla y devolverla
    '        Dim oper As New Framework.ProcesosDN.OperacionDN
    '        Dim verbo As New Framework.ProcesosDN.VerboDN

    '        verbo.VinculoMetodo = pVinculoMetodo
    '        oper.VerboOperacion = verbo
    '        oper.Nombre = pVinculoMetodo.NombreMetodo

    '        Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
    '        gi.Guardar(oper)

    '        Return oper


    '    End Using





    'End Function

    ''' <summary>
    ''' recupera las OPR referidas por transiciones de inicio de proceso, donde ellas son el origen y disponen entre sus DNs autorizadas del tipo pasado
    ''' </summary>
    ''' <param name="pHuellaEntidadDatos"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarColProcesosActivos(ByVal pHuellaEntidadDatos As HEDN) As ColOperacionRealizadaDN
        Dim operacionesAD As Framework.Procesos.ProcesosAD.OperacionesAD
        Dim colOPRs As ColOperacionRealizadaDN

        Using tr As New Transaccion()

            operacionesAD = New Framework.Procesos.ProcesosAD.OperacionesAD()

            colOPRs = operacionesAD.RecuperarColProcesosActivos(pHuellaEntidadDatos)

            tr.Confirmar()

            Return colOPRs

        End Using
    End Function

    Public Function RecuperarUltimaOperacionRealizada(ByVal pHuellaEntidadDatos As HEDN) As OperacionRealizadaDN


    End Function

    ''' <summary>
    ''' operaciones a las que se puede transitar desde la operacion origen
    ''' </summary>
    ''' <param name="pOperacionOrigen"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarOperacionesSiguientesPosibles(ByVal pOperacionOrigen As OperacionDN) As ColTransicionDN
        Dim operacionesAD As Framework.Procesos.ProcesosAD.OperacionesAD
        Dim colTrans As ColTransicionDN

        Using tr As New Transaccion()

            operacionesAD = New Framework.Procesos.ProcesosAD.OperacionesAD()

            colTrans = operacionesAD.RecuperarTransicionesSiguientesPosibles(pOperacionOrigen)

            tr.Confirmar()

            Return colTrans

        End Using

    End Function


    Public Function RecuperarTransicionesSiguientesPosibles(ByVal pColOperacionRealizada As ColOperacionRealizadaDN) As ColTransicionDN
        Dim colTranResultado, ColTranInicial As ColTransicionDN
        colTranResultado = New ColTransicionDN

        For Each oper As OperacionRealizadaDN In pColOperacionRealizada

            ColTranInicial = Me.RecuperarTransicionesSiguientesPosibles(oper.Operacion)



            ' si operacion esta abierta solo valen las sub transiciones y si esta cerrada solo las siguientes

            If oper.EstadoActividad = EstadoActividad.Cerrada Then

                colTranResultado.AddRange(ColTranInicial.RecuperarTranscionesXTipo(TipoTransicionDN.Normal))


            Else


                ' si esta iniciada ve las subordinadas que auún no se han iniciado
                ColTranInicial = ColTranInicial.RecuperarTranscionesXTipo(TipoTransicionDN.Subordianda)


                For Each tran As TransicionRealizadaDN In oper.ColTRIniciadas
                    ColTranInicial.EliminarEntidadDNxGUID(tran.Transicion.GUID)
                Next


                colTranResultado.AddRange(ColTranInicial)

            End If


        Next

        Return colTranResultado

    End Function

    Public Function RecuperarTransicionesSiguientesPosibles(ByVal pColOperacion As ColOperacionDN) As ColTransicionDN
        Dim colTr As New ColTransicionDN

        For Each oper As OperacionDN In pColOperacion

            colTr.AddRange(RecuperarOperacionesSiguientesPosibles(oper))
        Next

        Return colTr

    End Function
    Public Function RecuperarTransicionesSiguientesPosibles(ByVal pOperacion As OperacionDN) As ColTransicionDN

        Return RecuperarOperacionesSiguientesPosibles(pOperacion)




    End Function
    Public Function RecuperarEjecutorCliente(ByVal nombreCliente As String) As EjecutoresDeClienteDN
        Dim operacionesAD As Framework.Procesos.ProcesosAD.OperacionesAD
        Dim ejecutorCliente As EjecutoresDeClienteDN

        Using tr As New Transaccion()

            operacionesAD = New Framework.Procesos.ProcesosAD.OperacionesAD()

            ejecutorCliente = operacionesAD.RecuperarEjecutorCliente(nombreCliente)

            tr.Confirmar()

            Return ejecutorCliente

        End Using

    End Function

    Public Function RecuperarOperacionxID(ByVal id As String) As OperacionDN
        Return MyBase.Recuperar(Of OperacionDN)(id)


    End Function

#End Region


End Class
