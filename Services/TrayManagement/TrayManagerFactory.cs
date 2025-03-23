using System.Windows.Forms;
using poji.Configuration;
using poji.Interfaces;

namespace poji.Services.TrayManagement
{
    /// <summary>
    /// Factory for creating tray managers
    /// </summary>
    public class TrayManagerFactory
    {
        /// <summary>
        /// Creates a system tray manager with the specified configuration
        /// </summary>
        public static ITrayManager CreateTrayManager(Form ownerForm, TrayIconConfiguration configuration = null)
        {
            configuration ??= new TrayIconConfiguration();
            return new SystemTrayManager(ownerForm, configuration);
        }
    }
}