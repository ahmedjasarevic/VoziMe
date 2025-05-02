using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;

namespace VoziMe.Models
{
    internal class DriverRating
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("driverid")]
        public int DriverId { get; set; }

        [Column("customerid")]
        public int CustomerId { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("ratedat")]
        public DateTime RatedAt { get; set; }
    }
}
