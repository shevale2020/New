using DNAPGateway.EFLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;

namespace DNAPGateway.SFTPCommunicator
{
    public class ScannedPOBoxSync
    {
        List<POBoxControlFileSummary> _controlFilesList = null;
        Dictionary<string, POBoxInvoiceFileSummary> _invoiceFilesList = null;
       
        public bool DownloadPOBox(string poBoxName = "", bool forceDownload = false)
        {
            bool bResult = false, multiCtrlFiles = false;
            IList<ScannedPOBox> poBoxes;
            _invoiceFilesList = new Dictionary<string, POBoxInvoiceFileSummary>();
            Globals.DBLogger.InfoFormat("DownloadPOBox: Downloading for {0}...", poBoxName);
            using (GatewayDBContext dbContext = new EFLibrary.GatewayDBContext())
            {
                if (string.IsNullOrWhiteSpace(poBoxName))
                    poBoxes = dbContext.ScannedPOBoxes.Where(x => x.IsDownloadEnabled == true).ToList();
                else
                    poBoxes = dbContext.ScannedPOBoxes.Where(x => x.IsDownloadEnabled == true && x.POBoxName == poBoxName).ToList();
            }
            //Download pobox batches
            foreach (ScannedPOBox poBox in poBoxes)
            {
                _controlFilesList = new List<POBoxControlFileSummary>();
                bResult = DownloadControlFiles(poBox, forceDownload);
                if (bResult)
                {
                    foreach (POBoxControlFileSummary controlFile in _controlFilesList)
                    {
                        ScannedPOBoxBatch batch = new ScannedPOBoxBatch(controlFile.LocalFileFullName);
                        bResult = batch.ParseControlFile();
                        if (bResult)
                        {
                            bResult = DownloadPOBoxInvoices(controlFile, batch);
                        }
                    }
                    multiCtrlFiles = (_controlFilesList.Count > 1);
                }
            }

            //Generate email from the list
            if (_invoiceFilesList.Count > 0)
            {
                EmailGenerator email = new EmailGenerator();
                email.GenerateInvoiceDownloadSummaryMessage(_invoiceFilesList, multiCtrlFiles);
                bResult = true;
            }

            return bResult;
        }

        private bool DownloadControlFiles(ScannedPOBox poBoxEntity, bool forceDownload)
        {
            try
            {
                bool bResult = false;
                Globals.DBLogger.InfoFormat("DownloadControlFiles: Downloading Control Files for {0}...", poBoxEntity.POBoxName);
                //Create Invoice Local folders if not exist
                DirectoryInfo diInvoiceRoot = new DirectoryInfo(Path.Combine(Globals.LocalPOBoxRootFolder, poBoxEntity.POBoxName));
                if (!diInvoiceRoot.Exists) diInvoiceRoot.Create();
                using (GatewayDBContext dbContext = new EFLibrary.GatewayDBContext())
                {
                    using (SFTPHelper sftpHelper = new SFTPHelper())
                    {
                        //Define and initialize variables
                        string ctrlNameWOExt;
                        FileInfo fiControlFile;
                        TransferOperationResult transferResult = null;
                        IEnumerable<RemoteFileInfo> sftpControlFiles = null;
                        POBoxControlFileSummary controlSummary;

                        // List control files
                        bResult = sftpHelper.EnumerateSFTPFiles(string.Format("{0}{1}/", Globals.SFTPPOBoxRootFolder, poBoxEntity.POBoxName),
                                Globals.InvoicControlSFTPSearch, ref sftpControlFiles);
                        if (!bResult)
                            return false;

                        //If any control file is found then only start the download
                        //else dont proceed with download
                        //Download all the files
                        foreach (RemoteFileInfo sftpFI in sftpControlFiles)
                        {
                            //Check if control file is already processed, and download if not
                            ctrlNameWOExt = Globals.FileNameWOExt(sftpFI.Name);
                            controlSummary = dbContext.POBoxControlFileSummaries
                                .Where(x => (x.ControlFileName.Equals(ctrlNameWOExt, StringComparison.OrdinalIgnoreCase))
                                && (x.POBoxName == poBoxEntity.POBoxName) && (x.IsProcessed == true))
                                .FirstOrDefault();

                            if (controlSummary == null || forceDownload)
                            {
                                // Download individual control file
                                bResult = sftpHelper.DownloadSFTPFiles(sftpFI.FullName, Path.Combine(diInvoiceRoot.FullName,
                                            sftpFI.Name), ref transferResult);
                                // Move to next record in case of failure
                                if (!bResult)
                                    continue;

                                foreach (TransferEventArgs transfer in transferResult.Transfers)
                                {
                                    Globals.DBLogger.InfoFormat("DownloadControlFiles: Transferred: {0} to {1}", transfer.FileName, transfer.Destination);

                                    fiControlFile = new FileInfo(transfer.Destination);
                                    ctrlNameWOExt = Globals.FileNameWOExt(fiControlFile);
                                    // Perform data access using the context 
                                    controlSummary = dbContext.POBoxControlFileSummaries
                                                        .Where(x => (x.POBoxName == poBoxEntity.POBoxName)
                                                        && (x.ControlFileName.Equals(ctrlNameWOExt, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                                    //Add or Update Summary record
                                    if (controlSummary == null)
                                    {
                                        controlSummary = new POBoxControlFileSummary();
                                        dbContext.POBoxControlFileSummaries.Add(controlSummary);
                                    }
                                    //Update the data
                                    controlSummary.POBoxName = poBoxEntity.POBoxName;
                                    controlSummary.ControlFileName = ctrlNameWOExt;
                                    controlSummary.ControlFileSource = "SFTP";
                                    controlSummary.LocalFileFullName = transfer.Destination;
                                    controlSummary.SFTPFileFullName = transfer.FileName;
                                    controlSummary.FileGeneratedTime = DateTime.Now;
                                    controlSummary.SFTPDownloadTime = DateTime.Now;
                                    controlSummary.ProcessingWorkstation = Environment.MachineName;
                                    controlSummary.IsProcessed = false;
                                    dbContext.SaveChanges();
                                    _controlFilesList.Add(controlSummary);
                                    bResult = true;
                                }
                            }
                        }
                    }
                }
                //Return true only if some file is found
                return bResult;
            }
            catch (Exception ex)
            {
                //Programme with attempt to download in next run.
                Globals.DBLogger.Error(string.Format("DownloadControlFiles: Error: {0}", ex.Message), ex);
                return false;
            }
        }

        private bool DownloadPOBoxInvoices(POBoxControlFileSummary controlFile, ScannedPOBoxBatch batch)
        {
            try
            {
                bool bResult = false;
                Globals.DBLogger.InfoFormat("DownloadPOBoxInvoices: Downloading Invoice Files for {0}...", controlFile.POBoxName);
                //Create Invoice Local folders if not exist
                DirectoryInfo diInvoiceRoot = new DirectoryInfo(Path.Combine(Globals.LocalPOBoxRootFolder, controlFile.POBoxName));
                if (!diInvoiceRoot.Exists) diInvoiceRoot.Create();
                using (GatewayDBContext dbContext = new EFLibrary.GatewayDBContext())
                {
                    using (SFTPHelper sftpHelper = new SFTPHelper())
                    {
                        //Define and initialize variables
                        FileInfo fiLocalInvoice;
                        string fileNameWOExt, invoiceKey;
                        TransferOperationResult transferResult = null;
                        IEnumerable<RemoteFileInfo> sftpInvoiceFiles = null;
                        POBoxInvoiceFileSummary invoiceSummary;
                       
                            //Iterate through invoice info and download all the invoices
                            foreach (InvoiceControlInfo invInfo in batch.InvoiceList)
                            {
                               

                                //Add invoice file record so that failures/ missing invoices can be tracked
                                // this may happen when control file mentions invoice but physical invoice is not present
                                fileNameWOExt = Globals.FileNameWOExt(invInfo.DocumentIdentifier);
                                // Perform data access using the context 
                                invoiceSummary = dbContext.POBoxInvoiceFileSummaries.Where(x => (x.POBoxName == controlFile.POBoxName)
                                        && (x.ControlFileName.Equals(controlFile.ControlFileName, StringComparison.OrdinalIgnoreCase))
                                        && (x.InvoiceFileName.Equals(fileNameWOExt, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                                //Add or Update Summary record
                                if (invoiceSummary == null)
                                {
                                    invoiceSummary = new POBoxInvoiceFileSummary();
                                    dbContext.POBoxInvoiceFileSummaries.Add(invoiceSummary);
                                }
                                //Update the data
                                invoiceSummary.POBoxName = controlFile.POBoxName;
                                invoiceSummary.ControlFileName = controlFile.ControlFileName;
                                invoiceSummary.InvoiceFileName = fileNameWOExt;
                                invoiceSummary.SFTPInvoiceFileFullName = controlFile.SFTPFileFullName;
                                invoiceSummary.LocalInvoiceFileFullName = controlFile.LocalFileFullName;
                                invoiceSummary.LocalMetaFileFullName = batch.CreateInvoiceMetaFile(new FileInfo(controlFile.LocalFileFullName), invInfo); 
                                this.TransferToKofaxFolder(controlFile.POBoxName, ref invoiceSummary);
                            //Save to database
                            dbContext.SaveChanges();
                                //Add to dictionary
                                invoiceKey = string.Format("{0}:{1}:{2}", invoiceSummary.POBoxName, invoiceSummary.ControlFileName, invoiceSummary.InvoiceFileName);
                                if (_invoiceFilesList.ContainsKey(invoiceKey))
                                    _invoiceFilesList[invoiceKey] = invoiceSummary;
                                else
                                    _invoiceFilesList.Add(invoiceKey, invoiceSummary);
                                // List invoice and metadata remote files
                                bResult = sftpHelper.EnumerateSFTPFiles(string.Format("{0}{1}/", Globals.SFTPPOBoxRootFolder, controlFile.POBoxName),
                                        invInfo.DocumentIdentifier, ref sftpInvoiceFiles);
                                // Move to next record in case of failure
                                if (!bResult)
                                    continue;

                                foreach (RemoteFileInfo sftpFI in sftpInvoiceFiles)
                                {

                                    // Download individual invoice file
                                    bResult = sftpHelper.DownloadSFTPFiles(sftpFI.FullName, Path.Combine(diInvoiceRoot.FullName, sftpFI.Name), ref transferResult);
                                    // Move to next record in case of failure
                                    if (!bResult)
                                        continue;
                                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                                    {
                                        Globals.DBLogger.InfoFormat("DownloadPOBoxInvoices: Transferred: {0} to {1}", transfer.FileName, transfer.Destination);

                                        fiLocalInvoice = new FileInfo(transfer.Destination);
                                        fileNameWOExt = Globals.FileNameWOExt(fiLocalInvoice);
                                        // Perform data access using the context 
                                        invoiceSummary = dbContext.POBoxInvoiceFileSummaries.Where(x => (x.POBoxName == controlFile.POBoxName)
                                            && (x.ControlFileName.Equals(controlFile.ControlFileName, StringComparison.OrdinalIgnoreCase))
                                            && (x.InvoiceFileName.Equals(fileNameWOExt, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                                        //Add or Update Summary record
                                        if (invoiceSummary == null)
                                        {
                                            invoiceSummary = new POBoxInvoiceFileSummary();
                                            dbContext.POBoxInvoiceFileSummaries.Add(invoiceSummary);
                                        }
                                        //Update the data
                                        invoiceSummary.POBoxName = controlFile.POBoxName;
                                        invoiceSummary.ControlFileName = controlFile.ControlFileName;
                                        invoiceSummary.InvoiceFileName = fileNameWOExt;
                                        invoiceSummary.SFTPInvoiceFileFullName = transfer.FileName;
                                        invoiceSummary.LocalInvoiceFileFullName = transfer.Destination;
                                        invoiceSummary.LocalMetaFileFullName =
                                            batch.CreateInvoiceMetaFile(new FileInfo(transfer.Destination), invInfo);
                                        //Copy files to KOFAX folders
                                        this.TransferToKofaxFolder(controlFile.POBoxName, ref invoiceSummary);
                                        //Save to database
                                        dbContext.SaveChanges();
                                        //Add to dictionary
                                        invoiceKey = string.Format("{0}:{1}:{2}", invoiceSummary.POBoxName, invoiceSummary.ControlFileName, invoiceSummary.InvoiceFileName);
                                        if (_invoiceFilesList.ContainsKey(invoiceKey))
                                            _invoiceFilesList[invoiceKey] = invoiceSummary;
                                        else
                                            _invoiceFilesList.Add(invoiceKey, invoiceSummary);
                                        bResult = true;
                                    }
                                }
                        }

                    }
                    //Update ControlFile record to IsProcessed
                    if (!string.IsNullOrWhiteSpace(controlFile.ControlFileName)
                        && !string.IsNullOrWhiteSpace(controlFile.POBoxName))
                    {
                        //Search for control file record and update the flag
                        POBoxControlFileSummary ctrlFile = dbContext.POBoxControlFileSummaries
                            .Where(x => x.ControlFileName == controlFile.ControlFileName
                            && x.POBoxName == controlFile.POBoxName).FirstOrDefault();
                        if (ctrlFile != null)
                        {
                            //update control file flags
                            ctrlFile.IsProcessed = true;
                            ctrlFile.IsArchived = false;
                            //Save to database
                            dbContext.SaveChanges();
                        }
                    }
                }

                bResult= LoadingBatchData(controlFile,batch);

                return bResult;
            }
            catch (Exception ex)
            {
                Globals.DBLogger.Error(string.Format("DownloadPOBoxInvoices: Error: {0}", ex.Message), ex);
                return false;
            }
        }

        private bool LoadingBatchData(POBoxControlFileSummary controlFile,ScannedPOBoxBatch batch)
        {
            try
            {
                using (SqlConnection sqlConn = new SqlConnection(Globals.WNSTracDBConn))
                {
                    
                    Globals.DBLogger.InfoFormat("DownloadPOBoxInvoices: LoadBatchData: {0} to {1}", controlFile.ControlFileName, "RicohMetadata");
                    string insertQuery = "INSERT INTO Ins_Ricoh_Metadata ([BatchNumber],[DocumentIdentifier],[CollectionDate],[ImageScanDate],[ScanOperatorID],[SourcePOBox],[InvoiceHeaderID],[Created_Date],[Created_By],[Updated_Date],[Updated_By]) VALUES (@BatchNumber,@DocumentIdentifier,@CollectionDate,@ImageScanDate,@ScanOperatorID,@SourcePOBox,@InvoiceHeaderID,@Created_Date,@Created_By,@Updated_Date,@Updated_By)";

                    using (SqlCommand sqlCmd = new SqlCommand(insertQuery, sqlConn))
                    {
                        sqlCmd.Parameters.Add("@BatchNumber", SqlDbType.NVarChar);
                        sqlCmd.Parameters.Add("@DocumentIdentifier", SqlDbType.NVarChar);
                        sqlCmd.Parameters.Add("@CollectionDate", SqlDbType.DateTime);
                        sqlCmd.Parameters.Add("@ImageScanDate", SqlDbType.DateTime);
                        sqlCmd.Parameters.Add("@ScanOperatorID", SqlDbType.NVarChar);
                        sqlCmd.Parameters.Add("@SourcePOBox", SqlDbType.NVarChar);
                        sqlCmd.Parameters.Add("@InvoiceHeaderID", SqlDbType.NVarChar);
                        sqlCmd.Parameters.Add("@Created_Date", SqlDbType.DateTime);
                        sqlCmd.Parameters.Add("@Created_By", SqlDbType.NVarChar);
                        sqlCmd.Parameters.Add("@Updated_Date", SqlDbType.DateTime);
                        sqlCmd.Parameters.Add("@Updated_By", SqlDbType.NVarChar);
                        sqlConn.Open();
                        foreach (InvoiceControlInfo invList in batch.InvoiceList)
                    {
                        sqlCmd.Parameters["@BatchNumber"].Value= invList.BatchNumber;
                        sqlCmd.Parameters["@DocumentIdentifier"].Value = invList.DocumentIdentifier;
                        sqlCmd.Parameters["@CollectionDate"].Value=invList.CollectionDate ;
                        sqlCmd.Parameters["@ImageScanDate"].Value = invList.ImageScanDate ;
                        sqlCmd.Parameters["@ScanOperatorID"].Value = invList.ScanOperatorID ;
                        sqlCmd.Parameters["@SourcePOBox"].Value = invList.SourcePOBox;
                        sqlCmd.Parameters["@InvoiceHeaderID"].Value = invList.InvoiceHeaderID ?? Convert.DBNull;
                        sqlCmd.Parameters["@Created_Date"].Value = DateTime.Now;
                        sqlCmd.Parameters["@Created_By"].Value = string.Format("Gateway:{0}", controlFile.ControlFileName);
                        sqlCmd.Parameters["@Updated_Date"].Value = null ?? Convert.DBNull;
                        sqlCmd.Parameters["@Updated_By"].Value = null ?? Convert.DBNull;
                        sqlCmd.CommandTimeout = Globals.SQLCommandTimeoutInbound;
                        if (sqlCmd.Connection.State != ConnectionState.Open)
                            sqlCmd.Connection.Open();
                        sqlCmd.ExecuteNonQuery();
                       
                    }
                        sqlCmd.Connection.Close();
                    }
                   
                    sqlConn.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Globals.DBLogger.Error(string.Format("LoadingBatchData: Error: {0}", ex.Message), ex);
                return false;
            }
           
        }

        private bool TransferToKofaxFolder(string poBoxName, ref POBoxInvoiceFileSummary invoice)
        {
            //Create Invoice Local folders if not exist
            DirectoryInfo diInvoiceRoot = new DirectoryInfo(Path.Combine(Globals.KTAPOBoxRootFolder, poBoxName));
            if (!diInvoiceRoot.Exists) diInvoiceRoot.Create();
            FileInfo fiLocalInvoice, fiLocalMetadata, fiKofaxInvoice, fiKofaxMetadata;

            //Copy local invoice to Kofax folder
            fiLocalInvoice = new FileInfo(invoice.LocalInvoiceFileFullName);
            fiKofaxInvoice = fiLocalInvoice.CopyTo(Path.Combine(diInvoiceRoot.FullName, fiLocalInvoice.Name), true);
            invoice.KofaxInvoiceFileFullName = fiKofaxInvoice.FullName;
            //Copy local metadata file to Kofax folder
            fiLocalMetadata = new FileInfo(invoice.LocalMetaFileFullName);
            fiKofaxMetadata = fiLocalMetadata.CopyTo(Path.Combine(diInvoiceRoot.FullName, fiLocalMetadata.Name), true);
            invoice.KofaxMetaFileFullName = fiKofaxMetadata.FullName;
            return true;
        }

        public bool ArchivePOBoxInvoices(string poBoxName = "")
        {
            try
            {
                bool bResult = false;
                List<POBoxControlFileSummary> controlFileList = null;
                using (GatewayDBContext dbContext = new EFLibrary.GatewayDBContext())
                {
                    if (string.IsNullOrWhiteSpace(poBoxName))
                        controlFileList = dbContext.POBoxControlFileSummaries.Where(x => x.IsProcessed && !x.IsArchived).ToList();
                    else
                    {
                        Globals.DBLogger.InfoFormat("ArchivePOBoxInvoices: Archiving Invoice Files for {0}...", poBoxName);
                        controlFileList = dbContext.POBoxControlFileSummaries
                            .Where(x => x.IsProcessed && !x.IsArchived
                                && x.POBoxName.Equals(poBoxName, StringComparison.OrdinalIgnoreCase)
                                && x.ProcessingWorkstation == Environment.MachineName)
                            .ToList();
                    }
                }
                foreach (POBoxControlFileSummary controlFile in controlFileList)
                {
                    bResult = ArchiveInvoices(controlFile);
                }
                return bResult;
            }
            catch (Exception ex)
            {
                Globals.DBLogger.Error(string.Format("ArchivePOBoxInvoices: Error: {0}", ex.Message), ex);
                return false;
            }
        }

        private bool ArchiveInvoices(POBoxControlFileSummary controlFile)
        {
            try
            {
                bool bResult = false;

                using (GatewayDBContext dbContext = new EFLibrary.GatewayDBContext())
                {
                    List<POBoxInvoiceFileSummary> invoiceList = null;
                    POBoxControlFileSummary ctrlLocal = null;
                    invoiceList = dbContext.POBoxInvoiceFileSummaries
                        .Where(x => (x.POBoxName == controlFile.POBoxName)
                        && (x.ControlFileName == controlFile.ControlFileName)
                        && (x.SFTPInvoiceFileFullName != "")).ToList();
                    if (invoiceList.Count == 0)
                    {
                        Globals.DBLogger.InfoFormat("ArchiveInvoices: No invoices to Archive for POBox:{0} and Batch:{1}...", controlFile.POBoxName, controlFile.ControlFileName);
                        return true;
                    }

                    Globals.DBLogger.InfoFormat("ArchiveInvoices: Archiving Invoice Files for POBox:{0} and Batch:{1}...", controlFile.POBoxName, controlFile.ControlFileName);
                    using (SFTPHelper sftpHelper = new SFTPHelper())
                    {
                        //Define and initialize variables
                        FileInfo fiSourceInvoice, fiSourceMeta, fiSourceControl, fiTargetInvoice, fiTargetMeta, fiTargetControl;
                        string sftpSourcePath, sftpTargetPath, sftpTargetFile;
                        DirectoryInfo diSourcePath, diTargetPath;
                        TransferOperationResult transferResult = null;

                        //Set SFTP Source and Target Paths
                        sftpSourcePath = string.Format("{0}{1}/", Globals.SFTPPOBoxRootFolder, controlFile.POBoxName);
                        sftpTargetPath = string.Format("{0}{1}/Success/", Globals.SFTPPOBoxRootFolder, controlFile.POBoxName);
                        diSourcePath = new DirectoryInfo(Path.Combine(Globals.LocalPOBoxRootFolder, controlFile.POBoxName));
                        diTargetPath = new DirectoryInfo(Path.Combine(Globals.LocalPOBoxRootFolder, controlFile.POBoxName, "Success"));
                        if (!diTargetPath.Exists)
                            diTargetPath.Create();

                        //Iterate through invoice info and download all the invoices
                        foreach (POBoxInvoiceFileSummary summary in invoiceList)
                        {
                            Globals.DBLogger.InfoFormat("ArchiveInvoices: Archiving Invoice File:{0} ...", summary.InvoiceFileName);

                            fiSourceInvoice = new FileInfo(summary.LocalInvoiceFileFullName);
                            fiSourceMeta = new FileInfo(summary.LocalMetaFileFullName);
                            fiTargetInvoice = new FileInfo(Path.Combine(diTargetPath.FullName, fiSourceInvoice.Name));
                            fiTargetMeta = new FileInfo(Path.Combine(diTargetPath.FullName, fiSourceMeta.Name));

                            //Move invoice on SFTP
                            sftpTargetFile = sftpTargetPath + fiSourceInvoice.Name;
                            bResult = sftpHelper.MoveOrUploadFile(summary.SFTPInvoiceFileFullName, sftpTargetFile, fiSourceInvoice.FullName);
                            if (bResult)
                                summary.SFTPInvoiceFileFullName = sftpTargetFile;

                            //Copy metadata file to SFTP
                            if (fiSourceMeta.Exists)
                            {
                                sftpTargetFile = sftpTargetPath + fiSourceMeta.Name;
                                bResult = sftpHelper.UploadLocalFile(fiSourceMeta.FullName, sftpTargetFile, ref transferResult);
                                if (bResult)
                                    summary.SFTPMetaFileFullName = sftpTargetFile;
                            }


                            //Move local Invoice file
                            if (fiSourceInvoice.Exists)
                            {
                                if (fiTargetInvoice.Exists)
                                    fiTargetInvoice.Delete();
                                fiSourceInvoice.MoveTo(fiTargetInvoice.FullName);
                                summary.LocalInvoiceFileFullName = fiSourceInvoice.FullName;
                            }
                            //Move local Metadata file
                            if (fiSourceMeta.Exists)
                            {
                                if (fiTargetMeta.Exists)
                                    fiTargetMeta.Delete();
                                fiSourceMeta.MoveTo(fiTargetMeta.FullName);
                                summary.LocalMetaFileFullName = fiSourceMeta.FullName;
                            }
                            //Save updated paths to Database
                            dbContext.SaveChanges();
                            bResult = true;
                        }
                        //Exit in case of failure
                        if (!bResult && (invoiceList.Count > 0))
                            return false;

                        //Update ControlFile record to IsProcessed
                        //Search for control file record and update the flag
                        ctrlLocal = dbContext.POBoxControlFileSummaries
                            .Where(x => (x.ControlFileName == controlFile.ControlFileName)
                            && (x.POBoxName == controlFile.POBoxName)).FirstOrDefault();
                        if (ctrlLocal != null)
                        {
                            fiSourceControl = new FileInfo(controlFile.LocalFileFullName);
                            fiTargetControl = new FileInfo(Path.Combine(diTargetPath.FullName, fiSourceControl.Name));
                            //Move invoice on SFTP
                            sftpTargetFile = sftpTargetPath + fiSourceControl.Name;
                            bResult = sftpHelper.MoveOrUploadFile(controlFile.SFTPFileFullName, sftpTargetFile, fiSourceControl.FullName);
                            if (bResult)
                                ctrlLocal.SFTPFileFullName = sftpTargetFile;

                            if (fiSourceControl.Exists)
                            {
                                //Move local files
                                if (fiTargetControl.Exists)
                                    fiTargetControl.Delete();
                                fiSourceControl.MoveTo(fiTargetControl.FullName);
                                ctrlLocal.LocalFileFullName = fiSourceControl.FullName;
                            }

                            //update control file flags
                            ctrlLocal.IsArchived = true;
                            //Save to database
                            dbContext.SaveChanges();
                        }
                        else
                        {
                            Globals.DBLogger.InfoFormat("ArchiveInvoices: Issue with Archival Process for POBox:{0} and Batch:{1}...", controlFile.POBoxName, controlFile.ControlFileName);
                            bResult = true;
                        }
                    }

                }
                return bResult;
            }
            catch (Exception ex)
            {
                Globals.DBLogger.Error(string.Format("ArchiveInvoices: Error: {0}", ex.Message), ex);
                return false;
            }
        }
    }

    public class ScannedPOBoxBatch
    {
        private FileInfo fiControlFile;
        public List<InvoiceControlInfo> InvoiceList;
        public ScannedPOBoxBatch(string controlFileFullName)
        {
            fiControlFile = new FileInfo(controlFileFullName);
            InvoiceList = new List<InvoiceControlInfo>();
        }

        public bool ParseControlFile()
        {
            try
            {
                bool bResult = false, bFirstLine = true, bTry;
                DateTime dtTemp;
                foreach (string line in File.ReadLines(fiControlFile.FullName, Encoding.UTF8))
                {
                    if (bFirstLine)
                    {
                        //Skip first line
                        bFirstLine = false;
                        continue;
                    }
                    // process the line
                    string[] fields = line.Split('|');
                    if (fields.Count() >= 5)
                    {
                        InvoiceControlInfo ctrlRec = new InvoiceControlInfo();
                        //extract the fields
                        ctrlRec.BatchNumber = fields[0].Trim();
                        ctrlRec.DocumentIdentifier = fields[1].Trim();
                        bTry = DateTime.TryParse(fields[2].Trim(), out dtTemp);
                        if (bTry)
                            ctrlRec.CollectionDate = dtTemp;
                        else
                            ctrlRec.CollectionDate = new DateTime(1, 1, 1);
                        bTry = DateTime.TryParse(fields[3].Trim(), out dtTemp);
                        if (bTry)
                            ctrlRec.ImageScanDate = dtTemp;
                        else
                            ctrlRec.ImageScanDate = new DateTime(1, 1, 1);
                        ctrlRec.ScanOperatorID = fields[4].Trim();
                        //Field is not coming from Scanning & Mailroom partner
                        ctrlRec.SourcePOBox = fields[5].Trim();
                        //Add the parsed record
                        InvoiceList.Add(ctrlRec);
                    }
                    bResult = true;
                }
                return bResult;
            }
            catch (Exception ex)

            {
                Globals.DBLogger.Error(string.Format("ScannedPOBoxBatch.ParseControlFile: Exception with message : {0}.", ex.Message), ex);
                return false;
            }
        }

        public string CreateInvoiceMetaFile(FileInfo invFileInfo, InvoiceControlInfo invCtrlInfo)
        {
            try
            {
                string ctrlFilePath = Path.Combine(invFileInfo.DirectoryName, Globals.FileNameWOExt(invFileInfo) + ".txt");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(ctrlFilePath))
                {
                    file.WriteLine(
                        string.Format("\"{0}\"|\"{1}\"|\"{2:MM/dd/yyyy HH:mm:ss}\"|\"{3:MM/dd/yyyy HH:mm:ss}\"|\"{4}\"|\"{5}\"",
                        invCtrlInfo.BatchNumber, invCtrlInfo.DocumentIdentifier, invCtrlInfo.CollectionDate, invCtrlInfo.ImageScanDate,
                        invCtrlInfo.ScanOperatorID, invCtrlInfo.SourcePOBox));
                }
                return ctrlFilePath;
            }
            catch (Exception ex)
            {
                Globals.DBLogger.Error(string.Format("CreateInvoiceControlFile: Error: {0}", ex.Message), ex);
                return "";
            }
        }
    }

    public struct InvoiceControlInfo
    {
        public string BatchNumber;
        public string DocumentIdentifier;
        public DateTime CollectionDate;
        public DateTime ImageScanDate;
        public string ScanOperatorID;
        public string SourcePOBox;
        public string InvoiceHeaderID;
    }

}
