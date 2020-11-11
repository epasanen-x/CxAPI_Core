using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using CxAPI_Core.dto;


namespace CxAPI_Core
{
    class restReport_4 : IDisposable
    {
        public resultClass token;

        public restReport_4(resultClass token)
        {
            this.token = token;
        }

        public bool fetchReportsbyDate()
        {
            if (token.debug && token.verbosity > 1) { Console.WriteLine("Running: {0}", token.report_name); }

            List<ReportTrace> trace = new List<ReportTrace>();
            List<ReportResultAll> resultNew = new List<ReportResultAll>();
            Dictionary<long, ReportStaging> start = new Dictionary<long, ReportStaging>();
            Dictionary<long, ReportStaging> end = new Dictionary<long, ReportStaging>();
            Dictionary<long, List<ReportResultAll>> last = new Dictionary<long, List<ReportResultAll>>();
            Dictionary<long, ScanCount> scanCount = new Dictionary<long, ScanCount>();
            Dictionary<DateTimeOffset, Dictionary<long, Dictionary<string, ReportResultExtended>>> extendedScan = new Dictionary<DateTimeOffset, Dictionary<long, Dictionary<string, ReportResultExtended>>>();
            getScanResults scanResults = new getScanResults();
            getScans scans = new getScans();
            getProjects projects = new getProjects(token);
            Dictionary<Guid, Teams> teams = projects.CxTeams;
            // List<ScanObject> scan = scans.getScan(token);
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
                findFirstorLastScan(s.Project.Id, s, resultStatistics[s.Id], teams, start, true);
                findFirstorLastScan(s.Project.Id, s, resultStatistics[s.Id], teams, end, false);

                ReportResult result = scanResults.SetResultRequest(s.Id, "XML", token);
                if (result != null)
                {
                    trace.Add(new ReportTrace(s.Project.Id, s.Project.Name, teams[s.OwningTeamId].fullName, s.DateAndTime.StartedOn, s.Id, result.ReportId, "XML"));
                }
                if (trace.Count % token.max_threads == 0)
                {
                    waitForResult(trace, scanResults, resultNew, extendedScan, end, last);
                    trace.Clear();
                }
            }

            waitForResult(trace, scanResults, resultNew, extendedScan, end, last);
            trace.Clear();

            List<AgingOutput> reportOutputs = totalAllResults(extendedScan, end);
            if (token.debug) { Console.WriteLine("Processing data, number of rows: {0}", reportOutputs.Count); }
            if (token.pipe)
            {
                foreach (AgingOutput csv in reportOutputs)
                {
                    // Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", csv.ProjectName, csv.company, csv.team, csv.LastHigh, csv.LastMedium, csv.LastLow, csv.NewHigh, csv.NewMedium, csv.NewLow, csv.DiffHigh, csv.DiffMedium, csv.DiffLow, csv.NotExploitable, csv.Confirmed, csv.ToVerify, csv.firstScan, csv.lastScan, csv.ScanCount);
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
                string[] split = first.TeamName.Split('\\');
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

        private List<AgingOutput> totalAllResults(Dictionary<DateTimeOffset, Dictionary<long, Dictionary<string, ReportResultExtended>>> extendedScan, Dictionary<long, ReportStaging> end)
        {
            List<AgingOutput> reports = new List<AgingOutput>();

            Dictionary<string, AgingOutput> allResults = new Dictionary<string, AgingOutput>();

            Dictionary<long, Dictionary<string, ReportResultExtended>> scanByProject = new Dictionary<long, Dictionary<string, ReportResultExtended>>();
            Dictionary<string, ReportResultExtended> scanByUnique = new Dictionary<string, ReportResultExtended>();

            getScans scans = new getScans();
            List<DateTimeOffset> dates = extendedScan.Keys.ToList();
            dates.Sort();

            foreach (DateTimeOffset key in dates)
            {
                scanByProject = extendedScan[key];
                foreach (long projectId in scanByProject.Keys)
                {
                    scanByUnique = extendedScan[key][projectId];

                    foreach (string uniqueKey in scanByUnique.Keys)
                    {
                        AgingOutput aging;
                        if (!allResults.ContainsKey(uniqueKey))
                        {
                            string company = String.Empty;
                            string team = String.Empty;
                            string fileName = String.Empty;
                            string[] split = scanByUnique[uniqueKey].teamName.Split('\\');
                            if (split.Length > 1)
                            {
                                company = split[split.Length - 2];
                                team = split[split.Length - 1];
                            }
                            string[] fileSplit = scanByUnique[uniqueKey].fileName.Split('/');
                            if (fileSplit.Length > 1)
                            {
                                fileName = fileSplit[fileSplit.Length - 1];
                            }
                            aging = new AgingOutput()
                            {
                                ProjectName = scanByUnique[uniqueKey].projectName,
                                team = team,
                                company = company,
                                presetName = scanByUnique[uniqueKey].presetName,
                                Query = scanByUnique[uniqueKey].Query,
                                //similarityId = scanByUnique[uniqueKey].similarityId,
                                isFalsePositive = scanByUnique[uniqueKey].isFalsePositive,
                                //startState = scanByUnique[uniqueKey].state,
                                StateDesc = stateToString(scanByUnique[uniqueKey].state),
                                Status = scanByUnique[uniqueKey].status,
                                Severity = scanByUnique[uniqueKey].Severity,
                                //endState = scanByUnique[uniqueKey].state,
                                //endStateDesc = stateToString(scanByUnique[uniqueKey].state),
                                //endStatus = scanByUnique[uniqueKey].status,
                                //endSeverity = scanByUnique[uniqueKey].Severity,
                                lineNo = scanByUnique[uniqueKey].lineNo,
                                column = scanByUnique[uniqueKey].column,
                                //firstLine = scanByUnique[uniqueKey].firstLine,
                                fileName = fileName,
                                deepLink = scanByUnique[uniqueKey].deepLink,
                                remark = scanByUnique[uniqueKey].remark,
                                firstScan = key,
                                lastScan = key,
                                scanCount = 1
                            };
                            allResults.Add(uniqueKey, aging);
                            aging = allResults[uniqueKey];
                        }
                        else
                        {
                            aging = allResults[uniqueKey];
                            aging.isFalsePositive = scanByUnique[uniqueKey].isFalsePositive;
                            //aging.endState = scanByUnique[uniqueKey].state;
                            aging.StateDesc = stateToString(scanByUnique[uniqueKey].state);
                            aging.Status = scanByUnique[uniqueKey].status;
                            aging.Severity = scanByUnique[uniqueKey].Severity;
                            aging.lastScan = key;
                            aging.scanCount++;
                        }
                        aging.age = (DateTimeOffset.Now - aging.firstScan.Date).Days;

                        if (!isUniqueInProject(dates, projectId, uniqueKey, extendedScan))
                        {
                            aging.Status = "Fixed";
                        }
                        if ((aging.isFalsePositive.ToUpper().Contains("TRUE")) || (aging.Status.Contains("Fixed")))
                        {
                            aging.age = 0;
                        }
                        allResults[uniqueKey] = aging;
                    }
                }

            }
            reports = allResults.Values.ToList();
            return reports;
        }


        private bool process_CxResponse(XElement result, long report_id, long projectId, DateTimeOffset? scanDate, Dictionary<DateTimeOffset, Dictionary<long, Dictionary<string, ReportResultExtended>>> extendedScan)
        {
            try
            {
                Dictionary<long, Dictionary<string, ReportResultExtended>> scanByProject = new Dictionary<long, Dictionary<string, ReportResultExtended>>();
                Dictionary<string, ReportResultExtended> scanByUnique = new Dictionary<string, ReportResultExtended>();
                IEnumerable<XElement> fixedVulerability = from el in result.Descendants("Query").Descendants("Result")
                                                          select el;
                foreach (XElement el in fixedVulerability)
                {
                    XElement query = el.Parent;
                    XElement root = query.Parent;
                    XElement path = el.Descendants("Path").FirstOrDefault();
                    XElement pathNode = path.Descendants("PathNode").FirstOrDefault();
                    //List<XElement> allNode = path.Descendants("PathNode").Elements().ToList();
                    //IEnumerable<XElement> allNode = path.Descendants("PathNode").Elements();
                    XElement lastNode = path.Descendants("PathNode").LastOrDefault();
                    XElement snippet = pathNode.Descendants("Snippet").FirstOrDefault();
                    XElement line = snippet.Descendants("Line").FirstOrDefault();
                    long SimilarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString());
                    ReportResultExtended isfixed = new ReportResultExtended()
                    {
                        Query = query.Attribute("name").Value.ToString(),
                        Group = query.Attribute("group").Value.ToString(),
                        projectName = root.Attribute("ProjectName").Value.ToString(),
                        presetName = root.Attribute("Preset").Value.ToString(),
                        teamName = root.Attribute("TeamFullPathOnReportDate").Value.ToString(),
                        scanDate = Convert.ToDateTime(root.Attribute("ScanStart").Value.ToString()),
                        projectId = Convert.ToInt64(root.Attribute("ProjectId").Value.ToString()),
                        scanId = Convert.ToInt64(root.Attribute("ScanId").Value.ToString()),
                        status = el.Attribute("Status").Value.ToString(),
                        Severity = el.Attribute("Severity").Value.ToString(),
                        isFalsePositive = el.Attribute("FalsePositive").Value.ToString(),
                        resultId = Convert.ToInt64(path.Attribute("ResultId").Value.ToString()),
                        reportId = report_id,
                        nodeId = Convert.ToInt64(el.Attribute("NodeId").Value.ToString()),
                        similarityId = Convert.ToInt64(path.Attribute("SimilarityId").Value.ToString()),
                        pathId = Convert.ToInt64(path.Attribute("PathId").Value.ToString()),
                        state = Convert.ToInt32(el.Attribute("state").Value.ToString()),
                        fileName = el.Attribute("FileName").Value.ToString(),
                        lineNo = Convert.ToInt32(el.Attribute("Line").Value.ToString()),
                        column = Convert.ToInt32(el.Attribute("Column").Value.ToString()),
                        firstLine = line.Descendants("Code").FirstOrDefault().Value.ToString(),
                        nodeName = pathNode.Descendants("Name").FirstOrDefault().Value.ToString(),
                        queryId = Convert.ToInt64(query.Attribute("id").Value.ToString()),
                        remark = el.Attribute("Remark").Value.ToString(),
                        deepLink = el.Attribute("DeepLink").Value.ToString()
                    };
                    string uniqueKey = String.Format("{0}_{1}_{2}_{3}", isfixed.similarityId, isfixed.queryId, isfixed.lineNo, isfixed.column);
                    uniqueKey = makeHash(pathNode, lastNode, uniqueKey);
                    //uniqueKey = makeHash(allNode, uniqueKey);

                    ; if (token.debug && token.verbosity > 0)
                    {
                        Console.WriteLine(String.Format("Processing: project:{0} scanDate: {1} uniquekey: {2} pathId: {3} nodeId: {4} line:{5} column:{6}", isfixed.projectName, scanDate, uniqueKey, isfixed.pathId, isfixed.nodeId, isfixed.lineNo, isfixed.column));
                    }
                    if (!scanByUnique.TryAdd(uniqueKey, isfixed))
                    {
                        Console.Error.WriteLine(String.Format("Duplicate key: project:{0} scanDate: {1} uniquekey: {2} pathId: {3} nodeId: {4} line:{5} column:{6}", isfixed.projectName, scanDate, uniqueKey, isfixed.pathId, isfixed.nodeId, isfixed.lineNo, isfixed.column));
                    }
                }
                scanByProject.Add(projectId, scanByUnique);
                extendedScan.TryAdd((DateTimeOffset)scanDate, scanByProject);

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
        private bool findFirstorLastScan(long projectId, ScanObject scan, ScanStatistics scanStatistics, Dictionary<Guid, Teams> teams, Dictionary<long, ReportStaging> keyValuePairs, bool operation)
        {
            getScans scans = new getScans();

            string fullName = teams[scan.OwningTeamId].fullName;

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

        public bool waitForResult(List<ReportTrace> trace, getScanResults scanResults, List<ReportResultAll> resultNew, Dictionary<DateTimeOffset, Dictionary<long, Dictionary<string, ReportResultExtended>>> extendedScan, Dictionary<long, ReportStaging> end, Dictionary<long, List<ReportResultAll>> last)
        {
            bool waitFlag = false;
            DateTime wait_expired = DateTime.UtcNow;
            while (!waitFlag)
            {
                if (wait_expired.AddMinutes(2) < DateTime.UtcNow)
                {
                    Console.Error.WriteLine("waitForResult timeout! {0}", getTimeOutObjects(trace));
                    break;
                }

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
                            Console.Error.WriteLine("ReportId/ScanId {0}/{1} timeout!", rt.reportId, rt.scanId);
                            rt.isRead = true;
                            continue;
                        }
                        if (scanResults.GetResultStatus(rt.reportId, token))
                        {
                            if (token.debug && token.verbosity > 0) { Console.WriteLine("Got status for reportId {0}", rt.reportId); }
                            Thread.Sleep(2000);
                            var result = scanResults.GetResult(rt.reportId, token);
                            if (result != null)
                            {
                                if (token.debug && token.verbosity > 0) { Console.WriteLine("Got data for reportId {0}", rt.reportId); }
                                if (process_CxResponse(result, rt.reportId, rt.projectId, rt.scanTime, extendedScan))
                                {
                                    rt.isRead = true;
                                    getlastReport(result, end, last);
                                }
                                else
                                {
                                    rt.isRead = true;
                                    Console.Error.WriteLine("Failed processing reportId {0}", rt.reportId);
                                    if (token.debug && token.verbosity > 1)
                                    {
                                        Console.Error.WriteLine("Dumping XML:");
                                        Console.Error.Write(result.ToString());
                                    }
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
        private string stateToString(int state)
        {
            if (state == 0) { return "To Verify"; }
            if (state == 1) { return "Not Explotible"; }
            if (state == 2) { return "Confirmed"; }

            return "Other";
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

        private bool isUniqueInProject(List<DateTimeOffset> dates, long projectId, string uniqueKey, Dictionary<DateTimeOffset, Dictionary<long, Dictionary<string, ReportResultExtended>>> extendedScan)
        {
            bool result = false;
            Dictionary<string, ReportResultExtended> lastScan = new Dictionary<string, ReportResultExtended>();
            Dictionary<long, Dictionary<string, ReportResultExtended>> lastProject = new Dictionary<long, Dictionary<string, ReportResultExtended>>();

            List<DateTimeOffset> clone = new List<DateTimeOffset>(dates);
            clone.Reverse();


            foreach (DateTimeOffset date in clone)
            {
                lastProject = extendedScan[date];
                if (lastProject.ContainsKey(projectId))
                {
                    lastScan = lastProject[projectId];
                    break;
                }
            }
            result = lastScan.ContainsKey(uniqueKey);
            return result;
        }

        private string makeHash(XElement start, XElement last, string uniqueKey)
        {
            string result = String.Empty;
            using (StringWriter sw = new StringWriter())
            {
                start.Save(sw);
                last.Save(sw);
                result = sw.ToString();
            }
            result += uniqueKey;
            result = ComputeSha256Hash(result);
            return result;
        }

        private string makeHash(IEnumerable<XElement> allNodes, string uniqueKey)
        {
            string result = String.Empty;
            StringWriter sb = new StringWriter();
            foreach (XElement node in allNodes)
            {
                node.Save(sb);
            }
            result = sb.ToString() + uniqueKey;
            sb.Close();
            result = ComputeSha256Hash(result);
            return result;
        }
        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public void Dispose()
        {

        }

    }

}