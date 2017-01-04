﻿using System;
using System.Collections.Generic;
using System.Linq;
using OrganizationRegister.Application.Settings;
using OrganizationRegister.Common;

namespace OrganizationRegister.Application.Organization
{
    internal class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository organizationRepository;
        private readonly ISettingsRepository settingsRepository;

        public OrganizationService(IOrganizationRepository organizationRepository, ISettingsRepository settingsRepository)
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
        }

        public Guid AddOrganization(string businessId, string oid, string type, string municipalityCode, IEnumerable<LocalizedText> names, IEnumerable<LocalizedText> descriptions, 
            DateTime? validFrom, DateTime? validTo, IEnumerable<LocalizedText> nameAbbreviations)
        {
            IReadOnlyCollection<string> languageCodes = settingsRepository.GetDataLanguageCodes();
            // TODO: Wrap id generation into a service
            var id = Guid.NewGuid();
            var organization = new Organization(id, businessId, oid, type, municipalityCode, names, languageCodes) { Descriptions = descriptions, NameAbbreviations = nameAbbreviations, HomepageUrls = null};
            organization.SetValidity(validFrom, validTo);
            organizationRepository.AddOrganizationAndSave(organization);

           
            return id;
        }

        public Guid AddSubOrganization(Guid parentOrganizationId, string businessId, string oid, string type, string municipalityCode, IEnumerable<LocalizedText> names,
            IEnumerable<LocalizedText> descriptions, DateTime? validFrom, DateTime? validTo, IEnumerable<LocalizedText> nameAbbreviations)
        {
            IReadOnlyCollection<string> languageCodes = settingsRepository.GetDataLanguageCodes();
            // TODO: Wrap id generation into a service
            var id = Guid.NewGuid();
            var organization = new SubOrganization(id, businessId, oid, type, municipalityCode, names, languageCodes) { Descriptions = descriptions, NameAbbreviations = nameAbbreviations, HomepageUrls = null };
            organization.SetValidity(validFrom, validTo);
            organizationRepository.AddSubOrganizationAndSave(parentOrganizationId, organization);
            return id;
        }

        public IEnumerable<IHierarchicalOrganization> GetOrganizationHierarchy()
        {
            return organizationRepository.GetOrganizationHierarchy();
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
            string type, string municipalityCode, DateTime? validFrom, DateTime? validTo, IEnumerable<LocalizedText> nameAbbreviations)
        {
            Organization organization = GetOrganization(organizationId) as Organization;
            organization.BusinessId = businessId;
            organization.Oid = oid;
            organization.Names = names;
            organization.Descriptions = descriptions;
            organization.NameAbbreviations = nameAbbreviations;
            organization.SetType(type, municipalityCode);
            organization.SetValidity(validFrom, validTo);
            organizationRepository.UpdateOrganizationBasicInformation(organizationId, organization, organization is SubOrganization);
            organizationRepository.SaveChanges();
        }

        public void SetOrganizationContactInformation(Guid organizationId, string phoneNumber, string callChargeType, IEnumerable<LocalizedText> callChargeInfos, string emailAddress, IEnumerable<WebPage> webSites, IEnumerable<LocalizedText> homepageUrls)
         {
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
            Organization organization = GetOrganization(organizationId) as Organization;
            organization.SetPostalAddress(useVisitingAddress, streetAddresses, streetAddressPostalCode, streetAddressPostalDistricts, postOfficeBox, 
                postOfficeBoxAddressPostalCode, postOfficeBoxAddressPostalDistricts);
            organizationRepository.UpdateOrganizationPostalAddresses(organizationId, organization);
            organizationRepository.SaveChanges();
        }

        public void RemoveOrganization(Guid organizationId)
        {
            organizationRepository.RemoveOrganization(organizationId);
            organizationRepository.SaveChanges();
        }

        public void DeactivateOrganization(Guid organizationId)
        {
            organizationRepository.DeactivateOrganization(organizationId);
            organizationRepository.SaveChanges();
        }
    }
}
