﻿using System;

namespace ServiceRegister.Api.User
{
    public class User
    {
        public string EmailAddress { get; set; }
        public string Password { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string PhoneNumber { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid RoleId { get; set; }
    }
}