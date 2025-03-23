using System.Collections.Generic;

namespace poji.Configuration
{
    /// <summary>
    /// Configuration for tray icon
    /// </summary>
    public class TrayIconConfiguration
    {
        public string IconText { get; set; }
        public string AboutText { get; set; }
        public string AboutTitle { get; set; }
        public IList<float> ScaleOptions { get; set; }

        public TrayIconConfiguration()
        {
            ScaleOptions = new List<float> { 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f };
        }
    }
}