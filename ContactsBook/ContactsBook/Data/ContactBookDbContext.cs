using ContactsBook.Models;
using ContactsBook.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace ContactsBook.Data
{
    public class ContactBookDbContext : IdentityDbContext
    {
        public ContactBookDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<EmailAddressViewModel> EmailAddresses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Contact>()
                .HasMany(c => c.EmailAddress)
                .WithOne(e => e.Contact)
                .HasForeignKey(e => e.ContactId);



            modelBuilder.Entity<EmailAddressViewModel>()
                .HasOne(e => e.Contact)
                .WithMany(c => c.EmailAddress)
                .HasForeignKey(e => e.ContactId);
        }
    }

}