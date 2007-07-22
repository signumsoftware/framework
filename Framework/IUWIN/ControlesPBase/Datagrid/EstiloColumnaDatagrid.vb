Imports System
Imports System.ComponentModel


Namespace Datagrid.Estilo


    Public Class EstiloColumnaDatagrid


#Region "campos"
        Private mIzquierda As Integer
        Private mDerecha As Integer
        Private mTop As Integer
        Private mBottom As Integer
#End Region


#Region "propiedades"

        Public Property Izquierda() As Integer
            Get
                Return mIzquierda
            End Get
            Set(ByVal Value As Integer)
                mIzquierda = Value
            End Set
        End Property

        Public Property Derecha() As Integer
            Get
                Return mDerecha
            End Get
            Set(ByVal Value As Integer)
                mDerecha = Value
            End Set
        End Property

        Public Property Top() As Integer
            Get
                Return mTop
            End Get
            Set(ByVal Value As Integer)
                mTop = Value
            End Set
        End Property

        Public Property Bottom() As Integer
            Get
                Return mBottom
            End Get
            Set(ByVal Value As Integer)
                mBottom = Value
            End Set
        End Property

#End Region

#Region "constructor"
        Public Sub New(ByVal pValue As Integer)
            Me.EstablecerEstilo(pValue)
        End Sub

        Public Sub New(ByVal pTop As Integer, ByVal pDerecha As Integer, ByVal pBottom As Integer, ByVal pIzquierda As Integer)
            Me.ActualizarValoresEstilo(pTop, pDerecha, pBottom, pIzquierda)
        End Sub
#End Region

#Region "métodos"

        Public Overloads Sub EstablecerEstilo(ByVal pValue As Integer)
            mIzquierda = pValue
            mDerecha = pValue
            mTop = pValue
            mBottom = pValue
        End Sub

        Public Overloads Sub EstablecerEstilo(ByVal pTop As Integer, ByVal pDerecha As Integer, ByVal pBottom As Integer, ByVal pIzquierda As Integer)
            ActualizarValoresEstilo(pTop, pDerecha, pBottom, pIzquierda)
        End Sub

        Private Sub ActualizarValoresEstilo(ByVal pTop As Integer, ByVal pDerecha As Integer, ByVal pBottom As Integer, ByVal pIzquierda As Integer)
            mIzquierda = pIzquierda
            mDerecha = pDerecha
            mTop = pTop
            mBottom = pBottom
        End Sub

#End Region


    End Class



End Namespace

