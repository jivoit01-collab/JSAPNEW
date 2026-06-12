using JSAPNEW.Models;

namespace JSAPNEW.Authorization
{
    public static class ResourceAuthorizationHelper
    {
        public static async Task<bool> VerifyCompanyOwnsResource(
            int userId,
            int companyId,
            Func<int, Task<IEnumerable<CompanyModel>>> getUserCompanies)
        {
            if (userId <= 0 || companyId <= 0)
            {
                return false;
            }

            var companies = await getUserCompanies(userId);
            return companies != null && companies.Any(company => company.id == companyId);
        }

        public static async Task<bool> VerifyUserOwnsResource<TResource>(
            int resourceId,
            Func<Task<IEnumerable<TResource>>> getAuthorizedResources,
            Func<TResource, int> resourceIdSelector)
        {
            if (resourceId <= 0)
            {
                return false;
            }

            var resources = await getAuthorizedResources();
            return resources != null && resources.Any(resource => resourceIdSelector(resource) == resourceId);
        }
    }
}
