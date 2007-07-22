Namespace ADOHelper
    Public Class DataSetHelper

#Region "Metodos"
        Public Shared Sub AddColEnumerada(ByRef dt As DataTable, ByVal typo As System.Type, ByVal nombreColOrigenDatos As String, ByVal nombreColDestino As String)
            Dim i As Int64

            Try
                'Comprobamos los datos
                If (dt Is Nothing) Then
                    Throw New ApplicationException("Error: hay que introducir un DataTable")
                End If

                If (typo Is Nothing) Then
                    Throw New ApplicationException("Error: hay que introducir un tipo enumerado")
                End If

                If (nombreColOrigenDatos = String.Empty Or nombreColDestino = String.Empty) Then
                    Throw New ApplicationException("Error: hay que introducir el nombre de la columna origen y destino")
                End If

                'Si el dataset no contiene la columna destino la añadimos
                If (Not dt.Columns.Contains(nombreColDestino)) Then
                    dt.Columns.Add(New DataColumn(nombreColDestino, GetType(String)))
                End If

                'Ahora recorre todas las filas y añade los elementos correspondientes
                For i = 0 To dt.Rows.Count - 1
                    dt.Rows(i)(nombreColDestino) = [Enum].Parse(typo, dt.Rows(i)(nombreColOrigenDatos))
                Next

            Catch ex As Exception
                Throw
            End Try
        End Sub

        Public Shared Sub EliminarNull(ByRef dt As DataTable)
            Dim i As Int64
            Dim Columna As DataColumn

            Try
                If (dt Is Nothing) Then
                    Throw New ApplicationException("Error: hay que introducir un DataTable")
                End If

                ' Recorremos el DataTable y sustituimos todos los null por cadenas vacías
                For i = 0 To dt.Rows.Count - 1
                    For Each Columna In dt.Columns
                        If (Columna.DataType Is GetType(Boolean)) Then
                            If (dt.Rows(i)(Columna) Is Nothing OrElse dt.Rows(i)(Columna) Is DBNull.Value) Then
                                dt.Rows(i)(Columna) = False
                            End If

                        ElseIf (Columna.DataType Is GetType(Decimal) Or Columna.DataType Is GetType(Long) Or Columna.DataType Is GetType(Integer)) Then
                            If (dt.Rows(i)(Columna) Is Nothing OrElse dt.Rows(i)(Columna) Is DBNull.Value) Then
                                dt.Rows(i)(Columna) = 1
                            End If

                        Else
                            If (dt.Rows(i)(Columna) Is Nothing OrElse dt.Rows(i)(Columna) Is DBNull.Value) Then
                                dt.Rows(i)(Columna) = String.Empty
                            End If
                        End If
                    Next Columna
                Next

            Catch ex As Exception
                Throw
            End Try
        End Sub

        'Public Shared Sub FormatearNumeroAMoneda(ByRef dt As DataTable, ByVal NombreColumna As String, ByVal NombreColClonada As String, ByVal NumeroDeDecimales As Int16)
        '    Dim i As Int64
        '    Dim Formateador As Framework.IU.FormateadorMoneda

        '    Try
        '        If (dt Is Nothing) Then
        '            Throw New ApplicationException("Error: hay que introducir un DataTable")
        '        End If

        '        dt.Columns.Add(New DataColumn(NombreColClonada, GetType(String)))
        '        Formateador = New Framework.IU.FormateadorMoneda
        '        Formateador.NumeroDecimales = NumeroDeDecimales

        '        For i = 0 To dt.Rows.Count - 1
        '            dt.Rows(i)(NombreColClonada) = Formateador.Fotmatear(dt.Rows(i)(NombreColumna))
        '        Next

        '    Catch ex As Exception
        '        Throw ex
        '    End Try
        'End Sub

        Public Shared Sub UnirNombreCompleto(ByRef dt As DataTable, ByVal NombreColNombre As String, ByVal NombreColApellido1 As String, ByVal NombreColApellido2 As String, ByVal NombreColDestinoUnion As String, ByVal CaracterSeparador As String)
            Dim i As Int64

            Try
                If (dt Is Nothing) Then
                    Throw New ApplicationException("Error: UnionNombreCompleto --> El DataTable está vacío")
                End If

                If (NombreColNombre = String.Empty Or NombreColApellido1 = String.Empty Or NombreColApellido2 = String.Empty Or NombreColDestinoUnion = String.Empty) Then
                    Throw New ApplicationException("Error: UnionNombreCompleto --> Ninguno de los datos tipo String puede estar vacío")
                End If

                If (CaracterSeparador = String.Empty) Then
                    CaracterSeparador = " "
                End If

                dt.Columns.Add(New DataColumn(NombreColDestinoUnion, GetType(String)))

                'Ahora recorre todas las filas y añade los elementos correspondientes
                For i = 0 To dt.Rows.Count - 1
                    dt.Rows(i)(NombreColDestinoUnion) = dt.Rows(i)(NombreColNombre) & CaracterSeparador & dt.Rows(i)(NombreColApellido1) & CaracterSeparador & dt.Rows(i)(NombreColApellido2)
                Next

            Catch ex As Exception
                Throw
            End Try
        End Sub

        Public Shared Sub UnirDireccion(ByRef dt As DataTable, ByVal NombreTipoVia As String, ByVal NombreNombreVia As String, ByVal NombreNumeroVia As String, ByVal NombreLocalidad As String, ByVal NombreColDestino As String)
            Dim sDireccion As String
            Dim i As Int64

            Try
                If (dt Is Nothing) Then
                    Throw New ApplicationException("Error: UnirDireccion --> El DataTable está vacío")
                End If

                dt.Columns.Add(New DataColumn(NombreColDestino, GetType(String)))

                For i = 0 To dt.Rows.Count - 1
                    sDireccion = dt.Rows(i)(NombreTipoVia) 'pAcpDn.Inmueble.InmuebleBasico.DireccionInmueble.Direccion.ViaDN.TipoVia.Nombre
                    sDireccion = sDireccion & " " & dt.Rows(i)(NombreNombreVia) 'pAcpDn.Inmueble.InmuebleBasico.DireccionInmueble.Direccion.ViaDN.Nombre
                    sDireccion = sDireccion & ", " & dt.Rows(i)(NombreNumeroVia) 'pAcpDn.Inmueble.InmuebleBasico.DireccionInmueble.Direccion.Numero
                    sDireccion = sDireccion & " - " & dt.Rows(i)(NombreLocalidad) 'pAcpDn.Inmueble.InmuebleBasico.DireccionInmueble.Direccion.ViaDN.Localidad.Nombre

                    dt.Rows(i)(NombreColDestino) = sDireccion 'dt.Rows(i)(NombreColNombre) & CaracterSeparador & dt.Rows(i)(NombreColApellido1) & CaracterSeparador & dt.Rows(i)(NombreColApellido2)
                Next

            Catch ex As Exception
                Throw
            End Try
        End Sub

        Public Shared Function ClonarTabla(ByRef dt As DataTable) As DataTable
            Dim dtAux As New DataTable
            Dim i As Int64
            Dim Columna As DataColumn
            Dim dr As DataRow

            Try
                If (dt Is Nothing) Then
                    Throw New ApplicationException("Error: hay que introducir un DataTable")
                End If

                If dt.Rows.Count = 0 Then
                    Throw New ApplicationException("Error: el DataTable está vacío")
                End If

                'Primero creamos las columnas de la nueva tabla
                For Each Columna In dt.Columns
                    dtAux.Columns.Add(New DataColumn(Columna.ColumnName()))
                Next

                'Recorremos todas las filas del DataTable y la añadimos a otro
                For i = 0 To dt.Rows.Count - 1
                    dr = dtAux.NewRow()

                    For Each Columna In dt.Columns
                        dr(Columna.ColumnName) = dt.Rows(i)(Columna.ColumnName)
                    Next

                    dtAux.Rows.Add(dr)
                Next

                Return dtAux

            Catch ex As Exception
                Throw
            End Try
        End Function
#End Region

    End Class
End Namespace
