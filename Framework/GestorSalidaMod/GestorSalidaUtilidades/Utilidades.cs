using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Framework.GestorSalida
{
    public class Utilidades
    {
        /// <summary>
        /// Descomprime un archivo zipeado en un directorio determinado.
        /// Si el archivo zipeado contiene varios archivos o carpetas las descomprime en la estructura correspondiente.
        /// </summary>
        /// <param name="DirectorioGeneral">El directorio en el que se quiere descomprimir el archivo</param>
        /// <param name="DocumentoZip">El documento zip cargado en un array de bytes</param>
        public static void Deszipear(string DirectorioGeneral, Byte[] DocumentoZip)
        {
            Stream streamArchivo = new MemoryStream(DocumentoZip);
            using (ZipInputStream zipis = new ZipInputStream(streamArchivo))
            {
                ZipEntry Entrada;
                while ((Entrada = zipis.GetNextEntry()) != null)
                {
                    string nombreDirectorio = Path.GetDirectoryName(Entrada.Name);
                    string nombreFichero = Path.GetFileName(Entrada.Name);

                    //creamos el directorio si se trata de un directorio
                    if (!string.IsNullOrEmpty(nombreDirectorio))
                    {
                        Directory.CreateDirectory(Path.Combine(DirectorioGeneral, nombreDirectorio));
                    }
                    if (!string.IsNullOrEmpty(nombreFichero))
                    {
                        using (FileStream fs = File.Create(Entrada.Name))
                        {
                            int size = 2048;
                            Byte[] datos = new Byte[size];
                            size = zipis.Read(datos, 0, datos.Length);
                            while (size > 0)
                            {
                                fs.Write(datos, 0, size);
                                size = zipis.Read(datos, 0, datos.Length);
                            }
                            fs.Flush();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Genera un documento zipeado a partir de un objeto Documento y con la compresión establecida.
        /// </summary>
        /// <param name="Documento">La clase documento que contiene el documento que se quiere zipear</param>
        /// <param name="Compresion">El nivel de compresión deseado</param>
        /// <returns>Un array de bytes que contiene el archivo comprimido</returns>
        public static Byte[] Zipear(Documento Documento, NivelCompresion Compresion)
        {
            List<Documento> lista = new List<Documento>();
            lista.Add(Documento);
            return Zipear(lista,Compresion);
        }

        /// <summary>
        /// Genera un archivo zipeado que contiene varios archivos, con el nivel de compresión establecido.
        /// </summary>
        /// <param name="Documentos">La lista tipada de todos los documentos que se quieren comprimir</param>
        /// <param name="Compresion">El nivel de compresión que se quiere utilizar</param>
        /// <returns>Un array de bytes que contiene el archivo comprimido</returns>
        public static Byte[] Zipear(List<Documento> Documentos, NivelCompresion Compresion)
        {
            Stream salida = new MemoryStream();
            ZipOutputStream outs = new ZipOutputStream(salida);
            outs.SetLevel((int)Compresion);

            Byte[] Buffer = new Byte[4096];
            foreach (Documento documento in Documentos)
            {
                Stream inputs = new MemoryStream(documento.DocumentoCargado);
                ZipEntry entrada = new ZipEntry(documento.NombreArchivo);
                entrada.DateTime = DateTime.Now;
                outs.PutNextEntry(entrada);

                int bytesOrigen = 1;
                while (bytesOrigen > 0)
                {
                    bytesOrigen = inputs.Read(Buffer, 0, Buffer.Length);
                    outs.Write(Buffer, 0, bytesOrigen);
                }
                inputs.Close();
            }

            outs.Finish();

            Byte[] DocumentoZip = new Byte[salida.Length];
            salida.Position = 0;
            salida.Read(DocumentoZip, 0, (int)salida.Length);
            salida.Close();
            outs.Close();



            return DocumentoZip;
        }


        public enum NivelCompresion
        {
            Optimo = 9,
            MuyAlto = 8,
            Alto = 7,
            MedioAlto = 6,
            Medio = 5,
            MedioBajo = 4,
            BajoAlto = 3,
            BajoMedio = 2,
            Bajo = 1,
            MuyBajo = 0
        }


        public class Documento
        {
            public Byte[] DocumentoCargado;
            public string NombreArchivo;
        }


    }
}
