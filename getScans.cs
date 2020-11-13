using System;
using System.Collections.Generic;
using System.Text;
using CxAPI_Core.dto;
using Newtonsoft.Json;

namespace CxAPI_Core
{
    class getScans
    {
        public List<ScanObject> getScan(resultClass token)
        {
            List<ScanObject> sclass = new List<ScanObject>();
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(CxConstant.CxScans);
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path, 10);
                if (token.status == 0)
                {
                    sclass = JsonConvert.DeserializeObject<List<ScanObject>>(token.op_result);
                }
                else
                {
                    throw new MissingFieldException("Failure to get scan results. Please check token validity and try again");
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getScan: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return sclass;
        }

        public List<ScanObject> getScanbyId(resultClass token, string projectId)
        {
            List<ScanObject> sclass = new List<ScanObject>();
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxProjectScan, projectId));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path, 10);
                if (token.status == 0)
                {
                    sclass = JsonConvert.DeserializeObject<List<ScanObject>>(token.op_result);
                }
                else
                {
                    throw new MissingFieldException("Failure to get scan results. Please check token validity and try again");
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getScan: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return sclass;
        }
        public List<ScanObject> getLastScanbyId(resultClass token, string projectId)
        {
            List<ScanObject> sclass = new List<ScanObject>();
            if (token.max_scans > 0)
            {
                sclass = getLastScanbyId(token, projectId, token.max_scans);
                return sclass;
            }
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxLastProjectScan, projectId));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path, 10);
                if (token.status == 0)
                {
                    sclass = JsonConvert.DeserializeObject<List<ScanObject>>(token.op_result);
                }
                else
                {
                    throw new MissingFieldException("Failure to get scan results. Please check token validity and try again");
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getScan: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return sclass;
        }
        public List<ScanObject> getLastScanbyId(resultClass token, string projectId, int lastcnt)
        {
            List<ScanObject> sclass = new List<ScanObject>();
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxLastNProjectScan, projectId, lastcnt));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path, 10);
                if (token.status == 0)
                {
                    sclass = JsonConvert.DeserializeObject<List<ScanObject>>(token.op_result);
                }
                else
                {
                    throw new MissingFieldException("Failure to get scan results. Please check token validity and try again");
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getScan: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return sclass;
        }

        public List<ScanObject> getLastScan(resultClass token)
        {
            List<ScanObject> sclass = new List<ScanObject>();
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxLastScan));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path, 10);
                if (token.status == 0)
                {
                    sclass = JsonConvert.DeserializeObject<List<ScanObject>>(token.op_result);
                }
                else
                {
                    throw new MissingFieldException("Failure to get scan results. Please check token validity and try again");
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getScan: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return sclass;
        }


        public ScanSettings getScanSettings(resultClass token, string projectId)
        {
            ScanSettings sclass = new ScanSettings();
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxScanSettings, projectId));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path, 10);
                
                if (token.status == 0)
                {
                    sclass = JsonConvert.DeserializeObject<ScanSettings>(token.op_result);
                }
                else
                {
                    throw new MissingFieldException("Failure to get scan settings. Please check token validity and try again");
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getScan: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return sclass;
        }


        public ScanStatistics getScansStatistics(long scanId, resultClass token)
        {
            ScanStatistics scanStatistics = new ScanStatistics();
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxScanStatistics, scanId));
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }


                httpGet.get_Http(token, path);
                if (token.status == 0)
                {
                    scanStatistics = JsonConvert.DeserializeObject<ScanStatistics>(token.op_result);
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getScansStatistics: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return scanStatistics;
        }

        public List<Teams> getTeams(resultClass token)
        {
            List<Teams> tclass = new List<Teams>();
            string path = String.Empty;

            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(CxConstant.CxTeams);
                if (token.debug && token.verbosity > 1) { Console.WriteLine("API: {0}", path); }
                httpGet.get_Http(token, path);
                if (token.status == 0)
                {
                    tclass = JsonConvert.DeserializeObject<List<Teams>>(token.op_result);
                }
                else
                {
                    throw new MissingFieldException("Failure to get teams. Please check token validity and try again");
                }
            }
            catch (Exception ex)
            {
                if (token.debug && token.verbosity > 0)
                {
                    Console.Error.WriteLine("getTeams: {0}, Message: {1} Trace: {2}", path, ex.Message, ex.StackTrace);
                }
            }
            return tclass;
        }

        public string getFullName(List<Teams> teams, string id)
        {
            string result = String.Empty;
            foreach (Teams team in teams)
            {
                if (id == team.id)
                {
                    result = team.fullName;
                    break;
                }
            }
            return result;
        }

    }
}
