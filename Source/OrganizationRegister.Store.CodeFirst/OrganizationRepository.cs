﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using OrganizationRegister.Application;
using OrganizationRegister.Application.Location;
using OrganizationRegister.Application.Organization;
using OrganizationRegister.Application.Settings;
using OrganizationRegister.Common;
using OrganizationRegister.Store.CodeFirst.Model;
using OrganizationRegister.Store.CodeFirst.Querying;
using Address = OrganizationRegister.Store.CodeFirst.Model.Address;
using WebPage = OrganizationRegister.Common.WebPage;

namespace OrganizationRegister.Store.CodeFirst
{
    internal class OrganizationRepository : IOrganizationRepository
    {
        private readonly IStoreContext context;
        private readonly ISettingsRepository settingsRepository;

        public OrganizationRepository(IStoreContext context, ISettingsRepository settingsRepository)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (settingsRepository == null)
            {
                throw new ArgumentNullException("settingsRepository");
            }
            this.context = context;
            this.settingsRepository = settingsRepository;
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }

        public void AddOrganizationAndSave(IOrganization organization)
        {
            if (HasActiveOrganization(organization.BusinessId, organization.Id))
            {
                throw new ArgumentException(string.Format("Organization with business id '{0}' already added.", organization.BusinessId));
            }

            CreateAndSaveOrganizationWithBasicInformation(organization);
        }

        public void AddSubOrganizationAndSave(Guid parentOrganizationId, IOrganization organization)
        {

            Organization dbOrganization = CreateAndSaveOrganizationWithBasicInformation(organization);
            Organization dbParentOrganization = GetDbOrganization(parentOrganizationId);
            dbOrganization.ParentOrganization = dbParentOrganization;
            context.SaveChanges();
        }

        public IReadOnlyCollection<IHierarchicalOrganization> GetOrganizationHierarchy()
        {
            var query = new ActiveOrganizationsQuery(context.Organizations);
            IEnumerable<Organization> dbOrganizations = query.Execute();
            return CreateHierarchicalOrganizations(dbOrganizations.ToList());
        }

        public IReadOnlyCollection<IHierarchicalOrganization> GetOrganizationHierarchy(bool includeFutureOrganizations)
        {
            IEnumerable<Organization> dbOrganizations;
            if (includeFutureOrganizations)
            {
                var query = new ActiveCurrentAndFutureOrganizationsQuery(context.Organizations);
                dbOrganizations = query.Execute();
            }
            else
            {
                var query = new ActiveCurrentOrganizationsQuery(context.Organizations);
                dbOrganizations = query.Execute();
            }

            var dbOrgs = dbOrganizations.ToList();
            // Filter out orphan suborganizations whose parent (future org) is not present.
            // Filtering is made by first finding all root orgs and then calling FilterByParentOrganization for each of them
            var rootOrgs = dbOrgs.Where(org => org.ParentOrganization == null);
            var orgs = new List<Organization>();
            foreach (var rootOrg in rootOrgs)
            {
                orgs.AddRange(FilterByParentOrganization(dbOrgs, rootOrg.Id));
            }

            return CreateHierarchicalOrganizations(orgs);            
        }

        public IReadOnlyCollection<IHierarchicalOrganization> GetOrganizationHierarchyForOrganization(Guid? organizationId, bool includeFutureOrganizations)
        {
            IEnumerable<Organization> dbOrganizations;
            if (includeFutureOrganizations)
            {
                var query = new ActiveCurrentAndFutureOrganizationsQuery(context.Organizations);
                dbOrganizations = query.Execute();
            }
            else
            {
                var query = new ActiveCurrentOrganizationsQuery(context.Organizations);
                dbOrganizations = query.Execute();
            }

            return CreateHierarchicalOrganizationsForOrganization(FilterByParentOrganization(dbOrganizations.ToList(), organizationId), (Guid) organizationId);
        }

        public IReadOnlyCollection<IHierarchicalOrganization> GetCompleteOrganizationHierarchyForOrganization(Guid organizationId)
        {
            var query = new ActiveOrganizationsQuery(context.Organizations);
            var dbOrganizations = query.Execute();
            return CreateHierarchicalOrganizationsForOrganization(FilterByParentOrganization(dbOrganizations.ToList(), organizationId), organizationId);
        }


        public IReadOnlyCollection<IOrganizationListItem> GetOrganizationListForOrganization(Guid organizationId)
        {
            var query = new ActiveCurrentOrganizationsQuery(context.Organizations);
            var dbOrganizations = query.Execute();
            return CreateOrganizationListItems(FilterByParentOrganization(dbOrganizations.ToList(), organizationId));
        }

        public IReadOnlyCollection<IOrganizationName> GetOrganizations()
        {
            var query = new ActiveOrganizationsQuery(context.Organizations);
            IEnumerable<Organization> dbOrganizations = query.Execute();
            return CreateOrganizationNames(dbOrganizations.ToList());
        }

        public IReadOnlyCollection<IOrganizationName> GetMunicipalMainOrganizations(int municipalityCode)
        {
            var query = new ActiveCurrentMainMunicipalOrganizationsQuery(context.Organizations, municipalityCode);
            IEnumerable<Organization> dbOrganizations = query.Execute();
            return CreateOrganizationNames(dbOrganizations.ToList());
        }

        public IReadOnlyCollection<IOrganizationName> GetMainOrganizations()
        {
            var query = new ActiveMainOrganizationsQuery(context.Organizations);
            IEnumerable<Organization> dbOrganizations = query.Execute();
            return CreateOrganizationNames(dbOrganizations.ToList());
        }

        public IReadOnlyCollection<IOrganizationName> GetMainOrganizationsNames()
        {
            return CreateOrganizationNames(context.Organizations.ToList());
        }

        public IHierarchicalOrganization GetHierarchicalOrganization(Guid id)
        {
            Organization dbOrganization = GetDbOrganization(id);
            return OrganizationFactory.CreateHierarchicalOrganization(id, dbOrganization.GetNames(),
                (dbOrganization.ParentOrganizationId != null) ? dbOrganization.ParentOrganization.Id : (Guid?) null, dbOrganization.ValidFrom, dbOrganization.ValidTo);
        }

        public IOrganization GetOrganization(Guid id)
        {
            Organization dbOrganization = GetDbOrganization(id);
            ICollection<Model.WebPage> webPages = dbOrganization.WebPages;
            ICollection<AddressLanguageSpecification> languageSpecificAddressData = dbOrganization.GetLanguageSpecificStreetAddressData();
            ICollection<Address> postalAddresses = dbOrganization.PostalAddresses;
            Guid? visitingAddressId = (dbOrganization.VisitingAddress != null) ? dbOrganization.VisitingAddress.Id : (Guid?) null;

            string phoneNumber = null;
            string callChargeInfoType = null;
            Guid? phoneNumberId = null;

            ICollection<PhoneNumberLanguageSpecification> languageSpecifiPhoneNumberData = null;

            if (dbOrganization.PhoneNumber != null)
            {
                phoneNumberId = dbOrganization.PhoneNumber.Id;
                phoneNumber = dbOrganization.PhoneNumber.Number;
                callChargeInfoType = dbOrganization.PhoneNumber.ChargeType.Type;
                languageSpecifiPhoneNumberData = dbOrganization.PhoneNumber.LanguageSpecifications;
            }

            string emailAddress = (dbOrganization.EmailAddress != null) ? dbOrganization.EmailAddress.Email : null;

            return OrganizationFactory.CreateOrganization(id, dbOrganization.NumericId, dbOrganization.BusinessId, dbOrganization.Oid, dbOrganization.Type.Name,
                dbOrganization.GetNames(), dbOrganization.GetDescriptions(), dbOrganization.MunicipalityCode, dbOrganization.ValidFrom, dbOrganization.ValidTo,
                phoneNumber, callChargeInfoType, CreatePhoneCallChargeInfos(phoneNumberId, languageSpecifiPhoneNumberData), emailAddress, CreateWebPages(webPages),
                CreateStreetAddresses(visitingAddressId, languageSpecificAddressData), dbOrganization.GetStreetAddressPostalCode(),
                CreateLocalities(visitingAddressId, languageSpecificAddressData), CreateAddressQualifiers(visitingAddressId, languageSpecificAddressData),
                CreatePostalStreetAddresses(postalAddresses), dbOrganization.GetPostalStreetAddressPostalCode(),
                CreatePostalStreetAddressLocalities(postalAddresses),
                dbOrganization.GetPostalPostOfficeBoxAddressPostOfficeBox(), dbOrganization.GetPostalPostOfficeBoxAddressPostalCode(),
                CreatePostalPostOfficeBoxAddressLocalities(postalAddresses), dbOrganization.UseStreetAddressAsPostalAddress,
                dbOrganization.ParentOrganizationId.HasValue,
                settingsRepository.GetDataLanguageCodes(), dbOrganization.GetHomepageUrls(), dbOrganization.GetNameAbbreviations(), dbOrganization.CanBeTransferredToFsc, dbOrganization.CanBeResponsibleDeptForService);
        }

        public IOrganizationName GetOrganizationName(Guid id)
        {
            Organization dbOrganization = GetDbOrganization(id);
            return OrganizationFactory.CreateOrganizationName(dbOrganization.Id, dbOrganization.GetNames());
        }

        public void UpdateOrganizationBasicInformation(Guid id, IBasicInformation information, bool allowExistingBusinessId)
        {
            Organization dbOrganization = GetDbOrganization(id);
            if (!allowExistingBusinessId && HasActiveOrganization(information.BusinessId, id))
            {
                throw new ArgumentException(string.Format("Organization with business id '{0}' already added.", information.BusinessId));
            }
            dbOrganization.SetBasicInformation(information, context);
        }

        public void UpdateOrganizationContactInformation(Guid id, IContactInformation information)
        {
            Organization dbOrganization = GetDbOrganization(id);
            dbOrganization.SetContactInformation(information, context);
        }

        public void UpdateOrganizationVisitingAddress(Guid id, IVisitingAddress address)
        {
            Organization dbOrganization = GetDbOrganization(id);
            dbOrganization.SetVisitingAddress(address, context);
        }

        public void UpdateOrganizationPostalAddresses(Guid id, IPostalAddress addresses)
        {
            Organization dbOrganization = GetDbOrganization(id);
            dbOrganization.SetPostalAddresses(addresses, context);
        }

        public bool HasActiveOrganization(string businessId, Guid? excludedOrganizationId)
        {
            return excludedOrganizationId.HasValue ?
                context.Organizations.Any(org => org.BusinessId.Equals(businessId) && org.Active && !org.Id.Equals(excludedOrganizationId.Value) && org.ParentOrganizationId == null) :
                context.Organizations.Any(org => org.BusinessId.Equals(businessId) && org.Active);
        }

        public void RemoveOrganization(Guid id)
        {
            RemoveSubOrganizations(id);
            Organization dbOrganization = GetDbOrganization(id);
            dbOrganization.RemoveAllLanguageSpecificData();
            dbOrganization.RemoveAllWebPages(context);
            dbOrganization.RemoveVisitingAddress(context);
            dbOrganization.RemovePostalAddresses(context);
            context.Organizations.Remove(dbOrganization);
        }

        public void DeactivateOrganization(Guid id)
        {
            var subOrganizationQuery = new SubOrganizationQuery(context.Organizations);
            foreach (Organization subOrganization in subOrganizationQuery.Execute(id).ToList())
            {
                DeactivateOrganization(subOrganization.Id);
            }

            Organization dbOrganization = GetDbOrganization(id);
            dbOrganization.Active = false;
        }

        private Organization GetDbOrganization(Guid id)
        {
            var organizationQuery = new OrganizationQuery(context.Organizations);
            return organizationQuery.Execute(id);
        }

        private void RemoveSubOrganizations(Guid id)
        {
            var subOrganizationQuery = new SubOrganizationQuery(context.Organizations);
            foreach (Organization subOrganization in subOrganizationQuery.Execute(id).ToList())
            {
                RemoveOrganization(subOrganization.Id);
            }
        }

        private Organization CreateAndSaveOrganizationWithBasicInformation(IOrganization organization)
        {
           
            Organization dbOrganization = new Organization { Id = organization.Id };
            dbOrganization.SetBasicInformation(organization, context); 
            context.Organizations.Add(dbOrganization);
            context.SaveChanges();
            return dbOrganization;
        }

        private IEnumerable<WebPage> CreateWebPages(IEnumerable<Model.WebPage> webPages)
        {
            return webPages == null
                ? Enumerable.Empty<WebPage>() : webPages.Select(address => new WebPage(address.Name, address.Url, context.GetWebPageType(address.TypeId).Type));
        }

        private IEnumerable<LocalizedText> CreatePhoneCallChargeInfos(Guid? phoneNumberId, ICollection<PhoneNumberLanguageSpecification> callChargeInfos)
        {

            if (phoneNumberId.HasValue)
            {
                var query = new LanguageSpecificPhoneNumberDataQuery(callChargeInfos);
                return query.Execute(phoneNumberId.Value).Select(info => new LocalizedText(info.Language.Language.Code, info.CallChargeInfo));
            }
            return Enumerable.Empty<LocalizedText>();
        
        }

        private static IEnumerable<LocalizedText> CreateStreetAddresses(Guid? addressId, ICollection<AddressLanguageSpecification> languageSpecificAddressData)
        {
            if (addressId.HasValue)
            {
                var query = new LanguageSpecificAddressDataQuery(languageSpecificAddressData);
                return query.Execute(addressId.Value).Select(address => new LocalizedText(address.Language.Language.Code, address.StreetAddress));
            }
            return Enumerable.Empty<LocalizedText>();
        }

        private static IEnumerable<LocalizedText> CreatePostalStreetAddresses(ICollection<Address> postalAddresses)
        {
            var query = new PostalStreetAddressQuery(postalAddresses);
            Address postalStreetAddress = query.Execute();
            return postalStreetAddress != null ? CreateStreetAddresses(postalStreetAddress.Id, postalStreetAddress.LanguageSpecifications) :
                Enumerable.Empty<LocalizedText>();
        }

        private static IEnumerable<LocalizedText> CreateLocalities(Guid? addressId, ICollection<AddressLanguageSpecification> languageSpecificAddressData)
        {
            if (addressId.HasValue)
            {
                var query = new LanguageSpecificAddressDataQuery(languageSpecificAddressData);
                return query.Execute(addressId.Value).Select(address => new LocalizedText(address.Language.Language.Code, address.PostalDistrict));
            }
            return Enumerable.Empty<LocalizedText>();
        }

        private static IEnumerable<LocalizedText> CreatePostalStreetAddressLocalities(ICollection<Address> postalAddresses)
        {
            var query = new PostalStreetAddressQuery(postalAddresses);
            Address address = query.Execute();
            return address != null ? CreateLocalities(address.Id, address.LanguageSpecifications) : Enumerable.Empty<LocalizedText>();
        }

        private static IEnumerable<LocalizedText> CreatePostalPostOfficeBoxAddressLocalities(ICollection<Address> postalAddresses)
        {
            var query = new PostalPostOfficeBoxAddressQuery(postalAddresses);
            Address address = query.Execute();
            return address != null ? CreateLocalities(address.Id, address.LanguageSpecifications) : Enumerable.Empty<LocalizedText>();
        }

        private static IEnumerable<LocalizedText> CreateAddressQualifiers(Guid? addressId, ICollection<AddressLanguageSpecification> languageSpecificAddressData)
        {
            if (addressId.HasValue)
            {
                var query = new LanguageSpecificAddressDataQuery(languageSpecificAddressData);
                return query.Execute(addressId.Value).Select(address => new LocalizedText(address.Language.Language.Code, address.Qualifier));
            }
            return Enumerable.Empty<LocalizedText>();
        }

        private static IReadOnlyCollection<IHierarchicalOrganization> CreateHierarchicalOrganizations(IReadOnlyCollection<Organization> dbOrganizations)
        {
            List<IHierarchicalOrganization> organizations = new List<IHierarchicalOrganization>();
            foreach (Organization dbOrganization in dbOrganizations)
            {
                organizations.Add(OrganizationFactory.CreateHierarchicalOrganization(dbOrganization.Id, dbOrganization.GetNames(),
                    (dbOrganization.ParentOrganizationId != null) ? dbOrganization.ParentOrganization.Id : (Guid?) null, dbOrganization.ValidFrom, dbOrganization.ValidTo));
            }
            return HierarchicalCollection<IHierarchicalOrganization>.CreateHierarchy(organizations).ToList();
        }

        private static IReadOnlyCollection<IHierarchicalOrganization> CreateHierarchicalOrganizationsForOrganization(IReadOnlyCollection<Organization> dbOrganizations, Guid organizationId)
        {
            List<IHierarchicalOrganization> organizations = new List<IHierarchicalOrganization>();
            foreach (Organization dbOrganization in dbOrganizations)
            {
                // set sub organizations parentorganizationid to null to make it as root organization of hierarchical organization
                organizations.Add(OrganizationFactory.CreateHierarchicalOrganization(dbOrganization.Id, dbOrganization.GetNames(),
                    (dbOrganization.ParentOrganizationId != null && dbOrganization.Id != organizationId) ? dbOrganization.ParentOrganization.Id : (Guid?)null, dbOrganization.ValidFrom, dbOrganization.ValidTo));
            }
            return HierarchicalCollection<IHierarchicalOrganization>.CreateHierarchy(organizations).ToList();
        }

        private static IReadOnlyCollection<IOrganizationName> CreateOrganizationNames(IReadOnlyCollection<Organization> dbOrganizations)
        {
            List<IOrganizationName> organizations = new List<IOrganizationName>();
            foreach (Organization dbOrganization in dbOrganizations)
            {
                organizations.Add(OrganizationFactory.CreateOrganizationName(dbOrganization.Id, dbOrganization.GetNames()));
            }
            return organizations;
        }


        private static IReadOnlyCollection<IOrganizationListItem> CreateOrganizationListItems(IReadOnlyCollection<Organization> dbOrganizations)
        {
            List<IOrganizationListItem> organizations = new List<IOrganizationListItem>();
            foreach (Organization dbOrganization in dbOrganizations)
            {
                organizations.Add(OrganizationFactory.CreateOrganizationListItem(dbOrganization.Id, dbOrganization.GetNames(), dbOrganization.Type.Name,
                    dbOrganization.CanBeTransferredToFsc, dbOrganization.CanBeResponsibleDeptForService));
            }
            return organizations;
        }


        private static IReadOnlyCollection<Organization> FilterByParentOrganization(IReadOnlyCollection<Organization> organizations, Guid? parentOrganizationId)
        {
            var filteredOrganizations = new List<Organization>();

            if (!parentOrganizationId.HasValue || !organizations.Any() || organizations.All(o => o.Id != parentOrganizationId.Value))
            {
                return filteredOrganizations.ToList();
            }
            // filter out root organizations
            var nonRootOrganizations = organizations.Where(org => org.Id == parentOrganizationId || org.ParentOrganizationId.HasValue).ToList();
            // get parent organization 
            var parentOrganization = nonRootOrganizations.Single(x => x.Id == parentOrganizationId);
            // traverse organization hierarchy to get child organizations
            Action<Organization> traverse = null;
            traverse = (n) =>
            {
                filteredOrganizations.Add(n);
                nonRootOrganizations.Where(child => child.ParentOrganizationId == n.Id).ToList().ForEach(traverse);
            };
            // traverse from parent organization
            traverse(parentOrganization);

            return filteredOrganizations;
        }


    }
}