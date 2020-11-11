using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CxAPI_Core
{
    class dispatcher
    {
        public Stopwatch stopWatch;

        public resultClass dispatch(string[] args)
        {
            resultClass token = Configuration.mono_command_args(args);
            fetchToken newtoken = new fetchToken();
            if (token.status != 0) { return token; }
            secure secure = new secure(token);
            _options.debug = token.debug;
            _options.level = token.verbosity;
            _options.test = token.test;
            _options.token = token;

            switch (token.api_action)
            {
                case api_action.getToken:
                    {

                        newtoken.get_token(secure.decrypt_Credentials());
                        break;
                    }
                case api_action.storeCredentials:
                    {
                        storeCredentials cred = new storeCredentials();
                        token = cred.save_credentials(token);
                        break;
                    }
                case api_action.scanResults:
                    {
                        token = newtoken.get_token(secure.decrypt_Credentials());
                        if (token.report_name.Contains("REST_REPORT_1"))
                        {
                            using (restReport_1 restReport = new restReport_1(token))
                            {
                                if (token.report_name == "REST_REPORT_1")
                                {
                                    restReport.fetchReportsbyDate();
                                }
                            }
                        }
                        else if (token.report_name.Contains("REST_REPORT_2"))
                        {
                            using (restReport_2 restReport = new restReport_2(token))
                            {
                                if (token.report_name == "REST_REPORT_2")
                                {
                                    restReport.fetchReportsbyDate();
                                }
                            }
                        }
                        else if (token.report_name.Contains("REST_REPORT_3"))
                        {
                            using (restReport_3 restReport = new restReport_3(token))
                            {
                                if (token.report_name == "REST_REPORT_3")
                                {
                                    restReport.fetchReportsbyDate();
                                }
                            }
                        }
                        else if (token.report_name.Contains("REST_REPORT_4"))
                        {
                            using (restReport_4 restReport = new restReport_4(token))
                            {
                                if (token.report_name == "REST_REPORT_4")
                                {
                                    restReport.fetchReportsbyDate();
                                }
                            }
                        }
                        else if (token.report_name.Contains("REST_REPORT_5"))
                        {
                            using (restReport_5 restReport = new restReport_5(token))
                            {
                                if (token.report_name == "REST_REPORT_5")
                                {
                                    restReport.fetchReportsbyDate();
                                }
                            }
                        }
                        else if (token.report_name.Contains("REST_STORE_1"))
                        {
                            using (restStore_1 restReport = new restStore_1(token))
                            {
                                if (token.report_name == "REST_STORE_1")
                                {
                                    restReport.fetchResultsbyDate();
                                }
                            }
                        }
                        using (CxSoapSDK cxSoapSDK = new CxSoapSDK(token))
                        {
                            if (token.report_name == "REPORT_1")
                            {
                                cxSoapSDK.makeProjectScanCsv_1();
                            }
                            if (token.report_name == "REPORT_2")
                            {
                                cxSoapSDK.makeProjectScanCsv_2();
                            }
                            if (token.report_name == "REPORT_3")
                            {
                                cxSoapSDK.makeProjectScanCsv_3();
                            }
                        }

                        break;
                    }
                default:
                    {
                        Console.WriteLine("Cannot find valid report name or operation {0}-{1}", token.api_action, token.report_name);
                        break;
                    }
            }
            return token;
        }
        public dispatcher()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();
            Console.WriteLine("Start Time: {0}", DateTime.UtcNow.ToString());
        }
        public void Elapsed_Time()
        {
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
            Console.WriteLine("Stop Time: {0}", DateTime.UtcNow.ToString());
            Console.WriteLine("Total elapsed time: {0}", elapsedTime);
        }
    }
}
