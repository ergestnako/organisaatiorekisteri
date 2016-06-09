﻿"use strict";

module OrganizationRegister
{
    export class Organization implements Affecto.Base.IModel
    {
        public name: string;

        //public namesLang: Array<LocalizedText>;

        
        public description: string;
        public descriptionAsHtml: string;
        public validFromText: string;
        public validToText: string;
        public validFrom: string; // Web API data
        public validTo: string; // Web API data
        public webPageUrl: string;
        public webPageName: string;
        public webPageType: string;
        public editedWebPageUrl: string;
        public editedWebPageName: string;
        public editedWebPageType: string;
        public visitingStreetAddress: string;
        public visitingAddressPostalCode: string;
        public visitingAddressPostalDistrict: string;
        public visitingAddressQualifier: string;
        public postalStreetAddressStreet: string;
        public postalStreetAddressPostalCode: string;
        public postalStreetAddressPostalDistrict: string;
        public postalPostOfficeBoxAddressPostOfficeBox: string;
        public postalPostOfficeBoxAddressPostalCode: string;
        public postalPostOfficeBoxAddressPostalDistrict: string;
        public postalAddressTypes: PostalAddressTypes;

        constructor(public id?: string,
            public numericId?: number,
            public names?: Array<LocalizedText>,
            public businessId?: string,
            private descriptions?: Array<LocalizedText>,
            public oid?: string,
            public type?: string,
            public municipalityCode?: number,
            public validFromDate?: Date,
            public validToDate?: Date,
            public phoneNumber?: string,
            public phoneCallFee?: string,
            public emailAddress?: string,
            public webPages?: Array<WebPage>,
            public visitingAddress?: StreetAddress,
            public visitingAddressQualifiers?: Array<LocalizedText>,
            public useVisitingAddressAsPostalAddress?: boolean,
            public postalStreetAddress?: StreetAddress,
            public postalPostOfficeBoxAddress?: PostOfficeBoxAddress,
            public isSubOrganization?: boolean)
        {
            if (names != null && names.length > 0)
            {
                this.name = names[0].localizedValue;
            }

            this.names = this.initializeLocalizedTexts(names, ["fi"]);

            if (descriptions != null && descriptions.length > 0)
            {
                this.description = descriptions[0].localizedValue;
                this.descriptionAsHtml = Affecto.HtmlContent.escapeAndReplaceNewLines(this.description);
            }
            this.setValidityTexts();
            this.initializeVisitingAddress(visitingAddress, visitingAddressQualifiers);
            this.initializePostalAddress(postalStreetAddress, postalPostOfficeBoxAddress);
            if (this.webPages == null)
            {
                this.webPages = new Array<WebPage>();
            }
        }

        private initializeLocalizedTexts(texts: Array<LocalizedText>, requiredLangs: string[]): Array<LocalizedText>
        {
            let langs = DataLocalization.languageCodes;
            let localizedTexts = new Array<LocalizedText>();

            langs.forEach((item) =>
            {
                // init LocalizedText with existing value or create with empty value + set isrequired flag
                localizedTexts.push(new LocalizedText(String(item), this.getLocalizedTextValue(texts,String(item)), (requiredLangs.indexOf(String(item)) >= 0)));   
            });

            return localizedTexts;
        }

       
        private getLocalizedTextValue(texts: Array<LocalizedText>, languageCode: string): string
        {   
            if (texts!=null && texts.some((arrVal: LocalizedText) => (languageCode === arrVal.languageCode)))
            {
                return (texts.filter((arrVal: LocalizedText) => (languageCode === arrVal.languageCode)))[0]
                    .localizedValue;
            }
            else
            {
                return "";
            }
        }

        private getLocalizedTextsWithValues(texts: Array<LocalizedText>)
        {
            var localizedTexts = new Array<LocalizedText>();
            texts.forEach((item) =>
            {
                if (item.localizedValue != null && item.localizedValue !== "")
                    localizedTexts.push(item);
            });
            return localizedTexts;
        }


        private setValidityTexts()
        {
            this.validFromText = Affecto.DateConverter.toFinnishDate(this.validFromDate);
            this.validToText = Affecto.DateConverter.toFinnishDate(this.validToDate);
            this.validFrom = Affecto.DateConverter.toISO8601Date(this.validFromDate);
            this.validTo = Affecto.DateConverter.toISO8601Date(this.validToDate);
        }

        private initializeVisitingAddress(visitingAddress?: StreetAddress,
            visitingAddressQualifiers?: Array<LocalizedText>): void
        {
            if (visitingAddress != null &&
                visitingAddress.streetAddresses != null &&
                visitingAddress.streetAddresses.length > 0 &&
                visitingAddress.postalDistricts != null &&
                visitingAddress.postalDistricts.length > 0)
            {
                this.visitingStreetAddress = visitingAddress.streetAddresses[0].localizedValue;
                this.visitingAddressPostalCode = visitingAddress.postalCode;
                this.visitingAddressPostalDistrict = visitingAddress.postalDistricts[0].localizedValue;
            }
            if (visitingAddressQualifiers != null && visitingAddressQualifiers.length > 0)
            {
                this.visitingAddressQualifier = visitingAddressQualifiers[0].localizedValue;
            }
        }

        private initializePostalAddress(streetAddress?: StreetAddress, postOfficeBoxAddress?: PostOfficeBoxAddress):
        void
        {
            this.postalAddressTypes = new PostalAddressTypes();
            if (streetAddress != null &&
                streetAddress.streetAddresses != null &&
                streetAddress.streetAddresses.length > 0 &&
                streetAddress.postalDistricts != null &&
                streetAddress.postalDistricts.length > 0)
            {
                this.postalStreetAddressStreet = streetAddress.streetAddresses[0].localizedValue;
                this.postalStreetAddressPostalCode = streetAddress.postalCode;
                this.postalStreetAddressPostalDistrict = streetAddress.postalDistricts[0].localizedValue;
                this.addPostalAddressType(PostalAddressType[PostalAddressType.SeparateStreetAddress]);
            }
            else
            {
                if (this.useVisitingAddressAsPostalAddress)
                {
                    this.addPostalAddressType(PostalAddressType[PostalAddressType.SameAsVisitingAddress]);
                }
                else
                {
                    this.setAvailablePostalStreetAddressTypes();
                }
            }
            if (postOfficeBoxAddress != null &&
                postOfficeBoxAddress.postalDistricts != null &&
                postOfficeBoxAddress.postalDistricts.length > 0)
            {
                this.postalPostOfficeBoxAddressPostOfficeBox = postOfficeBoxAddress.postOfficeBox;
                this.postalPostOfficeBoxAddressPostalCode = postOfficeBoxAddress.postalCode;
                this.postalPostOfficeBoxAddressPostalDistrict = postOfficeBoxAddress.postalDistricts[0].localizedValue;
                this.addPostalAddressType(PostalAddressType[PostalAddressType.PostOfficeBoxAddress]);
            }
            else
            {
                this.postalAddressTypes.available.setAvailable(PostalAddressType.PostOfficeBoxAddress);
            }
        }

        public get effectivePostalStreetAddressStreet(): string
        {
            if (this.useVisitingAddressAsPostalAddress)
            {
                return this.visitingStreetAddress;
            }
            return this.postalStreetAddressStreet;
        }

        public get effectivePostalStreetAddressPostalCode(): string
        {
            if (this.useVisitingAddressAsPostalAddress)
            {
                return this.visitingAddressPostalCode;
            }
            return this.postalStreetAddressPostalCode;
        }

        public get effectivePostalStreetAddressPostalDistrict(): string
        {
            if (this.useVisitingAddressAsPostalAddress)
            {
                return this.visitingAddressPostalDistrict;
            }
            return this.postalStreetAddressPostalDistrict;
        }

        public isMunicipality(): boolean
        {
            return this.type === "Kunta";
        }

        public get typeProperty(): string
        {
            return this.type;
        }

        public set typeProperty(value: string)
        {
            this.type = value;
            if (!this.isMunicipality())
            {
                this.municipalityCode = null;
            }
        }

        public generateBasicInformationLocalizedAndFormattedTexts(): void
        {
           
            this.names = this.getLocalizedTextsWithValues(this.names);

            this.descriptions = new Array<LocalizedText>(new LocalizedText("fi", this.description));
            this.descriptionAsHtml = Affecto.HtmlContent.escapeAndReplaceNewLines(this.description);
            this.setValidityTexts();
        }

       
        
        public generateVisitingAddressLocalizedTexts(): void
        {
            this.visitingAddress = new StreetAddress(new Array<LocalizedText>(new LocalizedText("fi", this.visitingStreetAddress)), this.visitingAddressPostalCode,
                new Array<LocalizedText>(new LocalizedText("fi", this.visitingAddressPostalDistrict)));
            this.visitingAddressQualifiers = new Array<LocalizedText>(new LocalizedText("fi", this.visitingAddressQualifier));
        }

        public generatePostalAddressLocalizedTexts(): void
        {
            this.postalStreetAddress = new StreetAddress(new Array<LocalizedText>(new LocalizedText("fi", this.postalStreetAddressStreet)),
                this.postalStreetAddressPostalCode, new Array<LocalizedText>(new LocalizedText("fi", this.postalStreetAddressPostalDistrict)));
            this.postalPostOfficeBoxAddress = new PostOfficeBoxAddress(this.postalPostOfficeBoxAddressPostOfficeBox, this.postalPostOfficeBoxAddressPostalCode,
                new Array<LocalizedText>(new LocalizedText("fi", this.postalPostOfficeBoxAddressPostalDistrict)));
        }

        public hasMunicipalityCode(): boolean
        {
            return this.municipalityCode != null;
        }

        public hasBusinessId(): boolean
        {
            return this.businessId != null && this.businessId !== "";
        }

        public hasPhoneNumber(): boolean
        {
            return this.phoneNumber != null && this.phoneNumber !== "";
        }

        public hasPhoneCallFee(): boolean
        {
            return this.phoneCallFee != null && this.phoneCallFee !== "";
        }

        public hasEmailAddress(): boolean
        {
            return this.emailAddress != null && this.emailAddress !== "";
        }

        public hasWebPage(): boolean
        {
            return this.hasWebPageUrl() && this.webPageName != null && this.webPageName !== "" && this.webPageType != null && this.webPageType !== "";
        }

        public hasWebPageUrl(): boolean
        {
            return this.webPageUrl != null && this.webPageUrl !== "";
        }

        public hasEditedWebPage(): boolean
        {
            return this.hasEditedWebPageUrl() && this.editedWebPageName != null && this.editedWebPageName !== "" && this.editedWebPageType != null && this.editedWebPageType !== "";
        }

        public hasEditedWebPageUrl(): boolean
        {
            return this.editedWebPageUrl != null && this.editedWebPageUrl !== "";
        }

        public hasContactInformation(): boolean
        {
            return this.hasPhoneNumber() || this.hasEmailAddress() || this.webPages.length > 0 || this.hasPhoneCallFee();
        }

        public hasVisitingAddressPostalCode(): boolean
        {
            return this.visitingAddressPostalCode != null && this.visitingAddressPostalCode !== "";
        }

        public hasPostalStreetAddressPostalCode(): boolean
        {
            return this.postalStreetAddressPostalCode != null && this.postalStreetAddressPostalCode !== "";
        }

        public hasPostalPostOfficeBoxAddressPostalCode(): boolean
        {
            return this.postalPostOfficeBoxAddressPostalCode != null && this.postalPostOfficeBoxAddressPostalCode !== "";
        }

        public hasVisitingAddressParts(): boolean
        {
            return (this.visitingStreetAddress != null && this.visitingStreetAddress !== "") || (this.visitingAddressPostalCode != null && this.visitingAddressPostalCode !== "")
                || (this.visitingAddressPostalDistrict != null && this.visitingAddressPostalDistrict !== "");
        }

        public addWebPage(name: string, url: string, pageType: string): void
        {
            if (name != null && name !== "" && url != null && url !== "" && pageType != null && pageType !== "" && !this.containsWebPageUrl(url))
            {
                this.webPages.push(new WebPage(name, url, pageType));
            }
        }

        public removeWebPage(url: string): void
        {
            for (var i = 0; i < this.webPages.length; i++)
            {
                if (this.webPages[i].address === url)
                {
                    this.webPages.splice(i, 1);
                    break;
                }
            }
        }

        public isAdded(): boolean
        {
            return this.id != null;
        }

        public clearPostalPostOfficeBoxAddress(): void
        {
            this.postalPostOfficeBoxAddressPostOfficeBox = null;
            this.postalPostOfficeBoxAddressPostalCode = null;
            this.postalPostOfficeBoxAddressPostalDistrict = null;
        }

        public clearPostalStreetAddress(): void
        {
            this.postalStreetAddressStreet = null;
            this.postalStreetAddressPostalCode = null;
            this.postalStreetAddressPostalDistrict = null;
        }

        private containsWebPageUrl(url: string): boolean
        {
            return this.webPages.length > 0 && !this.webPages.every((item: WebPage) =>
            {
                return item.address !== url;
            });
        }

        public addPostalAddressType(addressType: string): void
        {
            this.postalAddressTypes.add(addressType);
            if (PostalAddressType[PostalAddressType.SameAsVisitingAddress] === addressType)
            {
                this.useVisitingAddressAsPostalAddress = true;
            }
        }

        public removePostOfficeBoxPostalAddress(): void
        {
            this.clearPostalPostOfficeBoxAddress();
            this.postalAddressTypes.remove(PostalAddressType.PostOfficeBoxAddress);
            this.postalAddressTypes.available.setAvailable(PostalAddressType.PostOfficeBoxAddress);
        }

        public removeSeparateStreetPostalAddress(): void
        {
            this.clearPostalStreetAddress();
            this.postalAddressTypes.remove(PostalAddressType.SeparateStreetAddress);
            this.setAvailablePostalStreetAddressTypes();
        }

        public removeSameAsVisitingAddressPostalAddress(): void
        {
            this.useVisitingAddressAsPostalAddress = false;
            this.postalAddressTypes.remove(PostalAddressType.SameAsVisitingAddress);
            this.setAvailablePostalStreetAddressTypes();
        }

        public setAvailablePostalStreetAddressTypes(): void
        {
            this.postalAddressTypes.setSameAsVisitingAddressPostalAddressAvailability(this.hasVisitingAddressParts());
            this.postalAddressTypes.setSeparateStreetAddressPostalAddressAvailability();
        }

        public canAddPostalAddress(): boolean
        {
            return this.postalAddressTypes.canAddPostalAddress();
        }

        public isValidValidity(): boolean
        {
            return this.validFromDate == null || this.validToDate == null || this.validFromDate <= this.validToDate;
        }
    }
}