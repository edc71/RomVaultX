﻿using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using RomVaultX.DB;
using RomVaultX.DB.DBAccess;
using RomVaultX.SupportedFiles;
using RomVaultX.SupportedFiles.GZ;
using RomVaultX.Util;
using DirectoryInfo = RomVaultX.IO.DirectoryInfo;
using FileInfo = RomVaultX.IO.FileInfo;
using Path = RomVaultX.IO.Path;

namespace RomVaultX
{
    public static class romRootScanner
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

            ScanRomRoot(@"RomRoot");

            DataAccessLayer.UpdateGotTotal();
            _bgw.ReportProgress(0, new bgwText("Scanning Files Complete"));
            _bgw = null;
            Program.SyncCont = null;
        }

        private static void ScanRomRoot(string directory)
        {
            _bgw.ReportProgress(0, new bgwText("Scanning Dir : " + directory));
            DirectoryInfo di = new DirectoryInfo(directory);

            FileInfo[] fi = di.GetFiles();

            _bgw.ReportProgress(0, new bgwRange2Visible(true));
            _bgw.ReportProgress(0, new bgwSetRange2(fi.Count()));

            for (int j = 0; j < fi.Count(); j++)
            {
                FileInfo f = fi[j];
                _bgw.ReportProgress(0, new bgwValue2(j));
                _bgw.ReportProgress(0, new bgwText2(f.Name));
                string ext = Path.GetExtension(f.Name);

                if (ext.ToLower() == ".gz")
                {
                    GZip gZipTest = new GZip();
                    ZipReturn errorcode = gZipTest.ReadGZip(f.FullName,false);
                    gZipTest.sha1Hash = VarFix.CleanMD5SHA1(Path.GetFileNameWithoutExtension(f.Name), 40);

                    if (errorcode != ZipReturn.ZipGood)
                        continue;
                    rvFile tFile = new rvFile();
                    tFile.CRC = gZipTest.crc;
                    tFile.MD5 = gZipTest.md5Hash;
                    tFile.SHA1 = gZipTest.sha1Hash;
                    tFile.Size = gZipTest.uncompressedSize;
                    tFile.CompressedSize = gZipTest.compressedSize;

                    FindStatus res = fileneededTest(tFile);

                    if (res != FindStatus.FoundFileInArchive)
                        DataAccessLayer.AddInFiles(tFile);

                }
                if (_bgw.CancellationPending)
                    return;
            }

            DirectoryInfo[] childdi = di.GetDirectories();
            foreach (DirectoryInfo d in childdi)
                ScanRomRoot(d.FullName);
        }

        private enum FindStatus
        {
            FileUnknown,
            FoundFileInArchive,
            FileNeededInArchive,
        };
        private static FindStatus fileneededTest(rvFile tFile)
        {
            // first check to see if we already have it in the file table
            bool inFileDB = FindInFiles.Execute(tFile); // returns true if found in File table
            return inFileDB ? FindStatus.FoundFileInArchive : FindStatus.FileNeededInArchive;
        }

    }
}
