using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSAPGateway.EFLibrary
{
    public static partial class Globals
    {

        public static string SFTPInboundRootFolder
        {
            get
            {
                return ConfigurationManager.AppSettings["SFTPInboundRootFolder"];
            }
        }
        public static string SFTPOutboundRootFolder
        {
            get
            {
                return ConfigurationManager.AppSettings["SFTPOutboundRootFolder"];
            }
        }
        public static string WinSCPExePath
        {
            get
            {
                try
                {
                    string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    Globals.DBLogger.InfoFormat("WinSCPExePath: Searching WinSCP at location...[{0}]", Path.Combine(exePath, "WinSCP.exe"));
                    FileInfo fiSCP = new FileInfo(Path.Combine(exePath, "WinSCP.exe"));
                    if (fiSCP.Exists)
                    {
                        Globals.DBLogger.InfoFormat("WinSCPExePath: Using WinSCP from location...[{0}]", fiSCP.FullName);
                        return fiSCP.FullName;
                    }
                    else
                    {
                        Globals.DBLogger.InfoFormat("WinSCPExePath: Using WinSCP from location...[{0}]", ConfigurationManager.AppSettings["WinSCPExePath"]);
                        return ConfigurationManager.AppSettings["WinSCPExePath"];
                    }

                }
                catch (Exception)
                {
                    return ConfigurationManager.AppSettings["WinSCPExePath"];
                }
            }
        }

        public static string EmailLocalFolder
        {
            get
            {
                return Path.Combine(ConfigurationManager.AppSettings["DataRootFolder"], ConfigurationManager.AppSettings["LocalEmailFolder"]);
            }
        }

        public static string LocalInboundRootFolder
        {
            get
            {
                return Path.Combine(ConfigurationManager.AppSettings["DataRootFolder"], ConfigurationManager.AppSettings["LocalInboundFolder"]);
            }
        }
        public static string LocalOutboundRootFolder
        {
            get
            {
                return Path.Combine(ConfigurationManager.AppSettings["DataRootFolder"], ConfigurationManager.AppSettings["LocalOutboundFolder"]);
            }
        }
        public static string KTAInvFilesFolder
        {
            get
            {
                return ConfigurationManager.AppSettings["KTAInvFilesFolder"];
            }
        }

        public static string WNSTracDBConn
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["WNSTrackDBConn"].ConnectionString;
            }
        }

        public static bool MailEnabled
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["MailEnabled"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static bool MailHostSSL
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["MailHostSSL"]);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        public static bool MailAttachments
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["MailAttachments"]);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        public static string MailUserID
        {
            get
            {
                return ConfigurationManager.AppSettings["MailUserID"];
            }
        }
        public static string MailPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["MailPassword"];
            }
        }
        public static string MailServerHost
        {
            get
            {
                return ConfigurationManager.AppSettings["MailServerHost"];
            }
        }
        public static int MailServerPort
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["MailServerPort"]);
                }
                catch (Exception)
                {
                    return 587;
                }
            }
        }
        public static string MailFrom
        {
            get
            {
                return ConfigurationManager.AppSettings["MailFrom"];
            }
        }
        public static string MailToPriority
        {
            get
            {
                return ConfigurationManager.AppSettings["MailToPriority"];
            }
        }
        public static string MailToOpsTeam
        {
            get
            {
                return ConfigurationManager.AppSettings["MailToOpsTeam"];
            }
        }
        public static string MailToInfo
        {
            get
            {
                return ConfigurationManager.AppSettings["MailToInfo"];
            }
        }
        public static string MailCC
        {
            get
            {
                return ConfigurationManager.AppSettings["MailCC"];
            }
        }

        public static string Environment
        {
            get
            {
                return ConfigurationManager.AppSettings["Environment"];
            }
        }
        public static int PollingIntervalInbound
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["PollingIntervalInbound"]);
                }
                catch (Exception)
                {
                    return 30;
                }
            }
        }
        public static int PollingIntervalOutbound
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["PollingIntervalOutbound"]);
                }
                catch (Exception)
                {
                    return 30;
                }
            }
        }

        public static int PollingIntervalPOBox
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["PollingIntervalPOBox"]);
                }
                catch (Exception)
                {
                    return 15;
                }
            }
        }

        public static int NoOfRowsCommitBlock
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["NoOfRowsCommitBlock"]);
                }
                catch (Exception)
                {
                    return 2000;
                }
            }
        }

        public static int SQLCommandTimeoutInbound
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["SQLCommandTimeoutInbound"]);
                }
                catch (Exception)
                {
                    return 180;
                }
            }
        }
        public static int SQLCommandTimeoutOutbound
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["SQLCommandTimeoutOutbound"]);
                }
                catch (Exception)
                {
                    return 180;
                }
            }
        }
        public static int Log4NetDaysToKeep
        {
            get
            {
                try
                {
                    return Convert.ToInt16(ConfigurationManager.AppSettings["Log4NetDaysToKeep"]);
                }
                catch (Exception)
                {
                    return 7;
                }
            }
        }
        public static bool EnableInboundService
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["EnableInboundService"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static bool FollowInboundSchedule
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["FollowInboundSchedule"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static bool EnableOutboundService
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["EnableOutboundService"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public static bool EnableScannedInvoiceService
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["EnableScannedInvoiceService"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static bool EnablePreUploadEmail
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreUploadEmail"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static bool EnableFileReceivedEmail
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["EnableFileReceivedEmail"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static bool EnableErrorEmail
        {
            get
            {
                try
                {
                    return Convert.ToBoolean(ConfigurationManager.AppSettings["EnableErrorEmail"]);
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }
        public static string GatewayVersion
        {
            get
            {
                try
                {
                    return Convert.ToString(ConfigurationManager.AppSettings["GatewayVersion"]);
                }
                catch (Exception)
                {
                    return DateTime.Today.ToShortDateString();
                }
            }
        }
        public static string CryptoPublicKey
        {
            get
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["CryptoPublicKey"]))
                        return (Path.Combine(ConfigurationManager.AppSettings["DataRootFolder"], ConfigurationManager.AppSettings["CryptoPublicKey"]));
                    else
                        return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        public static string CryptoPrivateKey
        {
            get
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["CryptoPrivateKey"]))
                        return (Path.Combine(ConfigurationManager.AppSettings["DataRootFolder"], ConfigurationManager.AppSettings["CryptoPrivateKey"]));
                    else
                        return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        public static string CryptoPassword
        {
            get
            {
                try
                {
                    return ConfigurationManager.AppSettings["CryptoPassword"];
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        public static string CryptoUser
        {
            get
            {
                try
                {
                    return ConfigurationManager.AppSettings["CryptoUser"];
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        public static string CryptoFileExtension
        {
            get
            {
                try
                {
                    return ConfigurationManager.AppSettings["CryptoFileExtension"];
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }
        public static string CryptoDBSymKey
        {
            get
            {
                try
                {
                    return ConfigurationManager.AppSettings["CryptoDBSymKey"];
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        public static string CryptoDBCert
        {
            get
            {
                try
                {
                    return ConfigurationManager.AppSettings["CryptoDBCert"];
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

    }
}
