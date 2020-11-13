using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using CxAPI_Core.dto;

namespace CxAPI_Core
{
    class restReport_1 : IDisposable
    {
        public resultClass token;

        public restReport_1(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReportsbyDate()
        {
            List<ReportTrace> trace = new List<ReportTrace>();
            List<ReportResultAll> resultNew = new List<ReportResultAll>();
            Dictionary<long, ReportStaging> start = new Dictionary<long, ReportStaging>();
            Dictionary<long, ReportStaging> end = new Dictionary<long, ReportStaging>();
            Dictionary<long, List<ReportResultAll>> last = new Dictionary<long, List<ReportResultAll>>();
            Dictionary<long, ScanCount> scanCount = new Dictionary<long, ScanCount>();
            getScanResults scanResults = new getScanResults();
            getScans scans = new getScans();
            List<Teams> teams = scans.getTeams(token);
            List<ScanObject> scan = scans.getScan(token);
            foreach (ScanObject s in scan)
            {
                if ((s.DateAndTime != null) && (s.Status.Id == 7) && (s.DateAndTime.StartedOn > token.start_time) && (s.DateAndTime.StartedOn < token.end_time))
                {
                    if (matchProjectandTeam(s, teams))
                    {
                        setCount(s.Project.Id, scanCount);
                        findFirstorLastScan(s.Project.Id,  s, teams ,start, true);
                        findFirstorLastScan(s.Project.Id, s, teams , end, false);

                        ReportResult result = scanResults.SetResultRequest(s.Id, "XML", token);
                        if (result != null)
                        {
                            trace.Add(new ReportTrace(s.Project.Id, s.Project.Name, scans.getFullName(teams, s.OwningTeamId), s.DateAndTime.StartedOn, s.Id, result.ReportId, "XML"));
                        }
                        if (trace.Count % 5 == 0)
                        {
                            waitForResult(trace, scanResults, resultNew, end, last);
                            trace.Clear();
                        }
                    }
                }
            }
            waitForResult(trace, scanResults, resultNew, end, last);
            trace.Clear();

            List<ReportOutput> reportOutputs = totalScansandReports(start, end, resultNew, last, scanCount);
            if (token.pipe)
            {
                foreach (ReportOutput csv in reportOutputs)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", csv.ProjectName, csv.company,csv.team, csv.LastHigh, csv.LastMedium, csv.LastLow, csv.NewHigh, csv.NewMedium, csv.NewLow, csv.DiffHigh, csv.DiffMedium, csv.DiffLow, csv.NotExploitable, csv.Confirmed, csv.ToVerify, csv.firstScan, csv.lastScan, csv.ScanCount);
                }
            }
            else
            {
                csvHelper csvHelper = new csvHelper();
                csvHelper.writeCVSFile(reportOutputs, token);
            }
            return true;
        }

        private bool getlastReport(XElement result, Dictionary<long, ReportStaging> end, Dictionary<long, List<ReportResultAll>> last)
        {
            foreach (long key in end.Keys)
            {
                ReportStaging staging = end[key];
                if (result.Attribute("ScanId").Value == staging.ScanId.ToString())
                {
                    last.Add(staging.ProjectId, process_LastScan(result, staging.ScanId));
                }
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

        private List<ReportOutput> totalScansandReports(Dictionary<long, ReportStaging> start, Dictionary<long, ReportStaging> end, List<ReportResultAll> resultNew, Dictionary<long, List<ReportResultAll>> lastScan, Dictionary<long, ScanCount> scanCount)
        {
            List<ReportOutput> reports = new List<ReportOutput>();
            getScans scans = new getScans();
            foreach (long key in start.Keys)
            {
                ReportOutput report = new ReportOutput();

                ReportStaging first = start[key];
                ReportStaging last = end[key];
                List<ReportResultAll> lastScanResults = lastScan[key];
                foreach (ReportResultAll result in resultNew)
                {
                    if (result.projectId == first.ProjectId)
                    {
                        if (result.status == "New")
                        {
                            if (result.Severity == "High") { report.NewHigh++; }
                            else if (result.Severity == "Medium") { report.NewMedium++; }
                            else if (result.Severity == "Low") { report.NewLow++; }
                        }
                    }    
                }
                foreach (ReportResultAll result in lastScanResults)
                {
                    if (result.state == 0) { report.ToVerify++; }
                    else if (result.state == 1) { report.NotExploitable++; }
                    else if (result.state == 2) { report.Confirmed++; }
                }
                //report.TeamName = first.TeamName;
                string[] split;
                if (first.TeamName.Contains('\\'))
                {
                    split = first.TeamName.Split('\\');
                }
                else
                {
                    split = first.TeamName.Split('/');
                }
                if (split.Length > 1)
                {
                    report.company = split[split.Length - 2];
                    report.team = split[split.Length - 1];
                }
                report.ProjectName = first.ProjectName;
                report.StartHigh = first.High;
                report.StartMedium = first.Medium;
                report.StartLow = first.Low;
                report.firstScan = first.dateTime;

                report.LastHigh = last.High;
                report.LastMedium = last.Medium;
                report.LastLow = last.Low;
                report.lastScan = last.dateTime;

                report.DiffHigh = first.High - last.High;
                report.DiffMedium = first.Medium - last.Medium;
                report.DiffLow = first.Low - last.Low;
                report.ScanCount = scanCount[key].count;
                reports.Add(report);

            }
            return reports;
        }

        private bool process_CxResponse(XElement result, List<ReportResultAll> response)
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
                    ReportResultAll isnew = new ReportResultAll()
                    {
                        Query = query.Attribute("name").Value.ToString(),
                        Group = query.Attribute("group").Value.ToString(),
                        projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                        scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                        status = el.Attribute("Status").Value.ToString(),
                        Severity = el.Attribute("Severity").Value.ToString(),
                        state = Convert.ToInt32(el.Attribute("state").Value.ToString()),
                        teamName = root.Attribute("TeamFullPathOnReportDate").Value.ToString()
                    };
                    response.Add(isnew);

                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

        }
        private List<ReportResultAll> process_LastScan(XElement result, long scanId)
        {
            List<ReportResultAll> reportResults = new List<ReportResultAll>();
            try
            {
                if (result.Attribute("ScanId").Value == scanId.ToString())
                {
                    IEnumerable<XElement> lastScan = from el in result.Descendants("Query").Descendants("Result")
                                                     select el;
                    foreach (XElement el in lastScan)
                    {
                        XElement query = el.Parent;
                        XElement root = query.Parent;
                        ReportResultAll isnew = new ReportResultAll()
                        {
                            Query = query.Attribute("name").Value.ToString(),
                            Group = query.Attribute("group").Value.ToString(),
                            projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                            scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                            status = el.Attribute("Status").Value.ToString(),
                            Severity = el.Attribute("Severity").Value.ToString(),
                            state = Convert.ToInt32(el.Attribute("state").Value.ToString())
                        };

                        reportResults.Add(isnew);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            return reportResults;

        }
        private bool findFirstorLastScan(long projectId, ScanObject scan, List<Teams> teams, Dictionary<long, ReportStaging> keyValuePairs, bool operation)
        {
            getScans scans = new getScans();

            string fullName = scans.getFullName(teams, scan.OwningTeamId);

            if (keyValuePairs.ContainsKey(scan.Project.Id))
            {

                bool start = false;
                ReportStaging reportStaging = keyValuePairs[scan.Project.Id];
                long diff = DateTimeOffset.Compare(reportStaging.dateTime, (DateTimeOffset)scan.DateAndTime.StartedOn);
                if (operation)
                {
                    start = (diff > 0) ? true : false;
                }
                else
                {
                    start = (diff < 0) ? true : false;
                }
                if (start)
                {
                    ScanStatistics scanStatistics = scans.getScansStatistics(scan.Id, token);
                    ReportStaging staging = new ReportStaging()
                    {
                        ProjectId = scan.Project.Id,
                        ProjectName = scan.Project.Name,
                        TeamName = fullName,
                        dateTime = (DateTimeOffset)scan.DateAndTime.StartedOn,
                        High = scanStatistics.HighSeverity,
                        Medium = scanStatistics.MediumSeverity,
                        Low = scanStatistics.LowSeverity,
                        ScanId = scan.Id
                    };
                    keyValuePairs[scan.Project.Id] = staging;
                }
            }
            else
            {
                ScanStatistics scanStatistics = scans.getScansStatistics(scan.Id, token);
                keyValuePairs.Add(scan.Project.Id, new ReportStaging()
                {
                    ProjectId = scan.Project.Id,
                    ProjectName = scan.Project.Name,
                    TeamName = fullName,
                    dateTime = (DateTimeOffset)scan.DateAndTime.StartedOn,
                    High = scanStatistics.HighSeverity,
                    Medium = scanStatistics.MediumSeverity,
                    Low = scanStatistics.LowSeverity,
                    ScanId = scan.Id
                });
            }
            return true;
        }
        public bool matchProjectandTeam(ScanObject s, List<Teams> teams)
        {
            bool result = false;
            getScans scans = new getScans();

            string fullName = scans.getFullName(teams, s.OwningTeamId);

            if ((String.IsNullOrEmpty(token.project_name) || ((!String.IsNullOrEmpty(token.project_name)) && (s.Project.Name.ToLower().Contains(token.project_name.ToLower())))))
            {
                if ((String.IsNullOrEmpty(token.team_name) || ((!String.IsNullOrEmpty(token.team_name)) && (!String.IsNullOrEmpty(fullName)) && (fullName.ToLower().Contains(token.team_name.ToLower())))))
                {
                    result = true;
                }
            }
            return result;
        }


        public bool waitForResult(List<ReportTrace> trace, getScanResults scanResults, List<ReportResultAll> resultNew, Dictionary<long, ReportStaging> end ,Dictionary<long, List<ReportResultAll>> last )
        {
            bool waitFlag = false;
            while (!waitFlag)
            {
                if (token.debug && token.verbosity > 0) { Console.WriteLine("Sleeping 3 second(s)"); }
                Thread.Sleep(3000);

                foreach (ReportTrace rt in trace)
                {
                    waitFlag = true;
                    if (!rt.isRead)
                    {
                        waitFlag = false;
                        if (rt.TimeStamp.AddMinutes(2) < DateTime.UtcNow)
                        {
                            Console.Error.WriteLine("ReportId/ScanId {0)/{1} timeout!", rt.reportId, rt.scanId);
                            rt.isRead = true;
                            continue;
                        }
                        if (scanResults.GetResultStatus(rt.reportId, token))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Got status for reportId {0}", rt.reportId); }
                            var result = scanResults.GetResult(rt.reportId, token);
                            if (result != null)
                            {
                                if (token.debug && token.verbosity > 0) { Console.WriteLine("Got data for reportId {0}", rt.reportId); }
                                if (process_CxResponse(result, resultNew))
                                {
                                    rt.isRead = true;
                                    getlastReport(result, end, last);
                                }
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



        public void Dispose()
        {

        }

    }

}