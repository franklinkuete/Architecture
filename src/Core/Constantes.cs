

public static class Constante
{
    public static class Role
    {
        public const string SUPERADMIN = "SuperAdministrator";
        public const string ADMINISTRATOR = "Administrator";
        public const string USERMANAGER = "UserManager";
        public const string ROLEMANAGER = "RoleManager";
        public const string ZEROACCESS = "ZeroAccess";
    }

    public static class Prefix
    {
        public const string DBPrefix = "[Dabatase] ";
        public const string HandlerPrefix = "[Handler] ";
        public const string ApiPrefix = "[Api]";
        public const string MetricsPrefix = "[Metric]";
        public const string RepositoryPrefix = "[Repository]";
        public const string KafkaPrefix = "[Kafka]";
        public const string MassTransitPrefix = "[Kafka (MassTransit)]";
        public const string CachePrefix = "[Cache]";
        public const string BusinessValidationPrefix = "[BusinessValidation]";
        public const string RequestValidationPrefix = "[RequestValidation]";

    }
}
