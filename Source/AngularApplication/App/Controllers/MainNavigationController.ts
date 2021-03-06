﻿"use strict";

module OrganizationRegister
{
    export class MainNavigationController extends Affecto.Login.LoginDrivenController
    {
        public static $inject = ["$scope", "$location", "busyIndicationService", "userService", "organizationService", "authenticationService", "useExternalLogin"];

        public currentSection: CurrentNavigationSection;
        public organizations: Array<OrganizationName>;
        public selectedOrganizationId: string;
        
        constructor($scope: Affecto.Base.IViewScope, private $location: angular.ILocationService, private busyIndicationService: Affecto.BusyIndication.IBusyIndicationService,
            public userService: UserService, private organizationService: OrganizationService, authenticationService: Affecto.Login.IAuthenticationService,
            private useExternalLogin: boolean)
        {
            super($scope, authenticationService);
            $scope.controller = this;
            this.setCurrentSection();
        }

        public logOut()
        {
            this.organizationService.setSearchTerm("");
            this.authenticationService.logOut();
            if (this.useExternalLogin)
            {
                this.$location.path(Route.logout);
            }
            else
            {
                this.$location.path(Route.login);
            }
        }

        public areUsersActive(): boolean
        {
            return this.currentSection === CurrentNavigationSection.Users;
        }

        public areOrganizationsActive(): boolean
        {
            return this.currentSection === CurrentNavigationSection.Organizations;
        }

        public changeToOrganizations(): void
        {
            this.currentSection = CurrentNavigationSection.Organizations;
        }

        public changeToUsers(): void
        {
            this.currentSection = CurrentNavigationSection.Users;
        }

        public canCurrentUserModifyUsers(): boolean
        {
            return this.user != null && this.user.hasPermission(Permission.maintenanceOfUsers);
        }

        public canCurrentUserModifyOrganizations(): boolean
        {
            var user: AuthenticatedUser = <AuthenticatedUser>(this.user);
            return this.user != null && (user.hasPermission(Permission.maintenanceOfOrganizationData));
        }

        protected onUserLoggedIn(): void
        {
            this.currentSection = CurrentNavigationSection.Organizations;
        }

        private isInUserPage(): boolean
        {
            return this.$location.path().indexOf("User") !== -1;
        }

        private setCurrentSection(): void
        {
            if (this.isInUserPage())
            {
                this.currentSection = CurrentNavigationSection.Users;
            }
            else
            {
                this.currentSection = CurrentNavigationSection.Organizations;
            }            
        }
    }
}