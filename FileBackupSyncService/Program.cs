using System;
using System.IO;
using Topshelf;

namespace FileBackupSyncService
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(cfg =>
            {
                //cfg.RunAs("username", "password");
                //otherwise default is run as local system (this has high prevliges).

                cfg.UseNLog();
                cfg.EnableServiceRecovery(recoveryOption =>
                {
                    recoveryOption.RestartService(1);
                });
                cfg.EnablePauseAndContinue();
                cfg.SetServiceName("FileBackupSyncService");
                cfg.SetDisplayName("Documents/Finance New Files Backup Sync");
                cfg.SetDescription("Syncronizes the documents/finance folder on local to backup for new files");
                cfg.StartAutomatically();
                cfg.Service<FileBackupSync>(serviceInstance =>
                {
                    serviceInstance.ConstructUsing(() => new FileBackupSync());
                    serviceInstance.WhenStarted(fbs => fbs.Start());
                    serviceInstance.WhenStopped(fbs => fbs.Stop());
                    serviceInstance.WhenPaused(fbs => fbs.Pause());
                    serviceInstance.WhenContinued(fbs => fbs.Continue());
                });
            });
        }
    }
}
