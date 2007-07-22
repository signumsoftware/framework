#Region "Importaciones"

Imports System.Collections.Generic

#End Region

Namespace DN
    'Clase que mapea la informacion de una clase por completo
    Public Class InfoDatosMapInstClaseDN

#Region "Atributos"
        Protected mNombreCompleto As String
        Protected mInfoCampos As New ColInfoDatosMapInstCampoDN
        Protected mHTDatos As New Hashtable
        Protected mColTriger As New List(Of Triger)
        Protected mColTiposTrazas As New List(Of Type)
        Protected mColTipoAtributo As New List(Of TipoAtributo)
        Protected mTablaHistoria As String
#End Region

#Region "Propiedades"







        Public Property TablaHistoria() As String

            Get
                Return mTablaHistoria
            End Get

            Set(ByVal value As String)
                mTablaHistoria = value

            End Set
        End Property





        Public Property ColTipoAtributo() As List(Of TipoAtributo)
            Get
                Return Me.mColTipoAtributo
            End Get
            Set(ByVal value As List(Of TipoAtributo))
                Me.mColTipoAtributo = value
            End Set
        End Property


        Public Property ColTiposTrazas() As List(Of Type)
            Get
                Return mColTiposTrazas
            End Get
            Set(ByVal value As List(Of Type))
                mColTiposTrazas = value
            End Set
        End Property
        Public Property ColTriger() As List(Of Triger)
            Get
                Return mColTriger
            End Get
            Set(ByVal value As List(Of Triger))
                mColTriger = value
            End Set
        End Property


        Public Property NombreCompleto() As String
            Get
                Return mNombreCompleto
            End Get
            Set(ByVal value As String)
                mNombreCompleto = value
            End Set
        End Property

        Public Property InfoCampos() As ColInfoDatosMapInstCampoDN
            Get
                Return mInfoCampos
            End Get
            Set(ByVal value As ColInfoDatosMapInstCampoDN)
                mInfoCampos = value
            End Set
        End Property

        Public Property HTDatos() As Hashtable
            Get
                Return mHTDatos
            End Get
            Set(ByVal value As Hashtable)
                mHTDatos = value
            End Set
        End Property

        Public Property ItemDatoMapeado(ByVal pTipo As TiposDatosMapInstClaseDN) As Object
            Get
                Return mHTDatos.Item(pTipo)
            End Get
            Set(ByVal Value As Object)
                mHTDatos.Item(pTipo) = Value
            End Set
        End Property
#End Region

#Region "Metodos"


        'Public Sub AddTriger(ByVal pNombre As String, ByVal pSentencia As String)



        '    mTrigers.Add()
        'End Sub


        Public Function GetCampoXNombre(ByVal pNombre As String) As InfoDatosMapInstCampoDN
            Dim infoCampo As InfoDatosMapInstCampoDN
            Dim nombreCampo As String
            If pNombre.Substring(0, 1) = "_" Then
                nombreCampo = pNombre.Substring(1)
            Else
                nombreCampo = pNombre
            End If
            For Each infoCampo In mInfoCampos
                If (infoCampo.NombreCampo = nombreCampo) Then
                    Return infoCampo
                End If
            Next

            Return Nothing
        End Function


        Public Function GetCampoXNombre(ByVal cv As Framework.TiposYReflexion.DN.InfoTypeInstCampoValDN) As InfoDatosMapInstCampoDN
            Return GetCampoXNombre(cv.Campo.Name)
        End Function
#End Region

    End Class



    Public Class Triger

        Public Sub New(ByVal pNombre As String, ByVal pSentencia As String)
            Nombre = pNombre
            Sentencia = pSentencia
        End Sub

        Public Nombre As String
        Public Sentencia As String



    End Class

End Namespace


Public Enum TipoAtributo

    tablaHistoricoa
End Enum

