Imports Framework.DatosNegocio

Imports FN.GestionPagos.DN

<Serializable()> _
Public Class TarifaDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mColLineaProducto As ColLineaProductoDN
    Protected mRiesgo As IRiesgoDN
    Protected mDatosTarifa As IDatosTarifaDN
    Protected mFEfecto As Date
    Protected mAMD As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
    Protected mImporte As Double
    'Protected mFraccionamientosXML As String
    Protected mNombreFraccionaminento As String
    Protected mGrupoFraccionamientos As GrupoFraccionamientosDN
    Protected mFraccionamiento As FN.GestionPagos.DN.FraccionamientoDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorCol(Of ColLineaProductoDN)(New ColLineaProductoDN(), mColLineaProducto)
        CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias, mAMD)
    End Sub

#End Region

#Region "Propiedades"











    <RelacionPropCampoAtribute("mFraccionamiento")> _
    Public Property Fraccionamiento() As FN.GestionPagos.DN.FraccionamientoDN
        Get
            Return mFraccionamiento
        End Get
        Set(ByVal value As FN.GestionPagos.DN.FraccionamientoDN)
            CambiarValorRef(Of FN.GestionPagos.DN.FraccionamientoDN)(value, mFraccionamiento)
            Me.mNombreFraccionaminento = mFraccionamiento.Nombre
        End Set
    End Property












    Public Function CalcualrImporteDia() As Double
        Return Me.mImporte / mAMD.IncrementarFecha(mFEfecto).Subtract(mFEfecto).TotalDays
    End Function
    Public Property AMD() As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
        Get
            Return mAMD
        End Get
        Set(ByVal value As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)
            CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(value, mAMD)
        End Set
    End Property

    Public Property FEfecto() As Date

        Get
            Return mFEfecto
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFEfecto)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColLineaProducto")> _
    Public Property ColLineaProducto() As ColLineaProductoDN
        Get
            Return mColLineaProducto
        End Get
        Set(ByVal value As ColLineaProductoDN)
            CambiarValorCol(Of ColLineaProductoDN)(value, mColLineaProducto)
        End Set
    End Property

    <RelacionPropCampoAtribute("mRiesgo")> _
    Public Property Riesgo() As IRiesgoDN
        Get
            Return mRiesgo
        End Get
        Set(ByVal value As IRiesgoDN)
            CambiarValorRef(Of IRiesgoDN)(value, mRiesgo)
        End Set
    End Property

    <RelacionPropCampoAtribute("mDatosTarifa")> _
    Public Property DatosTarifa() As IDatosTarifaDN
        Get
            Return mDatosTarifa
        End Get
        Set(ByVal value As IDatosTarifaDN)
            CambiarValorRef(Of IDatosTarifaDN)(value, mDatosTarifa)
            AsignarmeDatosTarifa()
        End Set
    End Property

    <RelacionPropCampoAtribute("mGrupoFraccionamientos")> _
    Public Property GrupoFraccionamientos() As GrupoFraccionamientosDN
        Get
            'mGrupoFraccionamientos = AsignarFraccionamientoFromXML()
            Return mGrupoFraccionamientos
        End Get
        Set(ByVal value As FN.GestionPagos.DN.GrupoFraccionamientosDN)
            'mGrupoFraccionamientos = value
            'CambiarValorVal(Of String)(AsignarFraccionamientoXML(), mFraccionamientosXML)
            CambiarValorRef(Of GrupoFraccionamientosDN)(value, mGrupoFraccionamientos)
        End Set
    End Property

    Public Property Importe() As Double
        Get
            Return mImporte
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mImporte)
        End Set
    End Property

    Public ReadOnly Property NombreFraccionaminento() As String
        Get
            Return mNombreFraccionaminento
        End Get

    End Property


    Public ReadOnly Property ColProductos() As FN.Seguros.Polizas.DN.ColProductoDN

        Get
            ColProductos = New FN.Seguros.Polizas.DN.ColProductoDN


            For Each lp As FN.Seguros.Polizas.DN.LineaProductoDN In Me.ColLineaProducto
                ColProductos.Add(lp.Producto)
            Next


        End Get
    End Property

#End Region

#Region "Métodos"

    Public Function CoberturasIguales(ByVal pTarifa As TarifaDN) As Boolean
        Dim col1, col2 As ColCoberturaDN

        col1 = Me.RecuperarCoberturas
        col2 = pTarifa.RecuperarCoberturas

        If col1.Count <> col2.Count Then
            Return False
        End If

        For Each cob As CoberturaDN In col2

            If Not col1.Contiene(cob, CoincidenciaBusquedaEntidadDN.Todos) Then
                Return False
            End If
        Next

        Return True

    End Function

    Public Function RecuperarCobertura(ByVal pGUID As String) As CoberturaDN
        For Each lp As LineaProductoDN In Me.mColLineaProducto
            Dim cob As CoberturaDN = lp.RecuperarCobertura(pGUID)
            If Not cob Is Nothing Then
                Return cob
            End If
        Next

        Return Nothing

    End Function

    Public Function RecuperarCoberturas() As ColCoberturaDN
        Dim col As New ColCoberturaDN
        For Each lp As LineaProductoDN In Me.mColLineaProducto
            col.AddRangeObjectUnico(lp.Producto.ColCoberturas)
        Next

        Return col

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Me.mAMD.Anyos < 0 Then
            pMensaje = "años debe ser mayor o igual a cero"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Me.mAMD.Dias < 0 Then
            pMensaje = "dias debe ser mayor o igual a cero"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Me.mAMD.Dias < 1 And Me.AMD.Anyos < 1 Then
            pMensaje = "la tarifa debe efectuarse para algun perido contratado"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mDatosTarifa Is Nothing Then
            pMensaje = "El objeto Datos de tarifa no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ComprobarProductosDependientes() Then
            pMensaje = "La tarifa debe contener todos los productos dependientes"
            Return EstadoIntegridadDN.Inconsistente
        End If
        



        Me.mNombreFraccionaminento = Me.mFraccionamiento.Nombre
        'mFraccionamientosXML = AsignarFraccionamientoXML()

        AsignarmeDatosTarifa()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function ComprobarProductosDependientes() As Boolean
        If mColLineaProducto IsNot Nothing Then
            For Each lp As LineaProductoDN In mColLineaProducto
                If (lp.Establecido OrElse lp.Ofertado) AndAlso lp.Producto IsNot Nothing AndAlso lp.Producto.ColProdDependientes IsNot Nothing Then
                    For Each prodDep As ProductoDN In lp.Producto.ColProdDependientes


                        Dim milp As LineaProductoDN = mColLineaProducto.RecuperarLineaProductoxProducto(prodDep)
                        If milp Is Nothing Then
                            Return False
                        End If

                        If lp.Ofertado AndAlso Not milp.Ofertado Then
                            Return False
                        End If
                        If lp.Establecido AndAlso Not milp.Establecido Then
                            Return False
                        End If
                    Next
                End If
            Next
        End If


        Return True
    End Function



    Public Function CompletarProductosDependientes() As FN.Seguros.Polizas.DN.ColLineaProductoDN


        CompletarProductosDependientes = New FN.Seguros.Polizas.DN.ColLineaProductoDN

        If mColLineaProducto IsNot Nothing Then
            For Each lp As LineaProductoDN In mColLineaProducto
                If (lp.Establecido OrElse lp.Ofertado) AndAlso lp.Producto IsNot Nothing AndAlso lp.Producto.ColProdDependientes IsNot Nothing Then
                    For Each prodDep As ProductoDN In lp.Producto.ColProdDependientes

                        ' como no existe debe añadirse

                        Dim milp As LineaProductoDN = mColLineaProducto.RecuperarLineaProductoxProducto(prodDep)

                        If milp Is Nothing Then
                            milp = CompletarProductosDependientes.RecuperarLineaProductoxProducto(prodDep)
                        End If


                        If milp Is Nothing Then
                            milp = New LineaProductoDN
                            milp.Producto = prodDep
                            CompletarProductosDependientes.AddUnico(milp)
                        End If

                        If lp.Ofertado Then
                            milp.Ofertado = lp.Ofertado()
                        End If
                        If lp.Establecido Then
                            milp.Establecido = lp.Establecido
                        End If





                    Next
                End If
            Next
        End If


        If CompletarProductosDependientes.Count > 0 Then
            mColLineaProducto.AddRange(CompletarProductosDependientes)
            CompletarProductosDependientes.AddRange(CompletarProductosDependientes)
        End If




    End Function

    Private Sub AsignarmeDatosTarifa()
        If mDatosTarifa IsNot Nothing Then
            If mDatosTarifa.Tarifa Is Nothing Then
                mDatosTarifa.Tarifa = Me
            Else
                If mDatosTarifa.Tarifa IsNot Me Then
                    mDatosTarifa.Tarifa = Me
                End If
            End If

        End If
    End Sub

    Private Function AsignarFraccionamientoXML() As String
        If mGrupoFraccionamientos IsNot Nothing Then
            Return mGrupoFraccionamientos.ToXML()
        End If

        Return String.Empty

    End Function

    'Private Function AsignarFraccionamientoFromXML() As GrupoFraccionamientosDN
    '    If Not String.IsNullOrEmpty(mFraccionamientosXML) Then
    '        Dim tr As New System.IO.StringReader(mFraccionamientosXML)
    '        Dim gf As New GrupoFraccionamientosDN()

    '        Return gf.FromXML(tr)
    '    Else
    '        Return Nothing
    '    End If
    'End Function

#End Region


End Class


<Serializable()> _
Public Class ColTarifaDN
    Inherits ArrayListValidable(Of TarifaDN)

End Class





