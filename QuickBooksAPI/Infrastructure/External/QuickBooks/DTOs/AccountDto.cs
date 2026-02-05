namespace QuickBooksAPI.Infrastructure.External.QuickBooks.DTOs
{
    public class QuickBooksCoaResponse
    {
        public QueryResponse QueryResponse { get; set; }
    }

    public class QueryResponse
    {
        public List<AccountDto> Account { get; set; }
    }

    public class AccountDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool SubAccount { get; set; }
        public string FullyQualifiedName { get; set; }
        public bool Active { get; set; }
        public string Classification { get; set; }
        public string AccountType { get; set; }
        public string AccountSubType { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal CurrentBalanceWithSubAccounts { get; set; }
        public CurrencyRef CurrencyRef { get; set; }
        public string Domain { get; set; }
        public bool Sparse { get; set; }
        public string SyncToken { get; set; }
        public AccountMetaData? MetaData { get; set; }
    }

    public class CurrencyRef
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class AccountMetaData
    {
        public DateTime CreateTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }
    }
}
