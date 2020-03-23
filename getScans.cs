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
                httpGet.get_Http(token, path);
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
                    Console.Error.WriteLine("getScan: {0}, Message: {1} Trace: {3}", path, ex.Message, ex.StackTrace);
                }
            }
            return sclass;
        }

        public ScanStatistics getScansStatistics(long scanId,resultClass token)
        {
            ScanStatistics scanStatistics = new ScanStatistics();
            string path = String.Empty;
            try
            {
                get httpGet = new get();
                secure token_secure = new secure(token);
                token_secure.findToken(token);
                path = token_secure.get_rest_Uri(String.Format(CxConstant.CxScanStatistics, scanId));
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
                    Console.Error.WriteLine("getScansStatistics: {0}, Message: {1} Trace: {3}", path, ex.Message, ex.StackTrace);
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
                    Console.Error.WriteLine("getTeams: {0}, Message: {1} Trace: {3}", path, ex.Message, ex.StackTrace);
                }
            }
            return tclass;
        }

        public string getFullName(List<Teams> teams, Guid id)
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
