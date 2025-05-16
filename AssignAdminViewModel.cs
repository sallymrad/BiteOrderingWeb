using BiteOrderWeb.Models;
using System.Collections.Generic;

namespace BiteOrderWeb.ViewModels
{
    public class AssignAdminViewModel
    {
        public int RestaurantId { get; set; }
        public List<Users> AvailableAdmins { get; set; }
    }
}

