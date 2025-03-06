using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;
using System.Management;


namespace FMSoftlab.WorkflowTasks.Tasks
{
    public class ApplicationPoolIdentity
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ApplicationPoolConfiguration
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public ApplicationPoolIdentity Identity { get; set; }
        public ApplicationPoolConfiguration()
        {
            Identity = new ApplicationPoolIdentity();
        }
    }
    public class IISApplicationParams : TaskParamsBase
    {
        public string SiteName { get; set; }
        public string VirtualPath { get; set; }
        public string PhysicalPath { get; set; }
        public ApplicationPoolConfiguration ApplicationPool { get; set; }
        public override void LoadResults(IGlobalContext globalContext)
        {
            ApplicationPool=new ApplicationPoolConfiguration();
        }
    }
    public class IISApplicationManager : BaseTaskWithParams<IISApplicationParams>
    {
        public IISApplicationManager(string name, IGlobalContext globalContext, IISApplicationParams taskParams, ILogger log) : base(name, globalContext, taskParams, log)
        {

        }

        public IISApplicationManager(string name, IGlobalContext globalContext, BaseTask parent, IISApplicationParams taskParams, ILogger log) : base(name, globalContext, parent, taskParams, log)
        {

        }

        public override async Task Execute()
        {
            IISManager manager = new IISManager(_log);
            await manager.SetupIIS(
                TaskParams.SiteName,
                TaskParams.ApplicationPool.Name,
                TaskParams.ApplicationPool.Identity.Username,
                TaskParams.ApplicationPool.Identity.Password,
                TaskParams.VirtualPath,
                TaskParams.PhysicalPath);
        }
    }
    public class IISManager
    {
        private readonly ILogger _log;
        public IISManager(ILogger log)
        {
            _log=log;
        }
        public void CreateApplicationPool(string appPoolName, string domainUser, string password)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                ApplicationPool appPool = serverManager.ApplicationPools[appPoolName];

                if (appPool == null)
                {
                    appPool = serverManager.ApplicationPools.Add(appPoolName);
                    appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                    appPool.ProcessModel.UserName = domainUser;
                    appPool.ProcessModel.Password = password;

                    serverManager.CommitChanges();
                    _log?.LogInformation($"Application Pool '{appPoolName}' created successfully.");
                }
                else
                {
                    _log?.LogWarning($"Application Pool '{appPoolName}' already exists.");
                }
            }
        }

        public void CreateVirtualApplication(string siteName, string appPath, string physicalPath, string appPoolName)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                Site site = serverManager.Sites[siteName];

                if (site == null)
                {
                    _log?.LogWarning($"Site '{siteName}' not found.");
                    return;
                }
                Application app = site.Applications[appPath];

                if (app == null)
                {
                    app = site.Applications.Add(appPath, physicalPath);
                    app.ApplicationPoolName = appPoolName;

                    serverManager.CommitChanges();
                    _log?.LogInformation($"Virtual application '{appPath}' created under site '{siteName}'.");
                }
                else
                {
                    _log?.LogWarning($"Application '{appPath}' already exists under site '{siteName}'.");
                }
            }
        }

        public async Task SetupIIS(string siteName, string appPoolName, string domainUser, string password, string appPath, string physicalPath)
        {
            await Task.Run(() =>
            {
                CreateApplicationPool(appPoolName, domainUser, password);
                CreateVirtualApplication(siteName, appPath, physicalPath, appPoolName);
            });
        }
    }

    public class RemoteIISManager
    {
        public void CreateRemoteAppPool(string remoteServer, string appPoolName)
        {
            ConnectionOptions options = new ConnectionOptions
            {
                Username = @"DOMAIN\AdminUser",
                Password = "Password123",
                Authority = "ntlmdomain:DOMAIN"
            };

            ManagementScope scope = new ManagementScope($@"\\{remoteServer}\root\WebAdministration", options);
            scope.Connect();

            ManagementClass appPoolClass = new ManagementClass(scope, new ManagementPath("ApplicationPool"), null);
            ManagementObject newPool = appPoolClass.CreateInstance();
            newPool["Name"] = appPoolName;
            newPool.Put();

            Console.WriteLine($"Application Pool '{appPoolName}' created on '{remoteServer}'.");
        }
    }
}
