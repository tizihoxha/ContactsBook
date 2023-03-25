namespace ContactsBook.Models.Domain
{
    public class EmailAddresses
    {
        

        public Guid Id { get; set; }
        public string EmailAddress { get; set; }
        public Guid ContactId { get; set; }
        public virtual Contact Contact { get; set; }


    }
}

