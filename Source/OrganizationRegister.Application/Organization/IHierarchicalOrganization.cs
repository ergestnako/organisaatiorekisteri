﻿using System;
using System.Collections.Generic;

namespace OrganizationRegister.Application.Organization
{
    public interface IHierarchicalOrganization : IOrganizationName, IHierarchical
    {
        DateTime? ValidFrom { get; }
        DateTime? ValidTo { get; }

        IEnumerable<IHierarchicalOrganization> SubOrganizations { get; }
    }
}