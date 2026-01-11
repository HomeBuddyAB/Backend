using HomeBuddy_API.Models;
using System.Threading;
using System.Threading.Tasks;

namespace HomeBuddy_API.Interfaces.SlugURLInterfaces
{ 
	public interface IUrlBuilderService
	{
		string GroupUrl(string slugOrObjectId, string? sku = null);
		string VariantRedirectUrl(string sku);
	}
}