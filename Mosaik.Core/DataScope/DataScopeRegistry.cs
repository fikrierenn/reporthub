namespace Mosaik.Core.DataScope
{
    // DI ile kayıtlı tüm IUserDataScope'ları tutar, scope key ile resolve eder.
    // UserDataFilterInjector ve ReportsController buradan IUserDataScope çeker.
    public class DataScopeRegistry
    {
        private readonly Dictionary<string, IUserDataScope> _scopes;

        public DataScopeRegistry(IEnumerable<IUserDataScope> scopes)
        {
            _scopes = scopes.ToDictionary(s => s.Scope, StringComparer.OrdinalIgnoreCase);
        }

        public IUserDataScope? Get(string scope) =>
            _scopes.TryGetValue(scope, out var found) ? found : null;

        public IEnumerable<IUserDataScope> All => _scopes.Values;

        public IEnumerable<IUserDataScope> RequireExplicitGrant =>
            _scopes.Values.Where(s => s.RequiresExplicitGrant);
    }
}
