using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;

namespace CxAPI_Core
{
    class Configuration
    {
        public static IConfigurationRoot _configuration;
        public static string[] _keys;


        public static IConfigurationRoot configuration(string[] args)
        {
            Console.WriteLine(@"Using default configuration in path {0}", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .AddCommandLine(args);
            _keys = args;

            _configuration = builder.Build();


            return _configuration;
        }
        public static IConfigurationRoot configuration(string[] args, string path)
        {
            string _os = RuntimeInformation.OSDescription;
            string _ospath = _os.Contains("Windows") ? "\\" : "/";
            string set_path = String.IsNullOrEmpty(path) ? System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) : System.IO.Path.GetDirectoryName(path);
            string set_file = String.IsNullOrEmpty(path) ? "appsettings.json" : System.IO.Path.GetFileName(path);
            Console.WriteLine(@"Using configuration in path {0}{1}{2}", set_path, _ospath, set_file);
            IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(set_path)
            .AddJsonFile(set_file, optional: true, reloadOnChange: true)
            .AddCommandLine(args);
            _keys = args;

            _configuration = builder.Build();


            return _configuration;
        }
        public static IConfigurationRoot configuration()
        {
            if (_configuration != null) return _configuration;

            Console.WriteLine(@"Using executable path configuration {0}", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();
            return _configuration;
        }

        public static string getVersion()
        {
            Assembly assembly = Assembly.Load("CxApi_Core");
            Version ver = assembly.GetName().Version;
            return "Version: " + ver.Major.ToString() + "." + ver.Minor.ToString() + "." + ver.Revision.ToString() + " build (" + ver.Build.ToString() + ")";
        }

        public static settingClass get_settings()
        {
            settingClass _settings = new settingClass();
            _configuration.GetSection("CxRest").Bind(_settings);
            return _settings;
        }

        public static HttpClient _HttpClient(resultClass token)
        {
            if (token.use_proxy)
            {
                Uri uri = new Uri(token.proxy_url);
                Console.WriteLine(@"Proxy found {0}://{1}:{2}", uri.Scheme, uri.Host, uri.Port);
                HttpClientHandler handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy(uri),
                    UseProxy = true
                };
                if (!String.IsNullOrEmpty(token.proxy_username))
                {
                    handler.Credentials = new NetworkCredential(token.proxy_username, token.proxy_password, token.proxy_domain);
                    if (token.debug)
                    {
                        Console.WriteLine(@"Using proxy {0} with credentials {1}\\{2}", token.proxy_url, token.proxy_domain, token.proxy_username);
                    }
                }
                else if (token.proxy_use_default)
                {
                    string[] domain = token.user_name.Replace(@"\\", "").Split('\\');
                    if (domain.Length == 1)
                    {
                        handler.Credentials = new NetworkCredential(domain[0], token.credential);
                        if (token.debug)
                        {
                            Console.WriteLine(@"Using proxy {0} with credentials {1}", token.proxy_url, domain[0]);
                        }
                    }
                    else
                    {
                        handler.Credentials = new NetworkCredential(domain[1], token.credential, domain[0]);
                        if (token.debug)
                        {
                            Console.WriteLine(@"Using proxy {0} with credentials {1}\\{2}", token.proxy_url, domain[0], domain[1]);
                        }
                    }
                }
                HttpClient httpclient = new HttpClient(handler, true);
                Console.WriteLine(@"Proxy configuration {0}", handler.Proxy.GetProxy(new Uri(token.CxUrl)));
                return httpclient;
            }
            else
            {
                return new HttpClient();
            }
        }
        public static resultClass mono_command_args(string[] args)
        {
            resultClass token = new resultClass();
            List<string> extra;

            var p = new OptionSet() {
                { "a|appsettings=", "Optional, point to alternate appsettings directory",
                  v => token.appsettings = v },
                { "t|get_token", "Fetch the bearer token from the CxSAST service",
                  v => token.api_action = api_action.getToken },
                { "c|store_credentials", "Store username and credential in an encrypted file",
                  v => token.api_action = api_action.storeCredentials },
                { "s|scan_results", "Get scan results, filtered by time and project",
                  v => token.api_action = api_action.scanResults },
                { "rn|report_name=", "Select desired report",
                  v => token.report_name = v },
                { "pn|project_name=", "Filter with project name, Will return project if any portion of the project name is a match",
                  v => token.project_name = v },
                { "tn|team_name=", "Filter with team name, Will return a team if any portion of the team name is a match",
                  v => token.team_name = v },
                { "ps|preset_name=", "Filter with preset name, Will return a preset if any portion of the team name is a match",
                  v => token.preset = v },
                { "pi|pipe", "Do not write to file but pipe output to stdout. Useful when using other API's",
                  v => token.pipe = true },
                { "path|file_path=", "Override file path in configuration",
                  v => token.file_path = v },
                { "file|file_name=", "Override file name in configuration",
                  v => token.file_name = v },
                { "sr|save_result=", "Enable saving of results as XML",
                  v => token.save_result = v },
                { "srp|save_result_path=", "Override save result path in configuration",
                  v => token.save_result_path = v },
                { "u|user_name=", "The username to use to retreive the token (REST) or session (SOAP)",
                  v => token.user_name = v },
                { "p|password=", "The password needed to retreive the token (REST) or session (SOAP)",
                  v => token.credential = v },
                { "st|start_time=", "Last scan start time",
                  v => token.start_time = DateTime.Parse(v) },
                { "et|end_time=", "Last scan end time",
                  v => token.end_time = DateTime.Parse(v) },
                //add proxy stuff
                { "up|use_proxy", "Use web proxy",
                  v => token.use_proxy = true },
                { "ud|proxy_use_default", "Use default credentials",
                  v => token.proxy_use_default = true },
                { "pu|proxy_user_name=", "Proxy User Name",
                  v => token.proxy_username = v },
                { "pp|proxy_password=", "Proxy Password",
                  v => token.proxy_password = v },
                { "pd|proxy_domain=", "Proxy Domain",
                  v => token.proxy_domain = v},
                //proxy
                { "v|verbose=", "Change degrees of debugging info",
                  v => token.verbosity = Convert.ToInt32(v) },
                { "mt|max_threads=", "Change the max number of report requests to CxManager",
                  v => token.max_threads = Convert.ToInt32(v) },
                { "ms|max_scans=", "Change the max number of report requests to CxManager",
                  v => token.max_scans = Convert.ToInt32(v) },
                { "d|debug", "Output debugging info ",
                  v => token.debug = true },
                { "T|test", "Wait at end of program ",
                  v => token.test = true },
                { "?|h|help",  "show your options",
                  v => token.api_action = api_action.help},
            };
            try
            {
                extra = p.Parse(args);
                Configuration.configuration(args, token.appsettings);
                token._setresultClass();
                settingClass _settings = get_settings();
                if (_settings.use_proxy || token.use_proxy)
                {
                    token.use_proxy = true;
                    token.proxy_use_default = token.proxy_use_default ? true : _settings.proxy_use_default;
                    token.proxy_url = String.IsNullOrEmpty(token.proxy_url) ? _settings.proxy_url : token.proxy_url;
                    Console.WriteLine("Using proxy {0}", token.proxy_url);
                }
                token.save_result_filename = String.IsNullOrEmpty(token.save_result_filename) ? _settings.CxResultFileName : token.save_result_filename;
                token.save_result_path = String.IsNullOrEmpty(token.save_result_path) ? _settings.CxResultFilePath : token.save_result_path;
                _settings.CxFilePath = string.IsNullOrEmpty(_settings.CxDefaultFilePath) ? _settings.CxFilePath : _settings.CxDefaultFilePath;
                token.file_name = String.IsNullOrEmpty(token.file_name) ? _settings.CxDefaultFileName : token.file_name;
                token.file_path = String.IsNullOrEmpty(token.file_path) ? _settings.CxFilePath : token.file_path;
            }

            // if (String.IsNullOrEmpty(token.file_name) ? _configuration
            catch (OptionException e)
            {
                Console.Write("CxApi_Core: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try CxApi --help' for more information.");
                token.status = -1;
            }

            if (token.api_action == api_action.help)
            {
                ShowHelp(p);
            }
            return token;
        }


        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: CxApi action arguments");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }


}


