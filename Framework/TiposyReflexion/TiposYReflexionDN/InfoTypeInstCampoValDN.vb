#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

Imports Framework.TiposYReflexion.LN

#End Region

Namespace DN
    Public Class InfoTypeInstCampoValDN
        Implements IInfoTypeInstCampoDN

#Region "Atributos"
        Private _Campo As FieldInfo
        Private _Instancia As Object
        Protected mPrefijoMap As String = String.Empty
        Private _CampoRefPadre As InfoTypeInstCampoRefDN
        Private _InstanciaReferidora As Object
        Private _CamposHijos As List(Of IInfoTypeInstCampoDN)
#End Region

#Region "Constructores"
        Public Sub New(ByVal pCampoRefPadre As InfoTypeInstCampoRefDN, ByVal pCampo As FieldInfo, ByVal pInstancia As Object, ByVal pPrefijoMap As String)
            Dim pMensaje As String = String.Empty

            If (ValCampo(pCampo, pInstancia, pMensaje)) Then
                _Campo = pCampo
                _Instancia = pInstancia

            Else
                Throw New ApplicationException(pMensaje)
            End If

            _CampoRefPadre = pCampoRefPadre
            mPrefijoMap = pPrefijoMap
        End Sub
#End Region

#Region "Propiedades"
        Public Property CampoRefPAdre() As InfoTypeInstCampoRefDN Implements IInfoTypeInstCampoDN.CampoRefPadre
            Get
                Return _CampoRefPadre
            End Get
            Set(ByVal Value As InfoTypeInstCampoRefDN)
                If (Value.Campo.FieldType Is Me._Campo.ReflectedType) Then
                    _CampoRefPadre = Value

                Else
                    Throw New ApplicationException("Error: el tipo no es compatible")
                End If

                _CampoRefPadre.CamposHijos.Add(Me)
            End Set
        End Property

        Public Property Instancia() As Object Implements IInfoTypeInstCampoDN.Instancia
            Get
                Return _Instancia
            End Get
            Set(ByVal Value As Object)
                _Instancia = Value
            End Set
        End Property

        Public Property Campo() As FieldInfo Implements IInfoTypeInstCampoDN.Campo
            Get
                Return _Campo
            End Get
            Set(ByVal Value As System.Reflection.FieldInfo)
                If (InstanciacionReflexionHelperLN.EsRef(Value.FieldType)) Then
                    Throw New ApplicationException("Error: tipo incorrecto")

                Else
                    _Campo = Value
                End If
            End Set
        End Property

        Public ReadOnly Property NombreMap() As String Implements IInfoTypeInstCampoDN.NombreMap
            Get
                'El nombre que corresponde al campo en la fuente de datos
                'Debug.WriteLine(_Campo.Name)
                'If _Campo.Name.Substring(0, 1) <> "m" OrElse _Campo.Name.Contains("mPeriodoPlanificado") Then
                '    Beep()
                'End If



                If (mPrefijoMap = String.Empty) Then
         
                    Return _Campo.Name.Substring(1)

                Else
                    Return mPrefijoMap & "_" & _Campo.Name.Substring(1)
                End If
            End Get
        End Property

        Public ReadOnly Property Valor() As Object Implements IInfoTypeInstCampoDN.Valor
            Get
                If _Instancia Is Nothing Then
                    Return Nothing

                Else
                    Return _Campo.GetValue(_Instancia)
                End If
            End Get
        End Property

        Public Property InstanciaReferidora() As Object Implements IInfoTypeInstCampoDN.InstanciaReferidora
            Get
                Return _InstanciaReferidora
            End Get
            Set(ByVal Value As Object)
                If (Value.GetType Is _Campo.DeclaringType OrElse Value.GetType.IsSubclassOf(_Campo.DeclaringType)) Then
                    _InstanciaReferidora = Value
                    _Instancia = _Campo.GetValue(_InstanciaReferidora)

                Else
                    Throw New ApplicationException("Error: tipo inexacto")
                End If
            End Set
        End Property

        Public Property CamposHijos() As List(Of IInfoTypeInstCampoDN) Implements IInfoTypeInstCampoDN.CamposHijos
            Get
                If (_CamposHijos Is Nothing) Then
                    _CamposHijos = New List(Of IInfoTypeInstCampoDN)
                End If

                Return _CamposHijos
            End Get
            Set(ByVal Value As List(Of IInfoTypeInstCampoDN))
                _CamposHijos = Value
            End Set
        End Property
#End Region

#Region "Metodos Validacion"
        Public Shared Function ValCampo(ByVal pCampo As FieldInfo, ByVal pInstancia As Object, ByRef pMensaje As String) As Boolean
            If (pCampo Is Nothing) Then
                pMensaje = "Error: el campo no puede ser nothing"
                Return False
            End If

            If (pInstancia Is Nothing) Then
                Return True
            End If

            'El campo es un campo contenido en el tipo de la instancia 
            If (pCampo.ReflectedType Is pInstancia.GetType) Then
                Return True
            End If

            pMensaje = "Error: el campo no esta declarado en el tipo de la instancia, sino en: " & pCampo.DeclaringType.ToString

            Return False
        End Function
#End Region

    End Class
End Namespace
