using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;

namespace ElegrowEsslConsole
{
    class EmployeeCheckIN
    {
        public int empId;
        public string checkIn;
    }
    class EmployeeCheckOUT
    {
        public int empId;
        public string checkOut;
    }
    class Program
    {
        static void Main(string[] args)
        {
            UploadFile().Wait();
        }
        public static async Task UploadFile()
        {
            
            //first get the info for the last file name
            string LastFileName = Path.GetFullPath("E:\\essllog\\lastfilename.txt");
            //check if file is there... If not close the application
            Boolean isLastFileAvailable = File.Exists(LastFileName);
            List<EmployeeCheckIN> lstCheckIn = new List<EmployeeCheckIN>();
            List<EmployeeCheckOUT> lstCheckOut = new List<EmployeeCheckOUT>();
            if (isLastFileAvailable)
            {
                //do your business logic here
                //get the file info 
                StreamReader file = new StreamReader(LastFileName);
                int counter = 0;
                string defaultDate = "31012020";
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    defaultDate=line;
                    counter++;
                }
                file.Close();
                if(counter==0)
                    defaultDate = "31012020";

                //now convert this to date and set the next date 
                DateTime oDate = DateTime.ParseExact(defaultDate, "ddMMyyyy", System.Globalization.CultureInfo.InvariantCulture);
                Console.WriteLine(oDate.ToString());
                DateTime nDate = oDate.AddDays(1);
                string strNextDate = nDate.ToString("ddMMyyyy");
                /*Console.WriteLine(strNextDate);
                Console.WriteLine(LastFileName);*/
                //now we need to get the todays date 
                DateTime CDate = DateTime.Now;
                int diffDate = DateTime.Compare(CDate, nDate);
                string lastFileProcess="";
               
                while (diffDate > 0)
                {
                    var checkIn = new List<KeyValuePair<int, string>>();
                    var checkOut = new List<KeyValuePair<int, string>>();
                    //check for the files
                    Console.WriteLine("Process File = " + strNextDate);
                    if (File.Exists(@"E:\\essllog\\dv\\" + strNextDate + ".txt"))
                    {
                        //if file found 
                        Console.WriteLine("File Found");
                        Console.WriteLine("Processing");
                        //process the file 
                        StreamReader inputFile = new StreamReader(@"E:\\essllog\\dv\\" + strNextDate + ".txt");
                        string inputLine;
                        while ((inputLine = inputFile.ReadLine()) != null)
                        {
                            Console.WriteLine("Data="+inputLine);
                            //get the empId and datetime
                            String[] datas = inputLine.Split(',');
                            int EmpId = int.Parse(datas[0]);
                            string punchTime = datas[1];
                            var matches = from val in checkIn where val.Key == EmpId select val.Value;
                            if (matches.Count() == 0)
                            {
                                //if no entry in checkin add entry in check in 
                                checkIn.Add(new KeyValuePair<int, string>(EmpId, punchTime));

                            }
                            else
                            {
                                //add or update entry in checkout
                                int index = checkOut.FindIndex(kvp => kvp.Key == EmpId);
                                if (index == -1)
                                {
                                    //fresh entry
                                    checkOut.Add(new KeyValuePair<int, string>(EmpId, punchTime));
                                }
                                else
                                {
                                    checkOut[index] = new KeyValuePair<int, string>(EmpId, punchTime);
                                }
                                
                            }
                        }
                        inputFile.Close();
                        Console.WriteLine("Processed");
                        
                        if (checkIn.Count() != 0)
                        {
                            lastFileProcess = strNextDate;
                            Console.WriteLine("Making Api Calls");

                            //
                            foreach (var item in checkIn)
                            {
                                var emp = new EmployeeCheckIN();
                                emp.empId = item.Key;
                                emp.checkIn = item.Value;
                                lstCheckIn.Add(emp);
                            }
                            foreach (var item in checkOut)
                            {
                                var emp = new EmployeeCheckOUT();
                                emp.empId = item.Key;
                                emp.checkOut = item.Value;
                                lstCheckOut.Add(emp);
                            }
                            Console.WriteLine("Last Processed File = "+lastFileProcess);

                        }

                    }
                    else
                    {
                        //file fnot found
                        Console.WriteLine("File Not Found");
                    }
                    nDate = nDate.AddDays(1);
                    strNextDate = nDate.ToString("ddMMyyyy");
                    
                    diffDate = DateTime.Compare(CDate, nDate);
                }
                //make api call and mark this file as last proceessed file
                //check in checkout json code

                if (lstCheckIn.Count() != 0)
                {
                        var CheckInJson = JsonConvert.SerializeObject(lstCheckIn);
                        var CheckOutJson = JsonConvert.SerializeObject(lstCheckOut);
                        string cis = CheckInJson.ToString().Remove(CheckInJson.ToString().Count() - 1,1);
                        string cos = CheckOutJson.ToString().Remove(0, 1);
                        string PostDataString = cis + "," + cos;
                        //Console.WriteLine(PostDataString);
                        /*Console.WriteLine("Check In Request=" + CheckInJson.ToString());*/

                        //make APi Request
                        string authToken = "98c822ebbab9b8563f03751094ac58e5";
                        string requestURL = "https://people.zoho.in/people/api/attendance/bulkImport";
                        HttpClient client = new HttpClient();


                        var Postvalues = new Dictionary<string, string>
                        {
                            { "authtoken", authToken },
                            { "data", PostDataString },
                            {  "dateFormat","yyyy-MM-dd HH:mm:ss" }
                        };
                        var content = new FormUrlEncodedContent(Postvalues);
                        var response = await client.PostAsync(requestURL, content);
                        string result = response.Content.ReadAsStringAsync().Result;
                        Console.WriteLine("Check In API Result =" + result);
                        int totalentries = CheckInJson.Count() + CheckOutJson.Count();
                        //Console.WriteLine("Total Entries = " + totalentries.ToString());
                        //close the file 
                        StreamWriter sw = new StreamWriter(LastFileName);
                        sw.WriteLine(lastFileProcess);
                        sw.Close();
                }
                

            }
            else
            {
                Console.WriteLine("\nLast File is not available");
            }
            
            // basic use of "Console.ReadKey()" method 
            //Console.ReadKey();
        }
    }
    
}
