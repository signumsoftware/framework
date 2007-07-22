Imports Framework.Operaciones.OperacionesDN
Imports FN.RiesgosVehiculos.DN
Imports Framework.DatosNegocio

Public Class RVIRecSumiValorLN
    Inherits RecSumiValorBaseDN

    Protected mTarifaDN As FN.Seguros.Polizas.DN.TarifaDN

    Property Tarifa() As FN.Seguros.Polizas.DN.TarifaDN
        Get
            Return mTarifaDN
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.TarifaDN)
            mTarifaDN = value
        End Set
    End Property


    Public Function Contiene(ByVal pcol As IList, ByVal pEntidadDN As IHuellaEntidadDN, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As IHuellaEntidadDN

        Dim Ent As IHuellaEntidadDN
        If pEntidadDN Is Nothing Then
            Throw New ApplicationException("no se admite nothing como valor de busqueda")
        End If

        For Each objeto As Object In pcol

            If TypeOf objeto Is IHuellaEntidadDN Then
                Ent = objeto
                If pEntidadDN.GUIDReferida.ToLower = Ent.GUIDReferida.ToLower Then


                    Select Case pCoincidencia
                        Case CoincidenciaBusquedaEntidadDN.Clones
                            If Not pEntidadDN Is Ent Then
                                Return Ent
                            End If
                        Case CoincidenciaBusquedaEntidadDN.MismaRef
                            If pEntidadDN Is Ent Then
                                Return Ent
                            End If
                        Case CoincidenciaBusquedaEntidadDN.Todos
                            Return Ent
                    End Select
                End If
            End If

        Next

        Return Nothing

    End Function

    Public Function ContieneProvisional(ByVal pcol As IList, ByVal pEntidadDN As IOperacionCache, ByVal pCoincidencia As CoincidenciaBusquedaEntidadDN) As IOperacionCache

        Dim Ent As IOperacionCache
        If pEntidadDN Is Nothing Then
            Throw New ApplicationException("no se admite nothing como valor de busqueda")
        End If

        For Each objeto As Object In pcol

            If TypeOf objeto Is IEntidadDN Then
                Ent = objeto
                If pEntidadDN.GUIDReferida.ToLower = Ent.GUIDReferida.ToLower Then


                    Select Case pCoincidencia
                        Case CoincidenciaBusquedaEntidadDN.Clones
                            If Not pEntidadDN Is Ent Then
                                Return Ent
                            End If
                        Case CoincidenciaBusquedaEntidadDN.MismaRef
                            If pEntidadDN Is Ent Then
                                Return Ent
                            End If
                        Case CoincidenciaBusquedaEntidadDN.Todos
                            Return Ent
                    End Select
                End If
            End If

        Next

        Return Nothing

    End Function

    Public Overrides Function CachearElemento(ByVal pElementos As Framework.DatosNegocio.IEntidadDN) As IList
        ' este metodo sabe crear las distientas claes de cachaedo dependiendo de los elementos de las operaciones
        ' primabase, modulador impuesto

        ' determianr el tipo de operaciona  cachear
        Dim op As Framework.Operaciones.OperacionesDN.IOperacionSimpleDN = pElementos
        Dim al As New ArrayList

        Dim procesado As Boolean = False

        If TypeOf op.Operando1 Is PrimabaseRVSVDN OrElse TypeOf op.Operando2 Is PrimabaseRVSVDN Then
            Dim entidad As OperacionPrimaBaseRVCacheDN = New OperacionPrimaBaseRVCacheDN(op, mTarifaDN)

            Dim ent As IHuellaEntidadDN = Contiene(Me.DataResults, entidad, CoincidenciaBusquedaEntidadDN.Todos)
            If ent Is Nothing Then
                Me.DataResults.Add(entidad)
                al.Add(entidad)
            End If

            procesado = True
        End If


        If TypeOf op.Operando1 Is ImpuestoRVSVDN OrElse TypeOf op.Operando2 Is ImpuestoRVSVDN Then
            Dim entidad As OperacionImpuestoRVCacheDN = New OperacionImpuestoRVCacheDN(op, mTarifaDN)
            If entidad.Aplicado Then ' no cachea si no se ha aplicado para reducir objetos en la bd

                Dim ent As IHuellaEntidadDN = Contiene(Me.DataResults, entidad, CoincidenciaBusquedaEntidadDN.Todos)
                If ent Is Nothing Then
                    Me.DataResults.Add(entidad)
                    al.Add(entidad)
                End If

            End If
            procesado = True
        End If

        If TypeOf op.Operando1 Is ModuladorRVSVDN OrElse TypeOf op.Operando2 Is ModuladorRVSVDN Then

            Dim entidad As OperacionModuladorRVCacheDN = New OperacionModuladorRVCacheDN(op, mTarifaDN)
            If entidad.Aplicado Then ' no cachea si no se ha aplicado para reducir objetos en la bd

                Dim ent As IHuellaEntidadDN = Contiene(Me.DataResults, entidad, CoincidenciaBusquedaEntidadDN.Todos)
                If ent Is Nothing Then
                    Me.DataResults.Add(entidad)
                    al.Add(entidad)
                End If

            End If
            procesado = True
        End If

        If TypeOf op.Operando1 Is FraccionamientoRVSVDN OrElse TypeOf op.Operando2 Is FraccionamientoRVSVDN Then

            Dim entidad As OperacionFracRVCacheDN = New OperacionFracRVCacheDN(op, mTarifaDN)
            If entidad.Aplicado Then ' no cachea si no se ha aplicado para reducir objetos en la bd

                Dim ent As IHuellaEntidadDN = Contiene(Me.DataResults, entidad, CoincidenciaBusquedaEntidadDN.Todos)
                If ent Is Nothing Then
                    Me.DataResults.Add(entidad)
                    al.Add(entidad)
                End If

            End If
            procesado = True
        End If

        If TypeOf op.Operando1 Is ComisionRVSVDN OrElse TypeOf op.Operando2 Is ComisionRVSVDN Then

            Dim entidad As OperacionComisionRVCacheDN = New OperacionComisionRVCacheDN(op, mTarifaDN)
            If entidad.Aplicado Then ' no cachea si no se ha aplicado para reducir objetos en la bd

                Dim ent As IOperacionCache = ContieneProvisional(Me.DataResults, entidad, CoincidenciaBusquedaEntidadDN.Todos)
                If ent Is Nothing Then
                    Me.DataResults.Add(entidad)
                    al.Add(entidad)
                End If

            End If

            procesado = True
        End If

        If TypeOf op.Operando1 Is BonificacionRVSVDN OrElse TypeOf op.Operando2 Is BonificacionRVSVDN Then

            Dim entidad As OperacionBonificacionRVCacheDN = New OperacionBonificacionRVCacheDN(op, mTarifaDN)
            If entidad.Aplicado Then ' no cachea si no se ha aplicado para reducir objetos en la bd

                Dim ent As IOperacionCache = ContieneProvisional(Me.DataResults, entidad, CoincidenciaBusquedaEntidadDN.Todos)
                If ent Is Nothing Then
                    Me.DataResults.Add(entidad)
                    al.Add(entidad)
                End If

            End If

            procesado = True
        End If

        If Not procesado Then
            Dim entidad As OperacionSumaRVCacheDN = New OperacionSumaRVCacheDN(op, mTarifaDN)

            Dim ent As IHuellaEntidadDN = Contiene(Me.DataResults, entidad, CoincidenciaBusquedaEntidadDN.Todos)
            If ent Is Nothing Then
                Me.DataResults.Add(entidad)
                al.Add(entidad)
            End If

            procesado = True
        End If

        'If Not al.Count = 0 Then
        '    Throw New ApplicationException("no se puedo resolver el tipo de operacion a cachear")
        'End If
        Return al

    End Function

    Public Overrides Function getSuministradorValor(ByVal pOperacion As Framework.Operaciones.OperacionesDN.IOperacionSimpleDN, ByVal posicion As Framework.Operaciones.OperacionesDN.PosicionOperando) As Framework.Operaciones.OperacionesDN.ISuministradorValorDN
        Throw New NotImplementedException
    End Function

    Public Overrides Sub ClearAll()
        MyBase.ClearAll()
        mTarifaDN = Nothing
    End Sub

    Public Function RecuperarColOpImpuestos() As ColOperacionImpuestoRVCacheDN
        Dim colOpImp As New ColOperacionImpuestoRVCacheDN()
        For Each elto As Object In mDataResults
            If TypeOf elto Is OperacionImpuestoRVCacheDN Then
                colOpImp.Add(elto)
            End If
        Next

        Return colOpImp

    End Function

    Public Function RecuperarColOpModulador() As ColOperacionModuladorRVCacheDN
        Dim colOpMod As New ColOperacionModuladorRVCacheDN
        For Each elto As Object In mDataResults
            If TypeOf elto Is OperacionModuladorRVCacheDN Then
                colOpMod.Add(elto)
            End If
        Next

        Return colOpMod

    End Function

    Public Function RecuperarColOpPB() As ColOperacionPrimaBaseRVCacheDN
        Dim colOpPB As New ColOperacionPrimaBaseRVCacheDN
        For Each elto As Object In mDataResults
            If TypeOf elto Is OperacionPrimaBaseRVCacheDN Then
                colOpPB.Add(elto)
            End If
        Next

        Return colOpPB

    End Function

    Public Function RecuperarColOpFraccionamiento() As ColOperacionFracRVCacheDN
        Dim colOpFrac As New ColOperacionFracRVCacheDN()

        For Each elto As Object In mDataResults
            If TypeOf elto Is OperacionFracRVCacheDN Then
                colOpFrac.Add(elto)
            End If
        Next

        Return colOpFrac
    End Function

    Public Function RecuperarColOpComisiones() As ColOperacionComisionRVCacheDN
        Dim colOpCom As New ColOperacionComisionRVCacheDN()

        For Each elto As Object In mDataResults
            If TypeOf elto Is OperacionComisionRVCacheDN Then
                colOpCom.Add(elto)
            End If
        Next

        Return colOpCom
    End Function

    Public Function RecuperarColOpBonificaciones() As ColOperacionBonificacionRVCacheDN
        Dim colOpCom As New ColOperacionBonificacionRVCacheDN()

        For Each elto As Object In mDataResults
            If TypeOf elto Is OperacionBonificacionRVCacheDN Then
                colOpCom.Add(elto)
            End If
        Next

        Return colOpCom
    End Function

    Public Function RecuperarColOpSuma() As ColOperacionSumaRVCacheDN
        Dim colOpSum As New ColOperacionSumaRVCacheDN
        For Each elto As Object In mDataResults
            If TypeOf elto Is OperacionSumaRVCacheDN Then
                colOpSum.Add(elto)
            End If
        Next

        Return colOpSum

    End Function

End Class
