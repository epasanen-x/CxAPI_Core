﻿using System;
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
            if (token.debug && token.verbosity > 1) { Console.WriteLine("Running: {0}", token.report_name); }

            Dictionary<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>> fix = new Dictionary<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>>();
            List<ReportTrace> trace = new List<ReportTrace>();
            Dictionary<string, ReportResultExtended> resultAll = new Dictionary<string, ReportResultExtended>();
            List<ReportResultExtended> report_output = new List<ReportResultExtended>();
            //            Dictionary<long, ReportStaging> start = new Dictionary<long, ReportStaging>();
            //            Dictionary<long, ReportStaging> end = new Dictionary<long, ReportStaging>();
            Dictionary<long, ScanCount> scanCount = new Dictionary<long, ScanCount>();
            getScanResults scanResults = new getScanResults();
            getScans scans = new getScans();
            getProjects projects = new getProjects(token);
            //List<ScanObject> scan = scans.getScan(token);
            Dictionary<string, Teams> teams = projects.CxTeams;
            List<ScanObject> scan = projects.filter_by_projects(token);
            Dictionary<long, ScanStatistics> resultStatistics = projects.CxResultStatistics;

            if (scan.Count == 0)
            {
                Console.Error.WriteLine("No scans were found, pleas check argumants and retry.");
                return false;
            }
            foreach (ScanObject s in scan)
            {
                setCount(s.Project.Id, scanCount);

                ReportResult result = scanResults.SetResultRequest(s.Id, "XML", token);
                if (result != null)
                {
                    trace.Add(new ReportTrace(s.Project.Id, s.Project.Name, teams[s.OwningTeamId].fullName, s.DateAndTime.StartedOn, s.Id, result.ReportId, "XML"));
                    if (trace.Count % token.max_threads == 0)
                    {
                        fetchReports(trace, scanResults, fix, resultAll, report_output);
                        trace.Clear();
                    }
                }

            }

            fetchReports(trace, scanResults, fix, resultAll, report_output);
            trace.Clear();

            addFixed(fix, report_output);
            if (token.debug) { Console.WriteLine("Processing data, number of rows: {0}", report_output.Count); }
            if (token.pipe)
            {
                foreach (ReportResultExtended csv in report_output)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", csv.projectName, csv.teamName, csv.presetName, csv.similarityId, csv.resultId, csv.reportId, csv.Severity, csv.status, csv.state, csv.Query, csv.Group, csv.scanDate);
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

        private bool process_CxResponse(long report_id, XElement result, Dictionary<string, ReportResultExtended> response, Dictionary<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>> fix, List<ReportResultExtended> report_output)
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
                    XElement pathNode = path.Descendants("PathNode").FirstOrDefault();
                    XElement snippet = pathNode.Descendants("Snippet").FirstOrDefault();
                    XElement line = (snippet != null) ? snippet.Descendants("Line").FirstOrDefault() : null;

                    //long ResultId = Convert.ToInt64(path.Attribute("ResultId").Value.ToString());
                    //string key = "New-" + ResultId.ToString();
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
                            resultId = Convert.ToInt64(path.Attribute("ResultId").Value.ToString()),
                            reportId = report_id,
                            nodeId = Convert.ToInt64(el.Attribute("NodeId").Value.ToString()),
                            scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                            status = el.Attribute("Status").Value.ToString(),
                            Severity = el.Attribute("Severity").Value.ToString(),
                            similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                            pathId = Convert.ToInt64(path.Attribute("PathId").Value.ToString()),
                            state = Convert.ToInt32(el.Attribute("state").Value.ToString()),
                            fileName = el.Attribute("FileName").Value.ToString(),
                            lineNo = Convert.ToInt32(el.Attribute("Line").Value.ToString()),

                            column = Convert.ToInt32(el.Attribute("Column").Value.ToString()),
                            firstLine = (line != null) ? line.Descendants("Code").FirstOrDefault().Value.ToString() : "",
                            queryId = Convert.ToInt64(query.Attribute("id").Value.ToString())
                        };
                        response.Add(key, isnew);
                        report_output.Add(isnew);
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
                    XElement pathNode = path.Descendants("PathNode").FirstOrDefault();
                    XElement snippet = pathNode.Descendants("Snippet").FirstOrDefault();
                    XElement line = (snippet != null) ? snippet.Descendants("Line").FirstOrDefault() : null;

                    //long ResultId = Convert.ToInt64(path.Attribute("ResultId").Value.ToString());
                    //string key = "Recurring-" + ResultId.ToString();
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
                            resultId = Convert.ToInt64(path.Attribute("ResultId").Value.ToString()),
                            reportId = report_id,
                            nodeId = Convert.ToInt64(el.Attribute("NodeId").Value.ToString()),
                            status = el.Attribute("Status").Value.ToString(),
                            Severity = el.Attribute("Severity").Value.ToString(),
                            similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                            pathId = Convert.ToInt64(path.Attribute("PathId").Value.ToString()),
                            state = Convert.ToInt32(el.Attribute("state").Value.ToString()),
                            fileName = el.Attribute("FileName").Value.ToString(),
                            lineNo = Convert.ToInt32(el.Attribute("Line").Value.ToString()),
                            column = Convert.ToInt32(el.Attribute("Column").Value.ToString()),
                            firstLine = (line != null) ? line.Descendants("Code").FirstOrDefault().Value.ToString() : "",
                            queryId = Convert.ToInt64(query.Attribute("id").Value.ToString())

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
                                nodeId = Convert.ToInt64(el.Attribute("NodeId").Value.ToString()),
                                Severity = el.Attribute("Severity").Value.ToString(),
                                resultId = Convert.ToInt64(path.Attribute("ResultId").Value.ToString()),
                                reportId = report_id,
                                similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                                pathId = Convert.ToInt64(path.Attribute("PathId").Value.ToString()),
                                state = Convert.ToInt32(el.Attribute("state").Value.ToString()),
                                fileName = el.Attribute("FileName").Value.ToString(),
                                lineNo = Convert.ToInt32(el.Attribute("Line").Value.ToString()),
                                column = Convert.ToInt32(el.Attribute("Column").Value.ToString()),
                                firstLine = (line != null) ? line.Descendants("Code").FirstOrDefault().Value.ToString() : "",
                                queryId = Convert.ToInt64(query.Attribute("id").Value.ToString())

                            };
                            response[key] = isrecurring;
                            report_output.Add(isrecurring);
                        }
                    }
                }
                IEnumerable<XElement> fixedVulerability = from el in result.Descendants("Query").Descendants("Result")
                                                          select el;
                foreach (XElement el in fixedVulerability)
                {
                    XElement query = el.Parent;
                    XElement root = query.Parent;
                    XElement path = el.Descendants("Path").FirstOrDefault();
                    XElement pathNode = path.Descendants("PathNode").FirstOrDefault();
                    XElement snippet = pathNode.Descendants("Snippet").FirstOrDefault();
                    XElement line = (snippet != null) ? snippet.Descendants("Line").FirstOrDefault() : null;
                    long SimilarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString());
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
                        resultId = Convert.ToInt64(path.Attribute("ResultId").Value.ToString()),
                        reportId = report_id,
                        nodeId = Convert.ToInt64(el.Attribute("NodeId").Value.ToString()),
                        similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                        pathId = Convert.ToInt64(path.Attribute("PathId").Value.ToString()),
                        state = Convert.ToInt32(el.Attribute("state").Value.ToString()),
                        fileName = el.Attribute("FileName").Value.ToString(),
                        lineNo = Convert.ToInt32(el.Attribute("Line").Value.ToString()),
                        column = Convert.ToInt32(el.Attribute("Column").Value.ToString()),
                        firstLine = (line != null) ? line.Descendants("Code").FirstOrDefault().Value.ToString() : "",
                        queryId = Convert.ToInt64(query.Attribute("id").Value.ToString())
                    };
                    string mix = String.Format("{0}-{1}-{2}-{3}-{4}", isfixed.projectId, isfixed.queryId, isfixed.lineNo, isfixed.column, isfixed.similarityId);
                    if (!fix.ContainsKey(isfixed.projectId))
                    {
                        fix.Add(isfixed.projectId, new Dictionary<DateTime, Dictionary<string, ReportResultExtended>>());
                        fix[isfixed.projectId].Add(isfixed.scanDate, new Dictionary<string, ReportResultExtended>());
                        fix[isfixed.projectId][isfixed.scanDate].Add(mix, isfixed);
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Unique keys: {0}, {1}, {2} {3} {4} {5}", isfixed.projectName, isfixed.similarityId, isfixed.projectId, isfixed.scanId, isfixed.queryId, isfixed.scanDate); }
                    }
                    else
                    {
                        if (!fix[isfixed.projectId].ContainsKey(isfixed.scanDate))
                        {
                            fix[isfixed.projectId].Add(isfixed.scanDate, new Dictionary<string, ReportResultExtended>());
                        }
                        if (!fix[isfixed.projectId][isfixed.scanDate].TryAdd(mix, isfixed))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Duplicate keys: {0}, {1}, {2} {3} {4} {5}", isfixed.projectName, isfixed.similarityId, isfixed.nodeId, isfixed.scanId, isfixed.queryId, isfixed.scanDate); }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failure reading XML from scan: report ID: {0}", report_id);
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                if (token.debug && token.verbosity > 1)
                {
                    Console.Error.WriteLine("Dumping XML:");
                    Console.Error.Write(result.ToString());
                }
                return true;
            }

        }
        private bool addFixed(Dictionary<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>> fix, List<ReportResultExtended> report_output)
        {
            foreach (KeyValuePair<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>> projects in fix)
            {
                Dictionary<DateTime, Dictionary<string, ReportResultExtended>> scanDate = projects.Value;
                var scan_date = from entry in scanDate orderby entry.Key ascending select entry;
                KeyValuePair<DateTime, Dictionary<string, ReportResultExtended>> keyValuePair = new KeyValuePair<DateTime, Dictionary<string, ReportResultExtended>>();

                foreach (KeyValuePair<DateTime, Dictionary<string, ReportResultExtended>> kv_dt in scan_date)
                {
                    if (keyValuePair.Key != DateTime.MinValue)
                    {
                        Dictionary<string, ReportResultExtended> last_scan = keyValuePair.Value;
                        Dictionary<string, ReportResultExtended> current_scan = kv_dt.Value;
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Compare: {0} {1}", keyValuePair.Key, kv_dt.Key); }
                        foreach (string key in last_scan.Keys)
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Project {0}, key {1}", last_scan[key].projectName, key); }
                            if (!current_scan.ContainsKey(key))
                            {
                                ReportResultExtended reportResult = last_scan[key];
                                reportResult.status = "Fixed";
                                report_output.Add(reportResult);
                            }
                        }
                    }
                    keyValuePair = kv_dt;
                }
            }
            return true;
        }

        private bool fetchReports(List<ReportTrace> trace, getScanResults scanResults, Dictionary<long, Dictionary<DateTime, Dictionary<string, ReportResultExtended>>> fix, Dictionary<string, ReportResultExtended> resultAll, List<ReportResultExtended> report_output)
        {
            bool waitFlag = false;
            //ConsoleSpinner spinner = new ConsoleSpinner();
            DateTime wait_expired = DateTime.UtcNow;


            while (!waitFlag)
            {
                //spinner.Turn();
                if (wait_expired.AddMinutes(2) < DateTime.UtcNow)
                {
                    Console.Error.WriteLine("waitForResult timeout! {0}", getTimeOutObjects(trace));
                    break;
                }
                waitFlag = true;
                if (token.debug && token.verbosity > 0) { Console.WriteLine("Sleeping 3 second(s)"); }
                Thread.Sleep(3000);
                foreach (ReportTrace rt in trace)
                {
                    if (token.debug && token.verbosity > 0) { Console.WriteLine("Looping thru {0}, isRead {1}", rt.reportId, rt.isRead); }
                    if (!rt.isRead)
                    {
                        waitFlag = false;
                        if (rt.TimeStamp.AddMinutes(1) < DateTime.UtcNow)
                        {
                            Console.Error.WriteLine("ReportId/ScanId {0}/{1} timeout!", rt.reportId, rt.scanId);
                            rt.isRead = true;
                            continue;
                        }
                        if (token.debug && token.verbosity > 0) { Console.WriteLine("Checking for report.Id {0}", rt.reportId); }
                        if (scanResults.GetResultStatus(rt.reportId, token))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Found report.Id {0}", rt.reportId); }
                            Thread.Sleep(2000);
                            var result = scanResults.GetResult(rt.reportId, token);
                            if (result != null)
                            {
                                if (process_CxResponse(rt.reportId, result, resultAll, fix, report_output))
                                {
                                    rt.isRead = true;
                                }
                                else
                                {
                                    rt.isRead = true;
                                    if (token.debug && token.verbosity > 1)
                                    {
                                        Console.Error.WriteLine("Dumping XML:");
                                        Console.Error.Write(result.ToString());
                                    }
                                    Console.Error.WriteLine("Failed processing reportId {0}", rt.reportId);
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine("Failed retrieving reportId {0}", rt.reportId);
                                rt.isRead = true;
                            }
                        }
                        else
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Waiting for reportId {0}", rt.reportId); }
                        }
                    }
                }
            }
            return true;
        }
        private string getTimeOutObjects(List<ReportTrace> trace)
        {
            string result = String.Empty;
            foreach (ReportTrace rt in trace)
            {
                result += String.Format("ProjectName {0}, ScanId {1}, TimeStamp {2}, isRead {3}", rt.projectName, rt.scanId, rt.TimeStamp, rt.isRead) + Environment.NewLine;
            }
            return result;
        }

        public bool matchProjectandTeam(ScanObject s, List<Teams> teams)
        {
            bool result = false;
            getScans scans = new getScans();

            string fullName = scans.getFullName(teams, s.OwningTeamId);

            if ((String.IsNullOrEmpty(token.project_name) || ((!String.IsNullOrEmpty(token.project_name)) && (s.Project.Name.Contains(token.project_name)))))
            {
                if ((String.IsNullOrEmpty(token.team_name) || ((!String.IsNullOrEmpty(token.team_name)) && (!String.IsNullOrEmpty(fullName)) && (fullName.Contains(token.team_name)))))
                {
                    result = true;
                }
            }
            return result;
        }
        public void Dispose()
        {

        }

    }
}