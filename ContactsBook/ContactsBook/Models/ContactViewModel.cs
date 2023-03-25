using ContactsBook.Models.Domain;
using System.Net.Mail;

namespace ContactsBook.Models
{
    public class ContactViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
       
        public List<EmailAddressViewModel> EmailAddress { get; set; }
    }

}
