namespace ELO.Models
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The object used for initializing and using our database
    /// </summary>
    public class DatabaseObject
    {
        /// <summary>
        /// Gets or sets Time period for full backup
        /// </summary>
        public string FullBackup { get; set; } = "0 */6 * * *";

        /// <summary>
        /// Gets or sets Time period for incremental backup
        /// </summary>
        public string IncrementalBackup { get; set; } = "0 2 * * *";

        /// <summary>
        /// Gets or sets a value indicating whether the config is created.
        /// </summary>
        public bool IsConfigCreated { get; set; }

        /// <summary>
        /// Gets or sets The name.
        /// </summary>
        public string Name { get; set; } = "RavenBOT";

        public string PrefixOverride { get; set; } = null;

        /// <summary>
        /// Gets or sets The urls.
        /// </summary>
        public List<string> Urls { get; set; } = new List<string>();

        /// <summary>
        /// The backup folder.
        /// </summary>
        public string BackupFolder => Directory.CreateDirectory("Backup").FullName;
    }
}