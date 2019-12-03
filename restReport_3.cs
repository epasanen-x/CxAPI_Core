using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using CxAPI_Core.dto;
using System.Threading;

namespace CxAPI_Core
{
    class restReport_3 : IDisposable
    {
        public resultClass token;

        public restReport_3(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReportsbyDate()
        {
            List<ReportTrace> trace = new List<ReportTrace>();
            Dictionary<string, ReportResultExtended> resultAll = new Dictionary<string, ReportResultExtended>();
            List<ReportResultExtended> report_output = new List<ReportResultExtended>();
            //            Dictionary<long, ReportStaging> start = new Dictionary<long, ReportStaging>();
            //            Dictionary<long, ReportStaging> end = new Dictionary<long, ReportStaging>();
            Dictionary<long, ScanCount> scanCount = new Dictionary<long, ScanCount>();
            ConsoleSpinner spinner = new ConsoleSpinner();
            bool waitFlag = false;
            getScanResults scanResults = new getScanResults();
            getScans scans = new getScans();
            List<ScanObject> scan = scans.getScan(token);
            foreach (ScanObject s in scan)
            {
                if ((s.DateAndTime != null) && (s.Status.Id == 7) && (s.DateAndTime.StartedOn > token.start_time) && (s.DateAndTime.StartedOn < token.end_time))
                {
                    if ((String.IsNullOrEmpty(token.project_name) || ((!String.IsNullOrEmpty(token.project_name)) && (s.Project.Name.Contains(token.project_name)))))
                    {
                        setCount(s.Project.Id, scanCount);

                        ReportResult result = scanResults.SetResultRequest(s.Id, "XML", token);
                        if (result != null)
                        {
                            trace.Add(new ReportTrace(s.Project.Id, s.Project.Name, s.DateAndTime.StartedOn, s.Id, result.ReportId, "XML"));
                        }

                    }
                }
            }
            while (!waitFlag)
            {
                spinner.Turn();
                waitFlag = true;
                if (token.debug && token.verbosity > 0) { Console.WriteLine("Sleeping 1 second(s)"); }
                Thread.Sleep(1000);
                foreach (ReportTrace rt in trace)
                {
                    if (!rt.isRead)
                    {
                        waitFlag = false;
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Testing report.Id {0}", rt.reportId); }
                        if (scanResults.GetResultStatus(rt.reportId, token))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Found report.Id {0}", rt.reportId); }
                            var result = scanResults.GetResult(rt.reportId, token);
                            if (result != null)
                            {
                                if (process_CxResponse(result, resultAll, report_output))
                                {
                                    rt.isRead = true;
                                }
                            }
                        }
                    }
                }
            }

            if (token.pipe)
            {
                foreach (ReportResultExtended csv in report_output)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", csv.projectName, csv.teamName, csv.presetName, csv.similarityId, csv.Severity, csv.status, csv.state, csv.Query, csv.Group, csv.scanDate);
                }
            }
            else
            {
                csvHelper csvHelper = new csvHelper();
                csvHelper.writeCVSFile(report_output, token);
            }
            return true;
        }


        private void setCount(long id, Dictionary<long, ScanCount> scanCount)
        {
            if (scanCount.ContainsKey(id))
            {
                ScanCount sc = scanCount[id];
                sc.count++;
                scanCount[id] = sc;
            }
            else
            {
                ScanCount sc = new ScanCount();
                sc.count = 1;
                scanCount.Add(id, sc);
            }
        }

        private bool process_CxResponse(XElement result, Dictionary<string, ReportResultExtended> response, List<ReportResultExtended> report_output)
        {
            try
            {
                IEnumerable<XElement> newVulerability = from el in result.Descendants("Query").Descendants("Result")
                                                        where (string)el.Attribute("Status").Value == "New"
                                                        select el;

                foreach (XElement el in newVulerability)
                {
                    XElement query = el.Parent;
                    XElement root = query.Parent;
                    XElement path = el.Descendants("Path").FirstOrDefault();
                    long SimilarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString());
                    string key = "New-" + SimilarityId.ToString();
                    ReportResultExtended resultExtended = response.GetValueOrDefault(key);
                    if (resultExtended == null)
                    {

                        ReportResultExtended isnew = new ReportResultExtended()
                        {
                            Query = query.Attribute("name").Value.ToString(),
                            Group = query.Attribute("group").Value.ToString(),
                            projectName = root.Attribute("ProjectName").Value.ToString(),
                            presetName = root.Attribute("Preset").Value.ToString(),
                            teamName = root.Attribute("Team").Value.ToString(),
                            scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                            projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                            scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                            status = el.Attribute("Status").Value.ToString(),
                            Severity = el.Attribute("Severity").Value.ToString(),
                            similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                            state = Convert.ToInt32(el.Attribute("state").Value.ToString())
                        };
                        response.Add(key, isnew);
                        report_output.Add(isnew);
                    }

                }
                IEnumerable<XElement> fixedVulerability = from el in result.Descendants("Query").Descendants("Result")
                                                          where (string)el.Attribute("Status").Value == "Fixed"
                                                          select el;
                foreach (XElement el in fixedVulerability)
                {
                    XElement query = el.Parent;
                    XElement root = query.Parent;
                    XElement path = el.Descendants("Path").FirstOrDefault();
                    long SimilarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString());
                    string key = "Fixed-" + SimilarityId.ToString();
                    ReportResultExtended resultExtended = response.GetValueOrDefault(key);
                    if (resultExtended == null)
                    {

                        ReportResultExtended isfixed = new ReportResultExtended()
                        {
                            Query = query.Attribute("name").Value.ToString(),
                            Group = query.Attribute("group").Value.ToString(),
                            projectName = root.Attribute("ProjectName").Value.ToString(),
                            presetName = root.Attribute("Preset").Value.ToString(),
                            teamName = root.Attribute("Team").Value.ToString(),
                            scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                            projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                            scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                            status = el.Attribute("Status").Value.ToString(),
                            Severity = el.Attribute("Severity").Value.ToString(),
                            similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                            state = Convert.ToInt32(el.Attribute("state").Value.ToString())
                        };
                        response.Add(key, isfixed);
                        report_output.Add(isfixed);
                    }
                }
                IEnumerable<XElement> recurringVulerability = from el in result.Descendants("Query").Descendants("Result")
                                                              where (string)el.Attribute("Status").Value == "Recurrent"
                                                              select el;
                foreach (XElement el in recurringVulerability)
                {
                    XElement query = el.Parent;
                    XElement root = query.Parent;
                    XElement path = el.Descendants("Path").FirstOrDefault();
                    long SimilarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString());
                    string key = "Recurring-" + SimilarityId.ToString();
                    ReportResultExtended resultExtended = response.GetValueOrDefault(key);
                    if (resultExtended == null)
                    {

                        ReportResultExtended isrecurring = new ReportResultExtended()
                        {
                            Query = query.Attribute("name").Value.ToString(),
                            Group = query.Attribute("group").Value.ToString(),
                            projectName = root.Attribute("ProjectName").Value.ToString(),
                            presetName = root.Attribute("Preset").Value.ToString(),
                            teamName = root.Attribute("Team").Value.ToString(),
                            scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                            projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                            scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                            status = el.Attribute("Status").Value.ToString(),
                            Severity = el.Attribute("Severity").Value.ToString(),
                            similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                            state = Convert.ToInt32(el.Attribute("state").Value.ToString())
                        };
                        response.Add(key, isrecurring);
                        report_output.Add(isrecurring);
                    }
                    else
                    {
                        int currentstate = Convert.ToInt32(el.Attribute("state").Value.ToString());
                        ReportResultExtended reportResult = response[key];
                        if (currentstate != reportResult.state)
                        {


                            ReportResultExtended isrecurring = new ReportResultExtended()
                            {
                                Query = query.Attribute("name").Value.ToString(),
                                Group = query.Attribute("group").Value.ToString(),
                                projectName = root.Attribute("ProjectName").Value.ToString(),
                                presetName = root.Attribute("Preset").Value.ToString(),
                                teamName = root.Attribute("Team").Value.ToString(),
                                scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                                projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                                scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                                status = el.Attribute("Status").Value.ToString(),
                                Severity = el.Attribute("Severity").Value.ToString(),
                                similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                                state = Convert.ToInt32(el.Attribute("state").Value.ToString())
                            };
                            response[key] = isrecurring;
                            report_output.Add(isrecurring);
                        }
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

        }

        public void Dispose()
        {

        }

    }

}