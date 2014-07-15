﻿using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using RomVaultX.DB;
using RomVaultX.IO;
using RomVaultX.SupportedFiles;
using RomVaultX.SupportedFiles.Files;
using RomVaultX.SupportedFiles.GZ;
using RomVaultX.SupportedFiles.Zip;
using RomVaultX.Util;
using DirectoryInfo = RomVaultX.IO.DirectoryInfo;
using FileInfo = RomVaultX.IO.FileInfo;
using Path = RomVaultX.IO.Path;

namespace RomVaultX
{

    public static class romScanner
    {
        private static BackgroundWorker _bgw;

        public static void ScanFiles(object sender, DoWorkEventArgs e)
        {
            _bgw = sender as BackgroundWorker;
            Program.SyncCont = e.Argument as SynchronizationContext;
            if (Program.SyncCont == null)
            {
                _bgw = null;
                return;
            }

            ScanADir(@"D:\RomVault1\trunk\Stage1\ToSort");

            _bgw.ReportProgress(0, new bgwText("Scanning Files Complete"));
            _bgw = null;
            Program.SyncCont = null;
        }


        private static void ScanADir(string directory)
        {
            _bgw.ReportProgress(0, new bgwText("Scanning Dir : " + directory));
            DirectoryInfo di = new DirectoryInfo(directory);

            FileInfo[] fi = di.GetFiles();

            foreach (FileInfo f in fi)
            {
                Debug.WriteLine(f.FullName);
                string ext = Path.GetExtension(f.Name);

                if (ext.ToLower() == ".zip")
                {
                    ZipFile fz = new ZipFile();
                    fz.ZipFileOpen(f.FullName, 0, true);
                    fz.DeepScan();

                    for (int i = 0; i < fz.LocalFilesCount(); i++)
                    {
                        Debug.WriteLine(fz.Filename(i));
                        rvFile tFile = new rvFile();
                        tFile.Size = fz.UncompressedSize(i);
                        tFile.CRC = fz.CRC32(i);
                        tFile.MD5 = fz.MD5(i);
                        tFile.SHA1 = fz.SHA1(i);

                        string outfile = Getfilename(tFile.SHA1);
                        // test if needed.
                        if (IO.File.Exists(outfile))
                            continue;

                        GZip gz = new GZip();
                        gz.crc = tFile.CRC;
                        gz.md5Hash = tFile.MD5;
                        gz.sha1Hash = tFile.SHA1;
                        gz.uncompressedSize = tFile.Size;

                        bool isZipTrrntzip = (fz.ZipStatus == ZipStatus.TrrntZip);
                        ulong compressedSize;
                        ushort method;
                        Stream zds;
                        fz.ZipFileOpenReadStream(i, isZipTrrntzip, out zds, out compressedSize, out method);
                        gz.compressedSize = compressedSize;
                        gz.WriteGZip(outfile, zds, isZipTrrntzip);
                        fz.ZipFileCloseReadStream();
                    }
                    fz.ZipFileClose();
                }
                else
                {
                    rvFile tFile = new rvFile();
                    UnCompFiles.CheckSumRead(f.FullName, true, out tFile.CRC, out tFile.MD5, out tFile.SHA1, out tFile.Size);

                    string outfile = Getfilename(tFile.SHA1);
                    // test if needed.
                    if (IO.File.Exists(outfile))
                        continue;

                    GZip gz = new GZip();
                    gz.crc = tFile.CRC;
                    gz.md5Hash = tFile.MD5;
                    gz.sha1Hash = tFile.SHA1;
                    gz.uncompressedSize = tFile.Size;

                    Stream ds;
                    int errorCode = IO.FileStream.OpenFileRead(f.FullName, out ds);
                    gz.WriteGZip(outfile, ds, false);
                    ds.Close();
                    ds.Dispose();
                }
            }

            DirectoryInfo[] childdi = di.GetDirectories();
            foreach (DirectoryInfo d in childdi)
                ScanADir(d.FullName);
        }

        private static string Getfilename(byte[] SHA1)
        {
            return @"RomRoot\" + VarFix.ToString(SHA1[0]) + @"\" +
                         VarFix.ToString(SHA1[1]) + @"\" +
                         VarFix.ToString(SHA1[2]) + @"\" +
                         VarFix.ToString(SHA1[3]) + @"\" + VarFix.ToString(SHA1) + ".gz";

        }

    }
}
