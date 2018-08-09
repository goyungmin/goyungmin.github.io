namespace ELO.Models
{
    using System.Collections.Generic;

    using ELO.Handlers;

    /// <summary>
    ///     The token model.
    /// </summary>
    public class TokenModel
    {
        /// <summary>
        ///     Gets or sets the token list.
        /// </summary>
        public List<TokenClass> TokenList { get; set; } = new List<TokenClass>();

        /// <summary>
        ///     Loads the token model
        /// </summary>
        /// <returns>
        ///     The <see cref="TokenModel" />.
        /// </returns>
        public static TokenModel Load()
        {
            using (var session = DatabaseHandler.Store.OpenSession())
            {
                var model = session.Load<TokenModel>("tokens");
                if (model == null)
                {
                    model = new TokenModel();
                    model.Save();
                }

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
                session.Store(this, "tokens");
                session.SaveChanges();
            }
        }

        /// <summary>
        ///     The Token Class
        /// </summary>
        public class TokenClass
        {
            /// <summary>
            ///     Gets or sets the Token
            /// </summary>
            public string Token { get; set; }

            /// <summary>
            ///     Gets or sets the days till expires
            /// </summary>
            public int Days { get; set; }
        }
    }
}