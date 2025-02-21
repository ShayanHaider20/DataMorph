using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClientApplication
{
    public static class ISO
    {
        public static string[] DE = new string[130];
        static public string gethashvalue(int bitindex)
        {
            Hashtable ht = new Hashtable();
            
            ht.Add(0, "Primary Bitmap");
            ht.Add(1, "Secondary Bitmap");
            ht.Add(2, "Primary account number PAN");
            ht.Add(3, "Processing Code");
            ht.Add(4, "Amount Transaction");
            ht.Add(5, "Amount settlement");
            ht.Add(6, "Amount cardholder billing");
            ht.Add(7, "Transmission date and time");
            ht.Add(8, "Amount cardholder billing fee");
            ht.Add(9, "Conversion rate settlement");
            ht.Add(10, "Conversion rate cardholder billing");
            ht.Add(11, "System trace audit number STAN");
            ht.Add(12, "Local transaction time hhmmss");
            ht.Add(13, "Local transaction date MMDD");
            ht.Add(14, "Expiration date YYMM");
            ht.Add(15, "Settlement date");
            ht.Add(16, "Currency conversion date");
            ht.Add(17, "Capture date");
            ht.Add(18, "Merchant type or merchant category code");
            ht.Add(19, "Acquiring institution");
            ht.Add(20, "PAN extended");
            ht.Add(21, "Forwarding institution");
            ht.Add(22, "Point of service entry mode");
            ht.Add(23, "Application PAN sequence number");
            ht.Add(24, "Function code or network international identifier NII");
            ht.Add(25, "Point of service condition code");
            ht.Add(26, "Point of service capture code");
            ht.Add(27, "Authorizing identification response length");
            ht.Add(28, "Transaction fee");
            ht.Add(29, "Settlement fee");
            ht.Add(30, "Transaction processing fee");
            ht.Add(31, "Settlement processing fee");
            ht.Add(32, "Acquiring institution identification code");
            ht.Add(33, "Forwarding institution identification code");
            ht.Add(34, "Primary account number extended");
            ht.Add(35, "Track 2 data");
            ht.Add(36, "Track 3 data");
            ht.Add(37, "Retrieval reference number");
            ht.Add(38, "Authorization identification response");
            ht.Add(39, "Response code");
            ht.Add(40, "Service restriction code");
            ht.Add(41, "Card acceptor terminal identification");
            ht.Add(42, "Card acceptor identification code");
            ht.Add(43, "Card acceptor name or location");
            ht.Add(44, "Additional response data");
            ht.Add(45, "Track 1 data");
            ht.Add(46, "Additional data ISO");
            ht.Add(47, "Additional data national");
            ht.Add(48, "Additional data private");
            ht.Add(49, "Currency code transaction");
            ht.Add(50, "Currency code settlement");
            ht.Add(51, "Currency code cardholder billing");
            ht.Add(52, "Personal identification number data");
            ht.Add(53, "Security related control information");
            ht.Add(54, "Additional amounts");
            ht.Add(55, "CC data EMV having multiple tags");
            ht.Add(56, "Reserved_ISO");
            ht.Add(57, "Reserved_national");
            ht.Add(58, "Reserved_national");
            ht.Add(59, "Reserved_national");
            ht.Add(60, "Reserved_national");
            ht.Add(61, "Reserved_private");
            ht.Add(62, "Reserved_private");
            ht.Add(63, "Reserved_private");
            ht.Add(64, "Message authentication code MAC");
            ht.Add(65, "Extended bitmap indicator");
            ht.Add(66, "Settlement code");
            ht.Add(67, "Extended payment code");
            ht.Add(68, "Receiving institution country code");
            ht.Add(69, "Settlement institution country code");
            ht.Add(70, "Network management information code");
            ht.Add(71, "Message number");
            ht.Add(72, "Last message number");
            ht.Add(73, "Action date YYMMDD");
            ht.Add(74, "Number of credits");
            ht.Add(75, "Credits reversal number");
            ht.Add(76, "Number of debits");
            ht.Add(77, "Debits reversal number");
            ht.Add(78, "Transfer number");
            ht.Add(79, "Transfer reversal number");
            ht.Add(80, "Number of inquiries");
            ht.Add(81, "Number of authorizations");
            ht.Add(82, "Credits processing fee amount");
            ht.Add(83, "Credits transaction fee amount");
            ht.Add(84, "Debits processing fee amount");
            ht.Add(85, "Debits transaction fee amount");
            ht.Add(86, "Total amount of credits");
            ht.Add(87, "Credits reversal amount");
            ht.Add(88, "Total amount of debits");
            ht.Add(89, "Debits reversal amount");
            ht.Add(90, "Original data elements");
            ht.Add(91, "File update code");
            ht.Add(92, "File security code");
            ht.Add(93, "Response indicator");
            ht.Add(94, "Service indicator");
            ht.Add(95, "Replacement amounts");
            ht.Add(96, "Message security code");
            ht.Add(97, "Net settlement amount");
            ht.Add(98, "Payee");
            ht.Add(99, "Settlement institution identification code");
            ht.Add(100, "Receiving institution identification code");
            ht.Add(101, "File name");
            ht.Add(102, "Account identification 1");
            ht.Add(103, "Account identification 2");
            ht.Add(104, "Transaction description");
            ht.Add(105, "Reserved for ISO use");
            ht.Add(106, "Reserved for ISO use");
            ht.Add(107, "Reserved for ISO use");
            ht.Add(108, "Reserved for ISO use");
            ht.Add(109, "Reserved for ISO use");
            ht.Add(110, "Reserved for ISO use");
            ht.Add(111, "Reserved for ISO use");
            ht.Add(112, "Reserved for national use");
            ht.Add(113, "Reserved for national use");
            ht.Add(114, "Reserved for national use");
            ht.Add(115, "Reserved for national use");
            ht.Add(116, "Reserved for national use");
            ht.Add(117, "Reserved for national use");
            ht.Add(118, "Reserved for national use");
            ht.Add(119, "Reserved for national use");
            ht.Add(120, "Reserved for private use");
            ht.Add(121, "Reserved for private use");
            ht.Add(122, "Reserved for private use");
            ht.Add(123, "Reserved for private use");
            ht.Add(124, "Reserved for private use");
            ht.Add(125, "Reserved for private use");
            ht.Add(126, "Reserved for private use");
            ht.Add(127, "Reserved for private use");
            ht.Add(128, "Message authentication code");

            string value = (string)ht[bitindex];
            return value;

        }
        public static string[] ReadData(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            string[] data = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] parts = line.Split(',');

                if (parts.Length >= 2)
                {
                    data[i] = parts[1].Trim();
                }
            }
            return data;
        }
        static public string GetTargetValueFromFile(string filePath, int targetLine)
            {
                string[] lines = File.ReadAllLines(filePath);
                if (targetLine >= 1 && targetLine <= lines.Length)
                {
                    string targetValue = lines[targetLine - 1];
                    return targetValue;
                }
                else
                {
                    return null; // Invalid target line number
                }
            }

        public static bool IsISO8583Format(string input)
        {
            // Define the ISO8583 format regular expression pattern
            string pattern = @"^\d{4}[0-9a-fA-F]{32}[0-9a-zA-Z]*$";

            // Check if the input matches the ISO8583 format
            bool isMatch = Regex.IsMatch(input, pattern);

            return isMatch;
        }


            static public bool matchproccode(string req)
            {
                BIM_ISO8583.NET.ISO8583 iso8583_2= new BIM_ISO8583.NET.ISO8583();
                string[] reqmesg = iso8583_2.Parse(req);

            string[] items = Directory.GetFiles("C:\\", "*.res.txt");
                foreach (string item in items)
                {
                    string content = File.ReadAllText(item);
                    if (content.StartsWith("MIT"))
                    {
                        string res = GetTargetValueFromFile(item, 5);
                        string res1 = GetTargetValueFromFile(item, 1);
                        //string[] readText = File.ReadAllLines(item);
                        //Console.WriteLine(readText);
                        if (res.Contains(','))
                        {
                            BIM_ISO8583.NET.ISO8583 iso8583 = new BIM_ISO8583.NET.ISO8583();
                            int fieldBit = Convert.ToInt32(res.Split(',')[0]);
                            string ValueonBit = res.Split(',')[1];
                            DE[fieldBit] = ValueonBit;
                            //Console.WriteLine(fieldBit + ":" + ValueonBit);
                            if (ValueonBit == reqmesg[3])
                            {
                                string mti = reqmesg[129];
                                string reqiso = iso8583.Build(reqmesg,mti);
                                return true;
                            }
                            else
                            {
                                continue;
                            }
                        }

                    }
                    else
                        continue;
                }
                return false;

            }

         

            static void StartServer()
            {
                // Start the server and listen for incoming connections
                string serverIp = "127.0.0.1"; // Replace with your server IP
                int serverPort = 8888; // Replace with your desired server port
                TcpListener server = new TcpListener(IPAddress.Parse(serverIp), serverPort);
                server.Start();
                Console.WriteLine("Server started. Listening for incoming connections...");

                while (true)
                {
                    // Accept a client connection
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

                    // Handle the client request in a separate thread
                    _ = HandleClientRequest(client); //  _ variable is used to discard the returned Task object because we don't need to await or use it further in this example
                }
            }

            static async Task HandleClientRequest(TcpClient client)

            {
                while (true)
                {
                    try
                    {
                        // Get the client's network stream for reading and writing
                        NetworkStream stream = client.GetStream();

                        // Read the request from the client
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        BIM_ISO8583.NET.ISO8583 iso8583 = new BIM_ISO8583.NET.ISO8583();

                        // Convert the received bytes to a string
                        string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("\nReceived Request: " + request);
                        // Console.WriteLine("Received Request Length: " + request.Length);
                        //string[] DE = new string[130];

                        Console.WriteLine("\nConverting hexa bitmap into binary!");
                        DE = iso8583.Parse(request);
                        Console.WriteLine(matchproccode(DE[3]));

                        Console.WriteLine("\nParsing ISO Request:-\n");
                        for (int i = 0; i < DE.Length; i++)
                        {
                            if (i == 129)
                            {
                                Console.WriteLine("MIT: " + DE[i]);
                            }
                            if (DE[i] != null && i != 129)
                            {
                                Console.WriteLine("Bit: " + i + " =  Field value: " + DE[i]);
                            }
                        }

                        Console.WriteLine("\nConverting ISO Request into JSON:");
                        string jsonRequest = ConvertIso8583ToJson(DE);
                        Console.WriteLine("\nISO8583 Message as JSON:\n" + jsonRequest);

                        Console.WriteLine("\nConverting ISO Request into XML:");
                        string xmlRequest = ConvertIso8583ToXml(DE);
                        Console.WriteLine("\nISO8583 Message as XML:\n" + xmlRequest);
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine("Error handling client request: " + ex.Message);
                    }

                }
            }
        static public string ConvertIso8583ToJson(string[] iso8583Message)
        {
            // Create a dictionary to hold the ISO8583 fields
            var iso8583Fields = new System.Collections.Generic.Dictionary<string, string>();

            // Populate the dictionary with the ISO8583 fields
            for (int i = 0; i < iso8583Message.Length; i++)
            {
                if (iso8583Message[i] != null && i != 129)
                {
                    iso8583Fields[gethashvalue(i)] = iso8583Message[i];
                }
            }



            // Convert the dictionary to JSON
            string json = JsonConvert.SerializeObject(iso8583Fields, Formatting.Indented);
            return json;
        }
        static public string ConvertIso8583ToXml(string[] iso8583Message)
        {
            XElement xmlRoot = new XElement("ISO8583");

            for (int i = 0; i < iso8583Message.Length; i++)
            {
                if (iso8583Message[i] != null && i != 129)
                {
                    string fieldName = gethashvalue(i).Replace(" ", "_");
                    XElement fieldElement = new XElement(fieldName, iso8583Message[i]);
                    xmlRoot.Add(fieldElement);
                }
            }
            return xmlRoot.ToString();
        }

        static public string ConvertIsoToFixedLength(string isoMessage)
        {
            int[] fieldLengths = new int[40];
            for (int i = 0; i < fieldLengths.Length; i++)
            {
                fieldLengths[i] = 5;
            }

            StringBuilder fixedLengthBuilder = new StringBuilder();
            int currentIndex = 0;
            for (int i = 0; i < fieldLengths.Length; i++)
            {
                int fieldLength = fieldLengths[i];
                if (currentIndex >= isoMessage.Length)
                {
                    break;
                    // fixedLengthBuilder.Append(" ".PadRight(fieldLength, ' ')); // Fill with spaces for missing fields

                }
                else if (currentIndex + fieldLength <= isoMessage.Length)
                {
                    string fieldValue = isoMessage.Substring(currentIndex, fieldLength);
                    if (fieldValue == "     ") break;
                    fieldValue = fieldValue ?? ""; // Replace null field with an empty string
                    fieldValue = fieldValue.PadRight(fieldLength, ' '); // Pad the field with spaces if necessary
                    currentIndex += fieldLength;
                    fixedLengthBuilder.Append(fieldValue);
                    
                }
                else
                {
                    string fieldValue = isoMessage.Substring(currentIndex);
                    if (fieldValue == "     ") break;
                    fieldValue = fieldValue ?? ""; // Replace null field with an empty string
                    fieldValue = fieldValue.PadRight(fieldLength, ' '); // Pad the field with spaces if necessary
                    fixedLengthBuilder.Append(fieldValue);
                    currentIndex = isoMessage.Length;
                }
               
               
            }

            return fixedLengthBuilder.ToString();
        }

        static public string ConvertIsoToDelimited(string isoMessage, char delimiter)
        {
            string[] DE2 = new string[130];
            string text = "";
            BIM_ISO8583.NET.ISO8583 iso2 = new BIM_ISO8583.NET.ISO8583();
            DE2 = iso2.Parse(isoMessage);
            for (int i = 0; i < DE2.Length; i++)
            {
                if (DE2[i] != null && i != 129)
                {
                    text += string.Join("", DE2[i]);
                    text = text + delimiter;
                }

            }
            return DE2[129] + delimiter + text;
            
        }
    }
}
    //static void Main(string[] args)
    //{
    //Console.WriteLine("Hello World!");
    //BIM_ISO8583.NET.ISO8583 iso8583 = new BIM_ISO8583.NET.ISO8583();

    //string ISO8583Message = "0200F2204000008080000400000000000000181660197105032103460000010000000000100429104720123456001499999999PKR222";
    //string path = @"D:\Balance Inquiry.txt";


    //}

    //}*/
    //3. <<< Declare String Arrays >>>

    /*   DEMessage[2] = "166019710503210346";
       DEMessage[3] = "000001";
       DEMessage[4] = "000000000010";
       DEMessage[7] = "0429104720";
       DEMessage[11] = "123456";
       DEMessage[18] = "0014";
       DEMessage[41] = "99999999";
       DEMessage[49] = "PKR";
       DEMessage[70] = "222";*/

    //4. <<< Use "Parse" method of object iso8583. >>>




    //That was it!!

    //If there is no error occurred then you have successfully parsed a valid ISO8583 message

    //Here's how to use the newly parsed ISO8583 Data Elements
    //string PrimaryBitMap = DE[0];
    // string SecondaryBitMap = DE[1];
    /* string PAN = DE[2];
     string ProcessingCode = DEMessage[3];
     string Amount = DEMessage[4];
     string TransmissionTime = DEMessage[7];
     string SystemTraceNo = DEMessage[11];
     string MCC = DEMessage[18];
     string TerminalID = DEMessage[41];       
     string CC = DEMessage[49];
     string NetworkManagementCode = DEMessage[70];
     string MTI = DEMessage[129];

     string IsMesage = iso8583.Build(DEMessage, "0200");
         //Displaying  Data Elements
     Console.WriteLine();
     Console.WriteLine("Please Press 'ENTER' to continue...");
     Console.ReadLine();

     Console.WriteLine(" PrimaryBitMap = DE[0]" + " = {0}", DE[0]);
     Console.WriteLine(" SecondaryBitMap = DE[1]" + "= {0}", DE[1]);
     Console.WriteLine(" PAN = DE[2]" + "= {0}", DE[2]);
     Console.WriteLine(" ProcessingCode = DE[3]" + "= {0}", DE[3]);
     Console.WriteLine(" Amount = DE[4]" + "= {0}", DE[4]);
     Console.WriteLine(" TransmissionTime = DE[7]" + "= {0}", DE[7]);
     Console.WriteLine(" SystemTraceNo = DE[11]" + "= {0}", DE[11]);
     Console.WriteLine(" MCC = DE[18]" + "= {0}", DE[18]);

     Console.Read();*/

