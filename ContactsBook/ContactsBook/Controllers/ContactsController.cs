using ContactsBook.Data;
using ContactsBook.Models;
using ContactsBook.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Text;
using OfficeOpenXml;
using System.Net.Mail;
using System.Linq;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;





namespace ContactsBook.Controllers
{
    [Authorize]

    public class ContactsController : Controller
    {
        private readonly ContactBookDbContext contactBookDbContext;

        public ContactsController(ContactBookDbContext contactBookDbContext)
        {
            this.contactBookDbContext = contactBookDbContext;
        }

        [HttpGet]

        public async Task<IActionResult> Index()
        {
            var contacts = await contactBookDbContext.Contacts.Include(e => e.EmailAddress).ToListAsync();
            return View(contacts);

        }
        

        [HttpGet("{Id}")]
        public async Task<IActionResult> Index(string contactSearch)
        {
            ViewData["GetContactDetails"] = contactSearch;

            var contacts = from x in contactBookDbContext.Contacts select x;

            if (!String.IsNullOrEmpty(contactSearch))
            {
                contacts = contacts.Where(x => x.Name.Contains(contactSearch) || x.EmailAddress.Any(e => e.EmailAddress.Contains(contactSearch)));

            }

            var searchResults = await contacts.AsNoTracking().ToListAsync();

            return View(searchResults);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToCsv(string contactSearch)
        {
            var contacts = from x in contactBookDbContext.Contacts select x;

            if (!String.IsNullOrEmpty(contactSearch))
            {
                contacts = contacts.Where(x => x.Name.Contains(contactSearch) || x.EmailAddress.Any(e => e.EmailAddress.Contains(contactSearch)));

            }

            var searchResults = await contacts.AsNoTracking().ToListAsync();

            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture)) // Use the complete namespace path to CsvWriter
            {
                csv.WriteRecords(searchResults);
                writer.Flush();
                stream.Position = 0;
                var content = await new StreamReader(stream).ReadToEndAsync();
                var bytes = Encoding.UTF8.GetBytes(content);
                return File(bytes, "text/csv", "contacts.csv");
            }
            

         
        }
        // Export search results to Excel file
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string contactSearch)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var contacts = from x in contactBookDbContext.Contacts select x;

            if (!String.IsNullOrEmpty(contactSearch))
            {
                contacts = contacts.Where(x => x.Name.Contains(contactSearch) || x.EmailAddress.Any(e => e.EmailAddress.Contains(contactSearch)));

            }

            var searchResults = await contacts.AsNoTracking().ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Contacts");
                worksheet.Cells.LoadFromCollection(searchResults, true);
                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "contacts.xlsx");
            }
        }

        [HttpGet]
        [Authorize(Roles="Admin")]
        public IActionResult Add()
        {
            var viewModel = new ContactViewModel
            {
                EmailAddress = new List<EmailAddressViewModel>()
            };
            return View(viewModel);
        }
        [HttpPost]
     

        public async Task<IActionResult> Add(ContactViewModel addContactRequest)
        {
            if (ModelState.IsValid)
            {
                var contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    Name = addContactRequest.Name,
                    Lastname = addContactRequest.Lastname,
                    PhoneNumber = addContactRequest.PhoneNumber,
                    Address = addContactRequest.Address,
                    EmailAddress = new List<EmailAddressViewModel>()
                };

                if (addContactRequest.EmailAddress != null)
                {
                    foreach (var emailAddress in addContactRequest.EmailAddress)
                    {
                        if (!string.IsNullOrEmpty(emailAddress.EmailAddress))
                        {
                            var email = new EmailAddressViewModel
                            {
                                Id = Guid.NewGuid(),
                                ContactId = contact.Id,
                                EmailAddress = emailAddress.EmailAddress
                            };
                            contact.EmailAddress.Add(email);
                            await contactBookDbContext.EmailAddresses.AddAsync(email);
                        }
                    }
                }

                await contactBookDbContext.Contacts.AddAsync(contact);
                await contactBookDbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }




        [HttpGet]
        public async Task<IActionResult> View(Guid id)
        {
            var contact = await contactBookDbContext.Contacts.Include(c => c.EmailAddress).FirstOrDefaultAsync(x => x.Id == id);
            if (contact != null)
            {
                var viewmodel = new UpdateViewModel()
                {
                    Id = contact.Id,
                    Name = contact.Name,
                    Lastname = contact.Lastname,
                    PhoneNumber = contact.PhoneNumber,
                    Address = contact.Address,
                    EmailAddress = contact.EmailAddress.Select(e => new EmailAddressViewModel { EmailAddress = e.EmailAddress }).ToList()

                };
                return await Task.Run(() => View("View", viewmodel));
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> View(UpdateViewModel model)
        {
            var contact = await contactBookDbContext.Contacts.Include(c => c.EmailAddress).FirstOrDefaultAsync(x => x.Id == model.Id);
            if (contact != null)
            {
                contact.Name = model.Name;
                contact.Lastname = model.Lastname;
                contact.PhoneNumber = model.PhoneNumber;
                contact.Address = model.Address;

                // remove any existing email addresses not in the updated model
                var emailAddressesToAdd = model.EmailAddress.Where(e => !contact.EmailAddress.Any(x => x.EmailAddress == e.EmailAddress)).ToList();
                foreach (var emailAddress in emailAddressesToAdd)
                {
                    var email = new EmailAddressViewModel()
                    {
                        Id = Guid.NewGuid(),
                        ContactId = contact.Id,
                        EmailAddress = emailAddress.ToString()
                    };
                    await contactBookDbContext.EmailAddresses.AddAsync(email);
                }

                var emailAddressesToRemove = contact.EmailAddress.Where(e => !model.EmailAddress.Any(x => x.EmailAddress == e.EmailAddress)).ToList();
                foreach (var emailAddress in emailAddressesToRemove)
                {
                    contact.EmailAddress.Remove(emailAddress);
                }


                await contactBookDbContext.SaveChangesAsync();
                return RedirectToAction("Index");

            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(UpdateViewModel model)
            {
                var contact = await contactBookDbContext.Contacts.FindAsync(model.Id);
                if (contact != null)
                {
                    contactBookDbContext.Contacts.Remove(contact);
                    await contactBookDbContext.SaveChangesAsync();
                    return RedirectToAction("Index");

                }
                return RedirectToAction("Index");

            }
        [HttpGet]
        public async Task<IActionResult> Import()
        {
            var importedContacts = await contactBookDbContext.Contacts.ToListAsync();

            return View(importedContacts);
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, char delimiter)
        {
            // Read the CSV file
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var reader = new StreamReader(stream))
                {
                    var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        IgnoreBlankLines = true,
                        MissingFieldFound = null,
                        Delimiter = delimiter.ToString()
                    };

                    using (var csv = new CsvHelper.CsvReader(reader, configuration))
                    {
                        var contacts = csv.GetRecords<ContactViewModel>().ToList();

                        foreach (var contact in contacts)
                        {
                            var newContact = new Contact
                            {
                                Id = Guid.NewGuid(),
                                Name = contact.Name,
                                Lastname = contact.Lastname,
                                PhoneNumber = contact.PhoneNumber,
                                Address = contact.Address,
                                EmailAddress = new List<EmailAddressViewModel>()
                            };

                            if (contact.EmailAddress != null)
                            {
                                foreach (var emailAddress in contact.EmailAddress)
                                {
                                    if (!string.IsNullOrEmpty(emailAddress.EmailAddress))
                                    {
                                        var email = new EmailAddressViewModel
                                        {
                                            Id = Guid.NewGuid(),
                                            ContactId = newContact.Id,
                                            EmailAddress = emailAddress.EmailAddress
                                        };
                                        newContact.EmailAddress.Add(email);
                                        await contactBookDbContext.EmailAddresses.AddAsync(email);
                                    }
                                }
                            }

                            await contactBookDbContext.Contacts.AddAsync(newContact);
                        }

                        await contactBookDbContext.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction("Index");
        }

    }
}
