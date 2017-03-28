using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Reflection;
using System.Windows;
using Signum.Entities.Basics;

namespace Signum.Windows.Extensions.Files
{
    public static class DropOutlookAttachmentExtension
    {
        public static bool CanHandleOutlookAttachment(this DragEventArgs e)
        {
            return e.Data.GetDataPresent("FileGroupDescriptor");
        }

        public static List<FileContent> DropOutlookAttachment(this DragEventArgs e)
        {
            if (!e.CanHandleOutlookAttachment())
                return null;

            List<FileContent> response = new List<FileContent>(); 

            OutlookDataObject dataObject = new OutlookDataObject(e.Data);

            string[] filenames = (string[])dataObject.GetData("FileGroupDescriptor");
            MemoryStream[] filestreams = (MemoryStream[])dataObject.GetData("FileContents");

            for (int fileIndex = 0; fileIndex < filenames.Length; fileIndex++)
            {
                string filename = filenames[fileIndex];
                MemoryStream filestream = filestreams[fileIndex];

                byte[] data = ReadFully(filestream);

                response.Add(new FileContent(filename, data));
            }

            return response;
        }

        static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

    }


    class OutlookDataObject : System.Windows.IDataObject
    {
        public OutlookDataObject(System.Windows.IDataObject underlyingDataObject)
        {
            this.underlyingDataObject = underlyingDataObject;
            this.comUnderlyingDataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)this.underlyingDataObject;
            FieldInfo innerDataField = this.underlyingDataObject.GetType().GetField("_innerData", BindingFlags.NonPublic | BindingFlags.Instance);
            this.oleUnderlyingDataObject = (System.Windows.IDataObject)innerDataField.GetValue(this.underlyingDataObject);
            this.getDataFromHGLOBALMethod = this.oleUnderlyingDataObject.GetType().GetMethod("GetDataFromHGLOBAL", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private class NativeMethods
        {
            [DllImport("kernel32.dll")]
            static extern IntPtr GlobalLock(IntPtr hMem);

            [DllImport("ole32.dll", PreserveSig = false)]
            internal static extern ILockBytes CreateILockBytesOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease);

            [DllImport("OLE32.DLL", CharSet = CharSet.Auto, PreserveSig = false)]
            internal static extern IntPtr GetHGlobalFromILockBytes(ILockBytes pLockBytes);

            [DllImport("OLE32.DLL", CharSet = CharSet.Unicode, PreserveSig = false)]
            internal static extern IStorage StgCreateDocfileOnILockBytes(ILockBytes plkbyt, uint grfMode, uint reserved);

            [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000000B-0000-0000-C000-000000000046")]
            internal interface IStorage
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                IStream CreateStream([In, MarshalAs(UnmanagedType.BStr)] string pwcsName, [In, MarshalAs(UnmanagedType.U4)] int grfMode, [In, MarshalAs(UnmanagedType.U4)] int reserved1, [In, MarshalAs(UnmanagedType.U4)] int reserved2);
                [return: MarshalAs(UnmanagedType.Interface)]
                IStream OpenStream([In, MarshalAs(UnmanagedType.BStr)] string pwcsName, IntPtr reserved1, [In, MarshalAs(UnmanagedType.U4)] int grfMode, [In, MarshalAs(UnmanagedType.U4)] int reserved2);
                [return: MarshalAs(UnmanagedType.Interface)]
                IStorage CreateStorage([In, MarshalAs(UnmanagedType.BStr)] string pwcsName, [In, MarshalAs(UnmanagedType.U4)] int grfMode, [In, MarshalAs(UnmanagedType.U4)] int reserved1, [In, MarshalAs(UnmanagedType.U4)] int reserved2);
                [return: MarshalAs(UnmanagedType.Interface)]
                IStorage OpenStorage([In, MarshalAs(UnmanagedType.BStr)] string pwcsName, IntPtr pstgPriority, [In, MarshalAs(UnmanagedType.U4)] int grfMode, IntPtr snbExclude, [In, MarshalAs(UnmanagedType.U4)] int reserved);
                void CopyTo(int ciidExclude, [In, MarshalAs(UnmanagedType.LPArray)] Guid[] pIIDExclude, IntPtr snbExclude, [In, MarshalAs(UnmanagedType.Interface)] IStorage stgDest);
                void MoveElementTo([In, MarshalAs(UnmanagedType.BStr)] string pwcsName, [In, MarshalAs(UnmanagedType.Interface)] IStorage stgDest, [In, MarshalAs(UnmanagedType.BStr)] string pwcsNewName, [In, MarshalAs(UnmanagedType.U4)] int grfFlags);
                void Commit(int grfCommitFlags);
                void Revert();
                void EnumElements([In, MarshalAs(UnmanagedType.U4)] int reserved1, IntPtr reserved2, [In, MarshalAs(UnmanagedType.U4)] int reserved3, [MarshalAs(UnmanagedType.Interface)] out object ppVal);
                void DestroyElement([In, MarshalAs(UnmanagedType.BStr)] string pwcsName);
                void RenameElement([In, MarshalAs(UnmanagedType.BStr)] string pwcsOldName, [In, MarshalAs(UnmanagedType.BStr)] string pwcsNewName);
                void SetElementTimes([In, MarshalAs(UnmanagedType.BStr)] string pwcsName, [In] System.Runtime.InteropServices.ComTypes.FILETIME pctime, [In] System.Runtime.InteropServices.ComTypes.FILETIME patime, [In] System.Runtime.InteropServices.ComTypes.FILETIME pmtime);
                void SetClass([In] ref Guid clsid);
                void SetStateBits(int grfStateBits, int grfMask);
                void Stat([Out]out System.Runtime.InteropServices.ComTypes.STATSTG pStatStg, int grfStatFlag);
            }

            [ComImport, Guid("0000000A-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface ILockBytes
            {
                void ReadAt([In, MarshalAs(UnmanagedType.U8)] long ulOffset, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pv, [In, MarshalAs(UnmanagedType.U4)] int cb, [Out, MarshalAs(UnmanagedType.LPArray)] int[] pcbRead);
                void WriteAt([In, MarshalAs(UnmanagedType.U8)] long ulOffset, IntPtr pv, [In, MarshalAs(UnmanagedType.U4)] int cb, [Out, MarshalAs(UnmanagedType.LPArray)] int[] pcbWritten);
                void Flush();
                void SetSize([In, MarshalAs(UnmanagedType.U8)] long cb);
                void LockRegion([In, MarshalAs(UnmanagedType.U8)] long libOffset, [In, MarshalAs(UnmanagedType.U8)] long cb, [In, MarshalAs(UnmanagedType.U4)] int dwLockType);
                void UnlockRegion([In, MarshalAs(UnmanagedType.U8)] long libOffset, [In, MarshalAs(UnmanagedType.U8)] long cb, [In, MarshalAs(UnmanagedType.U4)] int dwLockType);
                void Stat([Out]out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, [In, MarshalAs(UnmanagedType.U4)] int grfStatFlag);
            }

            [StructLayout(LayoutKind.Sequential)]
            internal sealed class POINTL
            {
                internal int x;
                internal int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal sealed class SIZEL
            {
                internal int cx;
                internal int cy;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            internal sealed class FILEGROUPDESCRIPTORA
            {
                internal uint cItems;
                internal FILEDESCRIPTORA[] fgd;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            internal sealed class FILEDESCRIPTORA
            {
                internal uint dwFlags;
                internal Guid clsid;
                internal SIZEL sizel;
                internal POINTL pointl;
                internal uint dwFileAttributes;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
                internal uint nFileSizeHigh;
                internal uint nFileSizeLow;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                internal string cFileName;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal sealed class FILEGROUPDESCRIPTORW
            {
                internal uint cItems;
                internal FILEDESCRIPTORW[] fgd;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal sealed class FILEDESCRIPTORW
            {
                internal uint dwFlags;
                internal Guid clsid;
                internal SIZEL sizel;
                internal POINTL pointl;
                internal uint dwFileAttributes;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
                internal uint nFileSizeHigh;
                internal uint nFileSizeLow;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                internal string cFileName;
            }
        }

        private System.Windows.IDataObject underlyingDataObject;
        private System.Runtime.InteropServices.ComTypes.IDataObject comUnderlyingDataObject;
        private System.Windows.IDataObject oleUnderlyingDataObject;
        private MethodInfo getDataFromHGLOBALMethod;

        public object GetData(Type format)
        {
            return this.GetData(format.FullName);
        }

        public object GetData(string format)
        {
            return this.GetData(format, true);
        }

        public object GetData(string format, bool autoConvert)
        {
            switch (format)
            {
                case "FileGroupDescriptor":
                    IntPtr fileGroupDescriptorAPointer = IntPtr.Zero;
                    try
                    {
                        MemoryStream fileGroupDescriptorStream = (MemoryStream)this.underlyingDataObject.GetData("FileGroupDescriptor", autoConvert);
                        byte[] fileGroupDescriptorBytes = new byte[fileGroupDescriptorStream.Length];
                        fileGroupDescriptorStream.Read(fileGroupDescriptorBytes, 0, fileGroupDescriptorBytes.Length);
                        fileGroupDescriptorStream.Close();

                        fileGroupDescriptorAPointer = Marshal.AllocHGlobal(fileGroupDescriptorBytes.Length);
                        Marshal.Copy(fileGroupDescriptorBytes, 0, fileGroupDescriptorAPointer, fileGroupDescriptorBytes.Length);

                        object fileGroupDescriptorObject = Marshal.PtrToStructure(fileGroupDescriptorAPointer, typeof(NativeMethods.FILEGROUPDESCRIPTORA));
                        NativeMethods.FILEGROUPDESCRIPTORA fileGroupDescriptor = (NativeMethods.FILEGROUPDESCRIPTORA)fileGroupDescriptorObject;

                        string[] fileNames = new string[fileGroupDescriptor.cItems];

                        IntPtr fileDescriptorPointer = (IntPtr)((int)fileGroupDescriptorAPointer + Marshal.SizeOf(fileGroupDescriptor.cItems));

                        for (int fileDescriptorIndex = 0; fileDescriptorIndex < fileGroupDescriptor.cItems; fileDescriptorIndex++)
                        {
                            NativeMethods.FILEDESCRIPTORA fileDescriptor = (NativeMethods.FILEDESCRIPTORA)Marshal.PtrToStructure(fileDescriptorPointer, typeof(NativeMethods.FILEDESCRIPTORA));
                            fileNames[fileDescriptorIndex] = fileDescriptor.cFileName;

                            fileDescriptorPointer = (IntPtr)((int)fileDescriptorPointer + Marshal.SizeOf(fileDescriptor));
                        }

                        return fileNames;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(fileGroupDescriptorAPointer);
                    }

                case "FileGroupDescriptorW":
                    IntPtr fileGroupDescriptorWPointer = IntPtr.Zero;
                    try
                    {
                        MemoryStream fileGroupDescriptorStream = (MemoryStream)this.underlyingDataObject.GetData("FileGroupDescriptorW");
                        byte[] fileGroupDescriptorBytes = new byte[fileGroupDescriptorStream.Length];
                        fileGroupDescriptorStream.Read(fileGroupDescriptorBytes, 0, fileGroupDescriptorBytes.Length);
                        fileGroupDescriptorStream.Close();

                        fileGroupDescriptorWPointer = Marshal.AllocHGlobal(fileGroupDescriptorBytes.Length);
                        Marshal.Copy(fileGroupDescriptorBytes, 0, fileGroupDescriptorWPointer, fileGroupDescriptorBytes.Length);

                        object fileGroupDescriptorObject = Marshal.PtrToStructure(fileGroupDescriptorWPointer, typeof(NativeMethods.FILEGROUPDESCRIPTORW));
                        NativeMethods.FILEGROUPDESCRIPTORW fileGroupDescriptor = (NativeMethods.FILEGROUPDESCRIPTORW)fileGroupDescriptorObject;

                        string[] fileNames = new string[fileGroupDescriptor.cItems];

                        IntPtr fileDescriptorPointer = (IntPtr)((int)fileGroupDescriptorWPointer + Marshal.SizeOf(fileGroupDescriptor.cItems));

                        for (int fileDescriptorIndex = 0; fileDescriptorIndex < fileGroupDescriptor.cItems; fileDescriptorIndex++)
                        {
                            NativeMethods.FILEDESCRIPTORW fileDescriptor = (NativeMethods.FILEDESCRIPTORW)Marshal.PtrToStructure(fileDescriptorPointer, typeof(NativeMethods.FILEDESCRIPTORW));
                            fileNames[fileDescriptorIndex] = fileDescriptor.cFileName;

                            fileDescriptorPointer = (IntPtr)((int)fileDescriptorPointer + Marshal.SizeOf(fileDescriptor));
                        }

                        return fileNames;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(fileGroupDescriptorWPointer);
                    }

                case "FileContents":
                    string fgdFormatName;
                    if (GetDataPresent("FileGroupDescriptorW"))
                        fgdFormatName = "FileGroupDescriptorW";
                    else if (GetDataPresent("FileGroupDescriptor"))
                        fgdFormatName = "FileGroupDescriptor";
                    else
                        return null;

                    string[] fileContentNames = (string[])this.GetData(fgdFormatName);

                    MemoryStream[] fileContents = new MemoryStream[fileContentNames.Length];

                    for (int fileIndex = 0; fileIndex < fileContentNames.Length; fileIndex++)
                    {
                        fileContents[fileIndex] = this.GetData(format, fileIndex);
                    }

                    return fileContents;
            }

            return this.underlyingDataObject.GetData(format, autoConvert);
        }

        public MemoryStream GetData(string format, int index)
        {
            FORMATETC formatetc = new FORMATETC()
            {
                cfFormat = (short)DataFormats.GetDataFormat(format).Id,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = index,
                ptd = new IntPtr(0),
                tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_ISTORAGE | TYMED.TYMED_HGLOBAL
            };
            STGMEDIUM medium = new STGMEDIUM();

            this.comUnderlyingDataObject.GetData(ref formatetc, out medium);

            switch (medium.tymed)
            {
                case TYMED.TYMED_ISTORAGE:
                    NativeMethods.IStorage iStorage = null;
                    NativeMethods.IStorage iStorage2 = null;
                    NativeMethods.ILockBytes iLockBytes = null;
                    System.Runtime.InteropServices.ComTypes.STATSTG iLockBytesStat;
                    try
                    {
                        iStorage = (NativeMethods.IStorage)Marshal.GetObjectForIUnknown(medium.unionmember);
                        Marshal.Release(medium.unionmember);

                        iLockBytes = NativeMethods.CreateILockBytesOnHGlobal(IntPtr.Zero, true);
                        iStorage2 = NativeMethods.StgCreateDocfileOnILockBytes(iLockBytes, 0x00001012, 0);

                        iStorage.CopyTo(0, null, IntPtr.Zero, iStorage2);
                        iLockBytes.Flush();
                        iStorage2.Commit(0);

                        iLockBytesStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
                        iLockBytes.Stat(out iLockBytesStat, 1);
                        int iLockBytesSize = (int)iLockBytesStat.cbSize;

                        byte[] iLockBytesContent = new byte[iLockBytesSize];
                        iLockBytes.ReadAt(0, iLockBytesContent, iLockBytesContent.Length, null);

                        return new MemoryStream(iLockBytesContent);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(iStorage2);
                        Marshal.ReleaseComObject(iLockBytes);
                        Marshal.ReleaseComObject(iStorage);
                    }

                case TYMED.TYMED_ISTREAM:
                    IStream iStream = null;
                    System.Runtime.InteropServices.ComTypes.STATSTG iStreamStat;
                    try
                    {
                        iStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
                        Marshal.Release(medium.unionmember);

                        iStreamStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
                        iStream.Stat(out iStreamStat, 0);
                        int iStreamSize = (int)iStreamStat.cbSize;

                        byte[] iStreamContent = new byte[iStreamSize];
                        iStream.Read(iStreamContent, iStreamContent.Length, IntPtr.Zero);

                        return new MemoryStream(iStreamContent);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(iStream);
                    }

                case TYMED.TYMED_HGLOBAL:
                    return (MemoryStream)this.getDataFromHGLOBALMethod.Invoke(this.oleUnderlyingDataObject, new object[] { DataFormats.GetDataFormat((short)formatetc.cfFormat).Name, medium.unionmember });
            }

            return null;
        }

        public bool GetDataPresent(Type format)
        {
            return this.underlyingDataObject.GetDataPresent(format);
        }

        public bool GetDataPresent(string format)
        {
            return this.underlyingDataObject.GetDataPresent(format);
        }

        public bool GetDataPresent(string format, bool autoConvert)
        {
            return this.underlyingDataObject.GetDataPresent(format, autoConvert);
        }

        public string[] GetFormats()
        {
            return this.underlyingDataObject.GetFormats();
        }

        public string[] GetFormats(bool autoConvert)
        {
            return this.underlyingDataObject.GetFormats(autoConvert);
        }

        public void SetData(object data)
        {
            this.underlyingDataObject.SetData(data);
        }

        public void SetData(Type format, object data)
        {
            this.underlyingDataObject.SetData(format, data);
        }

        public void SetData(string format, object data)
        {
            this.underlyingDataObject.SetData(format, data);
        }

        public void SetData(string format, object data, bool autoConvert)
        {
            this.underlyingDataObject.SetData(format, data, autoConvert);
        }

    }

}

