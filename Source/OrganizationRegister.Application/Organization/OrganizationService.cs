﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OrganizationRegister.Application.Settings;
using OrganizationRegister.Common;
using Affecto.Authentication.Claims;
using OrganizationRegister.Common.User;

namespace OrganizationRegister.Application.Organization
{
    internal class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository organizationRepository;
        private readonly ISettingsRepository settingsRepository;
        private readonly IAuthenticatedUserContext userContext;

        public OrganizationService(IOrganizationRepository organizationRepository, ISettingsRepository settingsRepository, IAuthenticatedUserContext userContext = null)
        {
            if (organizationRepository == null)
            {
                throw new ArgumentNullException("organizationRepository");
            }
            if (settingsRepository == null)
            {
                throw new ArgumentNullException("settingsRepository");
            }
            this.organizationRepository = organizationRepository;
            this.settingsRepository = settingsRepository;
            this.userContext = userContext;
        }

        public Guid AddOrganization(string businessId, string oid, string type, string municipalityCode, IEnumerable<LocalizedText> names, IEnumerable<LocalizedText> descriptions, 
            DateTime? validFrom, DateTime? validTo, IEnumerable<LocalizedText> nameAbbreviations, bool canBeTransferredToFsc, bool canBeResponsibleDeptForService)
        {
            CheckAddOrganizationPermission();

            IReadOnlyCollection<string> languageCodes = settingsRepository.GetDataLanguageCodes();
            // TODO: Wrap id generation into a service
            var id = Guid.NewGuid();
            var organization = new Organization(id, businessId, oid, type, municipalityCode, names, languageCodes, canBeTransferredToFsc, canBeResponsibleDeptForService) { Descriptions = descriptions, NameAbbreviations = nameAbbreviations, HomepageUrls = null};
            organization.SetValidity(validFrom, validTo);
            organizationRepository.AddOrganizationAndSave(organization);
            return id;
        }

        public Guid AddSubOrganization(Guid parentOrganizationId, string businessId, string oid, string type, string municipalityCode, IEnumerable<LocalizedText> names,
            IEnumerable<LocalizedText> descriptions, DateTime? validFrom, DateTime? validTo, IEnumerable<LocalizedText> nameAbbreviations, bool canBeTransferredToFsc, bool canBeResponsibleDeptForService)
        {
            CheckManageOrganizationPermission(parentOrganizationId);

            IReadOnlyCollection<string> languageCodes = settingsRepository.GetDataLanguageCodes();
            // TODO: Wrap id generation into a service
            var id = Guid.NewGuid();
            var organization = new SubOrganization(id, businessId, oid, type, municipalityCode, names, languageCodes, canBeTransferredToFsc, canBeResponsibleDeptForService) { Descriptions = descriptions, NameAbbreviations = nameAbbreviations, HomepageUrls = null };
            organization.SetValidity(validFrom, validTo);
            organizationRepository.AddSubOrganizationAndSave(parentOrganizationId, organization);
            return id;
        }

        public IEnumerable<IHierarchicalOrganization> GetOrganizationHierarchy()
        {
            return organizationRepository.GetOrganizationHierarchy();
        }

        public IEnumerable<IHierarchicalOrganization> GetOrganizationsAsFlatlist(string searchTerm, Guid? organizationId)
        {
            // TODO : repository methods for searching organizations
            IEnumerable<IHierarchicalOrganization> orgs; 
            if (organizationId != null)
            {
                orgs = organizationRepository.GetCompleteOrganizationHierarchyForOrganization((Guid)organizationId).Flatten(o => o.SubOrganizations);
            }
            else
            {
                orgs = organizationRepository.GetOrganizationHierarchy().Flatten(o => o.SubOrganizations);
            }

            return from org in orgs
                   where org.Names.Any(x => x.LocalizedValue!= null && x.LocalizedValue.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                                select org;

        }


        public IEnumerable<IOrganizationListItem> GetOrganizationListForMunicipality(int rootMunicipalityCode)
        {
            var orgs = new List<IOrganizationListItem>();
            foreach (var org in organizationRepository.GetMunicipalMainOrganizations(rootMunicipalityCode))
            {
                orgs.AddRange(organizationRepository.GetOrganizationListForOrganization(org.Id));
            }
            return orgs;
        }

        public IEnumerable<IHierarchicalOrganization> GetOrganizationHierarchy(bool includeFutureOrganizations)
        {
            return organizationRepository.GetOrganizationHierarchy(includeFutureOrganizations);
        }

        public IEnumerable<IHierarchicalOrganization> GetOrganizationHierarchyForOrganization(Guid? organizationId, bool includeFutureOrganizations)
        {
            return organizationRepository.GetOrganizationHierarchyForOrganization(organizationId, includeFutureOrganizations);
        }

        public IEnumerable<IHierarchicalOrganization> GetCompleteOrganizationHierarchyForOrganization(Guid organizationId)
        {
            return organizationRepository.GetCompleteOrganizationHierarchyForOrganization(organizationId);
        }

        public IEnumerable<IOrganizationName> GetOrganizations()
        {
            return organizationRepository.GetOrganizations();
        }

        public IEnumerable<IOrganizationName> GetMainOrganizations()
        {
            return organizationRepository.GetMainOrganizations();
        }

        public IEnumerable<IOrganizationName> GetMainOrganizationsNames()
        {
            return organizationRepository.GetMainOrganizationsNames();
        }

        public IOrganization GetOrganization(Guid organizationId)
        {
            return organizationRepository.GetOrganization(organizationId);
        }

        public IOrganizationName GetOrganizationName(Guid organizationId)
        {
            return organizationRepository.GetOrganizationName(organizationId);
        }

        public void SetOrganizationBasicInformation(Guid organizationId, string businessId, string oid, IEnumerable<LocalizedText> names, IEnumerable<LocalizedText> descriptions, 
            string type, string municipalityCode, DateTime? validFrom, DateTime? validTo, IEnumerable<LocalizedText> nameAbbreviations, bool canBeTransferredToFsc, bool canBeResponsibleDeptForService)
        {
            CheckManageOrganizationPermission(organizationId);

            Organization organization = GetOrganization(organizationId) as Organization;
            organization.BusinessId = businessId;
            organization.Oid = oid;
            organization.Names = names;
            organization.Descriptions = descriptions;
            organization.NameAbbreviations = nameAbbreviations;
            organization.CanBeTransferredToFsc = canBeTransferredToFsc;
            organization.SetType(type, municipalityCode);
            organization.SetValidity(validFrom, validTo);
            organization.SetCanBeResponsibleDeptForService(canBeResponsibleDeptForService);
            organizationRepository.UpdateOrganizationBasicInformation(organizationId, organization, organization is SubOrganization);
            organizationRepository.SaveChanges();
        }

        public void SetOrganizationContactInformation(Guid organizationId, string phoneNumber, string callChargeType, IEnumerable<LocalizedText> callChargeInfos, string emailAddress, IEnumerable<WebPage> webSites, IEnumerable<LocalizedText> homepageUrls)
         {

            CheckManageOrganizationPermission(organizationId);

            Organization organization = GetOrganization(organizationId) as Organization;
            organization.SetCallInformation(phoneNumber, callChargeType, callChargeInfos);
            organization.EmailAddress = emailAddress;
            organization.WebPages = webSites;
            organization.HomepageUrls = homepageUrls;
            organizationRepository.UpdateOrganizationContactInformation(organizationId, organization);
            organizationRepository.SaveChanges();
        }

        public void SetOrganizationVisitingAddress(Guid organizationId, IEnumerable<LocalizedText> streetAddresses, string postalCode, IEnumerable<LocalizedText> postalDistricts, 
            IEnumerable<LocalizedText> qualifiers)
        {
            CheckManageOrganizationPermission(organizationId);

            Organization organization = GetOrganization(organizationId) as Organization;
            organization.SetVisitingAddress(streetAddresses, postalCode, postalDistricts);
            organization.VisitingAddressQualifiers = qualifiers;
            organizationRepository.UpdateOrganizationVisitingAddress(organizationId, organization);
            organizationRepository.SaveChanges();
        }

        public void SetOrganizationPostalAddresses(Guid organizationId, bool useVisitingAddress, IEnumerable<LocalizedText> streetAddresses, string streetAddressPostalCode,
            IEnumerable<LocalizedText> streetAddressPostalDistricts, string postOfficeBox, string postOfficeBoxAddressPostalCode, 
            IEnumerable<LocalizedText> postOfficeBoxAddressPostalDistricts)
        {
            CheckManageOrganizationPermission(organizationId);

            Organization organization = GetOrganization(organizationId) as Organization;
            organization.SetPostalAddress(useVisitingAddress, streetAddresses, streetAddressPostalCode, streetAddressPostalDistricts, postOfficeBox, 
                postOfficeBoxAddressPostalCode, postOfficeBoxAddressPostalDistricts);
            organizationRepository.UpdateOrganizationPostalAddresses(organizationId, organization);
            organizationRepository.SaveChanges();
        }

        public void RemoveOrganization(Guid organizationId)
        {
            CheckManageOrganizationPermission(organizationId);

            organizationRepository.RemoveOrganization(organizationId);
            organizationRepository.SaveChanges();
        }

        public void DeactivateOrganization(Guid organizationId)
        {
            CheckManageOrganizationPermission(organizationId);

            organizationRepository.DeactivateOrganization(organizationId);
            organizationRepository.SaveChanges();
        }



        private void CheckManageOrganizationPermission(Guid organizationId)
        {
            if (userContext == null)
            {
                throw new ArgumentNullException("userContext");
            }

            Guid userOrganizationId = userContext.GetUserOrganizationId();
            var userOrgs = this.GetCompleteOrganizationHierarchyForOrganization(userOrganizationId).Flatten(o => o.SubOrganizations);
            userContext.CheckPermission(userOrgs.Any(o => o.Id == organizationId)
                ? Permissions.Organization.MaintenanceOfOwnOrganizationData : Permissions.Organization.MaintenanceOfAllOrganizationData);
        }

        private void CheckAddOrganizationPermission()
        {
            if (userContext == null)
            {
                throw new ArgumentNullException("userContext");
            }
            userContext.CheckPermission(Permissions.Organization.MaintenanceOfAllOrganizationData);
        }
    }
}
