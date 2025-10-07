namespace OnlineConfiguration
{
    public static class UrlProvider
    {
        private const string ENVIRONMENT_PROD = "prod";
        private const string ENVIRONMENT_STAGE = "stage";

#if !BUILD_PRODUCTION
        public static bool CHEAT_prod = false;
#endif

        /// <summary>
        /// "prod" или "stage"
        /// </summary>
        public static string ENVIRONMENT
        {
            get
            {
#if BUILD_PRODUCTION
                return ENVIRONMENT_PROD;
#else
                if (CHEAT_prod)
                    return ENVIRONMENT_PROD;
                else
                    return ENVIRONMENT_STAGE;
#endif
            }
        }

        public static string GetEnvironment(bool isProd)
            => isProd ? ENVIRONMENT_PROD : ENVIRONMENT_STAGE;
    }
}