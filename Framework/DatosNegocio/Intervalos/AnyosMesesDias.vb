
Namespace Localizaciones.Temporales

    <Serializable()> Public Class AnyosMesesDias
        Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

        Protected mSegundos As Integer
        Protected mMinutos As Integer
        Protected mHoras As Integer
        Protected mAnyos As Integer
        Protected mMeses As Integer
        Protected mDias As Integer

#End Region

#Region "Propiedades"

        Public Property Segundos() As Integer
            Get
                Return mSegundos
            End Get
            Set(ByVal value As Integer)
                mSegundos = value
            End Set
        End Property

        Public Property Minutos() As Integer
            Get
                Return mMinutos
            End Get
            Set(ByVal value As Integer)
                mMinutos = value
            End Set
        End Property

        Public Property Horas() As Integer
            Get
                Return mHoras
            End Get
            Set(ByVal value As Integer)
                mHoras = value
            End Set
        End Property

        Public Property Anyos() As Integer
            Get
                Return mAnyos
            End Get
            Set(ByVal value As Integer)
                mAnyos = value
            End Set
        End Property

        Public Property Meses() As Integer
            Get
                Return mMeses
            End Get
            Set(ByVal value As Integer)
                mMeses = value
            End Set
        End Property

        Public Property Dias() As Integer
            Get
                Return mDias
            End Get
            Set(ByVal value As Integer)
                mDias = value
            End Set
        End Property

#End Region

#Region "Métodos"

        Public Function IncrementarFecha(ByVal pFechaOriginal As Date) As Date
            Dim fr As Date = pFechaOriginal.AddYears(Me.mAnyos)
            fr = fr.AddMonths(Me.mMeses)
            fr = fr.AddDays(Me.mDias)

            fr = fr.Add(New TimeSpan(Me.mHoras, Me.mMinutos, Me.mSegundos))
            Return fr

        End Function

        Public Shared Function CalcularDirAMD(ByVal fechaMayor As Date, ByVal fechaMenor As Date) As AnyosMesesDias
            Dim amd As New AnyosMesesDias()
            Dim ts As TimeSpan

            If fechaMayor = Nothing OrElse fechaMenor = Nothing OrElse fechaMenor > fechaMayor Then
                Return Nothing
            End If

            amd.Anyos = fechaMayor.Year - fechaMenor.Year
            amd.Meses = fechaMayor.Month - fechaMenor.Month
            If fechaMayor.Month < fechaMenor.Month OrElse (fechaMayor.Month = fechaMenor.Month And fechaMayor.Day < fechaMenor.Day) Then
                amd.Anyos = amd.Anyos - 1
                amd.Meses = amd.Meses + 11
            End If

            fechaMenor = fechaMenor.AddYears(amd.Anyos)
            fechaMenor = fechaMenor.AddMonths(amd.Meses)

            ts = fechaMayor.Subtract(fechaMenor)
            amd.Dias = ts.Days

            Return amd

        End Function

        Public Overrides Function ToString() As String
            Dim salida As String = String.Empty
            If Me.Anyos <> 0 Then
                salida += Me.mAnyos.ToString() & " años"
            End If
            If Me.Meses <> 0 Then
                If Not String.IsNullOrEmpty(salida) Then
                    salida += " "
                End If
                salida += Me.Meses.ToString & " meses"
            End If
            If Me.Dias <> 0 Then
                If Not String.IsNullOrEmpty(salida) Then
                    salida += " "
                End If
                salida += Me.Dias.ToString()
                If Me.Dias = 1 Then
                    salida += " día"
                Else
                    salida += " días"
                End If
            End If
            Return salida
        End Function

        Public Function TotalDiasDesde(ByVal pFechaInicio As Date) As Integer
            Return IncrementarFecha(pFechaInicio).Subtract(pFechaInicio).TotalDays
        End Function

#End Region


    End Class

End Namespace
