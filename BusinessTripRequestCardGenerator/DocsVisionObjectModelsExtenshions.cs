using DocsVision.BackOffice.ObjectModel.Services;
using DocsVision.BackOffice.ObjectModel;
using DocumentFormat.OpenXml.InkML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocsVision.Platform.ObjectModel;

namespace BusinessTripRequestCardGenerator
{
    internal static class DocsVisionObjectModelsExtenshions
    {
        public static PartnersCompany? GetPartnersOrganization( this StaffEmployee employee, ObjectContext context)
        {
            IPartnersService partnersService = context.GetService<IPartnersService>();
            StaffUnit employeeDepartment = employee.Unit;
            StaffUnit topLevelEmployeeUnit = employeeDepartment;

            while (topLevelEmployeeUnit.ParentUnit != null &&
                   topLevelEmployeeUnit.ParentUnit.Type != StaffUnitType.Organization)
            {
                topLevelEmployeeUnit = topLevelEmployeeUnit.ParentUnit;
            }

            var targetCompany = partnersService.FindSameCompanyOnServer(null, topLevelEmployeeUnit.Name, "");
            //var department = targetCompany.Companies
            //                                 .FirstOrDefault(c => c.Name == employeeDepartment.Name);
            return targetCompany;
        }

        public static StaffEmployee? GetManager(this StaffEmployee employee, ObjectContext context)
        {
            if (employee == null) return null;
            IStaffService staffService = context.GetService<IStaffService>();
            return staffService.GetEmployeeManager(employee);
        }

        public static StaffUnit GetOrganization(this StaffEmployee employee, ObjectContext context)
        {
            StaffUnit organization = employee.Unit;
            while (organization.ParentUnit != null &&
                   organization.ParentUnit.Type != StaffUnitType.Organization)
            {
                organization = organization.ParentUnit;
            }
            return organization;
        }
    }
}
