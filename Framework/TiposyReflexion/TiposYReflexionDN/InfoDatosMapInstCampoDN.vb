#Region "Importaciones"

Imports System.Collections.Generic

#End Region

Namespace DN
    ' Clase que mapea la informacion de un campo de una clase
    Public Class InfoDatosMapInstCampoDN
        Inherits Framework.DatosNegocio.EntidadDN
#Region "Atributos"
        Protected mInfoDatosMapInstClase As InfoDatosMapInstClaseDN 'La clase padre que la contiene
        Protected mNombreCampo As String
        Protected mColCampoAtributo As New List(Of CampoAtributoDN)
        Protected mDatos As New ArrayList
        Protected mMapSubEntidad As InfoDatosMapInstClaseDN
        Protected mTamañoCampo As Double
#End Region

#Region "Propiedades"

        Public Property TamañoCampo() As Double
            Get
                Return Me.mTamañoCampo
            End Get
            Set(ByVal value As Double)
                mTamañoCampo = value
            End Set
        End Property

        Public Property InfoDatosMapInstClase() As InfoDatosMapInstClaseDN
            Get
                Return mInfoDatosMapInstClase
            End Get
            Set(ByVal Value As InfoDatosMapInstClaseDN)
                mInfoDatosMapInstClase = Value
                mInfoDatosMapInstClase.InfoCampos.Add(Me)
            End Set
        End Property

        Public Property NombreCampo() As String
            Get
                Return mNombreCampo
            End Get
            Set(ByVal value As String)
                mNombreCampo = value
            End Set
        End Property

        Public Property ColCampoAtributo() As List(Of CampoAtributoDN)
            Get
                Return mColCampoAtributo
            End Get
            Set(ByVal value As List(Of CampoAtributoDN))
                mColCampoAtributo = value
            End Set
        End Property

        Public Property Datos() As ArrayList
            Get
                Return mDatos
            End Get
            Set(ByVal value As ArrayList)
                mDatos = value
            End Set
        End Property

        Public Property MapSubEntidad() As InfoDatosMapInstClaseDN
            Get
                Return mMapSubEntidad
            End Get
            Set(ByVal value As InfoDatosMapInstClaseDN)
                mMapSubEntidad = value
            End Set
        End Property

#End Region

#Region "Metodos"
        Public Function RecuperarMApEntidadReferida(ByVal nombreCompletoEntidad As String) As InfoDatosMapInstClaseDN
            Dim i As Integer
            Dim o As Object
            Dim mc As InfoDatosMapInstClaseDN

            For i = 0 To mColCampoAtributo.Count
                o = mColCampoAtributo(i)

                If (TypeOf o Is InfoDatosMapInstClaseDN) Then
                    mc = o

                    If (mc.NombreCompleto = nombreCompletoEntidad) Then
                        Return mc
                    End If
                End If
            Next

            Return Nothing
        End Function
#End Region

    End Class

    Public Class ColInfoDatosMapInstCampoDN
        Inherits Framework.DatosNegocio.ArrayListValidable(Of InfoDatosMapInstCampoDN)



    End Class

End Namespace
