#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

Imports Framework.TiposYReflexion.LN

#End Region

Namespace DN
    Public Class InfoTypeInstCampoRefDN
        Implements IInfoTypeInstCampoDN

#Region "Atributos"
        Private _CampoRefPadre As InfoTypeInstCampoRefDN
        Private _Campo As FieldInfo
        Private _Instancia As Object
        Protected mPrefijoMap As String = String.Empty
        Private _CamposHijos As List(Of IInfoTypeInstCampoDN)
        Private _InstanciaReferidora As Object
#End Region

#Region "Constructores"
        Public Sub New(ByVal pCampoRefPadre As InfoTypeInstCampoRefDN, ByVal pCampo As FieldInfo, ByVal pInstancia As Object, ByVal pPrefijoMap As String)
            Dim pMensaje As String = String.Empty

            If ValCampo(pCampo, pInstancia, pMensaje) Then
                _Campo = pCampo
                Instancia = pInstancia

            Else
                Throw New ApplicationException(pMensaje)
            End If

            mPrefijoMap = pPrefijoMap
            CampoRefPadre = pCampoRefPadre
        End Sub
#End Region

#Region "Propiedades"
        Public Property InstanciaReferidora() As Object Implements IInfoTypeInstCampoDN.InstanciaReferidora
            Get
                Return _InstanciaReferidora
            End Get
            Set(ByVal Value As Object)
                If (Value.GetType Is _Campo.DeclaringType OrElse Value.GetType.IsSubclassOf(_Campo.DeclaringType)) Then
                    _InstanciaReferidora = Value
                    Instancia = _Campo.GetValue(_InstanciaReferidora)

                Else
                    Throw New ApplicationException("Error: tipo inexacto")
                End If
            End Set
        End Property

        Public Property CampoRefPadre() As InfoTypeInstCampoRefDN Implements IInfoTypeInstCampoDN.CampoRefPadre
            Get
                Return _CampoRefPadre
            End Get
            Set(ByVal Value As InfoTypeInstCampoRefDN)

                If (Value Is Nothing) Then
                    Exit Property
                End If

                ' If (Value.Campo.FieldType Is Me._Campo.DeclaringType) Then
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
                Dim campo As IInfoTypeInstCampoDN

                If (Value Is Nothing) Then
                    Exit Property
                End If

                If (Value.GetType Is Me._Campo.FieldType OrElse Not Value.GetType.GetInterface(_Campo.FieldType.Name) Is Nothing OrElse Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(Value.GetType, Me._Campo.FieldType)) Then
                    _Instancia = Value
                Else
                    Throw New ApplicationException("Error: el tipo no es compatible")
                End If

                'Si tengos campos hijos (por persistencia contenida) he de asignarles su instancia
                If (Not Me._CamposHijos Is Nothing) Then
                    For Each campo In _CamposHijos
                        campo.InstanciaReferidora = Me._Instancia
                    Next
                End If
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
                    Return RecortarNombreCampo(_Campo.Name)

                Else
                    Return mPrefijoMap & "_" & RecortarNombreCampo(_Campo.Name)
                End If
            End Get
        End Property

        Public ReadOnly Property Valor() As Object Implements IInfoTypeInstCampoDN.Valor
            Get
                Return _Instancia
            End Get
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
        Public Shared Function RecortarNombreCampo(ByVal nc As String) As String
            If nc.Substring(0, 1) = "_" Then
                Return nc.Substring(2)
            Else
                Return nc.Substring(1)

            End If
        End Function
#End Region

#Region "Metodos Validacion"
        Public Shared Function ValCampo(ByVal pCampo As System.Reflection.FieldInfo, ByVal pInstancia As Object, ByRef pMensaje As String) As Boolean
            If (pCampo Is Nothing) Then
                pMensaje = "Error: el campo no puede ser nothing"
                Return False
            End If

            If (InstanciacionReflexionHelperLN.EsRef(pCampo.FieldType) = False) Then
                pMensaje = "Error: el campo no es un tipo por referencia"
                Return False
            End If

            If (pInstancia Is Nothing) Then
                Return True
            End If

            'El campo es un campo contenido en el tipo de la instancia 
            If (pCampo.DeclaringType Is pInstancia.GetType) Then
                Return True
            End If

            pMensaje = "Error: el campo no esta declarado en el tipo de la instancia, sino en: " & pCampo.DeclaringType.ToString

            Return False
        End Function
#End Region

    End Class
End Namespace
