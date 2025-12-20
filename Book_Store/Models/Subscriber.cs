using System;
using System.ComponentModel.DataAnnotations;

namespace Book_Store.Models
{
    public class Subscriber
    {
        public int SubscriberId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

