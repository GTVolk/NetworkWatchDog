namespace NetworkWatchDog
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.NWDServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.NWDServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // NWDServiceProcessInstaller
            // 
            this.NWDServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.NWDServiceProcessInstaller.Password = null;
            this.NWDServiceProcessInstaller.Username = null;
            // 
            // NWDServiceInstaller
            // 
            this.NWDServiceInstaller.Description = "Watch Dog for resetting Ethernet connection if it lost";
            this.NWDServiceInstaller.DisplayName = "Network Watch Dog";
            this.NWDServiceInstaller.ServiceName = "Network Watch Dog";
            this.NWDServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.NWDServiceProcessInstaller,
            this.NWDServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller NWDServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller NWDServiceInstaller;
    }
}