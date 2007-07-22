#Region "Importaciones"

Imports System.Collections.Generic

#End Region

Namespace DN
    Public Class InfoTypeInstClaseDN

#Region "Atributos"
        Private _Instancia As Object
        Private _Tipo As System.Type
        Private _IdVal As InfoTypeInstCampoValDN
        Private _CamposValOriginal As List(Of InfoTypeInstCampoValDN)
        Private _CamposVal As List(Of InfoTypeInstCampoValDN)
        Private _CamposRef As List(Of InfoTypeInstCampoRefDN)
        Private _CamposRefContenidos As List(Of InfoTypeInstCampoRefDN)
        Private _CamposRefExteriores As List(Of InfoTypeInstCampoRefDN)
        Private _VinculosClasesCache As List(Of VinculoClaseDN)

#End Region

#Region "Constructores"
        Public Sub New(ByVal pTipo As System.Type)
            _Tipo = pTipo
        End Sub
#End Region

#Region "Propiedades"

        Public Property VinculosClasesCache() As List(Of VinculoClaseDN)
            Get
                Return _VinculosClasesCache
            End Get
            Set(ByVal value As List(Of VinculoClaseDN))
                _VinculosClasesCache = value
            End Set
        End Property

        Public Property InstanciaPrincipal() As Object
            Get
                Return Me._Instancia
            End Get

            Set(ByVal Value As Object)
                Dim mensaje As String = String.Empty

                If (Value Is Nothing) Then
                    Me._Instancia = Nothing
                    Exit Property
                End If

                If (ValInstancia(Me._Tipo, Value, mensaje) = False) Then
                    Throw New ApplicationException(mensaje)
                End If

                Me._Instancia = Value
                _Tipo = _Instancia.GetType

                'Vincular los campos de valor incluidos con sus instancias
                VincularCamposValorIncluidosAInstancias()
            End Set
        End Property

        Public Property IdVal() As InfoTypeInstCampoValDN
            Get
                Return _IdVal
            End Get
            Set(ByVal Value As InfoTypeInstCampoValDN)
                _IdVal = Value
            End Set
        End Property

        Public Property CamposRef() As List(Of InfoTypeInstCampoRefDN)
            Get
                Return _CamposRef
            End Get
            Set(ByVal Value As List(Of InfoTypeInstCampoRefDN))
                _CamposRef = Value
            End Set
        End Property

        Public Property CamposValOriginal() As List(Of InfoTypeInstCampoValDN)
            Get
                Return _CamposValOriginal
            End Get
            Set(ByVal Value As List(Of InfoTypeInstCampoValDN))
                _CamposValOriginal = Value
            End Set
        End Property

        Public Property CamposVal() As List(Of InfoTypeInstCampoValDN)
            Get
                Return _CamposVal
            End Get
            Set(ByVal Value As List(Of InfoTypeInstCampoValDN))
                _CamposVal = Value
            End Set
        End Property

        Public Property CamposRefContenidos() As List(Of InfoTypeInstCampoRefDN)
            Get
                Return _CamposRefContenidos
            End Get
            Set(ByVal Value As List(Of InfoTypeInstCampoRefDN))
                _CamposRefContenidos = Value
            End Set
        End Property

        Public Property CamposRefExteriores() As List(Of InfoTypeInstCampoRefDN)
            Get
                Return _CamposRefExteriores
            End Get
            Set(ByVal Value As List(Of InfoTypeInstCampoRefDN))
                _CamposRefExteriores = Value
            End Set
        End Property

        Public ReadOnly Property TablaNombre() As String
            Get

                Dim tipo As System.Type
                Dim fijacion As TiposYReflexion.DN.FijacionDeTipoDN
                ' Dim ih As TiposYReflexion.LN.InstanciacionReflexionHelperLN


                If _Tipo.Name.Contains("`") Then

                    tipo = TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(_Tipo, fijacion)

                    Return "tl" & _Tipo.Name.Replace("`", tipo.Name)
                Else
                    Return "tl" & _Tipo.Name
                End If

            End Get
        End Property

        Public Property Tipo() As System.Type
            Get
                Return Me._Tipo
            End Get
            Set(ByVal Value As System.Type)
                Dim mensaje As String = String.Empty

                If (ValTipo(Value, Me._Instancia, mensaje) = False) Then
                    Throw New ApplicationException(mensaje)
                End If

                Me._Tipo = Value
            End Set
        End Property
#End Region

#Region "Metodos"
        Private Sub VincularCamposValorIncluidosAInstancias()
            Dim cVal As InfoTypeInstCampoValDN
            Dim cRef As InfoTypeInstCampoRefDN

            For Each cVal In _CamposValOriginal
                cVal.Instancia = _Instancia
            Next

            For Each cRef In _CamposRef
                cRef.InstanciaReferidora = _Instancia
            Next

            For Each cVal In _CamposVal
                If (_CamposValOriginal.Contains(cVal) = False) Then
                    cVal.Instancia = cVal.CampoRefPAdre.Instancia
                End If
            Next
        End Sub

        Private Sub AsignarInstanciasCamposValReferidosHijos(ByVal pCampoValor As InfoTypeInstCampoValDN, ByVal pCamposValExteriores As List(Of InfoTypeInstCampoValDN))
            Dim colHijos As List(Of InfoTypeInstCampoValDN)
            Dim cValSub As InfoTypeInstCampoValDN

            colHijos = CamposValHijosDeCampoval(pCampoValor, pCamposValExteriores)

            For Each cValSub In colHijos
                cValSub.InstanciaReferidora = pCampoValor.Instancia
                AsignarInstanciasCamposValReferidosHijos(cValSub, colHijos)
            Next
        End Sub

        Private Sub AsignarInstanciasCamposReferidosHijos(ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal pCamposRefExteriores As List(Of InfoTypeInstCampoRefDN))
            Dim colHijos As List(Of InfoTypeInstCampoRefDN)
            Dim cRefSub As InfoTypeInstCampoRefDN

            colHijos = CamposRefHijosDeCampoRef(pCampoRef, pCamposRefExteriores)

            If (pCampoRef.Instancia Is Nothing) Then
                Exit Sub
            End If

            For Each cRefSub In colHijos
                cRefSub.InstanciaReferidora = pCampoRef.Instancia
                AsignarInstanciasCamposReferidosHijos(cRefSub, colHijos)
            Next
        End Sub

        Private Function CamposValHijosDeCampoval(ByVal pCamposVal As InfoTypeInstCampoValDN, ByVal pCamposValExteriores As List(Of InfoTypeInstCampoValDN)) As List(Of InfoTypeInstCampoValDN)
            Dim cValSub As InfoTypeInstCampoValDN

            CamposValHijosDeCampoval = New List(Of InfoTypeInstCampoValDN)

            For Each cValSub In pCamposValExteriores
                If cValSub.CampoRefPAdre Is pCamposVal Then
                    CamposValHijosDeCampoval.Add(cValSub)
                End If
            Next
        End Function

        Private Function CamposRefHijosDeCampoRef(ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal pCamposRefExteriores As List(Of InfoTypeInstCampoRefDN)) As List(Of InfoTypeInstCampoRefDN)
            Dim cRefSub As InfoTypeInstCampoRefDN

            CamposRefHijosDeCampoRef = New List(Of InfoTypeInstCampoRefDN)

            For Each cRefSub In CamposRefExteriores
                If cRefSub.CampoRefPadre Is pCampoRef Then
                    CamposRefHijosDeCampoRef.Add(cRefSub)
                End If
            Next
        End Function
#End Region

#Region "Metodos Validacion"
        Private Function ValInstancia(ByVal pTipo As System.Type, ByVal pInstancia As Object, ByRef pMensaje As String) As Boolean
            If (pTipo Is Nothing) Then
                Return True
            End If

            If (pInstancia.GetType Is pTipo) Then
                Return True
            End If

            pMensaje = "Error: no coinciden los tipos del tipo y la instancia"

            Return False
        End Function

        Private Function ValTipo(ByVal pTipo As System.Type, ByVal pInstancia As Object, ByRef pMensaje As String) As Boolean
            If (pInstancia Is Nothing) Then
                Return True
            End If

            If (pInstancia.GetType Is pTipo) Then
                Return True
            End If

            pMensaje = "Error: no coinciden los tipos del tipo y la instancia"

            Return False
        End Function
#End Region

    End Class
End Namespace
