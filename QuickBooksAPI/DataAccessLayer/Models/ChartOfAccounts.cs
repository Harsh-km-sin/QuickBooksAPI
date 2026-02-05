namespace QuickBooksAPI.DataAccessLayer.Models
{
    public class ChartOfAccounts
    {
        public int Id { get; set; }
        public string QBOId { get; set; }                     
        public string Name { get; set; }                   
        public bool SubAccount { get; set; }              
        public string FullyQualifiedName { get; set; }    
        public bool Active { get; set; }                   
        public string Classification { get; set; }        
        public string AccountType { get; set; }            
        public string AccountSubType { get; set; }        
        public decimal CurrentBalance { get; set; }        
        public decimal CurrentBalanceWithSubAccounts { get; set; } 
        public string CurrencyRefValue { get; set; }       
        public string CurrencyRefName { get; set; }        
        public string Domain { get; set; }                 
        public bool Sparse { get; set; }                   
        public string SyncToken { get; set; }             
        public DateTime CreateTime { get; set; }           
        public DateTime LastUpdatedTime { get; set; }
        public int UserId { get; set; }
        public string RealmId { get; set; }
    }
}
