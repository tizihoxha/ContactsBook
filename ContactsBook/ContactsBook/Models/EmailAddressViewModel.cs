using ContactsBook.Models.Domain;

namespace ContactsBook.Models
{
    public class EmailAddressViewModel
    {

        public Guid Id { get; set; }
        public string EmailAddress { get; set; }
        public Guid ContactId { get; set; }
        public virtual Contact Contact { get; set; }
        

    }
}
