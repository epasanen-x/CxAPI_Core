using System;
using System.Collections.Generic;
using System.Text;
using CxAPI_Core;

namespace CxSDKWebService
{
    public partial class CxSDKWebServiceSoapClient : System.ServiceModel.ClientBase<CxSDKWebService.CxSDKWebServiceSoap>, CxSDKWebService.CxSDKWebServiceSoap
    {
        static partial void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Description.ClientCredentials clientCredentials)
        {
            secure secure = new secure();
            settingClass settings = secure.get_settings();

            Console.WriteLine("CxSDKWebService called: {0}", settings.CxSDKWebService);
            Console.WriteLine("CxSDKWebService called: {0}", _options.token.user_name);
            serviceEndpoint.Address =
                new System.ServiceModel.EndpointAddress(new System.Uri(settings.CxSDKWebService),
                new System.ServiceModel.DnsEndpointIdentity(""));

            System.ServiceModel.BasicHttpBinding binding = new System.ServiceModel.BasicHttpBinding();
            binding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.None;
            serviceEndpoint.Binding = binding;

        }
    }

}
