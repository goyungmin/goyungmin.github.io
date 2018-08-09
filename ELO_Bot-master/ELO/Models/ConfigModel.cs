namespace ELO.Models
{
    using ELO.Handlers;

    /// <summary>
    /// The config model.
    /// </summary>
    public class ConfigModel
    {
        /// <summary>
        /// Gets or sets the amount of shards for the bot
        /// </summary>
        public int Shards { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the bot prefix
        /// </summary>
        public string Prefix { get; set; } = "+";

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        public string Token { get; set; } = "Token";

        /// <summary>
        /// Gets or sets a value indicating whether to log user messages.
        /// </summary>
        public bool LogUserMessages { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to log command usages.
        /// </summary>
        public bool LogCommandUsages { get; set; } = true;

        public string PurchaseLink { get; set; } = null;

        /// <summary>
        ///     Loads the token model
        /// </summary>
        /// <returns>
        ///     The <see cref="TokenModel" />.
        /// </returns>
        public static ConfigModel Load()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                var model = session.Load<ConfigModel>("Config") ?? new ConfigModel();
                return model;
            }
        }

        /// <summary>
        ///     Saves the token model
        /// </summary>
        public void Save()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                session.Store(this, "Config");
                session.SaveChanges();
            }
        }
    }
}